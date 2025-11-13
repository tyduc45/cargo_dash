using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectSpawnerWithPool : MonoBehaviour
{
    [Header("Pool Settings")]
    public Transform poolParent;
    public int prewarmPerType = 4;
    public int expandStep = 2;

    [Header("Initial velocity Settings")]
    public Vector2 minXY = new Vector2(-2f, 5f);
    public Vector2 maxXY = new Vector2(2f, 8f);

    [Header("Icon Mapping")]
    public CargoNameIconMap nameIconMap;
    public CargoTypeIconMap typeIconMap;

    private readonly Dictionary<string, List<GameObject>> poolsByName = new();
    private Vector3 offset;

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        offset = sr ? new Vector3(0f, sr.bounds.size.y, 0f) : Vector3.zero;
    }

    public void SpawnFromTimeline(TimelineGenerator.CargoSpawnPayload payload, List<WeightedPrefab> pool)
    {
        if (pool == null || pool.Count == 0) return;

        GameObject prefab = !string.IsNullOrEmpty(payload.prefabName)
            ? FindPrefabByName(payload.prefabName, pool)
            : WeightedPickPrefab(pool);
        if (prefab == null) { Debug.LogError("SpawnFromTimeline: no prefab."); return; }

        var go = GetFromPool(prefab);
        if (go == null) { Debug.LogWarning($"Pool exhausted for {prefab.name}"); return; }

        var cargo = go.GetComponent<Cargo>();
        if (cargo)
        {
            cargo.ownerSpawner = this;
            cargo.cargoType = payload.cargoType;
            cargo.cargoName = payload.cargoName;

            // ✅ 设置 Sprite
            if (nameIconMap && cargo) cargoIconSet(cargo, payload);
            if (cargo.nameLabel)
            {
                cargo.nameLabel.text = cargo.cargoName;
                cargo.nameLabel.enabled = true;
            }

            cargo.SetState(CargoState.Active);
        }

        go.transform.position = transform.position + offset;
        go.SetActive(true);

        SoundManager.Instance.PlaySound(SoundType.CargoSpawn, null, 0.15f);

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb)
        {
            float vx = Random.Range(minXY.x, maxXY.x);
            float vy = Random.Range(minXY.y, maxXY.y);
            rb.linearVelocity = new Vector2(vx, vy);
        }
    }

    private void cargoIconSet(Cargo c, TimelineGenerator.CargoSpawnPayload p)
    {
        if (c == null) return;

        var catSR = c.categorySR;
        var typeSR = c.typeSR;

        // Cargo 在 Awake 缓存的 categorySR 和 typeSR
        if (catSR)
        {
            catSR.sprite = nameIconMap?.GetIcon(p.cargoName);
            catSR.enabled = true;
        }

        if (typeSR)
        {
            typeSR.sprite = typeIconMap?.GetIcon(p.cargoType);
            typeSR.enabled = true;
        }
    }

    private GameObject GetFromPool(GameObject prefab)
    {
        string key = GetBaseName(prefab.name);

        if (!poolsByName.TryGetValue(key, out var list))
        {
            list = new List<GameObject>(prewarmPerType);
            poolsByName[key] = list;
            Prewarm(prefab, list, prewarmPerType);
        }

        foreach (var go in list)
        {
            if (go && !go.activeSelf)
            {
                var cargo = go.GetComponent<Cargo>();
                if (cargo) cargo.SetState(CargoState.InPool);

                var col = go.GetComponent<Collider2D>();
                if (col) col.enabled = true;
                var rb = go.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    rb.simulated = true;
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                }
                return go;
            }
        }

        Prewarm(prefab, list, expandStep);
        return list.FirstOrDefault(g => g && !g.activeSelf);
    }

    private void Prewarm(GameObject prefab, List<GameObject> list, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var obj = Instantiate(prefab, transform.position, Quaternion.identity);
            obj.SetActive(false);
            obj.tag = "cargos";
            if (poolParent) obj.transform.SetParent(poolParent);
            var cargo = obj.GetComponent<Cargo>();
            if (cargo) cargo.ownerSpawner = this;
            list.Add(obj);
        }
    }

    private GameObject FindPrefabByName(string name, List<WeightedPrefab> pool)
    {
        string key = GetBaseName(name);
        var wp = pool.FirstOrDefault(x => GetBaseName(x.prefab.name) == key);
        return wp?.prefab;
    }

    private GameObject WeightedPickPrefab(List<WeightedPrefab> pool)
    {
        int total = 0;
        foreach (var w in pool) total += Mathf.Max(0, w.weight);
        if (total <= 0) return pool[0].prefab;

        int r = Random.Range(0, total);
        foreach (var w in pool)
        {
            r -= Mathf.Max(0, w.weight);
            if (r < 0) return w.prefab;
        }
        return pool[pool.Count - 1].prefab;
    }

    public void ResetSpawner()
    {
        foreach (var kv in poolsByName)
        {
            foreach (var obj in kv.Value)
            {
                if (!obj) continue;
                obj.SetActive(false);
                var cargo = obj.GetComponent<Cargo>();
                if (cargo)
                {
                    cargo.hasCollided = false;
                    cargo.SetState(CargoState.InPool);
                }
                var rb = obj.GetComponent<Rigidbody2D>();
                if (rb)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
            }
        }
    }

    private string GetBaseName(string n)
    {
        if (string.IsNullOrEmpty(n)) return "";
        int i = n.IndexOf('(');
        return i >= 0 ? n.Substring(0, i).Trim() : n.Trim();
    }
}
