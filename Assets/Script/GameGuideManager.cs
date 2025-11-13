using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameGuideManager : MonoBehaviour
{
    public static GameGuideManager Instance;

    [Tooltip("If true, the GameGuide (if present in the scene) will open automatically after the level loads.")]
    public bool showOnLevelLoad = true;

    [Tooltip("Scene build indices (IDs) where the GameGuide should never open (e.g. main menu). Use build index from Build Settings.")]
    public int[] excludedSceneBuildIndexes = new int[] { };

    public GameObject gameGuidePanel; // discovered at runtime
    private CanvasGroup guideCanvasGroup;
    private bool isGuideOpen = false;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        FindGameGuidePanelInScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // reset state and search the newly loaded scene
        isGuideOpen = false;
        gameGuidePanel = null;
        guideCanvasGroup = null;
        StartCoroutine(FindPanelDelayedAndMaybeShow());
    }

    private IEnumerator FindPanelDelayedAndMaybeShow()
    {
        yield return new WaitForEndOfFrame();

        // If this scene is excluded (main menu by build index), do not find/show the guide
        if (IsExcludedScene()) yield break;

        FindGameGuidePanelInScene();

        if (gameGuidePanel != null && showOnLevelLoad)
        {
            // Prevent auto-opening when the game isn't in Playing state (paused / ended / completed)
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
                yield break;

            ShowGuide();
        }
    }

    private void FindGameGuidePanelInScene()
    {
        gameGuidePanel = null;
        guideCanvasGroup = null;

        var found = UnityEngine.Object.FindFirstObjectByType<GameGuideReference>(FindObjectsInactive.Include);
        if (found != null)
        {
            gameGuidePanel = found.gameObject;

            // ensure CanvasGroup exists so fading works
            guideCanvasGroup = gameGuidePanel.GetComponent<CanvasGroup>();
            if (guideCanvasGroup == null)
            {
                guideCanvasGroup = gameGuidePanel.AddComponent<CanvasGroup>();
                guideCanvasGroup.alpha = 0f;
            }
        }
    }

    private void Update()
    {
        // If guide is open but game left Playing state (paused / ended / completed), force hide it
        if (isGuideOpen && GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
        {
            HideGuide();
            return;
        }

        // Only toggle if the current scene contains a GameGuide panel and the scene is not excluded
        if (!IsExcludedScene() && gameGuidePanel != null && Input.GetKeyDown(KeyCode.Tab))
        {
            // If currently open allow closing regardless of game state.
            if (isGuideOpen)
            {
                ToggleGuide();
                return;
            }

            // Prevent opening when game is paused, ended or completed
            if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
            {
                // optional: provide debug info when attempting to open while not playing
                Debug.Log("GameGuideManager: cannot open guide while game state is " + GameManager.Instance.currentState);
                return;
            }

            ToggleGuide();
        }
    }

    public void ShowGuide()
    {
        // Don't allow showing in excluded scenes
        if (IsExcludedScene()) return;

        if (gameGuidePanel == null)
        {
            FindGameGuidePanelInScene();
            if (gameGuidePanel == null)
            {
                Debug.LogWarning("GameGuide panel not found in current scene.");
                return;
            }
        }

        if (isGuideOpen) return;

        // Prevent opening while the game is not in Playing state
        if (GameManager.Instance != null && GameManager.Instance.currentState != GameState.Playing)
        {
            Debug.Log("GameGuideManager: ShowGuide aborted because game state is " + GameManager.Instance.currentState);
            return;
        }

        // Close settings if they happen to be open to avoid UI conflicts
        SettingsManager.Instance?.HideSettings();

        // preserve current timescale and pause
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        // ensure a CanvasGroup is present for fading
        guideCanvasGroup = gameGuidePanel.GetComponent<CanvasGroup>() ?? gameGuidePanel.AddComponent<CanvasGroup>();
        guideCanvasGroup.alpha = 0f;

        // cancel any existing tweens and animate in (scale + fade)
        UITweens.CancelTweens(gameGuidePanel);
        UITweens.ScaleIn(gameGuidePanel, duration: 0.45f, startScaleMultiplier: 0.98f, delay: 0f, useEstimatedTime: true, useFade: true);

        isGuideOpen = true;
    }

    public void HideGuide()
    {
        if (gameGuidePanel == null || !isGuideOpen) return;

        // cancel existing tweens and animate out (scale + fade then deactivate)
        UITweens.CancelTweens(gameGuidePanel);
        UITweens.ScaleOut(gameGuidePanel, duration: 0.25f, endScaleMultiplier: 0f, delay: 0f, useEstimatedTime: true, deactivateOnComplete: true, useFade: true);

        isGuideOpen = false;

        // restore previous time scale (if it was zero or invalid, use 1)
        Time.timeScale = (previousTimeScale > 0f) ? previousTimeScale : 1f;
    }

    public void ToggleGuide()
    {
        if (isGuideOpen) HideGuide();
        else ShowGuide();
    }

    public bool IsGuideOpen => isGuideOpen;

    private bool IsExcludedScene()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentIndex < 0) return false;
        foreach (var idx in excludedSceneBuildIndexes)
        {
            if (idx == currentIndex) return true;
        }
        return false;
    }
}