using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Project.Gameplay3D; // 引用 Cargo3D

public class GameManager3D : MonoBehaviour
{
    public static GameManager3D Instance;

    public GameState currentState { get; private set; } = GameState.Paused;

    [Header("Game State")]
    public int backlogCount = 0;
    public int stackLimit = 8;
    public float gameTime = 0f;

    [Header("Fail Countdown")]
    public float failCountdownDuration = 10f;   // 超限后倒计时
    private float failCountdown = -1f;          // -1 表示没有在倒计时
    public TextMeshProUGUI failCountdownText;
    private bool countdownSoundStarted = false; // 红字倒计时

    [Header("Timer UI")]
    public TextMeshProUGUI gameTimeText;
    // 总用时
    public TextMeshProUGUI backlogDisplay;

    [Header("Score")]
    public int currentScore = 0;   // 本局得分
    public int bestScore = 0;      // 历史最高分
    private int totalScore = 0;    // 该关卡累计总分（只加正分）

    [Header("Pre-Start UI")]
    public GameObject preStartPanel;            // 全屏面板
    public TextMeshProUGUI preStartTitle;       // 例如：“3D关卡：操作说明”
    public TextMeshProUGUI preStartBody;        // 关卡目标/操作提示
    public Button preStartButton;               // “开始”按钮
    [Tooltip("按下此键也能开始")]
    public KeyCode preStartHotkey = KeyCode.Space;

    private bool waitingForStart = true;        // 载入后先等待开始

    public int GetCurrentScore() => currentScore;
    public int GetBestScore() => bestScore;

    private controller player;

    [Header("Level Config")]
    public LevelConfig3D levelConfig;

    private Color backlogOriginalColor;

    /// <summary>返回该关卡累计总分</summary>
    public int GetTotalScoreForCurrentLevel()
    {
        string key = $"TotalScore_{SceneManager.GetActiveScene().buildIndex}";
        return PlayerPrefs.GetInt(key, 0);
    }

    private void AddCurrentToTotalScore()
    {
        if (currentScore > 0)
        {
            string key = $"TotalScore_{SceneManager.GetActiveScene().buildIndex}";
            int total = PlayerPrefs.GetInt(key, 0);
            total += currentScore;
            PlayerPrefs.SetInt(key, total);
            PlayerPrefs.Save();
        }
    }

    // =================== 新增：事件驱动的积压计数 ===================
    [Header("Backlog Counting (Event-Driven)")]
    [Tooltip("开启后，积压计数由注册表驱动，仅在生成/交付/禁用等事件时刷新")]
    public bool useEventDrivenRegistry = true;

    [Tooltip("只统计已落地(grounded)的货物；关闭则统计全部处于 Active 状态的货物")]
    public bool countGroundedOnly = false;

    public bool includeCarriedInBacklog = true;

    // 活动货物注册表（仅记录“在场 Active”的货物引用）
    private readonly HashSet<Cargo3D> activeCargo = new HashSet<Cargo3D>();

    // ===============================================================

    // every time the scene is loaded , the game can be played
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        player = FindFirstObjectByType<controller>();

        // ✅ 启动时进入等待开始
        waitingForStart = true;
        currentState = GameState.Paused;   // 复用 Paused 作为“未开始”也可以
        Time.timeScale = 0f;
        gameTime = 0f;
        backlogCount = 0;
        failCountdown = -1f;
        countdownSoundStarted = false;
        if (failCountdownText) failCountdownText.text = "";

        if (player != null) player.enabled = false;

        // 显示说明面板并填充文案（可按需自定义）
        if (preStartPanel) preStartPanel.SetActive(true);
        if (preStartTitle) preStartTitle.text = "Level Description";
        if (preStartBody)
            preStartBody.text =
                "Welcome to the 3D level! As you can see, this is just a prototype, but it fits the theme and is completely playable! This level is basically a remake of the first 2D level, but without the buff system.\n\n" +
                "The operations are exactly the same as those in the 2D levels. You can find them in the \"How to play\" section of the main menu." +
                "If you haven't played 2D levels before, it doesn't matter. Here is a quick guide:\n\n" +
                "Your goal is to ensure that the number of cargos in the scene does not exceed the backlog threshold before the time runs out. Once the threshold is exceeded, you have 10 seconds to turn the situation around; otherwise, you lose!\n" +
                "In the game, two colors of cargos will appear. You can score and eliminate them by transporting them to the corresponding color receivers. Also, please note that some goods are highly elastic.\n" +
                "Sometimes, water pool will appear on the ground. Staying inside them will cause you to lose control and the goods will fall all over the place. Please be careful.\n" +
                "In addition, you have a compass. The blue will point to the receiver, while the green will point to the generator of the cargos.\n" +
                "Also, there is a stick in front of you representing your direction. When you face the cargo and the stick turns blue, it indicates that you can pick it up.\n" +
                "The above is all the information about the levels. Have fun and please challenge our 2D levels as much as possible.";

        if (preStartButton)
        {
            preStartButton.onClick.RemoveAllListeners();
            preStartButton.onClick.AddListener(BeginGame);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // ◀◀◀ 等待开始阶段：只响应开始热键，其余 Update 逻辑一律不跑
        if (waitingForStart)
        {
            if (Input.GetKeyDown(preStartHotkey))
                BeginGame();
            return;
        }


        // 暂停/恢复
        if (currentState == GameState.Playing && Input.GetKeyDown(KeyCode.Escape))
            PauseGame();
        else if (currentState == GameState.Paused && Input.GetKeyDown(KeyCode.Escape))
            OnGameResume();

        if (currentState == GameState.Playing)
        {
            gameTime += Time.deltaTime;
            UpdateGameTimeUI();

            UpdateBacklogUI();

            // 超限倒计时逻辑
            if (backlogCount >= stackLimit)
            {
                if (failCountdown < 0f)
                {
                    failCountdown = failCountdownDuration;

                    if (SoundManager.Instance != null && !countdownSoundStarted)
                    {
                        SoundManager.Instance.StartAlarmLoop(SoundType.BackLogAlarm, 0.25f);
                        countdownSoundStarted = true;
                    }
                }

                failCountdown -= Time.deltaTime;
                UpdateFailCountdownUI();

                if (failCountdown <= 0f)
                    OnGameEnd();
            }
            else
            {
                failCountdown = -1f;
                if (countdownSoundStarted)
                {
                    if (SoundManager.Instance != null)
                        SoundManager.Instance.StopAlarmLoop();
                    countdownSoundStarted = false;
                }
                UpdateFailCountdownUI();
            }

            // ✅ 检查是否可以提前完成
            int currentScoreNow = ScoreManager.Instance != null ? ScoreManager.Instance.GetScore() : currentScore;
            if (levelConfig != null)
            {
                // 情况 1: 时间到，自动结算
                if (gameTime >= levelConfig.levelDuration)
                {
                    if (backlogCount < stackLimit)
                    {
                        if (SoundManager.Instance != null)
                            SoundManager.Instance.PlaySound(SoundType.LevelSuccess, null, 0.45f);
                        OnLevelComplete();
                    }
                    else
                    {
                        if (SoundManager.Instance != null)
                            SoundManager.Instance.PlaySound(SoundType.LevelFailed, null, 0.45f);
                        OnGameEnd();
                    }
                }
            }
        }
    }

    public void BeginGame()
    {
        if (!waitingForStart) return;   // 防抖

        waitingForStart = false;
        currentState = GameState.Playing;

        // 计时从 0 开始
        gameTime = 0f;

        // 隐藏面板、恢复时间与玩家控制
        if (preStartPanel) preStartPanel.SetActive(false);
        Time.timeScale = 1f;
        if (player != null) player.enabled = true;

        // 如果你有 UIManager 的开局 UI，要关掉
        UIManager.Instance?.ShowUI(UIType.None);

        // 可选：给关卡广播一个“开始了”的事件，其他系统（生成器/Timeline）可订阅
        OnGameStarted?.Invoke();
    }

    // 可选：全局事件，供生成器/摄像机动画等订阅
    public static System.Action OnGameStarted;

    public void OnGameEnd()
    {
        if (currentState == GameState.Ended) return;
        currentState = GameState.Ended;
        Time.timeScale = 0f;
        if (player != null) player.enabled = false;

        if (countdownSoundStarted && SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAlarmLoop();
            countdownSoundStarted = false;
        }

        currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetScore() : 0;

        // ✅ 游戏失败也累计正分
        AddCurrentToTotalScore();
        PlayerPrefs.Save();

        UIManager.Instance.ShowUI(UIType.GameOverUI);
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Paused;
        Time.timeScale = 0f;
        if (player != null) player.enabled = false;

        // ✅ STOP COUNTDOWN TIMER SOUND ON PAUSE
        if (countdownSoundStarted && SoundManager.Instance != null)
        {
            SoundManager.Instance.StopWarningLoop();
        }

        UIManager.Instance.ShowUI(UIType.PauseUI);
    }

    public void OnGameResume()
    {
        if (currentState != GameState.Paused) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        UIManager.Instance.ShowUI(UIType.None);
        if (player != null) player.enabled = true;

        if (backlogCount >= stackLimit && failCountdown > 0f && SoundManager.Instance != null)
        {
            SoundManager.Instance.StartAlarmLoop(SoundType.BackLogAlarm, 0.25f);
        }
    }

    public void OnLevelComplete()
    {
        if (currentState == GameState.Completed) return;
        currentState = GameState.Completed;
        Time.timeScale = 0f;
        if (player != null) player.enabled = false;

        currentScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetScore() : 0;

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAlarmLoop();
            SoundManager.Instance.StopWarningLoop();
        }

        // ✅ 更新历史最高分
        string bestKey = $"BestScore_{SceneManager.GetActiveScene().buildIndex}";
        int best = PlayerPrefs.GetInt(bestKey, 0);
        if (currentScore > best)
        {
            PlayerPrefs.SetInt(bestKey, currentScore);
        }

        if (countdownSoundStarted && SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAlarmLoop();
            countdownSoundStarted = false;
        }
        // ✅ 累计总分（只加正分）
        AddCurrentToTotalScore();

        PlayerPrefs.Save();
        UIManager.Instance.ShowUI(UIType.CreditUI);
    }

    public int GetBestScoreForCurrentLevel()
    {
        string key = $"BestScore_{SceneManager.GetActiveScene().buildIndex}";
        return PlayerPrefs.GetInt(key, 0);
    }

    // ========= 兼容旧逻辑的接口（启用事件驱动时不改数） =========
    public void OnBacklogAdd()
    {
        Debug.Log("add one backlog count");
        if (!useEventDrivenRegistry)
            backlogCount++;
    }

    public void OnBacklogRemove()
    {
        if (!useEventDrivenRegistry)
            backlogCount = Mathf.Max(0, backlogCount - 1);
    }
    // =========================================================

    // =============== 注册表 API：供 Cargo3D 调用 =================
    public void RegisterActiveCargo(Cargo3D c)
    {
        if (!useEventDrivenRegistry || c == null) return;
        activeCargo.Add(c);
        UpdateBacklogFromRegistry();
    }

    public void UnregisterActiveCargo(Cargo3D c)
    {
        if (!useEventDrivenRegistry || c == null) return;
        if (activeCargo.Remove(c))
            UpdateBacklogFromRegistry();
    }

    private void UpdateBacklogFromRegistry()
    {
        if (!useEventDrivenRegistry) return;

        int n = 0;
        foreach (var c in activeCargo)
        {
            if (c == null || !c.gameObject.activeInHierarchy) continue;

            bool isActive = (c.state == CargoState3D.Active);
            bool isCarried = (c.state == CargoState3D.Carried);

            // 只统计 Active；或在开关打开时也统计 Carried
            if (!isActive && !(includeCarriedInBacklog && isCarried))
                continue;

            // “只数落地”只约束 Active 货物；Carried 一律计入
            if (countGroundedOnly && isActive && !c.isGrounded)
                continue;

            n++;
        }
        backlogCount = n;

        // 如果你希望立即刷新UI，可以这里再调用一次 UpdateBacklogUI();
        // UpdateBacklogUI();
    }
    // ==========================================================

    private void UpdateFailCountdownUI()
    {
        if (failCountdown > 0f)   // 只有倒计时大于 0 才显示
        {
            if (failCountdownText != null)
            {
                failCountdownText.gameObject.SetActive(true);
                failCountdownText.color = Color.red;
                failCountdownText.text = $"{failCountdown:0.0}s";
            }
        }
        else
        {
            if (failCountdownText != null)
                failCountdownText.gameObject.SetActive(false);
        }
    }

    private void UpdateBacklogUI()
    {
        if (backlogCount >= stackLimit)
        {
            backlogDisplay.color = Color.red;
        }
        backlogDisplay.text = $"{backlogCount}/{stackLimit}";
    }

    private void UpdateGameTimeUI()
    {
        if (gameTimeText)
            gameTimeText.text = $"{levelConfig.levelDuration - gameTime:0.0}s";
    }

    private SoundType GetMusicTypeForLevel(int levelId)
    {
        switch (levelId)
        {
            case 1: return SoundType.Level1Music;
            case 2: return SoundType.Level2Music;
            case 3: return SoundType.Level3Music;
            default: return SoundType.MainMenuMusic;
        }
    }
}
