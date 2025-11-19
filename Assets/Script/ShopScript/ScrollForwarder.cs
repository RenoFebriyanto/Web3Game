using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Attach ke ScrollBackground untuk forward scroll input ke ScrollRect
/// </summary>
public class ScrollForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScrollRect targetScrollRect;

    void Start()
    {
        if (targetScrollRect == null)
        {
            targetScrollRect = GetComponentInParent<ScrollRect>();
            
            if (targetScrollRect == null)
            {
                // Search in scene
                targetScrollRect = FindFirstObjectByType<ScrollRect>();
            }
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetScrollRect != null)
            targetScrollRect.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetScrollRect != null)
            targetScrollRect.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (targetScrollRect != null)
            targetScrollRect.OnEndDrag(eventData);
    }
}
