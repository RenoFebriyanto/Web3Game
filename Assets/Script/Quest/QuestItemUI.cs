using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI controller untuk satu quest row. 
/// - Slider hanya tampil sebagai progress bar (non-interactable).
/// - amountText menunjukan reward amount (misal "5.000") bukan progress.
/// - titleText diisi langsung dari QuestData.title (tidak menambah prefix).
/// </summary>
public class QuestItemUI : MonoBehaviour
{
    [Header("UI refs (assign in prefab)")]
    public TMP_Text titleText;          // QuestItem/Desk/... => judul quest (set langsung di QuestData.title)
    public Image iconImage;             // QuestItem/Cont/Icon/Itemss (opsional)
    public TMP_Text amountText;         // QuestItem/Cont/Icon/Amount -> menampilkan reward amount (mis: 5000)
    public Button actionButton;         // QuestItem/Cont/Claim/Button
    public TMP_Text actionButtonText;   // child TMP text pada actionButton

    [Header("Progress bar")]
    public Slider progressSlider;       // Slider component (non-interactable)
    public Image sliderFillImage;       // Slider -> Fill Area -> Fill (Image)
    public Image sliderBackgroundImage; // Slider background (Image)

    [Header("Colors")]
    public Color colorEmpty = new Color(0.90f, 0.90f, 0.90f); // abu-abu saat belum ada progress
    public Color colorFill = new Color(0.02f, 0.56f, 1.00f); // biru saat ada progress

    [Header("Optional sprites for button visual states")]
    public Sprite spriteLocked;
    public Sprite spriteReady;
    public Sprite spriteClaimed;
    public Image actionButtonImage;     // image component of the button (optional)

    // internal
    QuestData data;
    QuestManager manager;

    void Awake()
    {
        // Safety: if slider present, disable interactability & hide handle
        if (progressSlider != null)
        {
            progressSlider.interactable = false;
            if (progressSlider.handleRect != null && progressSlider.handleRect.gameObject != null)
            {
                progressSlider.handleRect.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Setup called by QuestManager when creating/refreshing list.
    /// </summary>
    public void Setup(QuestData q, QuestManager mgr)
    {
        data = q;
        manager = mgr;

        if (titleText != null) titleText.text = string.IsNullOrEmpty(q.title) ? q.questId : q.title;

        // icon optional
        if (iconImage != null && q.icon != null) iconImage.sprite = q.icon;

        // update amount text to show reward amount (format for coins)
        Refresh();

        // wire button
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButton);
        }
    }

    /// <summary>
    /// Refresh UI from manager state (progress & claimed)
    /// </summary>
    public void Refresh()
    {
        if (data == null) return;

        int cur = 0;
        bool claimed = false;
        if (manager != null)
        {
            cur = manager.GetProgress(data.questId);
            claimed = manager.IsClaimed(data.questId);
        }

        // Slider: set max & value
        if (progressSlider != null)
        {
            progressSlider.maxValue = Mathf.Max(1, data.requiredAmount);
            progressSlider.value = Mathf.Clamp(cur, 0, data.requiredAmount);

            // color logic: fill blue when value>0 else grey
            if (sliderFillImage != null) sliderFillImage.color = (progressSlider.value > 0f) ? colorFill : colorEmpty;
            if (sliderBackgroundImage != null) sliderBackgroundImage.color = Color.Lerp(colorEmpty, Color.white, 0.05f);
        }

        // amountText shows reward amount (not progress)
        if (amountText != null)
        {
            switch (data.rewardType)
            {
                case QuestRewardType.Coin:
                    amountText.text = data.rewardAmount.ToString("N0");
                    break;
                case QuestRewardType.Shard:
                    amountText.text = data.rewardAmount.ToString();
                    break;
                case QuestRewardType.Energy:
                    amountText.text = data.rewardAmount.ToString();
                    break;
                case QuestRewardType.Booster:
                    // show count for booster
                    amountText.text = data.rewardAmount.ToString();
                    break;
                default:
                    amountText.text = data.rewardAmount.ToString();
                    break;
            }
        }

        // Button state
        bool ready = cur >= data.requiredAmount && !claimed;

        if (actionButtonText != null)
        {
            if (claimed) actionButtonText.text = "Claimed";
            else if (ready) actionButtonText.text = "Claim";
            else actionButtonText.text = "Go";
        }

        if (actionButton != null)
        {
            actionButton.interactable = !claimed;
        }

        // Button sprite switching (optional)
        if (actionButtonImage != null)
        {
            if (claimed && spriteClaimed != null) actionButtonImage.sprite = spriteClaimed;
            else if (ready && spriteReady != null) actionButtonImage.sprite = spriteReady;
            else if (spriteLocked != null) actionButtonImage.sprite = spriteLocked;
        }
    }

    void OnActionButton()
    {
        if (data == null || manager == null) return;

        int cur = manager.GetProgress(data.questId);
        bool claimed = manager.IsClaimed(data.questId);

        if (!claimed && cur >= data.requiredAmount)
        {
            // Claim reward via manager (manager will handle economy/boosters)
            manager.ClaimReward(data.questId);
            // immediately refresh visuals
            Refresh();
        }
        else
        {
            // "Go" action — up to you: jump to level, open UI, etc.
            Debug.Log($"[QuestItemUI] GO for quest {data.questId}");
            // example: manager.GoToQuestTarget(data);
        }
    }

    // optional helper called externally when manager state changes
    public void OnManagerUpdated()
    {
        Refresh();
    }
}
