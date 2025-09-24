using UnityEngine;
using System.Collections;

public class GameplayQuestTracker : MonoBehaviour
{
    public static GameplayQuestTracker Instance { get; private set; }

    [Header("Automatic tracking toggles")]
    public bool trackPlayTime = true;
    public bool trackDistance = true;

    float playTimeAccumulator = 0f;
    float distanceAccumulator = 0f;
    Vector3 lastPosition;
    bool tracking = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable() { StartTracking(); }
    void OnDisable() { StopTracking(); }

    public void StartTracking()
    {
        tracking = true;
        playTimeAccumulator = 0f;
        distanceAccumulator = 0f;
        lastPosition = GetPlayerPositionSafe();
        StartCoroutine(TrackCoroutine());
    }

    public void StopTracking()
    {
        tracking = false;
        StopAllCoroutines();
    }

    IEnumerator TrackCoroutine()
    {
        while (tracking)
        {
            float dt = Time.deltaTime;
            if (trackPlayTime)
            {
                playTimeAccumulator += dt;
                // per detik kirim
                if (playTimeAccumulator >= 1f)
                {
                    int secs = Mathf.FloorToInt(playTimeAccumulator);
                    QuestManager.Instance?.AddPlayTimeSeconds(secs);
                    playTimeAccumulator -= secs;
                }
            }

            if (trackDistance)
            {
                Vector3 now = GetPlayerPositionSafe();
                float d = Vector3.Distance(now, lastPosition);
                if (d > 0f)
                {
                    distanceAccumulator += d;
                    // send integer meters rounded (or use game units as meters)
                    if (distanceAccumulator >= 1f)
                    {
                        int meters = Mathf.FloorToInt(distanceAccumulator);
                        QuestManager.Instance?.AddDistanceMeters(meters);
                        distanceAccumulator -= meters;
                    }
                }
                lastPosition = now;
            }

            yield return null;
        }
    }

    // safe-get player position (if rocket tagged "Player" etc)
    Vector3 GetPlayerPositionSafe()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) return player.transform.position;
        return Vector3.zero;
    }

    // Exposed API for pickups and other events
    public void RegisterCoinsCollected(long amount)
    {
        QuestManager.Instance?.AddCoinsCollected(amount);
    }

    public void RegisterDistanceMeters(long meters)
    {
        QuestManager.Instance?.AddDistanceMeters(meters);
    }

    public void RegisterPlayTimeSeconds(long secs)
    {
        QuestManager.Instance?.AddPlayTimeSeconds(secs);
    }
}
