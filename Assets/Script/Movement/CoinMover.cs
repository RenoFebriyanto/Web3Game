using UnityEngine;

public class CoinMover : MonoBehaviour
{
    public float speed = 3f;
    public float destroyY = -12f;

    public void SetSpeed(float s) { speed = s; }

    void Update()
    {
        transform.position += Vector3.down * speed * Time.deltaTime;
        if (transform.position.y < destroyY)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
