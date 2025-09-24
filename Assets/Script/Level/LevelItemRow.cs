// Assets/Script/Level/LevelItemRow.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelItemRow : MonoBehaviour
{
    [Header("UI references")]
    public TMP_Text levelNumberTMP;
    public Button button;
    public Image backgroundImage;
    public Image[] starImages; // 3 star images (1..3)
    public Sprite starEmpty;
    public Sprite starFull;
    public Sprite lockedSprite;
    public Sprite unlockedSprite;

    LevelDefinition def;

    void Awake()
    {
        if (button != null) button.onClick.AddListener(OnClick);
    }

    public void Setup(LevelDefinition d)
    {
        def = d;
        Refresh();
    }

    public void Refresh()
    {
        if (def == null) return;
        if (levelNumberTMP != null) levelNumberTMP.text = def.number.ToString();
        bool locked = def.locked || !IsLevelUnlockedInPrefs(def.id);
        if (backgroundImage != null) backgroundImage.sprite = locked ? lockedSprite : unlockedSprite;
        if (button != null) button.interactable = !locked;

        // load stars for this level
        int bestStars = LevelProgressManager.Instance?.GetBestStars(def.id) ?? 0;
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] != null) starImages[i].sprite = (i < bestStars) ? starFull : starEmpty;
            starImages[i].gameObject.SetActive(true);
        }
    }

    bool IsLevelUnlockedInPrefs(string id)
    {
        // fallback: also check PlayerPrefs stored per level unlocked if you used LevelManager.SaveDefinitionsState
        return PlayerPrefs.GetInt("Kulino_LevelLocked_" + id, def.locked ? 1 : 0) == 0 ? true : !def.locked;
    }

    void OnClick()
    {
        if (def == null) return;
        Debug.Log($"[LevelItemRow] Click level {def.number} (id={def.id})");
        LevelLoader.LoadLevel(def.id, def.number);
    }
}
