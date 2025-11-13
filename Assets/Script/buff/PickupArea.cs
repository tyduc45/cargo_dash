using UnityEngine;

[DisallowMultipleComponent]
public class PickupArea2D : MonoBehaviour
{
    [Header("Area Source")]
    public Collider2D areaCollider;   // 不填则自动取本物体上的 Collider2D

    [Header("Placement Guards")]
    [Tooltip("避障半径；>0 时会用 OverlapCircle 检查障碍")]
    public float clearanceRadius = 0.3f;
    public LayerMask obstacleMask;

    [Header("Ground Snap (可选)")]
    public bool snapToGround = false;
    public LayerMask groundMask;
    public float rayStartAbove = 2f;
    public float rayLengthDown = 10f;
    public float groundYOffset = 0.02f;

    [Header("Sampling")]
    public int maxAttempts = 30;

    void Reset()
    {
        areaCollider = GetComponent<Collider2D>();
        if (areaCollider) areaCollider.isTrigger = true;
    }

    public bool TrySamplePoint(out Vector3 result)
    {
        result = Vector3.zero;
        var col = areaCollider ? areaCollider : GetComponent<Collider2D>();
        if (!col) return false;

        Bounds b = col.bounds;
        for (int i = 0; i < Mathf.Max(1, maxAttempts); i++)
        {
            float x = Random.Range(b.min.x, b.max.x);
            float y = Random.Range(b.min.y, b.max.y);
            Vector2 p = new Vector2(x, y);
            Debug.Log($"run pos gen, pos : {p}");

            if (!col.OverlapPoint(p)) continue;

            if (clearanceRadius > 0f && obstacleMask.value != 0)
            {
                if (Physics2D.OverlapCircle(p, clearanceRadius, obstacleMask))
                    continue; // 被占用，重采样
            }

            Vector3 finalP = p;
            if (snapToGround && groundMask.value != 0)
            {
                Debug.Log($"run snap to ground");
                Vector2 start = new Vector2(p.x, p.y + rayStartAbove);
                var hit = Physics2D.Raycast(start, Vector2.down, rayLengthDown, groundMask);
                if (!hit.collider) continue;
                finalP = new Vector3(p.x, hit.point.y + groundYOffset, 0f);
            }

            result = finalP;
            Debug.Log($"spawn pos {p}, final pos {result}");
            return true;
        }
        return false;
    }
    //public bool TrySamplePoint(out Vector3 result)
    //{
    //    result = Vector3.zero;

    //    var col = areaCollider ? areaCollider : GetComponent<Collider2D>();
    //    if (!col) return false;

    //    Bounds b = col.bounds; // 世界坐标 AABB，与 OnDrawGizmos 的矩形一致:contentReference[oaicite:1]{index=1}

    //    for (int i = 0; i < Mathf.Max(1, maxAttempts); i++)
    //    {
    //        // 用 center/extents 在世界空间采样
    //        float x = Random.Range(b.center.x - b.extents.x, b.center.x + b.extents.x);
    //        float y = Random.Range(b.center.y - b.extents.y, b.center.y + b.extents.y);

    //        // 明确 z（2D 通常 0；若你的 prefab 需要特定 z，替换这里）
    //        Vector3 p = new Vector3(x, y, 0f);
    //        Debug.Log($"run pos generate");

    //        // 避障（世界坐标）
    //        if (clearanceRadius > 0f && obstacleMask.value != 0)
    //        {
    //            if (Physics2D.OverlapCircle(p, clearanceRadius, obstacleMask)) continue;
    //        }

    //        // 贴地（可选）
    //        Vector3 finalP = p;
    //        if (snapToGround && groundMask.value != 0)
    //        {
    //            Debug.Log($"run snaptoGround");
    //            Vector2 start = new Vector2(p.x, p.y + rayStartAbove);
    //            var hit = Physics2D.Raycast(start, Vector2.down, rayLengthDown, groundMask);
    //            if (!hit.collider) continue;
    //            finalP = new Vector3(p.x, hit.point.y + groundYOffset, 0f);
    //        }

    //        result = finalP;
    //        Debug.Log($"spawn pos {p}, final pos {result}");
    //        return true;
    //    }
    //    return false;
    //}

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        var col = areaCollider ? areaCollider : GetComponent<Collider2D>();
        if (!col) return;
        var c = new Color(0.2f, 0.8f, 1f, 0.15f);
        Gizmos.color = c;
        Gizmos.DrawCube(col.bounds.center, col.bounds.size);
    }
#endif
}
