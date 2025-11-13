using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class OutlineController : MonoBehaviour
{
    [Header("外轮廓材质（Unlit + Cull Front）")]
    public Material outlineMaterial;

    [Header("轮廓粗细（通过整体缩放实现）")]
    [Range(1.001f, 1.08f)]
    public float outlineScale = 1.03f;

    [Header("初始是否高亮")]
    public bool highlightOnStart = false;

    // 生成的轮廓克隆列表
    private readonly List<GameObject> _outlineClones = new List<GameObject>();
    private bool _built = false;

    void Awake()
    {
        BuildOutlineClones();
        SetHighlight(highlightOnStart, true);
    }

    void OnValidate()
    {
        if (_built)
        {
            // 实时更新缩放
            foreach (var clone in _outlineClones)
            {
                if (clone) clone.transform.localScale = Vector3.one * outlineScale;
            }
        }
    }

    public void SetHighlight(bool on, bool force = false)
    {
        if (!_built && !force) BuildOutlineClones();
        foreach (var go in _outlineClones)
        {
            if (go) go.SetActive(on);
        }
    }

    private void BuildOutlineClones()
    {
        if (outlineMaterial == null)
        {
            Debug.LogWarning($"[{name}] 未指定 Outline 材质。");
            return;
        }

        // 清理旧的
        foreach (var go in _outlineClones) if (go) Destroy(go);
        _outlineClones.Clear();

        // 仅处理静态 Mesh（MeshRenderer + MeshFilter）
        var meshes = GetComponentsInChildren<MeshFilter>(true);
        foreach (var mf in meshes)
        {
            var mr = mf.GetComponent<MeshRenderer>();
            if (!mr || !mf.sharedMesh) continue;

            var clone = new GameObject(mf.gameObject.name + "_Outline");
            clone.transform.SetParent(mf.transform, false);
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one * outlineScale;

            var cloneMF = clone.AddComponent<MeshFilter>();
            cloneMF.sharedMesh = mf.sharedMesh;

            var cloneMR = clone.AddComponent<MeshRenderer>();
            cloneMR.sharedMaterial = outlineMaterial;
            cloneMR.shadowCastingMode = ShadowCastingMode.Off;
            cloneMR.receiveShadows = false;
            cloneMR.allowOcclusionWhenDynamic = false;
            cloneMR.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            cloneMR.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            // 让轮廓和原物体同一 Layer（必要时配合相机/后续特效）
            clone.layer = mr.gameObject.layer;

            _outlineClones.Add(clone);
        }

        _built = true;
    }
}
