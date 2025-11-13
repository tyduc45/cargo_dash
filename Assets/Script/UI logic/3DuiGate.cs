using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

/// <summary>
/// 在“开始界面”显示期间隐藏其他 UI，开始游戏后恢复。
/// 建议把本脚本挂在【开始面板的根对象】（最好是独立 Canvas）上。
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("UI/PreStart UI Gate")]
public class PreStartUiGate : MonoBehaviour
{
    [Header("Auto Find")]
    [Tooltip("是否自动查找并隐藏场景中除自己之外的所有 Canvas。")]
    public bool autoFindOtherCanvases = true;

    [Tooltip("只隐藏根 Canvas（isRootCanvas==true），减少误伤子Canvas。推荐勾选。")]
    public bool onlyRootCanvases = true;

    [Tooltip("是否也隐藏 World Space 的 Canvas。若你的3D世界里有HUD想一起隐藏，勾选它。")]
    public bool alsoHideWorldSpaceCanvases = true;

    [Header("Manual")]
    [Tooltip("额外需要被隐藏的对象（非Canvas根，也可填具体面板）。")]
    public List<GameObject> extraHideObjects = new List<GameObject>();

    [Tooltip("白名单：这些对象（或Canvas）不会被隐藏。把EventSystem或需要常驻的提示拖进来。")]
    public List<GameObject> whiteList = new List<GameObject>();

    [Header("How To Hide")]
    [Tooltip("用 SetActive(false) 彻底隐藏对象（推荐）。若同Canvas上还有其他子UI且不想整体关可改用禁用Canvas。")]
    public bool hideBySetActive = true;

    [Tooltip("如果不使用SetActive，则改为禁用Canvas.enabled。")]
    public bool hideByDisableCanvas = false;

    [Header("Lifecycle")]
    [Tooltip("启用时立即应用隐藏。")]
    public bool applyOnEnable = true;

    [Tooltip("订阅 GameManager3D.OnGameStarted，在开始时自动释放。")]
    public bool subscribeGameStart = true;

    // 缓存
    private Canvas _selfCanvas;
    private readonly List<(GameObject go, bool wasActive)> _setActiveCache = new();
    private readonly List<(Canvas canvas, bool wasEnabled)> _canvasEnabledCache = new();
    private bool _applied;

    void Awake()
    {
        _selfCanvas = GetComponentInParent<Canvas>();
        // 防呆：至少一种隐藏方式
        if (!hideBySetActive && !hideByDisableCanvas)
            hideBySetActive = true;
    }

    void OnEnable()
    {
        if (applyOnEnable)
            Apply();

        if (subscribeGameStart)
            GameManager3D.OnGameStarted += Release;
    }

    void OnDisable()
    {
        if (subscribeGameStart)
            GameManager3D.OnGameStarted -= Release;
    }

    /// <summary>执行隐藏其他UI</summary>
    public void Apply()
    {
        if (_applied) return;
        _applied = true;

        // 1) 自动搜集其它 Canvas
        if (autoFindOtherCanvases)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases)
            {
                if (c == null) continue;
                if (!alsoHideWorldSpaceCanvases && c.renderMode == RenderMode.WorldSpace) continue;

                // 跳过自己 & 白名单
                if (IsSelfOrChildOfSelf(c.gameObject)) continue;
                if (IsInWhiteList(c.gameObject)) continue;

                if (onlyRootCanvases && !c.isRootCanvas) continue;

                HideTarget(c.gameObject, c);
            }
        }

        // 2) 手动额外对象
        foreach (var go in extraHideObjects)
        {
            if (!go) continue;
            if (IsSelfOrChildOfSelf(go)) continue;
            if (IsInWhiteList(go)) continue;

            var c = go.GetComponent<Canvas>();
            HideTarget(go, c);
        }

        // 3) 保险：不要动 EventSystem
        // （不做任何处理）
    }

    /// <summary>释放隐藏，恢复到进入场景前的状态</summary>
    public void Release()
    {
        if (!_applied) return;
        _applied = false;

        // 恢复 SetActive
        for (int i = _setActiveCache.Count - 1; i >= 0; i--)
        {
            var item = _setActiveCache[i];
            if (item.go) item.go.SetActive(item.wasActive);
        }
        _setActiveCache.Clear();

        // 恢复 Canvas.enabled
        for (int i = _canvasEnabledCache.Count - 1; i >= 0; i--)
        {
            var item = _canvasEnabledCache[i];
            if (item.canvas) item.canvas.enabled = item.wasEnabled;
        }
        _canvasEnabledCache.Clear();
    }

    /// <summary>
    /// 给“开始按钮”的 OnClick 直接绑定本方法：
    /// 先释放隐藏，再触发 GameManager3D 开始（若未开始）。
    /// </summary>
    public void OnStartButton()
    {
        Release();
        if (GameManager3D.Instance != null)
        {
            // BeginGame 内部有防抖，不会重复开始
            GameManager3D.Instance.BeginGame();
        }
    }

    private void HideTarget(GameObject go, Canvas canvasIfAny)
    {
        if (hideBySetActive)
        {
            _setActiveCache.Add((go, go.activeSelf));
            go.SetActive(false);
        }
        else if (hideByDisableCanvas && canvasIfAny != null)
        {
            _canvasEnabledCache.Add((canvasIfAny, canvasIfAny.enabled));
            canvasIfAny.enabled = false;
        }
        // 如需更细粒度（只隐藏某些子物体），请将它们放入 extraHideObjects
    }

    private bool IsSelfOrChildOfSelf(GameObject go)
    {
        if (!_selfCanvas) return false;
        return go == _selfCanvas.gameObject || go.transform.IsChildOf(_selfCanvas.transform);
    }

    private bool IsInWhiteList(GameObject go)
    {
        if (go == null) return true; // 保险
        foreach (var w in whiteList)
        {
            if (!w) continue;
            if (go == w || go.transform.IsChildOf(w.transform)) return true;
        }
        return false;
    }
}
