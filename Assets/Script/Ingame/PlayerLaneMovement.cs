using UnityEngine;

public class PlayerLaneMovement : MonoBehaviour
{
    [Header("Lane settings")]
    public float laneOffset = 2.5f;   // jarak antar lane di axis X
    public int laneCount = 3;         // jumlah lane (default 3)
    public float moveSpeed = 10f;     // kecepatan transisi (semakin besar = semakin cepat)

    private int currentLane = 1;      // mulai dari lane tengah (0=left,1=center,2=right)
    private Vector3 targetPosition;

    void Start()
    {
        // set posisi awal di lane tengah
        targetPosition = transform.position;
        targetPosition.x = LaneToWorldX(currentLane);
        transform.position = targetPosition;
    }

    void Update()
    {
        HandleInput();

        // gerakkan rocket menuju target lane secara smooth
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetPosition.x, Time.deltaTime * moveSpeed);
        transform.position = pos;
    }

    void HandleInput()
    {
        // Keyboard input (desktop)
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            MoveLane(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            MoveLane(1);

        // (Opsional) Mobile swipe bisa ditambahkan nanti
    }

    void MoveLane(int direction)
    {
        int newLane = Mathf.Clamp(currentLane + direction, 0, laneCount - 1);
        if (newLane != currentLane)
        {
            currentLane = newLane;
            targetPosition = transform.position;
            targetPosition.x = LaneToWorldX(currentLane);
        }
    }

    float LaneToWorldX(int laneIndex)
    {
        // lane 0 = kiri, laneCount-1 = kanan
        float centerLane = (laneCount - 1) / 2f;
        return (laneIndex - centerLane) * laneOffset;
    }
}
