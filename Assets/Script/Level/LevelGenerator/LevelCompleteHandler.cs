using System;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handle level complete - terintegrasi dengan KulinoCoinRewardSystem
/// KulinoCoinRewardSystem akan handle popup display otomatis
/// </summary>
public class LevelCompleteHandler : MonoBehaviour
{
    [Header("Auto-Find Components")]
    [Tooltip("Auto-find LevelGameSession")]
    public bool autoFindSession = true;

    private LevelGameSession session;
    private bool levelCompleted = false;

    void Start()
    {
        // Find session
        if (autoFindSession)
        {
            session = FindFirstObjectByType<LevelGameSession>();
            if (session == null)
            {
                Debug.LogWarning("[LevelCompleteHandler] LevelGameSession not found!");
            }
        }

        // Subscribe to level complete event
        if (session != null)
        {
            session.OnLevelCompleted.AddListener(OnLevelComplete);
            Debug.Log("[LevelCompleteHandler] ✓ Subscribed to OnLevelCompleted event");
        }
    }

    void OnDestroy()
    {
        if (session != null)
        {
            session.OnLevelCompleted.RemoveListener(OnLevelComplete);
        }
    }

    /// <summary>
    /// Called ketika level complete
    /// KulinoCoinRewardSystem akan handle popup display otomatis
    /// </summary>
    void OnLevelComplete()
    {
        if (levelCompleted)
        {
            Debug.LogWarning("[LevelCompleteHandler] Level already completed!");
            return;
        }

        levelCompleted = true;
        Debug.Log("[LevelCompleteHandler] ✓ Level Complete!");

        // Save stars progress
        var starManager = FindFirstObjectByType<GameplayStarManager>();
        if (starManager != null)
        {
            starManager.CompleteLevelWithStars();
        }

        // KulinoCoinRewardSystem akan handle popup secara otomatis
        // (sudah subscribe ke LevelGameSession.OnLevelCompleted)
        Debug.Log("[LevelCompleteHandler] Reward check delegated to KulinoCoinRewardSystem");
    }

    [ContextMenu("Test: Trigger Level Complete")]
    public void TestLevelComplete()
    {
        OnLevelComplete();
    }
}