using System.Collections.Generic;
using UnityEngine;

public class DevTools : MonoBehaviour
{
    public static DevTools Instance { get; private set; }

    public const string DevBypassKey = "DevBypassAllLevels";
    public bool DevBypass { get; private set; }

    public event System.Action<bool> OnDevBypassChanged;
    private readonly HashSet<LevelSelectorUI> selectors = new HashSet<LevelSelectorUI>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("_DevTools");
        DontDestroyOnLoad(go);
        go.AddComponent<DevTools>();
        go.AddComponent<DevConsole>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DevBypass = PlayerPrefs.GetInt(DevBypassKey, 0) == 1;
    }

    public void SetDevBypass(bool on)
    {
        if (DevBypass == on) { Notify(); return; }
        DevBypass = on;
        PlayerPrefs.SetInt(DevBypassKey, on ? 1 : 0);
        PlayerPrefs.Save();
        Notify();
    }

    public void ToggleDevBypass() => SetDevBypass(!DevBypass);

    public string ExecuteCommand(string cmd)
    {
        if (string.IsNullOrWhiteSpace(cmd)) return "";
        string trimmed = cmd.Trim();

        // ✅ 新增：支持 selectscene <index>
        if (trimmed.StartsWith("witness", System.StringComparison.OrdinalIgnoreCase))
        {
            
            if (LevelManager.Instance == null)
                return "LevelManager not found. Please ensure it exists in the current scene.";

            try
            {
                SoundManager.Instance.StopAllActiveAudio();
                LevelManager.Instance.LoadScene(8, "CrossFade", GetMusicTypeForLevel(8));
                return $"go to the credit scene";
            }
            catch (System.Exception ex)
            {
                return $"Failed to load scene credit";
            }
        }

        var c = trimmed.ToLowerInvariant();

        switch (c)
        {
            case "help":
            case "?":
                return "commands: dev on | dev off | dev toggle | dev status | witness | help";

            case "whosyourdaddy on":
                SetDevBypass(true);
                return "Developer bypass: ON (all levels unlocked)";

            case "whosyourdaddy off":
                SetDevBypass(false);
                return "Developer bypass: OFF (score gates restored)";

            case "dev toggle":
                ToggleDevBypass();
                return $"Developer bypass toggled: {(DevBypass ? "ON" : "OFF")}";

            case "dev status":
                return $"Developer bypass is {(DevBypass ? "ON" : "OFF")}";

            default:
                return $"Unknown command: {cmd}\nType 'help' for commands.";
        }
    }

    public void RegisterSelector(LevelSelectorUI ui)
    {
        if (ui != null) selectors.Add(ui);
    }

    public void UnregisterSelector(LevelSelectorUI ui)
    {
        if (ui != null) selectors.Remove(ui);
    }

    private void Notify()
    {
        foreach (var ui in selectors)
            if (ui) ui.RefreshButtons();

        OnDevBypassChanged?.Invoke(DevBypass);
        Debug.Log($"[DevTools] Dev bypass {(DevBypass ? "ON" : "OFF")}");
    }

    private SoundType GetMusicTypeForLevel(int levelId)
    {
        switch (levelId)
        {
            case 3: return SoundType.Level1Music;
            case 4: return SoundType.Level2Music;
            case 5: return SoundType.Level3Music;
            case 6: return SoundType.Level3Music;
            case 7: return SoundType.Level3DMusic;
            default: return SoundType.MainMenuMusic;
        }
    }
}