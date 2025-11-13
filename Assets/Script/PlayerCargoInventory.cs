using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 封装一件货物的基本信息
/// </summary>
[System.Serializable]
public class PlayerCargoInfo
{
    public string cargoName;
    public string cargoType;
    public Sprite cargoSprite;
    public Color cargoColor;

    public PlayerCargoInfo(string name, string type, Sprite sprite, Color color)
    {
        cargoName = name;
        cargoType = type;
        cargoSprite = sprite;
        cargoColor = color;
    }
}

/// <summary>
/// 提供接口给 UI，用来获取玩家当前手上所有货物的信息
/// </summary>
public class PlayerCargoInventory : MonoBehaviour
{
    private controller player;

    void Awake()
    {
        player = GetComponent<controller>();
        if (player == null)
        {
            Debug.LogError("[PlayerCargoInventory] 没找到 controller 组件，请确认该脚本挂在玩家对象上");
        }
    }

    /// <summary>
    /// 获取当前玩家手上所有货物的信息（从上到下）
    /// </summary>
    public List<PlayerCargoInfo> GetCargoInfos()
    {
        List<PlayerCargoInfo> infos = new List<PlayerCargoInfo>();
        if (player == null || player.cargoStack == null) return infos;

        // 注意：Stack 是 LIFO，为了展示顺序可转成数组
        foreach (var cargoObj in player.cargoStack)
        {
            if (cargoObj == null) continue;
            Cargo cargo = cargoObj.GetComponent<Cargo>();
            if (cargo == null) continue;

            string name = cargo.cargoName;
            string type = cargo.cargoType.ToString(); // 假设 cargoType 是枚举/字符串
            Sprite sprite = null;

            var sr = cargoObj.GetComponentInChildren<SpriteRenderer>();
            sprite = sr != null ? sr.sprite : null;
            Color color = sr != null ? sr.color : Color.white;

            infos.Add(new PlayerCargoInfo(name, type, sprite, color));
        }

        return infos;
    }
}
