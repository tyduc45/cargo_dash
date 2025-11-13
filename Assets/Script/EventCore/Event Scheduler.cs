using System.Collections.Generic;
using UnityEngine;

public class EventScheduler : MonoBehaviour
{
    [Header("Core")]
    public EventPool eventPool;
    public TimelineGenerator generator;
    public ObjectSpawnerWithPool spawner; // 仍保留给 SpawnCargo 用

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
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Playing) return;
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
        // === 新增：道具生成 ===
        if (te.eventName == "SpawnPickup")
        {
            // 从时间线载荷中读取 key（例如 pickup_speed / pickup_strong）
            var payload = JsonUtility.FromJson<TimelineGenerator.PickupSpawnPayload>(te.payloadJson ?? "{}");
            if (payload == null || string.IsNullOrEmpty(payload.key))
            {
                Debug.LogWarning("[EventScheduler] SpawnPickup payload invalid.");
                return;
            }

            // 选一个出生位置（固定点优先，其次区域内随机）
            Vector3 pos = GetPickupSpawnPosition();

            // 走独立对象池生成
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
            var payload = JsonUtility.FromJson<TimelineGenerator.CargoSpawnPayload>(te.payloadJson ?? "{}");
            if (spawner != null && generator != null && generator.config != null)
            {
                spawner.SpawnFromTimeline(payload, generator.config.appearancePool);
            }
            else
            {
                Debug.LogWarning("EventScheduler: spawner or config not assigned.");
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

    // ---------------------------
    // 位置选择：固定点优先 → 区域随机
    // 如果区域物体上挂了 PickupArea2D，则优先用它的 TrySamplePoint（带贴地/避障）
    // ---------------------------
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

