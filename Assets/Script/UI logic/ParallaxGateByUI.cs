using UnityEngine;

public class ParallaxGateByUI : MonoBehaviour
{
    [Header("要被禁用/启用的 Parallax 组件（可拖多个）")]
    public ParallaxCamera[] targets;

    [Header("自动寻找 MainCamera 上的 ParallaxCamera")]
    public bool findOnMainCamera = true;

    [Header("重新启用时把基准重置为当前相机位，避免跳动")]
    public bool resetBaseWhenReEnable = true;

    void Awake()
    {
        // 没手动拖的话，默认找 MainCamera 上的 ParallaxCamera
        if ((targets == null || targets.Length == 0) && findOnMainCamera && Camera.main)
        {
            var p = Camera.main.GetComponent<ParallaxCamera>();
            if (p) targets = new[] { p };
        }
    }

    void OnEnable() { Toggle(false); }  // UI 打开 → 关闭 Parallax
    void OnDisable() { Toggle(true); }  // UI 关闭 → 开启 Parallax

    void Toggle(bool enable)
    {
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (!t) continue;
            t.enabled = enable;
            if (enable && resetBaseWhenReEnable)
                t.ResetBaseToCurrent(); // 你现有脚本里就有这个方法
        }
    }
}
