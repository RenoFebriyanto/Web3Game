// Assets/Script/Level/LevelMapLayout.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelMapLayout : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] private RectTransform contentParent;
    [SerializeField] private GameObject levelButtonPrefab;
    [SerializeField] private ScrollRect scrollRect;

    [Header("Path Configuration")]
    [SerializeField] private float verticalSpacing = 150f;
    [SerializeField] private float horizontalAmplitude = 200f; // How wide the zigzag
    [SerializeField] private float pathCurveIntensity = 2f; // How curvy the path is
    [SerializeField] private int levelsPerRow = 7; // For calculating wave pattern

    [Header("Visual Effects")]
    [SerializeField] private LineRenderer pathLineRenderer;
    [SerializeField] private Color pathColor = Color.white;
    [SerializeField] private float pathWidth = 10f;
    [SerializeField] private Material pathMaterial;

    [Header("Animation")]
    [SerializeField] private bool animateOnBuild = true;
    [SerializeField] private float animationDelay = 0.05f;

    private List<LevelButtonUI> levelButtons = new List<LevelButtonUI>();
    private LevelManager levelManager;
    private LevelAutoGenerator levelGenerator;

    // Path patterns untuk variasi layout
    public enum PathPattern
    {
        Zigzag,
        Wave,
        Spiral,
        Snake,
        Curve
    }
    [SerializeField] private PathPattern currentPattern = PathPattern.Wave;

    void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        levelGenerator = FindObjectOfType<LevelAutoGenerator>();

        if (levelManager != null)
        {
            levelManager.OnLevelsChanged += BuildLevelMap;
            BuildLevelMap();
        }
    }

    void OnDestroy()
    {
        if (levelManager != null)
            levelManager.OnLevelsChanged -= BuildLevelMap;
    }

    [ContextMenu("Build Level Map")]
    public void BuildLevelMap()
    {
        if (levelManager == null || levelManager.levelDefinitions == null) return;

        ClearExistingButtons();

        var positions = CalculatePathPositions(levelManager.levelDefinitions.Count);

        for (int i = 0; i < levelManager.levelDefinitions.Count; i++)
        {
            CreateLevelButton(levelManager.levelDefinitions[i], positions[i], i);
        }

        DrawPathLine(positions);
        AdjustContentSize(positions);

        if (animateOnBuild)
        {
            StartCoroutine(AnimateButtonsIn());
        }
    }

    private List<Vector2> CalculatePathPositions(int levelCount)
    {
        List<Vector2> positions = new List<Vector2>();

        for (int i = 0; i < levelCount; i++)
        {
            Vector2 pos = GetPositionForLevel(i, currentPattern, levelCount); // Teruskan levelCount
            positions.Add(pos);
        }

        return positions;
    }

    private Vector2 GetPositionForLevel(int levelIndex, PathPattern pattern, int levelCount) // Tambah parameter levelCount
    {
        float y = -levelIndex * verticalSpacing; // Negatif karena UI turun
        float x = 0;

        switch (pattern)
        {
            case PathPattern.Zigzag:
                x = (levelIndex % 2 == 0) ? -horizontalAmplitude : horizontalAmplitude;
                break;

            case PathPattern.Wave:
                float wavePhase = (float)levelIndex / levelsPerRow * Mathf.PI * 2;
                x = Mathf.Sin(wavePhase) * horizontalAmplitude;
                break;

            case PathPattern.Snake:
                int rowInGroup = levelIndex % levelsPerRow;
                int groupIndex = levelIndex / levelsPerRow;

                if (groupIndex % 2 == 0) // Baris genap: kiri ke kanan
                {
                    x = -horizontalAmplitude + (rowInGroup * (2 * horizontalAmplitude) / (levelsPerRow - 1));
                }
                else // Baris ganjil: kanan ke kiri
                {
                    x = horizontalAmplitude - (rowInGroup * (2 * horizontalAmplitude) / (levelsPerRow - 1));
                }
                break;

            case PathPattern.Spiral:
                float spiralAngle = levelIndex * 0.5f;
                float spiralRadius = horizontalAmplitude * (1 + levelIndex * 0.02f);
                x = Mathf.Cos(spiralAngle) * spiralRadius;
                y += Mathf.Sin(spiralAngle) * 50f; // Tambah variasi vertikal
                break;

            case PathPattern.Curve:
                float curvePhase = (float)levelIndex / levelCount * Mathf.PI;
                x = Mathf.Sin(curvePhase) * horizontalAmplitude * pathCurveIntensity;
                x += Random.Range(-30f, 30f); // Tambah keacakan untuk tampilan alami
                break;
        }

        return new Vector2(x, y);
    }

    private void CreateLevelButton(LevelDefinition levelDef, Vector2 position, int index)
    {
        if (levelButtonPrefab == null)
        {
            Debug.LogError("[LevelMapLayout] Level button prefab is not assigned!");
            return;
        }

        GameObject buttonObj = Instantiate(levelButtonPrefab, contentParent);
        RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;

        // Setup button component
        var buttonUI = buttonObj.GetComponent<LevelButtonUI>();
        if (buttonUI == null)
        {
            buttonUI = buttonObj.AddComponent<LevelButtonUI>();
        }

        buttonUI.SetupLevel(levelDef, levelManager, levelGenerator);
        levelButtons.Add(buttonUI);

        // Initial animation setup
        if (animateOnBuild)
        {
            buttonObj.transform.localScale = Vector3.zero;
        }
    }

    private void DrawPathLine(List<Vector2> positions)
    {
        if (pathLineRenderer == null || positions.Count < 2) return;

        // Konversi posisi UI ke posisi dunia untuk LineRenderer
        List<Vector3> worldPositions = new List<Vector3>();

        for (int i = 0; i < positions.Count; i++)
        {
            // Konversi posisi UI ke posisi dunia
            Vector3 worldPos = contentParent.TransformPoint(positions[i]);
            worldPos.z = 0; // Jaga garis di depan
            worldPositions.Add(worldPos);
        }

        pathLineRenderer.positionCount = worldPositions.Count;
        pathLineRenderer.SetPositions(worldPositions.ToArray());
        pathLineRenderer.startColor = pathColor; // Atur warna awal
        pathLineRenderer.endColor = pathColor;   // Atur warna akhir
        pathLineRenderer.startWidth = pathWidth;
        pathLineRenderer.endWidth = pathWidth;

        if (pathMaterial != null)
        {
            pathLineRenderer.material = pathMaterial;
        }
    }

    private void AdjustContentSize(List<Vector2> positions)
    {
        if (contentParent == null || positions.Count == 0) return;

        // Calculate bounds
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        foreach (var pos in positions)
        {
            minY = Mathf.Min(minY, pos.y);
            maxY = Mathf.Max(maxY, pos.y);
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
        }

        // Add padding
        float padding = 100f;
        float contentWidth = (maxX - minX) + padding * 2;
        float contentHeight = Mathf.Abs(minY - maxY) + padding * 2;

        contentParent.sizeDelta = new Vector2(contentWidth, contentHeight);

        // Auto-scroll to first unlocked level
        ScrollToFirstUnlockedLevel();
    }

    private void ScrollToFirstUnlockedLevel()
    {
        if (scrollRect == null || levelButtons.Count == 0) return;

        // Find first locked level (next level to unlock)
        int targetIndex = 0;
        for (int i = 0; i < levelButtons.Count; i++)
        {
            if (!levelManager.IsUnlocked(levelButtons[i].GetLevelId()))
            {
                targetIndex = Mathf.Max(0, i - 1); // Go to last unlocked level
                break;
            }
        }

        if (targetIndex < levelButtons.Count)
        {
            StartCoroutine(ScrollToLevelCoroutine(targetIndex));
        }
    }

    private System.Collections.IEnumerator ScrollToLevelCoroutine(int levelIndex)
    {
        yield return new WaitForSeconds(0.5f); // Wait for animation

        if (levelIndex < levelButtons.Count)
        {
            var targetButton = levelButtons[levelIndex];
            var targetPos = targetButton.GetComponent<RectTransform>().anchoredPosition;

            // Calculate normalized position for ScrollRect
            float normalizedY = Mathf.Clamp01(-targetPos.y / contentParent.sizeDelta.y);

            // Smooth scroll animation
            float startY = scrollRect.verticalNormalizedPosition;
            float duration = 1f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                t = Mathf.SmoothStep(0, 1, t); // Smooth easing

                scrollRect.verticalNormalizedPosition = Mathf.Lerp(startY, normalizedY, t);
                yield return null;
            }
        }
    }

    private System.Collections.IEnumerator AnimateButtonsIn()
    {
        foreach (var button in levelButtons)
        {
            if (button != null)
            {
                button.gameObject.transform.localScale = Vector3.zero;

                // Animate scale in
                float duration = 0.3f;
                float elapsed = 0;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float scale = Mathf.Lerp(0, 1, EaseOutBack(t));
                    button.gameObject.transform.localScale = Vector3.one * scale;
                    yield return null;
                }

                button.gameObject.transform.localScale = Vector3.one;
                yield return new WaitForSeconds(animationDelay);
            }
        }
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void ClearExistingButtons()
    {
        foreach (var button in levelButtons)
        {
            if (button != null)
            {
                DestroyImmediate(button.gameObject);
            }
        }
        levelButtons.Clear();
    }

    // Public methods for changing patterns
    public void SetPathPattern(PathPattern pattern)
    {
        currentPattern = pattern;
        BuildLevelMap();
    }

    public void SetPathPattern(int patternIndex)
    {
        if (patternIndex >= 0 && patternIndex < System.Enum.GetValues(typeof(PathPattern)).Length)
        {
            SetPathPattern((PathPattern)patternIndex);
        }
    }
}