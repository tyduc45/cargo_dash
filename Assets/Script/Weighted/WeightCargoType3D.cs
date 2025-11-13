using UnityEngine;
using Project.Gameplay3D;

[System.Serializable]
public class WeightedCargoType3D : IWeighted
{
    public int Id;
    public CargoType3D cargoType;
    [Range(1, 100)] public int weight = 1;
    public int Weight => weight;
}
