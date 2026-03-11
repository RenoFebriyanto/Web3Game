using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Scrolling background menggunakan RawImage di dalam Canvas.
/// Otomatis responsive di semua device — tidak perlu resize manual.
/// </summary>
public class BGLOOP : MonoBehaviour
{
    [Tooltip("Kecepatan scroll. Positif = ke atas, negatif = ke bawah.")]
    public float speed = 0.1f;

    [Tooltip("RawImage yang menjadi background. Assign di Inspector.")]
    public RawImage bgRawImage;

    void Awake()
    {
        if (bgRawImage == null)
            bgRawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        if (bgRawImage == null) return;

        Rect uv = bgRawImage.uvRect;
        uv.y += speed * Time.deltaTime;
        bgRawImage.uvRect = uv;
    }
}