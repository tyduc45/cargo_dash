using UnityEngine;
using System.Collections;

public class EmergencyLockdown : GameEventBehaviour
{
    [Header("Prefab 设置")]
    public GameObject warningPrefab;   // 红色感叹号+警告线
    public GameObject wallPrefab;      // 隔离墙（最好把主要 Collider2D 放在根或唯一子物体里）

    [Header("时间参数")]
    public float warningDuration = 3f; // 警告持续
    public float dropDuration = 0.3f;  // 下落时间
    public float stayDuration = 5f;    // 停留时间
    public float riseDuration = 8f;    // 上升（消失）时间

    [Header("生成位置")]
    public Transform[] receivingPorts; // 接收口 Transform 列表
    private int lastIndex = -1;

    [Header("地面对齐 & 下落高度")]
    [Tooltip("用于检测地面的 Layer")]
    public LayerMask groundMask;
    [Tooltip("从这个高度开始向下射线寻找地面（相对接收口 Y）")]
    public float rayStartAbove = 20f;
    [Tooltip("射线长度")]
    public float rayLength = 60f;
    [Tooltip("微调落地后的 Y（比如皮带厚度等）")]
    public float groundYOffset = 0f;
    [Tooltip("从最终落点向上抬多高作为下落起点")]
    public float dropHeight = 12f;

    public override void Trigger()
    {
        StartCoroutine(ExecuteLockdown());
    }

    IEnumerator ExecuteLockdown()
    {
        if (receivingPorts == null || receivingPorts.Length == 0) yield break;

        // 1) 选位置（避免连击同一根柱子）
        int index = GetNextIndex();
        lastIndex = index;
        Transform target = receivingPorts[index];


        // 2) 警告标记（就在柱子位置）
     

        // --- 警告标记 ---
        GameObject warn = Instantiate(warningPrefab, target.position, Quaternion.identity);

        SoundManager.Instance.StartWarningLoop(SoundType.IsolationWallWarning, 0.4f);

        yield return new WaitForSeconds(warningDuration);

        SoundManager.Instance.StopWarningLoop();
        Destroy(warn);

        // 3) 用射线找到该 X 下的“地面”世界坐标 Y
        float groundY = FindGroundY(target.position.x, target.position.y + rayStartAbove, rayLength, groundMask);
        if (float.IsNaN(groundY))
        {
            // 兜底：没打到地面就用 target.y
            groundY = target.position.y;
        }
        groundY += groundYOffset;

        // 4) 先在大致位置生成墙，然后用“底边对齐地面”的方式精确校准
        Vector3 spawnPosRough = new Vector3(target.position.x, groundY + dropHeight, target.position.z);
        GameObject wall = Instantiate(wallPrefab, spawnPosRough, Quaternion.identity);

        // 5) 计算“最终落点”：把墙的底边严格对齐到 groundY，得到一个中心点 finalPos
        Vector3 finalPos = AlignBottomToGround(wall, groundY);

        // 6) 把墙抬到“落点 + dropHeight”作为动画起点
        wall.transform.position = finalPos + Vector3.up * dropHeight;

        // 7) 交给墙体脚本做下落/停留/上升；目标点传 finalPos（注意是中心点，不是 groundY）
        var iso = wall.GetComponent<IsolationWall>();
        if (iso != null)
        {
            iso.Init(finalPos, dropDuration, stayDuration, riseDuration);
        }
        else
        {
            // 简易兜底动画：直接插值到 finalPos，然后若无 iso 自己销毁
            StartCoroutine(FallbackDrop(wall.transform, finalPos));
        }
    }

    private int GetNextIndex()
    {
        if (receivingPorts.Length == 1) return 0;

        int newIndex;
        do
        {
            newIndex = Random.Range(0, receivingPorts.Length);
        } while (newIndex == lastIndex);
        return newIndex;
    }

    private float FindGroundY(float x, float startY, float dist, LayerMask mask)
    {
        // 先按 groundMask 扫一遍所有命中，再在里面挑“真正的地面”
        var hits = Physics2D.RaycastAll(new Vector2(x, startY), Vector2.down, dist, mask);

        // 选第一个“非触发器 & 不是玩家”的命中
        foreach (var h in hits)
        {
            if (!h.collider) continue;
            if (h.collider.isTrigger) continue;
            if (h.collider.CompareTag("player")) continue;

            Debug.DrawRay(new Vector3(x, startY, 0), Vector3.down * h.distance, Color.green, 1.5f);
            return h.point.y;
        }

        // 如果 groundMask 里没命中（配置可能漏了层），再做一次“无限制”兜底，并同样过滤玩家/触发器
        hits = Physics2D.RaycastAll(new Vector2(x, startY), Vector2.down, dist);
        foreach (var h in hits)
        {
            if (!h.collider) continue;
            if (h.collider.isTrigger) continue;
            if (h.collider.CompareTag("player")) continue;

            Debug.LogWarning($"[Lockdown] GroundMask missed real ground at x={x}. " +
                             $"Hit via fallback at y={h.point.y}. Please fix groundMask.");
            Debug.DrawRay(new Vector3(x, startY, 0), Vector3.down * h.distance, Color.yellow, 1.5f);
            return h.point.y;
        }

        // 兜底：真的什么都没打到 → 交给上层用 target.y
        Debug.LogWarning($"[Lockdown] Raycast miss at x={x}. Using fallback (target.y).");
        return float.NaN;
    }

    /// <summary>
    /// 让“墙的最底边”与 groundY 对齐，返回此时墙的世界 position（中心点）。
    /// 这比用 Pivot/offset 可靠，适配任意 prefab 结构（根/子物体上有 Collider2D 或 SpriteRenderer 都可）。
    /// </summary>
    private Vector3 AlignBottomToGround(GameObject wall, float groundY)
    {
        Transform t = wall.transform;

        // 取优先级：Collider2D > SpriteRenderer
        Bounds b;
        Collider2D col = wall.GetComponentInChildren<Collider2D>();
        if (col != null)
        {
            b = col.bounds;
        }
        else
        {
            SpriteRenderer sr = wall.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                b = sr.bounds;
            }
            else
            {
                // 没有可用的 bounds，直接返回
                return t.position;
            }
        }

        float bottom = b.min.y;           // 当前底边
        float delta = groundY - bottom;   // 需要上/下移的量
        t.position += new Vector3(0f, delta, 0f);

        return t.position; // 现在此位置下，墙的底边 == groundY
    }

    private IEnumerator FallbackDrop(Transform tr, Vector3 finalPos)
    {
        float t = 0f;
        Vector3 start = tr.position;
        while (t < dropDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dropDuration);
            tr.position = Vector3.Lerp(start, finalPos, k);
            yield return null;
        }
        yield return new WaitForSeconds(stayDuration);
        // 简单上升再销毁
        t = 0f;
        Vector3 riseStart = tr.position;
        Vector3 riseEnd = finalPos + Vector3.up * dropHeight;
        while (t < riseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / riseDuration);
            tr.position = Vector3.Lerp(riseStart, riseEnd, k);
            yield return null;
        }
        Destroy(tr.gameObject);
    }
}
