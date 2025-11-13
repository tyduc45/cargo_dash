using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class IsolationWall : MonoBehaviour
{
    [Header("Player Knockback")]
    [Tooltip("Knockback speed applied to player (units/sec). Uses linearVelocity to launch.")]
    public float knockbackSpeed = 1000f;

    [Tooltip("Knockback angle in degrees (0 = right, 90 = up). Direction is to the LEFT-UP of this angle.")]
    public float knockbackAngle = 45f;

    [Tooltip("How long the player stays stunned after being hit (seconds)")]
    public float stunDuration = 3f;

    [Tooltip("Temporarily set player's linear drag during launch (0 recommended). Restored after a short delay.")]
    public float tempLinearDrag = 0f;

    [Tooltip("Realtime seconds to keep tempLinearDrag before restoring")]
    public float dragRestoreDelay = 0.15f;

    [Header("Layers")]
    public LayerMask playerMask;                // 玩家层；用于落地前的范围扫描

    [Header("Safety")]
    public float postLandTriggerGrace = 0.05f;  // 落地后保持 trigger 的极短时间，保证击飞事件送达
    public float overlapShrink = 0.98f;         // OverlapBox 缩放，避免边缘抖动判断

    private Vector3 groundPos;
    private float dropDuration, stayDuration, riseDuration;

    private bool isDropping = false;    // 下落阶段：触发器，只做击飞
    private Collider2D[] allCols;       // 记录全体碰撞器，统一切换 trigger/实体
    private Rigidbody2D rb;

    [Header("Dust Animation")]
    [SerializeField] private GameObject dust;
    private Animator dustAnimator;
    private string dustAnimationName = "DustCloud";


    private string[] hitMessages = { "CRUSHED!", "OUCH!", "BAM!", "BONKED!" };


    public void Init(Vector3 groundTarget, float dropT, float stayT, float riseT)
    {
        groundPos = groundTarget;
        dropDuration = dropT;
        stayDuration = stayT;
        riseDuration = riseT;


        rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        if (dust != null)
        {
            dustAnimator = dust.GetComponent<Animator>();
        }

        // 收集所有子层级碰撞器，避免有子碰撞器仍为实体把玩家顶住
        allCols = GetComponentsInChildren<Collider2D>(includeInactive: true);
        SetAllCollidersTrigger(true);

        StartCoroutine(Lifecycle());
    }

    IEnumerator Lifecycle()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(groundPos.x, groundPos.y, transform.position.z);

        // --- Drop（触发器阶段，仅击飞） ---
        isDropping = true;

        float t = 0f;
        while (t < dropDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, t / dropDuration);
            yield return null;
        }
        transform.position = endPos;

        // 落地前，做一次范围扫描：兜底“初始就重叠/最后一帧才重叠”的情况
        KnockAnyPlayersOverlapping();

        // 给触发系统一个极短的缓冲，让刚进入/停留的触发事件得以及时处理
        if (postLandTriggerGrace > 0f)
            yield return new WaitForSeconds(postLandTriggerGrace);

        if (dustAnimator != null && !string.IsNullOrEmpty(dustAnimationName))
        {
            dust.SetActive(true);
            dustAnimator.Play(dustAnimationName);
            float animDuration = GetDustAnimationLength();
            LeanTween.delayedCall(animDuration, () => dust.SetActive(false));
        }

        SoundManager.Instance.PlaySound(SoundType.IsolationWallDrop, null, 0.5f);

        // 落地后→切为实体阻挡
        isDropping = false;
        SetAllCollidersTrigger(false);

        // --- Stay（阻挡阶段） ---
        yield return new WaitForSeconds(stayDuration);

        // --- Rise（上升回初始） ---
        // 上升阶段再次变回触发器，避免卡住玩家
        SetAllCollidersTrigger(false);

        t = 0f;
        Vector3 riseStart = transform.position;
        Vector3 riseEnd = startPos;
        while (t < riseDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(riseStart, riseEnd, t / riseDuration);
            yield return null;
        }

        Destroy(gameObject);
    }

    // 下落阶段：作为 Trigger，只做“确定性击飞”
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isDropping) return;
        TryKnock(other);
    }

    // 解决 “开始就重叠/进入丢帧” 的情况
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isDropping) return;
        TryKnock(other);
    }

    private void TryKnock(Collider2D other)
    {
        if (!other || !other.CompareTag("player")) return;

        var p = other.GetComponent<controller>();
        if (p == null) return;

        string statusText = hitMessages[Random.Range(0, hitMessages.Length)];
        TweenUtils.ShowStatusText(statusText, other.transform.position, 2f, GradientType.Hit);
        ////p.ApplyHitMaterial();
        TweenUtils.TriggerVignetteEffect();

        TweenUtils.ShowStunText("STUNNED!", p.transform, stunDuration);

        p.ApplyStunnedMaterial(stunDuration);
        SoundManager.Instance.PlaySound(SoundType.PlayerHurt, null, 0.3f);

       
        TweenUtils.TriggerChromaticAberration(0.3f, 0.5f);


        // 计算左上方向 knockbackAngle°
        Vector2 knockDir = new Vector2(
            -Mathf.Cos(knockbackAngle * Mathf.Deg2Rad),
             Mathf.Sin(knockbackAngle * Mathf.Deg2Rad)
        ).normalized;

        var prb = p.GetComponent<Rigidbody2D>();

        // 进入眩晕并禁用自控（避免玩家脚本立刻覆写速度）
        p.isStunning = true;
        p.SetControl(false);

        // 先清空货物再击飞
        while (p.cargoStack.Count != 0) p.ThrowCargo();
        p.playerReset();

        if (prb != null)
        {
            prb.WakeUp();
            float origDrag = prb.linearDamping;
            prb.linearDamping = tempLinearDrag;
            prb.linearVelocity = knockDir * knockbackSpeed;
            StartCoroutine(RestoreDragRealtime(prb, origDrag, dragRestoreDelay));
        }

        StartCoroutine(StunPlayer(p, stunDuration));
    }


    private void HitEffect(controller p)
    {
       p.ApplyHitMaterial();
    }
    

    // 落地瞬间用体积检测兜底（避免 isDropping 变 false 前没收到触发）
    private void KnockAnyPlayersOverlapping()
    {
        if (allCols == null || allCols.Length == 0) return;

        // 用所有碰撞器的合并 Bounds 做一个 OverlapBox
        Bounds merged = allCols[0].bounds;
        for (int i = 1; i < allCols.Length; i++)
            merged.Encapsulate(allCols[i].bounds);

        Vector2 size = merged.size * overlapShrink;
        Collider2D[] hits = Physics2D.OverlapBoxAll(merged.center, size, 0f, playerMask);
        foreach (var h in hits)
        {
            if (h && h.CompareTag("player"))
                TryKnock(h);
        }
    }

    private void SetAllCollidersTrigger(bool isTrigger)
    {
        if (allCols == null) return;
        foreach (var c in allCols)
        {
            if (c == null) continue;
            c.isTrigger = isTrigger;
        }
    }

    private IEnumerator RestoreDragRealtime(Rigidbody2D rb, float targetDrag, float delay)
    {
        float end = Time.realtimeSinceStartup + Mathf.Max(0f, delay);
        while (Time.realtimeSinceStartup < end) yield return null;
        if (rb != null) rb.linearDamping = targetDrag;
    }

    private IEnumerator StunPlayer(controller p, float stunTime)
    {
        Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
        // 等到“接近落地”，避免空中立刻恢复
        yield return new WaitUntil(() => prb && Mathf.Abs(prb.linearVelocity.y) < 0.01f);
        yield return new WaitForSeconds(0.2f);

        // 眩晕计时
        yield return new WaitForSeconds(stunTime);

        // 恢复
        p.isStunning = false;
        // 状态回复正常
        p.ApplyNormalMaterial();
        p.SetControl(true);
      
    }

    private float GetDustAnimationLength()
    {
        if (dustAnimator == null || dustAnimator.runtimeAnimatorController == null)
            return 1f;

        foreach (var clip in dustAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == dustAnimationName)
                return clip.length;
        }
        return 1f;
    }

    // 仅用于可视化调试 OverlapBox（非必需）
    private void OnDrawGizmosSelected()
    {
        if (allCols == null || allCols.Length == 0) return;
        Bounds merged = allCols[0].bounds;
        for (int i = 1; i < allCols.Length; i++)
            merged.Encapsulate(allCols[i].bounds);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(merged.center, merged.size * overlapShrink);
    }
}
