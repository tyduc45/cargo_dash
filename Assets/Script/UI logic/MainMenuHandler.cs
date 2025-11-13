using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuHandler : MonoBehaviour
{
    private void Start()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (SoundManager.Instance != null && (currentIndex == 2 || currentIndex == 0))
        {
            SoundManager.Instance.PlaySound(SoundType.MainMenuMusic, null, 0.25f);
        }
    }

    public void onStartButtonClicked()
    {
        UIManagerMainMenu.Instance.ShowUI(UIType.LevelSelectorUI);
        SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
    }

    public void onStartButtonClicked3D()
    {
        SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
        LevelManager.Instance.LoadScene(1,"CrossFade",SoundType.PauseMusic);
    }

    public void onSettingsButtonClicked()
    {
        UIManagerMainMenu.Instance.ShowUI(UIType.SettingsUI);
        SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
    }
    
    public void onExitButtonClicked()
    {
        SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
        Application.Quit();
    }
    public void OnBackButtonClicked3D()
    {
        SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
        LevelManager.Instance.LoadScene(0, "CrossFade", SoundType.MainMenuMusic);
    }
    public void OnBackButtonClicked()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
        
        // Check which UI system we're using
        if (UIManagerMainMenu.Instance != null)
        {
            // We're in main menu - go back to main menu
            UIManagerMainMenu.Instance.ShowUI(UIType.MainMenu);
        }
        else if (UIManager.Instance != null)
        {
            // We're in game - close settings and resume
            UIManager.Instance.HideAll();
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameResume();
        }
    }
}
