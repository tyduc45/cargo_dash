using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CargoTypeIconMap", menuName = "Game/Cargo Type ¡ú Icon Map")]
public class CargoTypeIconMap : ScriptableObject
{
	[System.Serializable]
	public struct Entry
	{
		public CargoType type;
		public Sprite icon;
	}

	public List<Entry> entries = new List<Entry>();
	private Dictionary<CargoType, Sprite> _dict;

	public Sprite GetIcon(CargoType t)
	{
		if (_dict == null)
		{
			_dict = new Dictionary<CargoType, Sprite>();
			foreach (var e in entries)
				if (!_dict.ContainsKey(e.type))
					_dict.Add(e.type, e.icon);
		}
		return _dict.TryGetValue(t, out var s) ? s : null;
	}
}
