using UnityEngine;

public class Planet : MonoBehaviour, IPoolable
{
    public SpriteRenderer spriteRenderer;
    public Collider2D obstacleCollider;
    public float lifespan = 30f; // optional auto-despawn
    float spawnTime;

    public void OnSpawned()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (lifespan > 0 && Time.time - spawnTime >= lifespan)
        {
            gameObject.SetActive(false);
        }
    }
}
