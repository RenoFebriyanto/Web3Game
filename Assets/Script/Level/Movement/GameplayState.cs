/// <summary>
/// Flag global sederhana untuk menandai apakah gameplay masih "berjalan".
/// Dipakai oleh mover-mover (CoinMover / PlanetMover / FragmentMover) supaya
/// berhenti bergerak begitu level selesai (menang ATAU game over),
/// tanpa perlu menyentuh Time.timeScale (yang ikut membekukan UI/animasi popup).
///
/// - Di-set false saat LevelCompleteUI.OnLevelComplete() / ShowGameOver() dipanggil.
/// - Di-reset ke true otomatis saat player baru spawn (lihat PlayerHealth.Awake()),
///   jadi tidak perlu reset manual saat pindah/reload scene.
/// </summary>
public static class GameplayState
{
    public static bool IsRunning = true;
}