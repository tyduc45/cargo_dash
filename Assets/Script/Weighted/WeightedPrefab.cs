using UnityEngine;

[System.Serializable]
public class WeightedPrefab : IWeighted
{
    public int Id;
    public GameObject prefab;       
    [Range(1, 100)]
    public int weight = 1;            // ³öÏÖ¸ÅÂÊ
    public int Weight => weight;
}