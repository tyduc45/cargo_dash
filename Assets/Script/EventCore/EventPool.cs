using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "EventPool", menuName = "Events/Event Pool")]
public class EventPool : ScriptableObject
{
    [Tooltip("Drag all GameEvents here")]
    public List<GameEvent> events = new List<GameEvent>();

    // Internal lookup table (used at runtime, not serialized)
    [NonSerialized] private Dictionary<string, GameEvent> lookup;

    void OnEnable()
    {
        RebuildLookup();
    }

    void OnValidate()
    {
        RebuildLookup();
    }

    /// <summary>
    /// Rebuild the lookup table
    /// </summary>
    private void RebuildLookup()
    {
        lookup = new Dictionary<string, GameEvent>(StringComparer.Ordinal);
        if (events == null) return;

        foreach (var e in events)
        {
            if (e == null || string.IsNullOrEmpty(e.eventName)) continue;

            if (!lookup.ContainsKey(e.eventName))
            {
                lookup[e.eventName] = e;
            }
            else
            {
                Debug.LogWarning($"[EventPool] Duplicate event name: {e.eventName}, ignored.", this);
            }
        }
    }

    /// <summary>
    /// Find by event name, return null if not found
    /// </summary>
    public GameEvent Find(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (lookup == null) RebuildLookup();
        return lookup.TryGetValue(name, out var ge) ? ge : null;
    }

    /// <summary>
    /// TryGet-style lookup
    /// </summary>
    public bool TryGetEvent(string name, out GameEvent gameEvent)
    {
        gameEvent = null;
        if (string.IsNullOrEmpty(name)) return false;
        if (lookup == null) RebuildLookup();
        return lookup.TryGetValue(name, out gameEvent);
    }

    /// <summary>
    /// Trigger an event directly by name (if it exists)
    /// </summary>
    public bool Trigger(string name)
    {
        var ge = Find(name);
        if (ge != null)
        {
            ge.Invoke();
            return true;
        }
        Debug.LogWarning($"[EventPool] Event not found: {name}", this);
        return false;
    }

    /// <summary>
    /// Return all available event names
    /// </summary>
    public string[] GetAllNames()
    {
        if (lookup == null) RebuildLookup();
        return lookup.Keys.ToArray();
    }

    /// <summary>
    /// ✅ 从池子中按权重随机选择一个事件
    /// </summary>
    public GameEvent PickWeighted()
    {
        if (events == null || events.Count == 0) return null;

        float total = 0;
        foreach (var e in events) total += Mathf.Max(0.0001f, e.Weight);
        float r = UnityEngine.Random.Range(0f, total);
        foreach (var e in events)
        {
            r -= Mathf.Max(0.0001f, e.Weight);
            if (r <= 0) return e;
        }
        return events[events.Count - 1];
    }
}