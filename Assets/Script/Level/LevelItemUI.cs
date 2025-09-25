using UnityEngine;
using UnityEngine.UI;
#if TMP_PRESENT
using TMPro;
#endif
using UnityEngine.Events;

public class LevelItemUI : MonoBehaviour
{
#if TMP_PRESENT
    public TMP_Text numberText;
#else
    public Text numberText;
#endif
    public Image lockIcon;
    public Image[] starIcons = new Image[3];
    public Sprite starEmpty;
    public Sprite starFilled;
    public Button button;

    [HideInInspector]
    public UnityEvent onClick = new UnityEvent();

    LevelDefData currentDef;

    void Awake()
    {
        if (button != null)
            button.onClick.AddListener(() => onClick.Invoke());
    }

    public void Setup(LevelDefData def)
    {
        currentDef = def;
#if TMP_PRESENT
        if (numberText != null) numberText.text = def.id.ToString();
#else
        if (numberText != null) numberText.text = def.id.ToString();
#endif
        if (lockIcon != null) lockIcon.gameObject.SetActive(def.locked);

        for (int i = 0; i < 3; i++)
        {
            if (starIcons != null && i < starIcons.Length && starIcons[i] != null)
            {
                starIcons[i].sprite = (i < def.bestStars) ? starFilled : starEmpty;
                starIcons[i].gameObject.SetActive(true);
            }
        }

        if (button != null) button.interactable = !def.locked;
    }
}
