using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CargoNameIconMap", menuName = "Game/Cargo Name ¡ú Icon Map")]
public class CargoNameIconMap : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string cargoName;
        public Sprite icon;
    }

    public List<Entry> entries = new List<Entry>();
    private Dictionary<string, Sprite> _dict;

    public Sprite GetIcon(string name)
    {
        if (_dict == null)
        {
            _dict = new Dictionary<string, Sprite>();
            foreach (var e in entries)
                if (!_dict.ContainsKey(e.cargoName))
                    _dict.Add(e.cargoName, e.icon);
        }
        return _dict.TryGetValue(name, out var s) ? s : null;
    }
}

