using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelSelectorUI : MonoBehaviour
{
    // —— 持久化键，保持与 DevTools 一致（DevTools 也使用同名键）——
    public const string DevBypassKey = "DevBypassAllLevels";

    // 对外静态查询：优先走 DevTools，没注入时回退到 PlayerPrefs（兼容旧逻辑）
    public static bool IsBypassOn()
    {
        if (DevTools.Instance != null) return DevTools.Instance.DevBypass;
        return PlayerPrefs.GetInt(DevBypassKey, 0) == 1;
    }

    [Header("Buttons")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Button nightshiftButton;
    public Button level3DButton;

    [Header("Score Requirements")]
    [Tooltip("解锁 Level2 所需 Level1 总分")]
    public int level2RequiredScore = 200;
    [Tooltip("解锁 Level3 所需 Level2 总分")]
    public int level3RequiredScore = 500;
    [Tooltip("解锁 NightShift 所需 Level3 总分")]
    public int nightShiftRequiredScore = 1;

    [Header("Transition")]
    public string levelTransition = "CrossFade";
    public CanvasGroup levelMenuObject;

    private void OnEnable()
    {
        // 向 DevTools 注册自己，便于其在命令切换后主动通知刷新
        DevTools.Instance?.RegisterSelector(this);
        SetupLevelButtons();
    }

    private void OnDisable()
    {
        // 从 DevTools 反注册
        DevTools.Instance?.UnregisterSelector(this);
    }

    // 可被 DevTools 调用：刷新按钮状态
    public void RefreshButtons() => SetupLevelButtons();

    private void ResetButton(Button btn)
    {
        if (!btn) return;
        btn.onClick.RemoveAllListeners(); // 先清监听
        btn.interactable = false;         // 再锁住
    }

    private void ClearAndWire(Button btn, int levelIndex)
    {
        if (!btn) return;
        btn.onClick.RemoveAllListeners();
        btn.interactable = true;
        btn.onClick.AddListener(() => LoadLevel(levelIndex));
    }

    private void SetupLevelButtons()
    {
        // 1) 全重置，杜绝“旁路状态残留”的监听器
        ResetButton(level1Button);
        ResetButton(level2Button);
        ResetButton(level3Button);
        ResetButton(nightshiftButton);
        ResetButton(level3DButton);

        // 2) 旁路：全部开放（来自 DevTools 的实时状态）
        if (IsBypassOn())
        {
            ClearAndWire(level1Button, 3);
            ClearAndWire(level2Button, 4);
            ClearAndWire(level3Button, 5);
            ClearAndWire(nightshiftButton, 6);
            ClearAndWire(level3DButton, 7);
            return;
        }

        // 3) 正常门槛
        ClearAndWire(level1Button, 3); // Level1 永远可进

        int totalLevel1 = PlayerPrefs.GetInt("TotalScore_3", 0);
        if (totalLevel1 >= level2RequiredScore) ClearAndWire(level2Button, 4);

        int totalLevel2 = PlayerPrefs.GetInt("TotalScore_4", 0);
        if (totalLevel2 >= level3RequiredScore) ClearAndWire(level3Button, 5);

        int nightshiftBuildIndex = 6;
        bool beatLevel3 = PlayerPrefs.GetInt("BeatLevel3", 0) == 1;

        // NightShift 解锁逻辑
        if (beatLevel3)
        {
            ClearAndWire(nightshiftButton, nightshiftBuildIndex);
        }
        else
        {
            ResetButton(nightshiftButton); // 完全锁住
        }

        // Level_3D 永远可进
        ClearAndWire(level3DButton, 7);
    }

    private void LoadLevel(int id)
    {
        
        // if enter from 3DlevelSelector, them go back to the 3Dmainmenu
        if(SceneManager.GetActiveScene().buildIndex == 1) GameManager.LastMenuIndex = 0;
        else// go back to 2d mainmenu
        {
            GameManager.LastMenuIndex = SceneManager.GetActiveScene().buildIndex;
        }
        

        Debug.Log($"LevelSelectorUI: LoadLevel({id}) invoked. LevelManager.Instance is {(LevelManager.Instance == null ? "NULL" : "present")}");

        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelSelectorUI: LevelManager.Instance is null — scene load aborted.");
            return;
        }

        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.45f);
        SoundManager.Instance?.FadeOutMusic(0.4f);

        SoundType musicType = GetMusicTypeForLevel(id);

        if (levelMenuObject)
        {
            LeanTween.alphaCanvas(levelMenuObject, 0f, 0.5f)
                     .setIgnoreTimeScale(true);
            levelMenuObject.interactable = false;
        }

        LevelManager.Instance.LoadScene(id, "CrossFade", musicType);
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
            default: return SoundType.MainMenuMusic; // Fallback
        }
    }
}
