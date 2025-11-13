using System.Collections.Generic;
using UnityEngine;

public static class GameEventRegistry
{
    private static readonly Dictionary<string, GameEventBehaviour> _map = new();

    public static void Register(string key, GameEventBehaviour behaviour)
    {
        _map[key] = behaviour;
        // Debug.Log($"[GameEventRegistry] зЂВс {key} -> {behaviour.name}");
    }

    public static GameEventBehaviour Get(string key)
    {
        _map.TryGetValue(key, out var b);
        return b;
    }
}
