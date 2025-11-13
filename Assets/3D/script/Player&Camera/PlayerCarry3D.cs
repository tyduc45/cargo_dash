using System.Collections.Generic;
using UnityEngine;

namespace Project.Gameplay3D
{
    public class PlayerCarry3D : MonoBehaviour
    {
        [Header("Mount / Stack")]
        public Transform stackRoot;
        public int capacity = 3;
        public Vector3 stackLocalOffset = new Vector3(0, 0.25f, 0);

        [Header("Interact Radius / Layers")]
        public float pickupRadius = 1.2f;     // 拾取最大半径（先做粗筛）
        public float receiverRadius = 1.6f;   // 提交仍然用圆形范围
        public LayerMask cargoMask;
        public LayerMask receiverMask;

        [Header("Facing / Aim")]
        public Transform aimIndicator;        // ✅ 指示器子物体（需要带 Renderer）
        public float coneHalfAngleDeg = 45f;  // ✅ 只能拾取正前方±45°
        public float minSpeedForFacing = 0.2f;// 速度阈值（低于此不更新朝向）
        public float facingSlerp = 14f;       // 朝向平滑度

        [Header("Indicator Placement")]
        public float indicatorForward = 0.6f;     // 指示器离玩家前方的距离
        public float indicatorUpFromHead = -0.25f; // 相对于“头顶”的额外上下偏移(负值=略低于头顶)

        [Header("Carry Mount (on head)")]
        public float headCarryOffset = 0.15f;   // 头顶再抬一点，避免穿模
        public float stackStepY = 0.35f;        // 每件货物往上叠的高度

        [Header("Indicator Color")]
        public Color indicatorNoTarget = new Color(1f, 0.25f, 0.25f, 1f);  // 红
        public Color indicatorHasTarget = new Color(0.25f, 0.6f, 1f, 1f);  // 蓝

        [Header("Drop / Throw")]
        public bool enableThrow = false;
        public float throwVelocity = 6f;

        [Header("Keys")]
        public KeyCode keyPick = KeyCode.J;
        public KeyCode keyPickAlt = KeyCode.Z;     // ✅ 新增：Z

        public KeyCode keyDeliver = KeyCode.K;
        public KeyCode keyDeliverAlt = KeyCode.X;  // ✅ 新增：X

        public KeyCode keyDrop = KeyCode.L;
        public KeyCode keyDropAlt = KeyCode.C;     // ✅ 新增：C

        readonly List<Cargo3D> stack = new List<Cargo3D>();
        CharacterController cc;

        bool PickPressed() => Input.GetKeyDown(keyPick) || Input.GetKeyDown(keyPickAlt);
        bool DeliverPressed() => Input.GetKeyDown(keyDeliver) || Input.GetKeyDown(keyDeliverAlt);
        bool DropPressed() => Input.GetKeyDown(keyDrop) || Input.GetKeyDown(keyDropAlt);

        // 朝向缓存
        Vector3 facing = Vector3.forward;     // 世界平面方向（y=0）
        Vector3 lastPos;
        bool inited;

        // 指示器渲染
        Renderer aimRenderer;
        MaterialPropertyBlock _mpb;

        [SerializeField]
        private AnimatorHolder animatorHolder;

        private Animator animator;

        void Awake()
        {
            if (!stackRoot)
            {
                var mount = new GameObject("StackRoot").transform;
                mount.SetParent(transform);
                mount.localPosition = new Vector3(0, 1.6f, 0);
                stackRoot = mount;
            }
            cc = GetComponent<CharacterController>();
            animator = animatorHolder.animator;
            // 指示器渲染器
            if (aimIndicator) aimIndicator.TryGetComponent(out aimRenderer);
            _mpb = new MaterialPropertyBlock();

            
        }

        void Start()
        {
            PlaceStackRootOnHead();
            facing = FlattenDir(transform.forward);
            lastPos = transform.position;
            inited = true;

            // 初始化指示器颜色为红色
            SetIndicatorColor(indicatorNoTarget);
        }

        void Update()
        {
            PlaceStackRootOnHead();
            UpdateFacingFromMovement();
            UpdateAimIndicator();

            // 根据能否拾取切换指示器颜色
            UpdateIndicatorColor();

            if (PickPressed()) TryPickup();
            if (DropPressed()) TryDrop();
            if (DeliverPressed()) TryDeliver();

            UpdateStackVisual();
        }

        /* ================= Facing / Aim ================= */

        void UpdateFacingFromMovement()
        {
            if (!inited) return;

            Vector3 vel = cc ? cc.velocity : (transform.position - lastPos) / Mathf.Max(0.0001f, Time.deltaTime);
            lastPos = transform.position;

            Vector3 planar = FlattenDir(vel);
            float speed = planar.magnitude;

            if (speed >= minSpeedForFacing)
            {
                Vector3 target = planar.normalized;
                facing = Vector3.Slerp(facing, target, 1f - Mathf.Exp(-facingSlerp * Time.deltaTime));
                facing = FlattenDir(facing).normalized;
            }
        }

        void UpdateAimIndicator()
        {
            if (!aimIndicator) return;
            if (facing.sqrMagnitude < 1e-6f) return;

            // 计算“头顶”在本地坐标的Y值（CharacterController更可靠）
            float headLocalY = HeadLocalY();

            // 基于头顶位置，在"当前朝向"前方 indicatorForward 米处放指示器
            Vector3 baseWorld = transform.position + Vector3.up * (headLocalY + indicatorUpFromHead);
            aimIndicator.position = baseWorld + facing.normalized * indicatorForward;
            aimIndicator.rotation = Quaternion.LookRotation(facing, Vector3.up);
        }

        void PlaceStackRootOnHead()
        {
            //if (!stackRoot) return;
            //float headY = HeadLocalY(); // 上面已实现
            //stackRoot.localPosition = new Vector3(0f, headY + headCarryOffset, 0f);
        }

        float HeadLocalY()
        {
            // 取 CC 的中心+半高 ≈ 头顶（不依赖物体原点）
            return cc ? (cc.center.y + cc.height * 0.5f) : 1.6f;
        }

        void UpdateCarryAnimation()
        {
            if(stack.Count > 0)
            {
                animator.SetBool("isCarrying", true);
            }
            else
            {
                animator.SetBool("isCarrying", false);
            }
        }

        Vector3 FlattenDir(Vector3 v) => new Vector3(v.x, 0f, v.z);

        /* ================= Indicator Color ================= */

        void UpdateIndicatorColor()
        {
            if (!aimRenderer) return;

            bool canPick = stack.Count < capacity && HasPickableInCone();
            SetIndicatorColor(canPick ? indicatorHasTarget : indicatorNoTarget);
        }

        void SetIndicatorColor(Color c)
        {
            // 优先设置 URP 的 _BaseColor；同时设置 _Color 以兼容旧着色器
            aimRenderer.GetPropertyBlock(_mpb);
            _mpb.SetColor("_BaseColor", c);
            _mpb.SetColor("_Color", c);
            aimRenderer.SetPropertyBlock(_mpb);
        }

        /* ================= Stack Visual ================= */

        void UpdateStackVisual()
        {
            for (int i = 0; i < stack.Count; i++)
            {
                var c = stack[i];
                if (!c) continue;
                var t = c.transform;

                if (t.parent != stackRoot) t.SetParent(stackRoot, false);

                // 仅向上叠，防止插进身体
                t.localPosition = new Vector3(0f, i * stackStepY, 0f);
                t.localRotation = Quaternion.identity;
            }
        }

        /* ================= Public Stack API ================= */

        public int Count => stack.Count;
        public Cargo3D Peek() => stack.Count > 0 ? stack[^1] : null;
        public Cargo3D Pop()
        {
            if (stack.Count == 0) return null;
            var last = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            return last;
        }

        /* ================= Interactions ================= */

        public void TryPickup()
        {
            if (stack.Count >= capacity) return;

            Cargo3D target = FindBestCargoInCone();
            if (!target) return;
            if (target.state != CargoState3D.Active) return;

            SoundManager.Instance?.PlaySound(SoundType.Pickup);

            // ⬇️ 直接挂到 stackRoot，不再 new ItemMount
            target.SetState(CargoState3D.Carried, stackRoot);
            stack.Add(target);
            UpdateCarryAnimation();
        }

        public void TryDrop()
        {
            var top = Pop();
            if (!top) return;

            Vector3 dir = facing.sqrMagnitude > 1e-6f ? facing : FlattenDir(transform.forward).normalized;

            Vector3 extraVel = Vector3.zero;
            if (enableThrow) extraVel = dir * throwVelocity;
            else if (cc != null) extraVel = FlattenDir(cc.velocity);

            top.DropFrom(transform, dir, extraVel); // 面向方向丢出（上次已加的重载）
            UpdateCarryAnimation();
            SoundManager.Instance?.PlaySound(SoundType.Throw);
        }

        void TryDeliver()
        {
            if (stack.Count == 0) return;
            var receiver = FindNearestReceiver();
            if (!receiver) return;

            int n = receiver.DeliverFrom(this);
            if (n > 0)
            {
                // 可加音效/飘字
                SoundManager.Instance?.PlaySound(SoundType.Interact);
                UpdateCarryAnimation();
            }
        }

        /* ================= Find helpers ================= */

        // 仅判断“是否存在可拾取物体在锥形范围”
        bool HasPickableInCone()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, cargoMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return false;

            Vector3 fwd = (facing.sqrMagnitude > 1e-6f ? facing : FlattenDir(transform.forward)).normalized;
            float cosHalf = Mathf.Cos(coneHalfAngleDeg * Mathf.Deg2Rad);

            foreach (var h in hits)
            {
                var c = h.GetComponentInParent<Cargo3D>();
                if (!c || c.state != CargoState3D.Active) continue;

                Vector3 to = c.transform.position - transform.position;
                Vector3 toPlanar = FlattenDir(to);
                float mag = toPlanar.magnitude;
                if (mag < 1e-4f) continue;

                float dot = Vector3.Dot(toPlanar / mag, fwd);
                if (dot >= cosHalf) return true; // 命中任意一个即可
            }
            return false;
        }

        // 锥内选择：优先距离更近；同距时 Y 更高
        Cargo3D FindBestCargoInCone()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius, cargoMask, QueryTriggerInteraction.Ignore);
            if (hits == null || hits.Length == 0) return null;

            Vector3 fwd = (facing.sqrMagnitude > 1e-6f ? facing : FlattenDir(transform.forward)).normalized;
            float cosHalf = Mathf.Cos(coneHalfAngleDeg * Mathf.Deg2Rad);

            Cargo3D best = null;
            float bestDistSq = float.MaxValue;
            float bestY = -float.MaxValue;

            foreach (var h in hits)
            {
                var c = h.GetComponentInParent<Cargo3D>();
                if (!c || c.state != CargoState3D.Active) continue;

                Vector3 to = c.transform.position - transform.position;

                Vector3 toPlanar = FlattenDir(to);
                float mag = toPlanar.magnitude;
                if (mag < 1e-4f) continue;

                float dot = Vector3.Dot(toPlanar / mag, fwd);
                if (dot < Mathf.Cos(coneHalfAngleDeg * Mathf.Deg2Rad)) continue;

                float distSq = to.sqrMagnitude;

                const float distEpsilon = 0.01f; // ~10cm^2
                if (distSq + distEpsilon < bestDistSq)
                {
                    best = c; bestDistSq = distSq; bestY = c.transform.position.y;
                }
                else if (Mathf.Abs(distSq - bestDistSq) <= distEpsilon)
                {
                    float y = c.transform.position.y;
                    if (y > bestY) { best = c; bestY = y; }
                }
            }

            return best;
        }

        DeliveryReceiver3D FindNearestReceiver()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, receiverRadius, receiverMask, QueryTriggerInteraction.Ignore);
            float best = float.MaxValue;
            DeliveryReceiver3D bestR = null;
            foreach (var h in hits)
            {
                var r = h.GetComponentInParent<DeliveryReceiver3D>();
                if (!r) continue;
                float d = (r.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; bestR = r; }
            }
            return bestR;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // 拾取圆形范围
            Gizmos.color = new Color(0, 1, 1, 0.6f);
            Gizmos.DrawWireSphere(transform.position, pickupRadius);

            // 提交圆形范围
            Gizmos.color = new Color(1, 1, 0, 0.6f);
            Gizmos.DrawWireSphere(transform.position, receiverRadius);

            // 扇形可视：仅辅助（你不需要画的话可以忽略）
            Vector3 fwd = Application.isPlaying ? facing : FlattenDir(transform.forward).normalized;
            if (fwd.sqrMagnitude > 1e-6f)
            {
                float r = pickupRadius;
                Vector3 left = Quaternion.Euler(0, -coneHalfAngleDeg, 0) * fwd;
                Vector3 right = Quaternion.Euler(0, +coneHalfAngleDeg, 0) * fwd;
                Vector3 pos = transform.position;
                Gizmos.color = new Color(1, 0.5f, 0, 0.8f);
                Gizmos.DrawLine(pos, pos + left * r);
                Gizmos.DrawLine(pos, pos + right * r);
            }
        }
#endif
    }
}
