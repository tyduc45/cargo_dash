using UnityEngine;
using System.Collections.Generic;

public class PoolManager : MonoBehaviour
{
    [System.Serializable]
    public class Entry { public string key; public GameObject prefab; public int prewarm = 4; public Transform parent; }

    public static PoolManager Instance { get; private set; }
    public List<Entry> entries = new List<Entry>();

    private readonly Dictionary<string, Queue<GameObject>> q = new();
    private readonly Dictionary<string, Transform> parents = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.key) || !e.prefab) continue;
            if (!q.ContainsKey(e.key)) q[e.key] = new Queue<GameObject>();

            var p = e.parent ? e.parent : new GameObject("Pool_" + e.key).transform;
            if (!e.parent) p.SetParent(transform);
            parents[e.key] = p;

            for (int i = 0; i < Mathf.Max(0, e.prewarm); i++)
            {
                var go = Instantiate(e.prefab, p);
                var r = go.AddComponent<ReturnToPoolOnDisable>();
                r.poolKey = e.key;
                go.SetActive(false); // OnDisable -> Return() 内部会入队（且有重入保护）
            }
        }
    }

    public GameObject Spawn(string key, Vector3 pos, Quaternion rot)
    {
        if (!q.ContainsKey(key)) { Debug.LogError($"[Pool] Unknown key: {key}"); return null; }
        GameObject go = (q[key].Count > 0) ? q[key].Dequeue() : Instantiate(entries.Find(x => x.key == key).prefab, parents[key]);
        var hook = go.GetComponent<ReturnToPoolOnDisable>(); if (!hook) hook = go.AddComponent<ReturnToPoolOnDisable>();
        hook.poolKey = key;
        go.transform.SetPositionAndRotation(pos, rot);
        go.SetActive(true);
        return go;
    }

    public void Return(string key, GameObject go)
    {
        if (!q.ContainsKey(key) || !go) return;
        go.SetActive(false);
        go.transform.SetParent(parents[key], false);
        q[key].Enqueue(go);
    }
}

public class ReturnToPoolOnDisable : MonoBehaviour
{
    [HideInInspector] public string poolKey;
    bool enqueued;
    void OnEnable() { enqueued = false; }
    void OnDisable()
    {
        if (enqueued) return;
        enqueued = true;
        if (!string.IsNullOrEmpty(poolKey) && PoolManager.Instance)
            PoolManager.Instance.Return(poolKey, gameObject);
    }
}
