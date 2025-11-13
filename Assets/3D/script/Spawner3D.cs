using System.Collections.Generic;
using UnityEngine;

namespace Project.Gameplay3D
{

    /// Timeline/关卡逻辑传入的生成荷载（接口可扩展）
    public struct CargoSpawnPayload3D
    {
        public CargoType3D cargoType;          // 业务字段（必选）
        public string cargoName;               // 业务字段（必选）
        public Vector3? overridePos;           // 覆盖精确位置
        public float? overrideYawDeg;          // 覆盖偏转角
        public float? overridePitchDeg;        // 覆盖
        public float? overrideSpeed;           // 速度
    }

    /// 沿墙面法线方向（Z+）喷射物体
    public class Spawner3D : MonoBehaviour
    {
        [Header("Template Prefab (Main)")]
        [Tooltip("【重要】关卡Timeline和调试生成时使用的【唯一模板】。该Prefab应挂载Cargo3D脚本和Shader材质。")]
        public GameObject cargoTemplatePrefab; // <--- 唯一的 Prefab 模板

        [Header("Pool")]
        public int prewarmPerType = 4;
        public int expandStep = 2;
        public Transform poolParent;

        // --- 1. 核心修改：池从 Dictionary 改为 List ---
        // 内部池：现在只为 cargoTemplatePrefab 服务
        readonly List<GameObject> cargoPool = new List<GameObject>();
        bool isPoolInitialized = false; // 标志位，防止多次预热



        [Header("Spawn Point Override")]
        [Tooltip("勾选后使用指定的Transform作为生成点，忽略随机区域")]
        public bool useSpawnPoint = false;
        [Tooltip("生成点Transform（当useSpawnPoint为true时使用）")]
        public Transform spawnPoint;

        public GameObject spawnerVisual;

        // ... (Header: Direction, Spawn Area, Spray, Collision, Events 不变 ...)
        [Header("Direction")]
        [Tooltip("勾选后使用本地 Z- 作为喷射方向")]
        public bool useNegativeZ = false;
        [Header("Spawn Area (local plane)")]
        public Vector2 areaSize = new Vector2(2.0f, 0.4f);
        public float areaPadding = 0.05f;
        public float forwardOffset = 0.05f;
        [Header("Spray (random angle & speed)")]
        [Tooltip("左右偏航(度)")]
        public float yawJitterDeg = 25f;
        [Tooltip("仰角范围(度)")]
        public Vector2 pitchDegRange = new Vector2(8f, 18f);
        [Tooltip("喷射速度范围(米/秒)")]
        public Vector2 speedRange = new Vector2(6f, 10f);
        [Tooltip("随机旋转扭矩")]
        public Vector2 torqueRange = new Vector2(-3f, 3f);
        [Header("Collision")]
        public float ignoreSelfCollisionTime = 0.15f;
        public LayerMask groundMask = ~0;
        [Header("Events (optional)")]
        public System.Action<Cargo3D> onSpawned;


        /* ================== Public API ================== */

        // 关卡/Timeline的核心调用
        public Cargo3D SpawnFromTimeline(CargoSpawnPayload3D payload)
        {
            // 永远使用唯一的模板
            GameObject prefab = cargoTemplatePrefab;

            if (!prefab)
            {
                Debug.LogError("[Spawner3D] 'Cargo Template Prefab' is not assigned in Inspector!", this);
                return null;
            }

            Vector3 pos = payload.overridePos ?? RandomPointOnArea();
            float yaw = payload.overrideYawDeg ?? Random.Range(-yawJitterDeg, yawJitterDeg);
            float pitch = payload.overridePitchDeg ?? Random.Range(pitchDegRange.x, pitchDegRange.y);
            float speed = payload.overrideSpeed ?? Random.Range(speedRange.x, speedRange.y);

            // SpawnInternal 会负责把 payload.cargoType 和 payload.cargoName 传递给 Cargo3D 脚本
            return SpawnInternal(prefab, pos, yaw, pitch, speed, payload.cargoType, payload.cargoName);
        }

        /* ================== Core ================== */

        Cargo3D SpawnInternal(GameObject prefab, Vector3 worldPos, float yawDeg, float pitchDeg, float speed, CargoType3D type, string nameStr)
        {
            if (!prefab) return null;

            // --- 2. 核心修改：从 cargoPool 获取 ---
            var go = GetFromPool(); // 不再需要传递 prefab
            if (!go)
            {
                // 如果 GetFromPool 失败 (通常因为 prefab 未设置)，它会返回 null
                Debug.LogError("[Spawner3D] GetFromPool failed, likely 'Cargo Template Prefab' is not set.", this);
                return null;
            }

            // ... (位置、旋转设置不变) ...
            //Quaternion rot = Quaternion.AngleAxis(yawDeg, transform.up) * Quaternion.AngleAxis(-pitchDeg, transform.right);
            //Vector3 dir = (rot * transform.forward).normalized;
            Vector3 baseForward = useNegativeZ ? -transform.forward : transform.forward;
            Quaternion rot = Quaternion.AngleAxis(yawDeg, transform.up) * Quaternion.AngleAxis(-pitchDeg, transform.right);
            Vector3 dir = (rot * baseForward).normalized;

            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            go.SetActive(true);

       
            TweenUtils.ScaleYTemporary(spawnerVisual, 0.1f, 0.4f, 0);

            SoundManager.Instance?.PlaySound(SoundType.CargoSpawn);


            // 业务字段：【关键】在这里设置数据
            var cg = go.GetComponent<Cargo3D>();
            if (cg)
            {
                cg.cargoType = type;   // 传递 Type
                cg.cargoName = nameStr; // 传递 Name
                cg.SetState(CargoState3D.Active, null); // 激活，Cargo3D 将在此方法内自我配置
            }

            // ... (物理、碰撞忽略不变) ...
            var rb = go.GetComponent<Rigidbody>();
            var col = go.GetComponent<Collider>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.AddForce(dir * speed, ForceMode.VelocityChange);

                if (torqueRange != Vector2.zero)
                {
                    Vector3 torque = new Vector3(
                        Random.Range(torqueRange.x, torqueRange.y),
                        Random.Range(torqueRange.x, torqueRange.y),
                        Random.Range(torqueRange.x, torqueRange.y));
                    rb.AddTorque(torque, ForceMode.VelocityChange);
                }
            }

            var selfCols = GetComponentsInChildren<Collider>(includeInactive: true);
            var cargoCols = go.GetComponentsInChildren<Collider>(includeInactive: false);
            StartCoroutine(IgnorePairs(selfCols, cargoCols, ignoreSelfCollisionTime));

            onSpawned?.Invoke(cg);
            return cg;
        }

        Vector3 RandomPointOnArea()
        {
            // 如果启用了生成点覆盖，返回该点位置
            if (useSpawnPoint && spawnPoint != null)
            {
                return spawnPoint.position;
            }

            float w = Mathf.Max(0f, areaSize.x - 2f * areaPadding);
            float h = Mathf.Max(0f, areaSize.y - 2f * areaPadding);
            Vector2 uv = new Vector2(Random.Range(-0.5f, 0.5f) * w, Random.Range(-0.5f, 0.5f) * h);

            Vector3 local = new Vector3(uv.x, uv.y, forwardOffset);
            return transform.TransformPoint(local);
        }

        /* ================== Pool ================== */

        // --- 3. 核心修改：GetFromPool 只操作 cargoPool ---
        GameObject GetFromPool()
        {
            // 安全检查：确保模板已设置
            if (cargoTemplatePrefab == null) return null;

            // 首次调用时，进行预热
            if (!isPoolInitialized)
            {
                Prewarm(prewarmPerType);
                isPoolInitialized = true;
            }

            // 1. 查找可用实例
            foreach (var go in cargoPool)
            {
                if (go && !go.activeSelf)
                {
                    ResetPooled(go);
                    return go;
                }
            }

            // 2. 池已用尽，动态扩容
            int expandCount = expandStep > 0 ? expandStep : 1; // 保证至少扩容1个
            Prewarm(expandCount);

            // 3. 返回新创建的实例 (它在列表末尾)
            var newGo = cargoPool[cargoPool.Count - 1];
            ResetPooled(newGo);
            return newGo;
        }

        // --- 4. 核心修改：Prewarm 只操作 cargoPool ---
        void Prewarm(int count)
        {
            // 安全检查
            if (cargoTemplatePrefab == null) return;

            for (int i = 0; i < count; i++)
            {
                var obj = Instantiate(cargoTemplatePrefab, transform.position, Quaternion.identity);
                obj.SetActive(false);
                if (poolParent) obj.transform.SetParent(poolParent, true);
                cargoPool.Add(obj); // 添加到唯一的池中
            }
        }

        //void ResetPooled(GameObject go)
        //{
        //    var rb = go.GetComponent<Rigidbody>();
        //    if (rb) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.useGravity = true; rb.isKinematic = false; }
        //    var col = go.GetComponent<Collider>();
        //    if (col) col.enabled = true;
        //}
        void ResetPooled(GameObject go)
        {
            // 1. 恢复 Rigidbody
            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = true;
                rb.isKinematic = false;
            }

            // 2. 恢复 Collider
            var col = go.GetComponent<Collider>();
            if (col) col.enabled = true;

            // 3. 恢复 Transform 缩放与朝向
            go.transform.localScale = Vector3.one;
            go.transform.rotation = Quaternion.identity;

            // 4. 确保渲染器重新可见
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }

        // --- 5. 核心修改：Base() 方法已删除，不再需要 ---

        System.Collections.IEnumerator IgnorePairs(Collider[] a, Collider[] b, float t)
        {
            foreach (var ca in a) foreach (var cb in b) if (ca && cb) Physics.IgnoreCollision(cb, ca, true);
            yield return new WaitForSeconds(t);
            foreach (var ca in a) foreach (var cb in b) if (ca && cb) Physics.IgnoreCollision(cb, ca, false);
        }

        /* ================== Gizmos ================== */
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0.6f, 0.1f, 0.8f);
            Matrix4x4 m = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Vector3 p0 = new(-areaSize.x * 0.5f, -areaSize.y * 0.5f, forwardOffset);
            Vector3 p1 = new(areaSize.x * 0.5f, -areaSize.y * 0.5f, forwardOffset);
            Vector3 p2 = new(areaSize.x * 0.5f, areaSize.y * 0.5f, forwardOffset);
            Vector3 p3 = new(-areaSize.x * 0.5f, areaSize.y * 0.5f, forwardOffset);
            Gizmos.DrawLine(p0, p1); Gizmos.DrawLine(p1, p2); Gizmos.DrawLine(p2, p3); Gizmos.DrawLine(p3, p0);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * (forwardOffset + 0.4f));
            Gizmos.matrix = m;
        }
    }
}