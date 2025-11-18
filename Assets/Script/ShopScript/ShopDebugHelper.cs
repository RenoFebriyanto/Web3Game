// ShopDebugHelper.cs - Fixed warnings CS0414 by removing unused fields (since they are assigned but never used)

using UnityEngine;
using System.Linq;

/// <summary>
/// Helper script untuk debug shop system
/// Attach ke GameObject ShopManager untuk quick debugging
/// </summary>
public class ShopDebugHelper : MonoBehaviour
{
    [Header("🔍 Debug Settings")]
    public bool enableDebugLogs = true;
    public bool autoCheckOnStart = true;
    
    [Header("📊 Shop System Status")]
    [SerializeField] private int totalShopItems = 0;
    
    void Start()
    {
        if (autoCheckOnStart)
        {
            Invoke(nameof(RunDiagnostics), 1f);
        }
    }
    
    [ContextMenu("🔍 Run Full Diagnostics")]
    public void RunDiagnostics()
    {
        Debug.Log("=== SHOP SYSTEM DIAGNOSTICS ===");
        
        CheckShopManager();
        CheckKulinoCoinManager();
        CheckPlayerEconomy();
        CheckShopItems();
        
        Debug.Log("=== DIAGNOSTICS COMPLETE ===");
    }
    
    [ContextMenu("💰 Check Kulino Coin Balance")]
    public void CheckKulinoCoinBalance()
    {
        if (KulinoCoinManager.Instance == null)
        {
            Debug.LogError("❌ KulinoCoinManager not found!");
            return;
        }
        
        double balance = KulinoCoinManager.Instance.GetBalance();
        Debug.Log($"💰 Current Kulino Coin Balance: {balance:F6} KC");
        
        if (balance <= 0)
        {
            Debug.LogWarning("⚠️ Balance is ZERO! Make sure:");
            Debug.LogWarning("  1. Wallet is connected");
            Debug.LogWarning("  2. Balance has been fetched");
            Debug.LogWarning("  3. Token account exists for this wallet");
        }
    }
    
    [ContextMenu("🔄 Force Refresh All")]
    public void ForceRefreshAll()
    {
        Debug.Log("🔄 Force refreshing all systems...");
        
        if (KulinoCoinManager.Instance != null)
        {
            KulinoCoinManager.Instance.RefreshBalance();
            Debug.Log("✓ Kulino Coin balance refresh triggered");
        }
        
        if (PlayerEconomy.Instance != null)
        {
            Debug.Log($"✓ Player Economy: Coins={PlayerEconomy.Instance.Coins}, Shards={PlayerEconomy.Instance.Shards}");
        }
    }
    
    void CheckShopManager()
    {
        var shopManager = FindFirstObjectByType<ShopManager>();
        
        if (shopManager == null)
        {
            Debug.LogError("❌ ShopManager not found in scene!");
            return;
        }
        
        Debug.Log("✅ ShopManager found");
        
        // Check database
        if (shopManager.database == null && (shopManager.shopItems == null || shopManager.shopItems.Count == 0))
        {
            Debug.LogWarning("⚠️ ShopManager has no items assigned!");
        }
    }
    
    void CheckKulinoCoinManager()
    {
        if (KulinoCoinManager.Instance == null)
        {
            Debug.LogError("❌ KulinoCoinManager not found!");
            Debug.LogError("   Make sure GameObject 'KulinoCoinManager' exists in scene");
            return;
        }
        
        Debug.Log("✅ KulinoCoinManager found");
        
        double balance = KulinoCoinManager.Instance.GetBalance();
        Debug.Log($"   Balance: {balance:F6} KC");
        
        if (balance <= 0)
        {
            Debug.LogWarning("⚠️ Kulino Coin balance is ZERO!");
        }
    }
    
    void CheckPlayerEconomy()
    {
        if (PlayerEconomy.Instance == null)
        {
            Debug.LogError("❌ PlayerEconomy not found!");
            return;
        }
        
        Debug.Log("✅ PlayerEconomy found");
        Debug.Log($"   Coins: {PlayerEconomy.Instance.Coins:N0}");
        Debug.Log($"   Shards: {PlayerEconomy.Instance.Shards}");
        Debug.Log($"   Energy: {PlayerEconomy.Instance.Energy}/{PlayerEconomy.Instance.MaxEnergy}");
    }
    
    void CheckShopItems()
    {
        var shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager == null) return;
        
        var items = shopManager.database?.items ?? shopManager.shopItems;
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("⚠️ No shop items found!");
            return;
        }
        
        totalShopItems = items.Count;
        Debug.Log($"📦 Total Shop Items: {totalShopItems}");
        Debug.Log("─────────────────────────────────");
        
        foreach (var item in items.Where(i => i != null))
        {
            Debug.Log($"📝 {item.displayName} ({item.itemId})");
            
            // Check payment methods
            int paymentMethods = 0;
            
            if (item.allowBuyWithCoins && item.coinPrice > 0)
            {
                Debug.Log($"   💰 Coin: {item.coinPrice:N0}");
                paymentMethods++;
            }
            
            if (item.allowBuyWithShards && item.shardPrice > 0)
            {
                Debug.Log($"   💎 Shard: {item.shardPrice}");
                paymentMethods++;
            }
            
            if (item.allowBuyWithKulinoCoin && item.kulinoCoinPrice > 0)
            {
                Debug.Log($"   🪙 Kulino Coin: {item.kulinoCoinPrice:F6} KC");
                paymentMethods++;
            }
            
            if (paymentMethods == 0)
            {
                Debug.LogWarning($"   ⚠️ NO PAYMENT METHODS ENABLED!");
            }
            
            Debug.Log("   ─────");
        }
    }
    
    [ContextMenu("📊 Print Shop Item Details")]
    public void PrintShopItemDetails()
    {
        var shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager == null)
        {
            Debug.LogError("ShopManager not found!");
            return;
        }
        
        var items = shopManager.database?.items ?? shopManager.shopItems;
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("No items to display!");
            return;
        }
        
        Debug.Log("=== DETAILED SHOP ITEMS ===");
        
        foreach (var item in items.Where(i => i != null))
        {
            Debug.Log($"\n📦 {item.displayName}");
            Debug.Log($"   ID: {item.itemId}");
            Debug.Log($"   Type: {item.rewardType}");
            Debug.Log($"   Amount: {item.rewardAmount}");
            Debug.Log($"   ─── Payment Options ───");
            Debug.Log($"   Coins: {(item.allowBuyWithCoins ? $"✅ {item.coinPrice:N0}" : "❌")}");
            Debug.Log($"   Shards: {(item.allowBuyWithShards ? $"✅ {item.shardPrice}" : "❌")}");
            Debug.Log($"   Kulino Coin: {(item.allowBuyWithKulinoCoin ? $"✅ {item.kulinoCoinPrice:F6} KC" : "❌")}");
        }
        
        Debug.Log("\n=== END DETAILS ===");
    }
}