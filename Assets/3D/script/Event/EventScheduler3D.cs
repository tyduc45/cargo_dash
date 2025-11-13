using System.Collections.Generic;
using UnityEngine;
using Project.Gameplay3D; // <-- 1. 添加 3D 命名空间

public class EventScheduler3D : MonoBehaviour
{
    [Header("Core")]
    public EventPool eventPool;
    public TimelineGenerator3D generator;

    // 2. 将 spawner 变量类型改为 Spawner3D
    public Spawner3D spawner;

    [Header("Pickup Spawn Locations (场景级引用)")]
    [Tooltip("固定出生点（优先使用）")]
    public Transform[] pickupPoints;

    [Tooltip("可作为生成区域的 Collider2D（Box/Circle/Polygon/Composite 均可）")]
    public Collider2D[] pickupAreas;

    private List<TimedEvent> eventList = new List<TimedEvent>();
    private float timer = 0f;
    private int currentIndex = 0;

    void Start()
    {
        timer = 0f; currentIndex = 0; eventList.Clear();

        if (generator && generator.config)
        {
            generator.Generate();
            eventList = generator.Generated;
        }
        else
        {
            Debug.LogWarning("EventScheduler: no generator or config provided.");
        }
    }

    public void Restart()
    {
        timer = 0f;
        currentIndex = 0;

        var dbg = FindObjectOfType<RestartDebug>();
        dbg?.LogStates("Inside EventScheduler.Restart (before Clear)");

        eventList.Clear();

        if (generator && generator.config)
        {
            generator.Generate();
            eventList = generator.Generated;
        }
        else
        {
            Debug.LogWarning("EventScheduler: no generator or config provided.");
        }
        Debug.Log("[EventScheduler] Restarted.");
        
    }

    void Update()
    {
        if (GameManager3D.Instance == null || GameManager3D.Instance.currentState != GameState.Playing) return;
        if (currentIndex >= eventList.Count) return;
        timer += Time.deltaTime;

        while (currentIndex < eventList.Count && timer >= eventList[currentIndex].timestamp)
        {
            Dispatch(eventList[currentIndex]);
            currentIndex++;
        }
    }

    void Dispatch(TimedEvent te)
    {
        Debug.Log($"te.eventName == spawnCargo ? {te.eventName} ,{te.eventName == "SpawnCargo"}");
        // === 新增：道具生成 ===
        if (te.eventName == "SpawnPickup")
        {
            // ... (此部分逻辑不变) ...
            var payload = JsonUtility.FromJson<TimelineGenerator.PickupSpawnPayload>(te.payloadJson ?? "{}");
            if (payload == null || string.IsNullOrEmpty(payload.key))
            {
                Debug.LogWarning("[EventScheduler] SpawnPickup payload invalid.");
                return;
            }
            Vector3 pos = GetPickupSpawnPosition();
            if (PoolManager.Instance != null)
            {
                var go = PoolManager.Instance.Spawn(payload.key, pos, Quaternion.identity);
                if (go == null)
                    Debug.LogWarning($"[EventScheduler] PoolManager spawn failed for key '{payload.key}'.");
            }
            else
            {
                Debug.LogWarning("[EventScheduler] PoolManager.Instance not found. Please add PoolManager to the scene.");
            }
            return;
        }

        // === 既有：货物生成 ===
        if (te.eventName == "SpawnCargo")
        {
            Debug.Log("It's working!!!!!");
            // 3. 解析 TimelineGenerator3D.cs 中定义的 payload 结构
            var timelinePayload = JsonUtility.FromJson<TimelineGenerator3D.CargoSpawnPayload3D>(te.payloadJson ?? "{}");

            if (spawner != null)
            {
                // 4. 创建 Spawner3D.cs (File 3) 所期望的 payload 结构
                var spawnerPayload = new Project.Gameplay3D.CargoSpawnPayload3D
                {
                    cargoType = timelinePayload.cargoType,
                    cargoName = timelinePayload.cargoName
                    // 注意：prefabName, overridePos 等字段保留默认(null)
                    // Spawner3D (File 3) 内部会处理这种情况（当 prefabName 为空时，它会调用 WeightedPick）
                };

                // 5. 调用 Spawner3D 的单参数方法
                spawner.SpawnFromTimeline(spawnerPayload);
            }
            else
            {
                Debug.LogWarning("EventScheduler: spawner (Spawner3D) not assigned.");
            }
            return;
        }

        // === 既有：普通事件 ===
        if (eventPool)
        {
            if (!eventPool.Trigger(te.eventName))
                Debug.LogWarning($"[EventScheduler] Event not found: {te.eventName}");
        }
    }

    // ... (GetPickupSpawnPosition 方法不变) ...
    Vector3 GetPickupSpawnPosition()
    {
        // 1) 固定点
        if (pickupPoints != null && pickupPoints.Length > 0)
        {
            var t = pickupPoints[Random.Range(0, pickupPoints.Length)];
            if (t) return t.position;
        }

        // 2) 区域随机
        if (pickupAreas != null && pickupAreas.Length > 0)
        {
            for (int tries = 0; tries < 40; tries++)
            {
                var col = pickupAreas[Random.Range(0, pickupAreas.Length)];
                if (!col) continue;

                // 若区域上挂了 PickupArea2D，优先用它（支持贴地/避障）
                var helper = col.GetComponent<PickupArea2D>();
                if (helper != null && helper.TrySamplePoint(out var p1))
                    return p1;

                // 退回：在 Bounds 内拒绝采样直到 OverlapPoint 成功
                var b = col.bounds;
                var p = new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y));
                if (col.OverlapPoint(p)) return p;
            }
        }

        // 3) 兜底：原点
        return Vector3.zero;
    }
}