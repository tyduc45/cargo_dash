using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum GameState { Playing, Paused, Ended, Completed }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public static int LastMenuIndex = 0; // record the last menu entered

    public GameState currentState { get; private set; } = GameState.Paused;

    private bool settingsOpen = false;

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
    private int totalScore = 0;     // ✅ 新增：该关卡累计总分（只加正分）

    public int GetCurrentScore() => currentScore;
    public int GetBestScore() => bestScore;


    [Header("Victory Effects")]
    public ParticleSystem victoryParticle; // assign in Inspector
    public bool playVictoryOnComplete = true;

    private controller player;

    [Header("Level Config")]
    public LevelConfig levelConfig;

    private Color backlogOriginalColor;

    /// ✅ 新增接口：返回该关卡累计总分
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

    // every time the scene is loaded , the game can be played
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        player = FindFirstObjectByType<controller>();
        if (player != null) player.enabled = true;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        gameTime = 0f;
        backlogCount = 0;
        failCountdown = -1f;
        countdownSoundStarted = false; // ✅ Reset countdown sound state
        if (failCountdownText) failCountdownText.text = "";


    }

    void Update()
    {
        
        
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
                // if (failCountdownText) failCountdownText.text = "";
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
                        // Timer reached 0 and backlog is under limit - SUCCESS
                        if (SoundManager.Instance != null)
                            SoundManager.Instance.PlaySound(SoundType.LevelSuccess, null, 0.45f);
                        OnLevelComplete();
                        if (SceneManager.GetActiveScene().buildIndex == 5)
                        {
                            PlayerPrefs.SetInt("BeatLevel3", 1);
                        }
                    }
                    else 
                    {
                        // Timer reached 0 but backlog still over limit - FAILURE
                        if (SoundManager.Instance != null)
                            SoundManager.Instance.PlaySound(SoundType.LevelFailed, null, 0.45f);
                        OnGameEnd();
                    }
                }
            }
        }
    }
    public void OnGameEnd()
    {
        if (currentState == GameState.Ended) return;
        currentState = GameState.Ended;
        Time.timeScale = 0f;
        if (player != null) player.enabled = false;

        // Ensure any looping alarms/warnings stop immediately
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAlarmLoop();
            SoundManager.Instance.StopWarningLoop();
        }
        countdownSoundStarted = false;

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

        // STOP countdown alarm sound on pause (was stopping warning loop mistakenly)
        if (countdownSoundStarted && SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAlarmLoop();
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

            // Play level win music centrally here
            if (playVictoryOnComplete)
            {
                SoundManager.Instance.PlaySound(SoundType.LevelWinMusic, null, 0.25f);
            }
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

        if (playVictoryOnComplete && victoryParticle != null)
        {
            var mainModule = victoryParticle.main;
            mainModule.useUnscaledTime = true;

            // Play the particle system
            victoryParticle.Play();
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

    public void OnBacklogAdd()
    {
        backlogCount++;
    }

    public void OnBacklogRemove()
    {
        backlogCount = Mathf.Max(0, backlogCount - 1);
    }


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
            case 3: return SoundType.Level1Music;
            case 4: return SoundType.Level2Music;
            case 5: return SoundType.Level3Music;
            default: return SoundType.MainMenuMusic;
        }
    }
}
