using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Settings UI")]
    public GameObject settingsPanel;
    public SettingsUIHandler settingsUIHandler;

    private bool isSettingsOpen = false;
    private UIType previousUIState = UIType.None;

    private void Awake()
    {
    
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        FindSettingsPanelInScene();
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
 
        isSettingsOpen = false;
        settingsPanel = null;
        settingsUIHandler = null;

        StartCoroutine(FindSettingsPanelDelayed());
    }

    private System.Collections.IEnumerator FindSettingsPanelDelayed()
    {

        yield return new WaitForEndOfFrame();
        FindSettingsPanelInScene();
    }

    private void FindSettingsPanelInScene()
    {
        settingsPanel = null;
        settingsUIHandler = null;
        var found = UnityEngine.Object.FindFirstObjectByType<SettingsRefrence>(FindObjectsInactive.Include);
        if (found != null)
        {
            settingsPanel = found.gameObject;
            settingsUIHandler = settingsPanel.GetComponent<SettingsUIHandler>();

        }
        else
        {
            Debug.LogWarning($"SettingsRefrence not found in scene: {SceneManager.GetActiveScene().name}");
        }

    }

    public void ShowSettings()
    {
        Debug.Log($"ShowSettings called. isSettingsOpen: {isSettingsOpen}");

        if (settingsPanel == null)
        {
            Debug.LogWarning("settingsPanel is NULL at ShowSettings ?attempting to find it in scene.");
            FindSettingsPanelInScene();

            if (settingsPanel == null)
            {
                Debug.LogError("Settings panel not found");
                return;
            }
        }

        if (!isSettingsOpen)
        {
            Debug.Log("About to show settings panel");

            if (UIManagerMainMenu.Instance != null)
            {
                Debug.Log("UIManagerMainMenu found, getting current UI state");
                previousUIState = GetCurrentUIState();
                Debug.Log($"Previous UI state: {previousUIState}");
                UIManagerMainMenu.Instance.HideAll();
                Debug.Log("Called HideAll()");
            }
            else
            {
                Debug.Log("UIManagerMainMenu.Instance is NULL (ok if not in main menu scene)");
            }

            settingsPanel.SetActive(true);
            Debug.Log("Settings panel activated");
            isSettingsOpen = true;
           // Time.timeScale = 0f;
            Debug.Log("Settings shown successfully");
        }
    }

    public void HideSettings()
    {
        if (settingsPanel != null && isSettingsOpen)
        {
            settingsPanel.SetActive(false);
            isSettingsOpen = false;
           // Time.timeScale = 1f;

            // Restore the previous UI
            if (UIManagerMainMenu.Instance != null && previousUIState != UIType.None)
            {
                UIManagerMainMenu.Instance.ShowUI(previousUIState);
            }
        }
    }

    public void ToggleSettings()
    {
        if (isSettingsOpen)
            HideSettings();
        else
            ShowSettings();
    }

    private UIType GetCurrentUIState()
    {
        if (UIManagerMainMenu.Instance == null) return UIType.None;
        return UIManagerMainMenu.Instance.GetCurrentUIType();
    }

    public bool IsSettingsOpen => isSettingsOpen;
}