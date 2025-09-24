using UnityEngine;

[CreateAssetMenu(menuName = "Kulino/LaneConfig")]
public class LaneConfig : ScriptableObject
{
    public int laneCount = 3;
    public float laneOffset = 2.5f;

    // helper: hitung X world untuk lane index
    public float GetLaneWorldX(int laneIndex)
    {
        float centerLane = (laneCount - 1) / 2f;
        return (laneIndex - centerLane) * laneOffset;
    }
}
