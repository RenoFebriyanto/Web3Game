using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Kulino/Shop Database", fileName = "ShopDatabase")]
public class ShopDatabase : ScriptableObject
{
    public List<ShopItemData> items = new List<ShopItemData>();
}
