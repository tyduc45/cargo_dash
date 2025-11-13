using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Project.Gameplay3D; // <-- 1. 添加 3D 命名空间

public class TimelineGenerator3D : MonoBehaviour
{
    public LevelConfig3D config;
    public List<TimedEvent> Generated = new List<TimedEvent>();
    public TimedEvent EndEvent;


    public void Generate()
    {
        Generated.Clear();
        if (!config)
        {
            Debug.LogError("TimelineGenerator: missing LevelConfig");
            return;
        }

        // ... (种子和冷却逻辑不变) ...
        int seed = (config.randomSeed != 0)
            ? config.randomSeed
            : (System.Environment.TickCount ^ (int)System.DateTime.UtcNow.Ticks);

        Random.InitState(seed);
        Debug.Log($"[TimelineGenerator] Using seed = {seed} (config.randomSeed={config.randomSeed})");

        var baseCdByName = new Dictionary<string, int>();
        var extraPctByName = new Dictionary<string, float>();
        if (config.allowedEvents != null)
        {
            foreach (var ge in config.allowedEvents)
            {
                if (!ge) continue;
                int baseCd = 0;
                float extraPct = 0.3f;

                if (config.eventCooldowns != null)
                {
                    var cd = config.eventCooldowns.FirstOrDefault(x => x.gameEvent == ge);
                    if (cd != null)
                    {
                        baseCd = Mathf.Max(0, cd.baseSeconds);
                        extraPct = Mathf.Clamp01(cd.extraPercent);
                    }
                }

                baseCdByName[ge.eventName] = baseCd;
                extraPctByName[ge.eventName] = extraPct;
            }
        }

        var nextReadyAt = new Dictionary<string, float>();
        if (config.allowedEvents != null)
        {
            foreach (var ge in config.allowedEvents)
            {
                if (!ge) continue;
                nextReadyAt[ge.eventName] = config.eventStartDelay;
            }
        }

        float nextEventAllowedAt = config.eventStartDelay;

        var pickupBaseCd = new Dictionary<string, int>();
        var pickupExtraPct = new Dictionary<string, float>();

        if (config.allowedPickups != null)
        {
            foreach (var pk in config.allowedPickups)
            {
                if (pk == null || string.IsNullOrEmpty(pk.key)) continue;
                int baseCd = 0; float extraPct = 0.3f;

                if (config.pickupCooldowns != null)
                {
                    var cd = config.pickupCooldowns.Find(x => x.key == pk.key);
                    if (cd != null) { baseCd = Mathf.Max(0, cd.baseSeconds); extraPct = Mathf.Clamp01(cd.extraPercent); }
                }

                pickupBaseCd[pk.key] = baseCd;
                pickupExtraPct[pk.key] = extraPct;
            }
        }

        var nextPickupReadyAt = new Dictionary<string, float>();
        if (config.allowedPickups != null)
        {
            foreach (var pk in config.allowedPickups)
            {
                if (pk == null || string.IsNullOrEmpty(pk.key)) continue;
                nextPickupReadyAt[pk.key] = config.pickupStartDelay;
            }
        }

        float nextPickupAllowedAt = config.pickupStartDelay;

        bool firstSpawnDone = false;
        float t = 0f;

        while (t < config.levelDuration)
        {
            float dt;

            if (!firstSpawnDone)
            {
                float firstMin = Mathf.Min(config.firstSpawnMin, config.firstSpawnMax);
                float firstMax = Mathf.Max(config.firstSpawnMin, config.firstSpawnMax);
                dt = Random.Range(firstMin, firstMax);

                firstSpawnDone = true;
            }
            else
            {
                float normMin = Mathf.Min(config.spawnIntervalMin, config.spawnIntervalMax);
                float normMax = Mathf.Max(config.spawnIntervalMin, config.spawnIntervalMax);
                dt = Random.Range(normMin, normMax);
            }

            t += dt;
            if (t > config.levelDuration) break;

            var type = PickWeighted(config.typePool); // 返回 WeightedCargoType
            var name = PickWeighted(config.namePool); // 返回 WeightedName

            // 2. 修复 Bug：'cargo' 未定义，应为 'type'
            if (type != null && name != null)
            {
                var payload = new CargoSpawnPayload3D
                {
                    // 4. (假设) 假设您的 WeightedCargoType 类有一个 CargoType3D 类型的字段叫 'cargoType'
                    //    'name' (WeightedName) 同理，有一个 'cargoName' 字段
                    cargoType = type.cargoType,
                    cargoName = name.cargoName
                };

                Generated.Add(new TimedEvent
                {
                    timestamp = t,
                    eventName = "SpawnCargo",
                    payloadJson = JsonUtility.ToJson(payload)
                });
            }

            // ... (事件 和 道具 的生成逻辑不变) ...

            // === 2) 事件：先看当前时刻 t 是否允许尝试触发（受全局门限控制） ===
            if (config.allowedEvents != null && config.allowedEvents.Count > 0 && t >= nextEventAllowedAt)
            {
                // 2.1 过滤出“已经过自身冷却（含随机缓冲）且到达全局门限”的事件
                var readyList = new List<GameEvent>();
                foreach (var ge in config.allowedEvents)
                {
                    if (!ge) continue;
                    float tReadyEvent = nextReadyAt.TryGetValue(ge.eventName, out var tr) ? tr : config.eventStartDelay;
                    float gate = Mathf.Max(tReadyEvent, nextEventAllowedAt);
                    if (t >= gate) readyList.Add(ge);
                }

                if (readyList.Count > 0)
                {
                    // 2.2 有可触发的 -> 按权重挑一个，并安排在 t 的一个小随机偏移处
                    var evt = PickWeighted(readyList);
                    if (evt != null)
                    {
                        float fireTime = Mathf.Min(
                            Mathf.Max(t + Random.Range(0.1f, 0.4f), nextEventAllowedAt),
                            config.levelDuration
                        );

                        Generated.Add(new TimedEvent
                        {
                            timestamp = fireTime,
                            eventName = evt.eventName,
                            payloadJson = null
                        });

                        // 计算并记录该事件下一次可触发时间：基础CD + 随机0~floor(基础CD*extra%)
                        int baseCd = baseCdByName.TryGetValue(evt.eventName, out var b) ? b : 0;
                        float pct = extraPctByName.TryGetValue(evt.eventName, out var p) ? p : 0.3f;
                        int maxExtra = Mathf.FloorToInt(baseCd * Mathf.Clamp01(pct));
                        int randomExtra = (maxExtra > 0) ? Random.Range(0, maxExtra + 1) : 0;
                        nextReadyAt[evt.eventName] = fireTime + baseCd + randomExtra;

                        // 全局最小间隔更新
                        nextEventAllowedAt = fireTime + Mathf.Max(config.minEventGap, 0.1f);
                    }
                }
                else
                {
                    // 2.3 都在CD里 -> 顺延到“最早可触发”的事件，并在该时刻（加微偏移）安排它
                    float bestTime = float.PositiveInfinity;
                    GameEvent bestEvt = null;

                    foreach (var ge in config.allowedEvents)
                    {
                        if (!ge) continue;

                        float tReadyEvent = nextReadyAt.TryGetValue(ge.eventName, out var tr) ? tr : config.eventStartDelay;
                        float candidate = Mathf.Max(tReadyEvent, nextEventAllowedAt);
                        if (candidate < bestTime)
                        {
                            bestTime = candidate;
                            bestEvt = ge;
                        }
                    }

                    if (bestEvt != null && bestTime <= config.levelDuration)
                    {
                        float fireTime = Mathf.Min(bestTime + Random.Range(0.1f, 0.4f), config.levelDuration);

                        Generated.Add(new TimedEvent
                        {
                            timestamp = fireTime,
                            eventName = bestEvt.eventName,
                            payloadJson = null
                        });

                        // 安排后，给该事件roll一次新的CD随机缓冲
                        int baseCd = baseCdByName.TryGetValue(bestEvt.eventName, out var b2) ? b2 : 0;
                        float pct = extraPctByName.TryGetValue(bestEvt.eventName, out var p2) ? p2 : 0.3f;
                        int maxExtra = Mathf.FloorToInt(baseCd * Mathf.Clamp01(p2));
                        int randomExtra = (maxExtra > 0) ? Random.Range(0, maxExtra + 1) : 0;
                        nextReadyAt[bestEvt.eventName] = fireTime + baseCd + randomExtra;

                        // 全局最小间隔更新
                        nextEventAllowedAt = fireTime + Mathf.Max(config.minEventGap, 0.1f);
                    }
                }
            }
            // === 3) 道具生成（并行系统） ===
            if (config.allowedPickups != null && config.allowedPickups.Count > 0 && t >= nextPickupAllowedAt)
            {
                // 3.1 过滤出“已过自身CD且到达全局门限”的道具
                var readyPickups = new List<WeightedPickupKey>();
                foreach (var pk in config.allowedPickups)
                {
                    if (pk == null || string.IsNullOrEmpty(pk.key)) continue;
                    float gate = Mathf.Max(nextPickupReadyAt.TryGetValue(pk.key, out var tr) ? tr : config.pickupStartDelay, nextPickupAllowedAt);
                    if (t >= gate) readyPickups.Add(pk);
                }

                if (readyPickups.Count > 0)
                {
                    // 3.2 按权重挑一个道具，安排在 t 的一个小偏移处
                    var chosen = PickWeighted(readyPickups);
                    if (chosen != null)
                    {
                        float fireTime = Mathf.Min(Mathf.Max(t + Random.Range(0.1f, 0.4f), nextPickupAllowedAt), config.levelDuration);

                        var payload = new PickupSpawnPayload { key = chosen.key };
                        Generated.Add(new TimedEvent { timestamp = fireTime, eventName = "SpawnPickup", payloadJson = JsonUtility.ToJson(payload) });

                        int baseCd = pickupBaseCd.TryGetValue(chosen.key, out var b) ? b : 0;
                        float pct = pickupExtraPct.TryGetValue(chosen.key, out var p) ? p : 0.3f;
                        int maxExtra = Mathf.FloorToInt(baseCd * Mathf.Clamp01(p));
                        int randomExtra = (maxExtra > 0) ? Random.Range(0, maxExtra + 1) : 0;
                        nextPickupReadyAt[chosen.key] = fireTime + baseCd + randomExtra;

                        nextPickupAllowedAt = fireTime + Mathf.Max(config.minPickupGap, 0.1f);
                    }
                }
                else
                {
                    // 3.3 都在CD里 -> 找最早可触发的道具排队
                    float bestTime = float.PositiveInfinity; WeightedPickupKey best = null;
                    foreach (var pk in config.allowedPickups)
                    {
                        if (pk == null || string.IsNullOrEmpty(pk.key)) continue;
                        float candidate = Mathf.Max(nextPickupReadyAt.TryGetValue(pk.key, out var tr) ? tr : config.pickupStartDelay, nextPickupAllowedAt);
                        if (candidate < bestTime) { bestTime = candidate; best = pk; }
                    }

                    if (best != null && bestTime <= config.levelDuration)
                    {
                        float fireTime = Mathf.Min(bestTime + Random.Range(0.1f, 0.4f), config.levelDuration);
                        var payload = new PickupSpawnPayload { key = best.key };
                        Generated.Add(new TimedEvent { timestamp = fireTime, eventName = "SpawnPickup", payloadJson = JsonUtility.ToJson(payload) });

                        int baseCd = pickupBaseCd.TryGetValue(best.key, out var b2) ? b2 : 0;
                        float pct = pickupExtraPct.TryGetValue(best.key, out var p2) ? p2 : 0.3f;
                        int maxExtra = Mathf.FloorToInt(baseCd * Mathf.Clamp01(p2));
                        int randomExtra = (maxExtra > 0) ? Random.Range(0, maxExtra + 1) : 0;
                        nextPickupReadyAt[best.key] = fireTime + baseCd + randomExtra;

                        nextPickupAllowedAt = fireTime + Mathf.Max(config.minPickupGap, 0.1f);
                    }
                }
            }
        }

        // === 结束事件 ===
        Generated.Add(EndEvent);
        Generated.Sort((a, b) => a.timestamp.CompareTo(b.timestamp));
    }

    // ====== 通用权重挑选方法 ======
    private T PickWeighted<T>(List<T> list) where T : IWeighted
    {
        if (list == null || list.Count == 0) return default;
        float sum = 0f;
        foreach (var w in list) sum += Mathf.Max(0.0001f, w.Weight);
        float r = Random.Range(0f, sum);
        foreach (var w in list)
        {
            r -= Mathf.Max(0.0001f, w.Weight);
            if (r <= 0f) return w;
        }
        return list[list.Count - 1];
    }

    [System.Serializable]
    public class CargoSpawnPayload3D
    {
        // 3. 修复：使用 3D 的 CargoType 枚举
        public Project.Gameplay3D.CargoType3D cargoType;
        public string cargoName;
    }

    [System.Serializable]
    public class PickupSpawnPayload
    {
        public string key; // PoolManager中的key，比如 pickup_speed / pickup_strong
    }
}