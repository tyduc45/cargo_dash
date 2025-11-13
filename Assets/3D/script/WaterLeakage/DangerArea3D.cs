using Project.Gameplay3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class DangerArea3D : MonoBehaviour
{
    [Header("停留阈值/生存")]
    [Tooltip("玩家在区域内停留多久后触发滑倒")]
    public float stayTime = 0.25f;

    [Tooltip("危险区存在时间（秒）")]
    public float lifetime = 3f;

    [Header("滑倒 + 眩晕（由 Controller3D 消费）")]
    [Tooltip("供 Controller3D.Slip 使用")]
    public float slipDistance = 0.6f;

    [Tooltip("供 Controller3D.Slip 使用")]
    public float slipDuration = 0.12f;

    [Tooltip("（未在此脚本直接使用）")]
    public float stunDuration = 1.0f;

    [Tooltip("强壮状态是否免疫")]
    public bool ignoreStrong = true;

    [Header("渲染(支持渐隐)")]
    [SerializeField] private Renderer[] allRenderers;

    [Header("丢货物（顺序丢出）")]
    [Tooltip("滑倒时，按该间隔依次丢出堆叠货物（<=0 则按物理帧丢出）")]
    public float sequentialDropInterval = 0.10f;

    private float timer;
    private Transform playerTf;
    private bool usedOnce;                 // 本次进入只触发一次
    private BoxCollider box;
    private Coroutine dropRoutine;

    // —— 修复减速“永久持续”的关键：记录区内玩家，在销毁/禁用时主动恢复 ——
    private readonly HashSet<Controller3D> _insiders = new HashSet<Controller3D>();

    void Awake()
    {
        box = GetComponent<BoxCollider>();
        box.isTrigger = true;

        if (allRenderers == null || allRenderers.Length == 0)
            allRenderers = GetComponentsInChildren<Renderer>();

        StartCoroutine(AutoDestroy());
    }

    void OnTriggerEnter(Collider col)
    {
        var pc = col.GetComponentInParent<Controller3D>();
        if (!pc) return;

        _insiders.Add(pc);     // 记录进入者
        pc.EnterSlowZone();    // 进入减速

        playerTf = col.transform.root;
        timer = 0f;
        usedOnce = false;      // 进入时重置一次性开关
    }

    private void OnTriggerStay(Collider col)
    {
        if (usedOnce) return;

        var pc = col.GetComponentInParent<Controller3D>();
        if (!pc) return;

        timer += Time.deltaTime;
        if (timer >= stayTime)
        {
            usedOnce = true;

            // 1) 顺序丢货（避免同帧同点抛出造成卡堆）
            var inv = col.GetComponentInParent<PlayerCarry3D>();
            if (inv != null && inv.Count > 0)
            {
                if (dropRoutine != null) StopCoroutine(dropRoutine);
                dropRoutine = StartCoroutine(DropStackSequentially(inv));
            }

            // 2) 触发滑倒
            Vector3 slideDir = Vector3.ProjectOnPlane(pc.transform.forward, Vector3.up);
            if (slideDir.sqrMagnitude < 0.01f)
                slideDir = Vector3.Cross(Vector3.up, pc.transform.forward);
            slideDir.Normalize();

            pc.Slip(slideDir);
        }
    }

    void OnTriggerExit(Collider col)
    {
        var pc = col.GetComponentInParent<Controller3D>();
        if (pc != null)
        {
            pc.ExitSlowZone();     // 离开即恢复
            _insiders.Remove(pc);  // 从集合移除
        }

        playerTf = null;
        timer = 0f;
        usedOnce = false;

        // 如果希望离开后就不再继续丢剩余货物，可取消注释：
        // if (dropRoutine != null) { StopCoroutine(dropRoutine); dropRoutine = null; }
    }

    private IEnumerator DropStackSequentially(PlayerCarry3D inv)
    {
        while (inv != null && inv.Count > 0)
        {
            inv.TryDrop();

            if (sequentialDropInterval <= 0f)
                yield return new WaitForFixedUpdate();          // 物理帧间隔，更稳
            else
                yield return new WaitForSeconds(sequentialDropInterval);
        }
        dropRoutine = null;
    }

    private IEnumerator AutoDestroy()
    {
        // 留出淡出时间：这里预留 0.3s
        float fadeDuration = 0.3f;
        yield return new WaitForSeconds(Mathf.Max(0f, lifetime - fadeDuration));

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = 1f - (t / fadeDuration);
            foreach (var r in allRenderers)
            {
                if (!r) continue;
                if (r.material.HasProperty("_Color"))
                {
                    var c = r.material.color;
                    c.a = a;
                    r.material.color = c;
                }
            }
            yield return null;
        }

        // —— 在销毁前，确保把仍在区域内的玩家恢复到正常速度 ——
        CleanupSlow();
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // 被禁用或即将销毁时，都做一次兜底恢复
        CleanupSlow();
    }

    private void CleanupSlow()
    {
        if (_insiders.Count == 0) return;

        // 将仍在集合中的玩家全部恢复
        foreach (var pc in _insiders)
        {
            if (pc) pc.ExitSlowZone();
        }
        _insiders.Clear();
    }

    void OnDrawGizmosSelected()
    {
        var b = GetComponent<BoxCollider>();
        if (b == null) return;
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0f, 0.6f, 1f, 0.8f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}


