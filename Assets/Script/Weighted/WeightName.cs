using UnityEngine;

[System.Serializable]
public class WeightedName : IWeighted
{
    public int Id;
    public string cargoName;
    [Range(1, 100)] public int weight = 1;
    public int Weight => weight;
}
