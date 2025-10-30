#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Editor helper untuk auto-attach UIButtonSound ke semua buttons.
/// Usage:
/// 1. Pilih root GameObject (e.g. Canvas) di hierarchy
/// 2. Right click → Add Button Sounds → To Selected & Children
/// </summary>
public class AddButtonSoundsHelper : MonoBehaviour
{
    [MenuItem("GameObject/Add Button Sounds/To Selected & Children", false, 0)]
    static void AddButtonSoundsToSelected()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        int addedCount = 0;
        int skippedCount = 0;

        foreach (var go in selected)
        {
            // Get all buttons in selected object and children
            Button[] buttons = go.GetComponentsInChildren<Button>(true);

            foreach (var btn in buttons)
            {
                // Skip if already has UIButtonSound
                if (btn.GetComponent<UIButtonSound>() != null)
                {
                    skippedCount++;
                    continue;
                }

                // Add UIButtonSound component
                var soundComp = btn.gameObject.AddComponent<UIButtonSound>();
                soundComp.playClickSound = true;
                soundComp.playHoverSound = true;

                EditorUtility.SetDirty(btn.gameObject);
                addedCount++;
            }
        }

        Debug.Log($"[AddButtonSounds] Added UIButtonSound to {addedCount} buttons. Skipped {skippedCount} (already have component).");

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
    }

    [MenuItem("GameObject/Add Button Sounds/Remove From Selected & Children", false, 1)]
    static void RemoveButtonSoundsFromSelected()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        int removedCount = 0;

        foreach (var go in selected)
        {
            UIButtonSound[] sounds = go.GetComponentsInChildren<UIButtonSound>(true);

            foreach (var sound in sounds)
            {
                DestroyImmediate(sound);
                removedCount++;
            }
        }

        Debug.Log($"[AddButtonSounds] Removed {removedCount} UIButtonSound components.");

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene()
        );
    }

    [MenuItem("GameObject/Add Button Sounds/Count Buttons in Selected", false, 2)]
    static void CountButtonsInSelected()
    {
        GameObject[] selected = Selection.gameObjects;

        if (selected == null || selected.Length == 0)
        {
            Debug.LogWarning("No GameObject selected!");
            return;
        }

        int totalButtons = 0;
        int withSound = 0;
        int withoutSound = 0;

        foreach (var go in selected)
        {
            Button[] buttons = go.GetComponentsInChildren<Button>(true);
            totalButtons += buttons.Length;

            foreach (var btn in buttons)
            {
                if (btn.GetComponent<UIButtonSound>() != null)
                {
                    withSound++;
                }
                else
                {
                    withoutSound++;
                }
            }
        }

        Debug.Log($"[AddButtonSounds] Total buttons: {totalButtons}\n" +
                  $"  - With UIButtonSound: {withSound}\n" +
                  $"  - Without UIButtonSound: {withoutSound}");
    }
}
#endif