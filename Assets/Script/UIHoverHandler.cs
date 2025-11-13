using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public float hoverScale = 1.2f;
    [HideInInspector] public Vector3 originalScale;

    public void OnPointerEnter(PointerEventData eventData)
    {
        UITweens.CancelTweens(gameObject);
        UITweens.ScaleOnHover(gameObject, hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UITweens.CancelTweens(gameObject);
        UITweens.ScaleOnHoverExit(gameObject, originalScale);
    }
}
