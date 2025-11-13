using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CreditHandler : MonoBehaviour
{
    public TextMeshProUGUI currentScoreText;
    public TextMeshProUGUI bestScoreText;
    public Button nextLevelButton;

    // Required totals to unlock next levels
    public int level2RequiredScore = 200;
    public int level3RequiredScore = 500;
    public int nightshiftRequiredScore = 500;

    private void OnEnable()
    {
        if (GameManager.Instance != null)
        {
            if (currentScoreText != null)
                currentScoreText.text = $"Your Score:{GameManager.Instance.GetCurrentScore()}";

            if (bestScoreText != null)
            {
                int bestThisLevel = GameManager.Instance.GetBestScoreForCurrentLevel();
                bestScoreText.text = $"Your Record:{bestThisLevel}";
            }
        }

        //SoundManager.Instance?.PlaySound(SoundType.LevelWinMusic, null, 0.25f);
        UpdateNextLevelButton();
    }

    private void UpdateNextLevelButton()
    {
        if (nextLevelButton == null) return;

        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        bool canPlayNext = false;

        // total score stored per scene
        int total = PlayerPrefs.GetInt($"TotalScore_{currentSceneIndex}", 0);
        if(PlayerPrefs.GetInt("DevBypassAllLevels", 0) == 1)
            canPlayNext = true;
        else if (currentSceneIndex == 3)
            canPlayNext = total >= level2RequiredScore;
        else if (currentSceneIndex == 4)
            canPlayNext = total >= level3RequiredScore;
        else if (currentSceneIndex == 5)
            canPlayNext = total >= nightshiftRequiredScore;
        else if(currentSceneIndex == 6)
        {
            canPlayNext = true;
        }
        else
        {
            nextLevelButton.interactable = false;
            return;
        }

        nextLevelButton.interactable = canPlayNext;
    }

    public void onRetryButtonClicked()
    {
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);

        // stop current music / one-shot sources before changing scenes
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLevelMusic();
            if (SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.Stop();
        }

        int id = SceneManager.GetActiveScene().buildIndex;
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadScene(id, "CrossFade", GetMusicTypeForLevel(id));
        else
            SceneManager.LoadScene(id);
    }

    public void onNextLevelButtonClicked()
    {
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);

        // stop current music / one-shot sources before changing scenes
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLevelMusic();
            if (SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.Stop();
        }

        if(SceneManager.GetActiveScene().buildIndex == 6)
        {
            if (LevelManager.Instance != null)
                LevelManager.Instance.LoadScene(8, "CrossFade", GetMusicTypeForLevel(8));
            return;
        }

        int id = SceneManager.GetActiveScene().buildIndex + 1;
        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadScene(id, "CrossFade", GetMusicTypeForLevel(id));

    }

    // go back to mainmenu
    public void onQuitButtonClicked()
    {
        int menuIndex = GameManager.LastMenuIndex;
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);

        // stop music in this level 
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLevelMusic();
            if (SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.Stop();
        }

        if (LevelManager.Instance != null)
            LevelManager.Instance.LoadScene(menuIndex, "CrossFade", SoundType.MainMenuMusic);
       
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
