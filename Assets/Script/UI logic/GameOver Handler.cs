using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUIHandler : MonoBehaviour
{

    private void OnEnable()
    {
        SoundManager.Instance?.PlaySound(SoundType.LevelFailedMusic, null, 0.25f);
    }
    public void OnRestartButtonClicked()
    {
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);

        
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLevelMusic();
            if (SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.Stop();
        }

        int currentSceneId = SceneManager.GetActiveScene().buildIndex;

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(currentSceneId, "CrossFade", GetMusicTypeForLevel(currentSceneId));
        }

    }

    public void OnQuitButtonClicked()
    {
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);
        int menuIndex = GameManager.LastMenuIndex;

        // stop music in this level
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopLevelMusic();
            if (SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.Stop();
        }

        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(menuIndex, "CrossFade", SoundType.MainMenuMusic);
        }

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
