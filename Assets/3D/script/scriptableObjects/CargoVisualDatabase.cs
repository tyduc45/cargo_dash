using UnityEngine;
using System.Collections.Generic;
using Project.Gameplay3D;

[CreateAssetMenu(fileName = "CargoVisualDatabase", menuName = "Gameplay/Cargo Visual Database")]
public class CargoVisualDatabase : ScriptableObject
{
    [System.Serializable] public class TypeColorMapping { public CargoType3D type; public Color color = Color.white; }
    [System.Serializable] public class NameIconMapping { public string name; public Texture2D icon; }

    // ★ 新增：名字→颜色，类型→图标
    [System.Serializable] public class NameColorMapping { public string name; public Color color = Color.white; }
    [System.Serializable] public class TypeIconMapping { public CargoType3D type; public Texture2D icon; }

    [Header("Defaults")]
    public Color defaultColor = Color.white;
    public Texture2D defaultIcon;

    [Header("Type → Color (旧)")]
    public List<TypeColorMapping> typeColors = new();
    [Header("Name → Icon (旧)")]
    public List<NameIconMapping> nameIcons = new();

    [Header("Name → Color (新)")]
    public List<NameColorMapping> nameColors = new();
    [Header("Type → Icon (新)")]
    public List<TypeIconMapping> typeIcons = new();

    Dictionary<CargoType3D, Color> _typeColorDict;
    Dictionary<string, Texture2D> _nameIconDict;
    Dictionary<string, Color> _nameColorDict;
    Dictionary<CargoType3D, Texture2D> _typeIconDict;

    bool _inited;

    void OnEnable() { _inited = false; }

    void Initialize()
    {
        if (_inited) return;
        _inited = true;

        _typeColorDict = new();
        if (typeColors != null)
            foreach (var m in typeColors)
                if (!_typeColorDict.ContainsKey(m.type)) _typeColorDict[m.type] = m.color;

        _nameIconDict = new(System.StringComparer.OrdinalIgnoreCase);
        if (nameIcons != null)
            foreach (var m in nameIcons)
                if (!string.IsNullOrEmpty(m.name) && !_nameIconDict.ContainsKey(m.name)) _nameIconDict[m.name] = m.icon;

        // ★ 新增两张字典
        _nameColorDict = new(System.StringComparer.OrdinalIgnoreCase);
        if (nameColors != null)
            foreach (var m in nameColors)
                if (!string.IsNullOrEmpty(m.name) && !_nameColorDict.ContainsKey(m.name)) _nameColorDict[m.name] = m.color;

        _typeIconDict = new();
        if (typeIcons != null)
            foreach (var m in typeIcons)
                if (!_typeIconDict.ContainsKey(m.type)) _typeIconDict[m.type] = m.icon;
    }

    // ===== 旧接口（向后兼容）=====
    public Color GetColor(CargoType3D type) { Initialize(); return _typeColorDict.TryGetValue(type, out var c) ? c : defaultColor; }
    public Texture2D GetIcon(string name) { Initialize(); return (!string.IsNullOrEmpty(name) && _nameIconDict.TryGetValue(name, out var t)) ? t : defaultIcon; }

    // ===== 新接口（用于“颜色=名字、图标=类型”）=====
    public Color GetNameColor(string name) { Initialize(); return (!string.IsNullOrEmpty(name) && _nameColorDict.TryGetValue(name, out var c)) ? c : defaultColor; }
    public Texture2D GetTypeIcon(CargoType3D type) { Initialize(); return _typeIconDict.TryGetValue(type, out var t) ? t : defaultIcon; }
}
