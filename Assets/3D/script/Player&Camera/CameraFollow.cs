using UnityEngine;

/// 顶视斜45°相机（恒定离地高度版）
/// - 相机始终保持与地面的垂直高度 = heightOverGround
/// - 焦点锚在玩家脚下地面 + focusOffset
/// - 可选：根据“相机脚下”的地面做1~2次迭代校正，减少坡面误差
public class TopDown45FollowStable : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;
    public Vector3 focusOffset = new Vector3(0f, 1.2f, 0f);

    [Header("构图")]
    [Range(10f, 80f)] public float tiltAngle = 45f;
    public bool followTargetYaw = false;
    public float yaw = 0f;
    public float yawOffset = 0f;

    [Header("恒定离地高度")]
    public float heightOverGround = 8f;   // 相机离地“垂直高度”（米）
    public bool matchGroundUnderCamera = true; // 用相机脚下地面再校正一次
    [Range(0, 3)] public int correctionIterations = 1; // 迭代次数(0~2足够)

    [Header("平滑(秒)")]
    public float focusSmooth = 0.12f;   // 焦点XZ平滑
    public float groundSmooth = 0.15f;  // 焦点地面高度平滑
    public float yawSmooth = 0.15f;   // 角度平滑
    public float distSmooth = 0.08f;   // 距离平滑

    [Header("地面采样")]
    public LayerMask groundMask = ~0;
    public float probeStartHeight = 2f;   // 射线/球体起点抬高
    public float probeDistanceDown = 50f;  // 向下探测距离
    public float probeRadius = 0.25f;// 球体半径（更稳）

    [Header("防穿模")]
    public bool avoidClipping = true;
    public float clipPadding = 0.25f;
    public float clipRadius = 0.2f;
    public float castOriginLift = 0.3f; // 提升起点，避免起点与地面相切
    public float minDistance = 2.5f;    // 与焦点的最小斜向距离下限
    public LayerMask clipMask = ~0;     // 排除玩家自身层

    // 运行态
    Vector3 focusPoint, focusVelXZ;
    float groundYFocus, groundYVel;
    float yawCurrent, yawVel;
    float distCurrent, distVel;

    void Start()
    {
        if (!target) target = GameObject.FindWithTag("Player")?.transform;
        groundYFocus = SampleGroundY(target ? target.position : transform.position, out bool ok);
        if (!ok) groundYFocus = (target ? target.position : transform.position).y;

        focusPoint = new Vector3(
            target ? target.position.x : transform.position.x,
            groundYFocus + focusOffset.y,
            target ? target.position.z : transform.position.z
        );

        yawCurrent = followTargetYaw ? GetTargetYaw() + yawOffset : yaw;
        distCurrent = 5f; // 初值，稍后会被目标距离覆盖
    }

    void LateUpdate()
    {
        if (!target) return;

        // —— 1) 焦点地面高度（在玩家XZ，做平滑）——
        float sampled = SampleGroundY(target.position, out bool got);
        if (!got) sampled = groundYFocus; // 无命中就沿用上次
        groundYFocus = Mathf.SmoothDamp(groundYFocus, sampled, ref groundYVel, groundSmooth);

        // 焦点：XZ随玩家(平滑)，Y=地面+偏移（不再随跳跃抖）
        Vector3 desiredFocus = new Vector3(target.position.x, groundYFocus + focusOffset.y, target.position.z);
        focusPoint.x = Mathf.SmoothDamp(focusPoint.x, desiredFocus.x, ref focusVelXZ.x, focusSmooth);
        focusPoint.z = Mathf.SmoothDamp(focusPoint.z, desiredFocus.z, ref focusVelXZ.z, focusSmooth);
        focusPoint.y = desiredFocus.y;

        // —— 2) 角度（固定45°俯仰 + 平滑 yaw）——
        float yawTarget = followTargetYaw ? GetTargetYaw() + yawOffset : yaw;
        yawCurrent = Mathf.SmoothDampAngle(yawCurrent, yawTarget, ref yawVel, yawSmooth);
        Quaternion rot = Quaternion.Euler(tiltAngle, yawCurrent, 0f);
        Vector3 toCamDir = -(rot * Vector3.forward); // 从焦点指向相机

        // —— 3) 由“恒定离地高度”解出斜向距离d —— 
        // 先以“焦点处地面”为基准的解析解：focus.y + d*sin(tilt) = ground(y)_focus + heightOverGround
        float sinTilt = Mathf.Sin(tiltAngle * Mathf.Deg2Rad);
        float desiredDist = Mathf.Max(minDistance, (heightOverGround - focusOffset.y) / Mathf.Max(0.001f, sinTilt));

        // 可选：以“相机脚下地面”为准再校正1~2次，减少坡面误差
        if (matchGroundUnderCamera && correctionIterations > 0)
        {
            for (int i = 0; i < correctionIterations; i++)
            {
                Vector3 camPosGuess = focusPoint + toCamDir * desiredDist;
                float groundAtCam = SampleGroundYAtXZ(camPosGuess, out bool ok2);
                if (!ok2) break; // 脚下无地面：保持上一个估计
                // 解方程： camY = focusY + toCamDir.y * d = groundAtCam + heightOverGround
                float dByCamGround = (groundAtCam + heightOverGround - focusPoint.y) / Mathf.Max(0.0001f, toCamDir.y);
                if (dByCamGround < 0f) dByCamGround = minDistance; // 防止极端数值
                desiredDist = Mathf.Max(minDistance, dByCamGround);
            }
        }

        // —— 4) 防穿模（保持最小距离，必要时退近）——
        if (avoidClipping)
        {
            Vector3 castOrigin = focusPoint + Vector3.up * castOriginLift;
            float maxCastDist = desiredDist + castOriginLift;
            if (Physics.SphereCast(castOrigin, clipRadius, toCamDir, out RaycastHit hit, maxCastDist, clipMask, QueryTriggerInteraction.Ignore))
            {
                float hitFromFocus = Mathf.Max(0f, hit.distance - castOriginLift);
                float dClip = Mathf.Max(minDistance, hitFromFocus - clipPadding);
                desiredDist = Mathf.Min(desiredDist, dClip); // 以不穿模为准
            }
        }

        // —— 5) 应用（平滑距离）——
        distCurrent = Mathf.SmoothDamp(distCurrent, desiredDist, ref distVel, distSmooth);
        Vector3 finalPos = focusPoint + toCamDir * distCurrent;
        transform.position = finalPos;
        transform.rotation = rot;
    }

    // 采样“玩家脚下”的地面高度（基于玩家XZ）
    float SampleGroundY(Vector3 origin, out bool ok)
    {
        Vector3 start = new Vector3(origin.x, origin.y + probeStartHeight, origin.z);
        if (Physics.SphereCast(start, probeRadius, Vector3.down, out RaycastHit hit, probeDistanceDown, groundMask, QueryTriggerInteraction.Ignore))
        { ok = true; return hit.point.y; }
        if (Physics.Raycast(start, Vector3.down, out hit, probeDistanceDown, groundMask, QueryTriggerInteraction.Ignore))
        { ok = true; return hit.point.y; }
        ok = false; return 0f;
    }

    // 采样“相机脚下”的地面高度（用相机的XZ）
    float SampleGroundYAtXZ(Vector3 cameraPos, out bool ok)
    {
        Vector3 start = new Vector3(cameraPos.x, cameraPos.y + probeStartHeight, cameraPos.z);
        if (Physics.SphereCast(start, probeRadius, Vector3.down, out RaycastHit hit, probeDistanceDown, groundMask, QueryTriggerInteraction.Ignore))
        { ok = true; return hit.point.y; }
        if (Physics.Raycast(start, Vector3.down, out hit, probeDistanceDown, groundMask, QueryTriggerInteraction.Ignore))
        { ok = true; return hit.point.y; }
        ok = false; return 0f;
    }

    float GetTargetYaw()
    {
        Vector3 f = target.forward; f.y = 0;
        if (f.sqrMagnitude < 0.0001f) f = Vector3.forward;
        return Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
    }
}
