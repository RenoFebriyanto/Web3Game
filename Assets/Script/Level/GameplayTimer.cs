using UnityEngine;
using TMPro;

/// <summary>
/// Script khusus untuk mengatur timer gameplay.
/// Format:
/// Dibawah 1 menit  -> detik.milidetik
/// Diatas 1 menit   -> menit.detik
/// </summary>
public class GameplayTimer : MonoBehaviour
{
    [Header("Timer Display")]
    public TMP_Text timerText;


    [Header("Timer Settings")]
    public bool enableTimer;

    private float timer;



    private void Start()
    {
        timer = 0;

        if (timerText != null)
        {
            timerText.text = "";
        }
    }


    private void Update()
    {
        if (enableTimer)
        {
            UpdateTimer();
        }
    }



    private void UpdateTimer()
    {
        timer += Time.deltaTime;


        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);
        int milliseconds = Mathf.FloorToInt((timer * 100) % 100);


        if (timer < 60)
        {
            // Format: detik.milidetik
            timerText.text = $"{seconds}.{milliseconds:00}";
        }
        else
        {
            // Format: menit.detik
            timerText.text = $"{minutes}.{seconds:00}";
        }
    }



    public void StartTimer()
    {
        timer = 0;
        enableTimer = true;

        if (timerText != null)
        {
            timerText.text = "0.00";
        }
    }



    public void StopTimer()
    {
        enableTimer = false;
    }



    public void ResetTimer()
    {
        timer = 0;

        if (timerText != null)
        {
            timerText.text = "0.00";
        }
    }



    public float GetTimer()
    {
        return timer;
    }
}