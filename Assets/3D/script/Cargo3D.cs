using System.Collections;
using UnityEngine;

namespace Project.Gameplay3D
{
    [RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Renderer))] // 确保有 Renderer
    public class Cargo3D : MonoBehaviour
    {
        [Header("Identity")]
        public string cargoName = "Red";
        public CargoType3D cargoType = CargoType3D.Common;

        [Header("Layers")]
        public LayerMask groundLayer;

        [Header("Physics Materials")]
        public PhysicsMaterial bouncyMaterial;
        public PhysicsMaterial normalMaterial;

        // --- 1. 新增：视觉数据库 ---
        [Header("Visuals")]
        [Tooltip("用于映射 Type->Color 和 Name->Icon 的数据库")]
        public CargoVisualDatabase visualDatabase;

        [Header("Tuning")]
        public float dropForwardOffset = 1.0f;
        public float dropUpOffset = 0.2f;
        public float dropImpulse = 2.5f;

        [Header("Debug")]
        public CargoState3D state = CargoState3D.Active;
        public bool isGrounded { get; private set; } = false;

        [Header("Collisions")]
        public float ignorePlayerCollisionTime = 0.15f;

        // --- 2. 新增：组件缓存 ---
        Rigidbody rb;
        Collider col;
        Transform originalParent;
        Renderer _renderer;
        MaterialPropertyBlock _propBlock; // 用于高效修改材质

        // --- 3. 新增：Shader 属性 ID ---
        // (这是 URP/Lit Shader 的标准属性名)
        private static readonly int _ColorID = Shader.PropertyToID("_Color");

        [SerializeField]
        private MeshRenderer meshRenderer;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
            originalParent = transform.parent;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            _renderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            if (col.material == null && normalMaterial != null)
            {
                col.material = normalMaterial;
            }

            // 初始化到当前 state（会做注册/反注册）
            SetState(state, null);
        }

        private void OnEnable()
        {
            if (GameManager3D.Instance)
            {
                if (state == CargoState3D.Active ||
                    (state == CargoState3D.Carried && GameManager3D.Instance.includeCarriedInBacklog))
                {
                    GameManager3D.Instance.RegisterActiveCargo(this);
                }
            }
        }

        private void OnDisable()
        {
            // 任何禁用都从活动表移除（防重算漏计）
            if (GameManager3D.Instance)
                GameManager3D.Instance.UnregisterActiveCargo(this);
        }

        /// <summary>唯一的状态切换入口：在此处理注册/反注册</summary>
        public void SetState(CargoState3D newState, Transform carrierMount)
        {
            state = newState;
            switch (state)
            {
                case CargoState3D.Active:
                    transform.SetParent(originalParent);
                    if (col) col.enabled = true;
                    if (rb)
                    {
                        rb.isKinematic = false;
                        rb.useGravity = true;
                    }
                    isGrounded = false;

                    ApplyProperties();

                    // —— 注册为活动货物 ——
                    if (GameManager3D.Instance)
                        GameManager3D.Instance.RegisterActiveCargo(this);
                    break;

                case CargoState3D.Carried:
                    if (carrierMount != null)
                    {
                        transform.SetParent(carrierMount);
                        transform.localPosition = Vector3.zero;
                        transform.localRotation = Quaternion.identity;
                    }
                    if (col) col.enabled = false;
                    if (rb)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    // —— 离开活动表 ——
                    if (GameManager3D.Instance)
                    {
                        if (GameManager3D.Instance.includeCarriedInBacklog)
                            GameManager3D.Instance.RegisterActiveCargo(this);
                        else
                            GameManager3D.Instance.UnregisterActiveCargo(this);
                    }
                    break;

                case CargoState3D.Delivered:
                case CargoState3D.InPool:
                    transform.SetParent(originalParent);
                    if (col) col.enabled = false;
                    if (rb)
                    {
                        rb.isKinematic = true;
                        rb.useGravity = false;
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    gameObject.SetActive(false);

                    // —— 离开活动表 ——
                    if (GameManager3D.Instance)
                        GameManager3D.Instance.UnregisterActiveCargo(this);

                    // 旧逻辑：交付 -1（事件驱动模式下为 no-op，保留兼容）
                    GameManager3D.Instance?.OnBacklogRemove();
                    break;
            }
        }

        /// <summary>按类型/名称应用外观与物理材质，仅设置“纯颜色”</summary>
        void ApplyProperties()
        {
            // 物理材质
            if (col == null) col = GetComponent<Collider>();
            if (cargoType == CargoType3D.Elasticity && bouncyMaterial != null) col.material = bouncyMaterial;
            else if (normalMaterial != null) col.material = normalMaterial;

            if (visualDatabase == null) return;

            // 颜色 = 名称映射（若改成“颜色=类型”，请用 visualDatabase.GetTypeColor(...)）
            Color baseColor = visualDatabase.GetNameColor(cargoName);

            // If you specifically want to target material slot 0 on a MeshRenderer:
            if (meshRenderer != null)
            {
                var mats = meshRenderer.sharedMaterials;
                if (mats != null && mats.Length > 0)
                {
                    var mat0 = mats[0];
                    if (mat0 != null && mat0.HasProperty(_ColorID))
                    {
                        _propBlock.Clear();
                        _propBlock.SetColor(_ColorID, baseColor);
                        // Set the property block on material index 0
                        meshRenderer.SetPropertyBlock(_propBlock, 0);
                    }
                }
            }
            else if (_renderer != null) // fallback if no MeshRenderer
            {
                var mats = _renderer.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    var mat = mats[i];
                    if (mat == null || !mat.HasProperty(_ColorID)) continue;

                    _propBlock.Clear();
                    _propBlock.SetColor(_ColorID, baseColor);
                    _renderer.SetPropertyBlock(_propBlock, i);
                }
            }
        }

        // —— 地面接触仅用于“落地标记”；旧的 +1 计数口保留兼容 ——
        void OnCollisionEnter(Collision collision)
        {
            if (state != CargoState3D.Active || isGrounded) return;
            if ((groundLayer.value & (1 << collision.gameObject.layer)) > 0)
            {
                isGrounded = true;
                if (GameManager3D.Instance != null)
                {
                    GameManager3D.Instance.OnBacklogAdd(); // 事件驱动时 no-op
                }
                else
                {
                    Debug.LogWarning("Cargo3D: GameManager3D.Instance is null, cannot add to backlog.");
                }
            }
        }

        /// <summary>从玩家处丢下到场景：Carried -> Active，并给初速度</summary>
        public void DropFrom(Transform playerRoot, Vector3 forwardDir, Vector3 extraVelocity)
        {
            forwardDir.y = 0f;
            if (forwardDir.sqrMagnitude < 1e-6f) forwardDir = playerRoot ? playerRoot.forward : Vector3.forward;
            forwardDir.Normalize();

            transform.SetParent(originalParent);
            if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
            if (col) col.enabled = false;

            Vector3 dropPos = (playerRoot ? playerRoot.position : transform.position)
                            + forwardDir * dropForwardOffset
                            + Vector3.up * dropUpOffset;
            transform.position = dropPos;
            transform.rotation = Quaternion.identity;
            Physics.SyncTransforms();

            // 进入 Active（会完成注册/属性应用）
            SetState(CargoState3D.Active, null);

            // 临时忽略与玩家的碰撞
            if (playerRoot)
            {
                var playerCols = playerRoot.GetComponentsInChildren<Collider>(includeInactive: false);
                var cargoCols = GetComponentsInChildren<Collider>(includeInactive: false);
                StartCoroutine(IgnorePairs(playerCols, cargoCols, ignorePlayerCollisionTime));
            }

            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(forwardDir * dropImpulse + extraVelocity, ForceMode.VelocityChange);
            }
        }

        private IEnumerator IgnorePairs(Collider[] a, Collider[] b, float t)
        {
            foreach (var ca in a) foreach (var cb in b)
                    if (ca && cb) Physics.IgnoreCollision(cb, ca, true);
            yield return new WaitForSeconds(t);
            foreach (var ca in a) foreach (var cb in b)
                    if (ca && cb) Physics.IgnoreCollision(cb, ca, false);
        }
    }
}