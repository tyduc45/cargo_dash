using UnityEngine;

[System.Serializable]
public class WeightedPickupKey : IWeighted
{
    [Tooltip("必须与对象池/PoolManager的 key 完全一致，例如 pickup_speed / pickup_strong")]
    public string key;

    // 在 Inspector 里用 float 调起来更顺手
    [Min(0.0001f)] public float weight = 1f;

    // IWeighted：接口要求 int；从可编辑的 float 映射为 int，并做下限保护
    public int Weight => Mathf.Max(1, Mathf.RoundToInt(weight));
}
