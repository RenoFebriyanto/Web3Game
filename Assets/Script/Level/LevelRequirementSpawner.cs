// LevelRequirementSpawner.cs
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class LevelRequirementSpawner : MonoBehaviour
{
    [Header("Assign these in the Inspector (or it will try to find automatically)")]
    public LevelGameSession session;           // optional, will try to Find in Start()
    public FragmentPrefabRegistry registry;    // your asset that maps fragment -> prefab
    public Transform spawnParent;              // parent for spawns (set to some empty GameObject in scene)

    [Header("Layout / lanes")]
    public int laneCount = 3;                  // jumlah lane (kiri/tengah/kanan)
    public float laneXOffset = 2.5f;           // jarak antar lane pada axis X
    public float startY = 8f;                  // Y posisi awal spawn
    public float ySpacing = 0.8f;              // jarak vertikal antar spawn pada satu requirement

    // defensive: possible field/property names used by different versions of LevelRequirement
    readonly string[] possibleCountNames = new[] { "count", "amount", "quantity", "qty" };
    readonly string[] possibleTypeNames = new[] { "type", "fragmentType", "fragment", "fragmentKey", "name" };
    readonly string[] possibleColorNames = new[] { "colorVariant", "variant", "color", "colorIndex", "variantIndex" };

    void Start()
    {
        // safer find to avoid obsolete warning depending on Unity version
        if (session == null)
        {
            try
            {
                // Unity 2023+ has FindFirstObjectByType
                session = UnityEngine.Object.FindFirstObjectByType<LevelGameSession>();
            }
            catch
            {
                // fallback if API not available
                if (session == null) session = FindFirstObjectByType<LevelGameSession>();
            }
        }

        if (session == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] No LevelGameSession found in scene. Assign it in Inspector.");
            return;
        }

        if (registry == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] FragmentPrefabRegistry not assigned. Assign the registry asset.");
            // continue (we'll early exit if cannot get prefab)
        }

        if (spawnParent == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] spawnParent not assigned. Using this.transform as parent.");
            spawnParent = this.transform;
        }

        // if session already has a current level loaded, spawn immediately
        if (session.currentLevel != null)
        {
            SpawnForCurrentLevel();
        }
        else
        {
            Debug.Log("[LevelRequirementSpawner] currentLevel is null on Start(). It will spawn when currentLevel is set.");
        }
    }

    // public helper to call from other code after you set session.currentLevel
    public void SpawnForCurrentLevel()
    {
        if (session == null || session.currentLevel == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] Cannot spawn: session or currentLevel is null.");
            return;
        }

        // try to get `requirements` collection (via field or property)
        object reqObj = GetMemberValueFlexible(session.currentLevel, new[] { "requirements", "requirementsList", "requirementsArray" });
        if (reqObj == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] currentLevel has no 'requirements' member (field/property).");
            return;
        }

        IEnumerable reqEnumerable = reqObj as IEnumerable;
        if (reqEnumerable == null)
        {
            Debug.LogWarning("[LevelRequirementSpawner] requirements member is not IEnumerable.");
            return;
        }

        int laneIndex = 0;
        foreach (var req in reqEnumerable)
        {
            // read count (with fallback names)
            int count = TryGetInt(req, possibleCountNames, 1);

            // read color / variant
            int colorVariant = TryGetInt(req, possibleColorNames, 0);

            // read type (could be enum, string, int)
            object typeVal = GetMemberValueFlexible(req, possibleTypeNames);
            if (typeVal == null)
            {
                Debug.LogWarning($"[LevelRequirementSpawner] requirement item missing type field (skipping). Req object type: {req.GetType().Name}");
                laneIndex++;
                continue;
            }

            // find prefab in registry using reflection attempts
            GameObject prefab = TryGetPrefabFromRegistry(registry, typeVal, colorVariant);
            if (prefab == null)
            {
                Debug.LogWarning($"[LevelRequirementSpawner] Could not find prefab for type={typeVal} variant={colorVariant}");
                laneIndex++;
                continue;
            }

            // spawn `count` copies, distribute vertically and by lane
            float center = (laneCount - 1) / 2f;
            float baseX = (laneIndex % laneCount - center) * laneXOffset;

            for (int i = 0; i < count; i++)
            {
                float jitterY = UnityEngine.Random.Range(-0.15f, 0.15f);
                Vector3 pos = new Vector3(baseX, startY - i * ySpacing + (laneIndex * -0.2f) + jitterY, 0f);
                GameObject go = Instantiate(prefab, pos, Quaternion.identity, spawnParent);
            }

            laneIndex++;
        }
    }

    // Try to get an int from object using a set of candidate member names
    int TryGetInt(object obj, string[] names, int defaultValue)
    {
        object v = GetMemberValueFlexible(obj, names);
        if (v == null) return defaultValue;
        if (v is int) return (int)v;
        if (v is long) return (int)(long)v;
        if (v is float) return Mathf.RoundToInt((float)v);
        if (v is string)
        {
            int parsed; return int.TryParse((string)v, out parsed) ? parsed : defaultValue;
        }
        try { return Convert.ToInt32(v); } catch { return defaultValue; }
    }

    // Generic getter: try many candidate names (fields and properties)
    object GetMemberValueFlexible(object obj, string[] candidateNames)
    {
        if (obj == null || candidateNames == null) return null;
        Type t = obj.GetType();
        foreach (var name in candidateNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null) return f.GetValue(obj);
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null) return p.GetValue(obj);
        }
        return null;
    }

    // attempt to call registry's GetPrefab-like method by trying various signatures
    GameObject TryGetPrefabFromRegistry(FragmentPrefabRegistry reg, object typeVal, int colorVariant)
    {
        if (reg == null) return null;
        Type regType = reg.GetType();

        // 1) Try methods named GetPrefab (most likely)
        var methods = regType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.IndexOf("GetPrefab", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();

        foreach (var m in methods)
        {
            var pars = m.GetParameters();
            try
            {
                if (pars.Length == 2)
                {
                    object arg0 = ConvertArgumentIfNeeded(typeVal, pars[0].ParameterType);
                    object arg1 = ConvertArgumentIfNeeded(colorVariant, pars[1].ParameterType);
                    var res = m.Invoke(reg, new object[] { arg0, arg1 });
                    if (res is GameObject go) return go;
                    if (res is UnityEngine.Object uo) return uo as GameObject;
                }
                else if (pars.Length == 1)
                {
                    object arg0 = ConvertArgumentIfNeeded(typeVal, pars[0].ParameterType);
                    var res = m.Invoke(reg, new object[] { arg0 });
                    if (res is GameObject go) return go;
                    if (res is UnityEngine.Object uo) return uo as GameObject;
                }
            }
            catch { /* ignore and try other overloads */ }
        }

        // 2) Try method named FindPrefab or Lookup
        var alt = regType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.Name.IndexOf("Find", StringComparison.OrdinalIgnoreCase) >= 0 || m.Name.IndexOf("Lookup", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        foreach (var m in alt)
        {
            var pars = m.GetParameters();
            try
            {
                if (pars.Length == 2)
                {
                    object arg0 = ConvertArgumentIfNeeded(typeVal, pars[0].ParameterType);
                    object arg1 = ConvertArgumentIfNeeded(colorVariant, pars[1].ParameterType);
                    var res = m.Invoke(reg, new object[] { arg0, arg1 });
                    if (res is GameObject go) return go;
                }
                else if (pars.Length == 1)
                {
                    object arg0 = ConvertArgumentIfNeeded(typeVal, pars[0].ParameterType);
                    var res = m.Invoke(reg, new object[] { arg0 });
                    if (res is GameObject go) return go;
                }
            }
            catch { }
        }

        // 3) fallback: look for a public list/array of entries and try to find one with matching key
        var fields = regType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (var f in fields)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(f.FieldType)) continue;
            var list = f.GetValue(reg) as IEnumerable;
            if (list == null) continue;
            foreach (var item in list)
            {
                var key = GetMemberValueFlexible(item, new[] { "key", "type", "name", "fragmentType" }) ?? GetMemberValueFlexible(item, new[] { "id" });
                if (key == null) continue;
                if (key.ToString().Equals(typeVal.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var prefabFld = item.GetType().GetField("prefab") ?? item.GetType().GetField("gameObject") ?? item.GetType().GetField("target");
                    if (prefabFld != null)
                    {
                        var pf = prefabFld.GetValue(item) as GameObject;
                        if (pf != null) return pf;
                    }
                }
            }
        }

        return null;
    }

    object ConvertArgumentIfNeeded(object value, Type targetType)
    {
        if (value == null) return null;
        if (targetType.IsAssignableFrom(value.GetType())) return value;

        // try convert enum by name 
        if (targetType.IsEnum)
        {
            try { return Enum.Parse(targetType, value.ToString(), true); } catch { }
        }

        try { return Convert.ChangeType(value, targetType); } catch { }

        if (targetType == typeof(string)) return value.ToString();

        return null;
    }
}
