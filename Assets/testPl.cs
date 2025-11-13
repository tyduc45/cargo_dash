using System.Collections.Generic;
using UnityEngine;

public class TestInventoryLogger : MonoBehaviour
{
    public PlayerCargoInventory inventory;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            List<PlayerCargoInfo> infos = inventory.GetCargoInfos();

            Debug.Log($"=== Player has {infos.Count} cargo(s) ===");
            for (int i = 0; i < infos.Count; i++)
            {
                var c = infos[i];
                Debug.Log($"[{i}] name={c.cargoName}, type={c.cargoType}, sprite={(c.cargoSprite != null ? c.cargoSprite.name : "null")},color={c.cargoColor}");
            }
        }
    }
}
