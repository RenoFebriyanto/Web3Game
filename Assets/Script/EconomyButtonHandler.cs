using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ✅ Handler untuk 4 button di PanelEconomy (kanan atas)
/// Attach ke: Coin, Shard, Energy, KulinoCoin buttons
/// </summary>
[RequireComponent(typeof(Button))]
public class EconomyButtonHandler : MonoBehaviour
{
    public enum EconomyType { Coin, Shard, Energy, KulinoCoin }

    [Header("⚙️ Settings")]
    [Tooltip("Tipe button ini")]
    public EconomyType buttonType = EconomyType.Coin;

    [Header("🔗 References (Auto-find if null)")]
    [Tooltip("ShopManager untuk buka shop panel")]
    public ShopManager shopManager;

    [Tooltip("ButtonManager untuk navigation")]
    public ButtonManager buttonManager;

    [Tooltip("OpenPhantomBuyCoin popup")]
    public GameObject openPhantomPopup;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();

        // Auto-find references
        if (shopManager == null)
            shopManager = FindFirstObjectByType<ShopManager>();

        if (buttonManager == null)
            buttonManager = FindFirstObjectByType<ButtonManager>();

        if (openPhantomPopup == null)
            openPhantomPopup = GameObject.Find("OpenPhantomBuyCoin");
    }

    void Start()
    {
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    void OnButtonClicked()
    {
        // Play sound
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayButtonClick();
        }

        switch (buttonType)
        {
            case EconomyType.Coin:
                OpenShopFilterCoins();
                break;

            case EconomyType.Shard:
                OpenShopFilterShards();
                break;

            case EconomyType.Energy:
                OpenShopFilterItems();
                break;

            case EconomyType.KulinoCoin:
                OpenPhantomBuyPopup();
                break;
        }

        Debug.Log($"[EconomyButton] {buttonType} clicked!");
    }

    void OpenShopFilterCoins()
    {
        // Buka shop panel
        if (buttonManager != null)
        {
            buttonManager.ShowShop();
        }

        // Set filter ke Items (Coin category)
        if (shopManager != null)
        {
            shopManager.ShowItems();
        }

        Debug.Log("[EconomyButton] ✓ Opened Shop → Filter: Items (Coins)");
    }

    void OpenShopFilterShards()
    {
        // Buka shop panel
        if (buttonManager != null)
        {
            buttonManager.ShowShop();
        }

        // Set filter ke Shard
        if (shopManager != null)
        {
            shopManager.ShowShard();
        }

        Debug.Log("[EconomyButton] ✓ Opened Shop → Filter: Shard");
    }

    void OpenShopFilterItems()
    {
        // Buka shop panel
        if (buttonManager != null)
        {
            buttonManager.ShowShop();
        }

        // Set filter ke Items (Energy category)
        if (shopManager != null)
        {
            shopManager.ShowItems();
        }

        Debug.Log("[EconomyButton] ✓ Opened Shop → Filter: Items (Energy)");
    }

    void OpenPhantomBuyPopup()
    {
        if (openPhantomPopup != null)
        {
            openPhantomPopup.SetActive(true);
            Debug.Log("[EconomyButton] ✓ Opened Phantom Buy Coin popup");
        }
        else
        {
            Debug.LogError("[EconomyButton] ❌ OpenPhantomBuyCoin GameObject not found!");
        }
    }
}