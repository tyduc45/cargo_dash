using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class MouseHoverOutline : MonoBehaviour
{
    [Header("联动组件")]
    public OutlineController outline;        // 拖物体上的 OutlineController
    public CanvasGroup tooltip;              // 拖 TooltipCanvas 的 CanvasGroup（可选）
    public TextMeshProUGUI textUI;           // 拖 Tooltip文字（可选）

    [Header("文案")]
    [TextArea] public string message = "Z 拾取 / X 提交 / C 丢弃";
    public float fadeDuration = 0.12f;

    Coroutine co;

    void Start()
    {
        if (textUI) textUI.text = message;
        if (tooltip) { tooltip.alpha = 0; tooltip.gameObject.SetActive(false); }
    }

    void OnMouseEnter() { Toggle(true); }
    void OnMouseExit() { Toggle(false); }

    void OnMouseDown()
    {
        // 可选：点击时做点事
        // Debug.Log("Clicked " + name);
    }

    void Toggle(bool on)
    {
        if (outline) outline.SetHighlight(on);
        if (!tooltip) return;

        if (on)
        {
            tooltip.gameObject.SetActive(true);
            StartFade(1f, false);
        }
        else
        {
            StartFade(0f, true);
        }
    }

    void StartFade(float target, bool deactivate)
    {
        if (co != null) StopCoroutine(co);
        co = StartCoroutine(FadeTo(target, deactivate));
    }

    System.Collections.IEnumerator FadeTo(float target, bool deactivate)
    {
        float start = tooltip.alpha, t = 0f;
        while (t < fadeDuration) { t += Time.deltaTime; tooltip.alpha = Mathf.Lerp(start, target, t / fadeDuration); yield return null; }
        tooltip.alpha = target;
        if (deactivate && target <= 0f) tooltip.gameObject.SetActive(false);
    }
}
