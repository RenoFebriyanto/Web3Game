using UnityEngine;

public class BGLOOP : MonoBehaviour
{
    [Tooltip("Kecepatan per detik. Positif = ke atas, negatif = ke bawah.")]
    public float speed = 0.1f;

    [Tooltip("Renderer yang memiliki material dengan texture yang di-set Wrap Mode = Repeat.")]
    public Renderer BgRender;

    void Update()
    {
        if (BgRender == null) return;

        // Ambil offset sekarang, tambahkan pada sumbu Y
        Vector2 offset = BgRender.material.mainTextureOffset;
        offset.y = (offset.y + speed * Time.deltaTime) % 1f; // jaga agar tetap di 0..1
        BgRender.material.mainTextureOffset = offset;
    }
}
