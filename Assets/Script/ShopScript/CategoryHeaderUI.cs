using UnityEngine;
using TMPro;

/// <summary>
/// CategoryHeaderUI - Simplified untuk container system
/// Version: 2.0 - Container System
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
