using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "Level/Config")]
public class LevelConfig : ScriptableObject
{
    [Header("Deterministic Random")]
    public int randomSeed = 12345;

    [Header("Timeline")]
    [Min(1f)] public float levelDuration = 60f;                // 关卡总时长
    [Header("Cargo Spawn Interval (seconds)")]
    [Min(0.1f)] public float spawnIntervalMin = 1.2f;

    [Min(0.1f)] public float spawnIntervalMax = 2.5f;

    [Header("First Cargo Spawn Window (seconds)")]
    [Range(0f, 10f)] public float firstSpawnMin = 1f;
    [Range(0f, 10f)] public float firstSpawnMax = 3f;

    [Header("Cargo Pool")]
    public List<WeightedPrefab> appearancePool;
    public List<WeightedCargoType> typePool;
    public List<WeightedName> namePool;

    [Header("Event Pool Control")]
    [Tooltip("可触发的事件（权重在 GameEvent 上设置）")]
    public List<GameEvent> allowedEvents;

    [Tooltip("两次事件之间的全局最小间隔（秒）")]
    public float minEventGap = 6f;

    [Tooltip("关卡开始多久后才允许第一次事件（秒）")]
    public float eventStartDelay = 3f;

    public int scoreToNextLevel = 0;

    // ================== 事件冷却配置（每关可覆写）==================
    [System.Serializable]
    public class EventCooldown
    {
        [Tooltip("要设置冷却的事件资源")]
        public GameEvent gameEvent;

        [Tooltip("基础冷却时间（秒）")]
        [Min(0)] public int baseSeconds = 0;

        [Tooltip("额外缓冲百分比（例如 0.3 表示 +30%，向下取整到秒）")]
        [Range(0f, 1f)] public float extraPercent = 0.3f;

        // 计算**有效冷却**（基础CD + 向下取整的额外缓冲）
        public int EffectiveSeconds()
        {
            return baseSeconds + Mathf.FloorToInt(baseSeconds * extraPercent);
        }
    }

    [Header("Per-Event Cooldowns (可选；不在此表中的事件视为CD=0)")]
    public List<EventCooldown> eventCooldowns = new List<EventCooldown>();

    [Header("Pickup Pool Control")]
    [Tooltip("本关可生成的道具（key需与对象池一致），以及权重")]
    public List<WeightedPickupKey> allowedPickups = new List<WeightedPickupKey>();

    [Tooltip("两次道具生成的全局最小间隔（秒）")]
    public float minPickupGap = 8f;

    [Tooltip("关卡开始多久后才允许第一次道具（秒）")]
    public float pickupStartDelay = 5f;

    [System.Serializable]
    public class PickupCooldown
    {
        [Tooltip("对象池中的key，例如 pickup_speed / pickup_strong")]
        public string key;

        [Min(0)] public int baseSeconds = 0;

        [Range(0f, 1f)] public float extraPercent = 0.3f;

        public int EffectiveSeconds()
        {
            return baseSeconds + Mathf.FloorToInt(baseSeconds * extraPercent);
        }
    }

    [Header("Per-Pickup Cooldowns (可选；不在此表中的视为CD=0)")]
    public List<PickupCooldown> pickupCooldowns = new List<PickupCooldown>();
}

