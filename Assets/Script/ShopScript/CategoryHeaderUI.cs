using UnityEngine;
using TMPro;

/// <summary>
/// CategoryHeaderUI - Simple header component
/// Layout dihandle oleh ShopManager dengan dynamic grid constraint
/// </summary>
public class CategoryHeaderUI : MonoBehaviour
{
    [Header("UI Components")]
    public TMP_Text headerText;
    
    /// <summary>
    /// Set header text
    /// </summary>
    public void SetText(string text)
    {
        if (headerText != null)
        {
            headerText.text = text;
        }
    }
}