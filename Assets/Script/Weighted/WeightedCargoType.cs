using UnityEngine;

[System.Serializable]
public class WeightedCargoType:IWeighted
{
    public int Id;
    public CargoType cargoType;
    [Range(1, 100)] public int weight = 1;
    public int Weight => weight;
}
