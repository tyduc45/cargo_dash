using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseLogic : MonoBehaviour
{
    [SerializeField]
    private DispalyText  dispalyText;

    private void OnEnable()
    {
       //Pause the current level music
        SoundManager.Instance?.PauseLevelMusic();
        SoundManager.Instance?.PauseAmbientLoops();
        // Play pause menu music on the main audioSource (not musicAudioSource)
        if (SoundManager.Instance != null)
        {
            AudioClip pauseClip = SoundManager.Instance.GetSoundClip(SoundType.PauseMusic);
            if (pauseClip != null && SoundManager.Instance.audioSource != null)
            {
                SoundManager.Instance.audioSource.clip = pauseClip;
                SoundManager.Instance.audioSource.loop = true;
                SoundManager.Instance.audioSource.volume = 0.25f * SoundManager.Instance.soundSettings.masterVolume * SoundManager.Instance.soundSettings.sfxVolume;
                SoundManager.Instance.audioSource.Play();
            }
        }

        // Show a random pause-fact if a DispalyText exists in scene
       dispalyText.ShowRandomMessage();
    }

    private void OnDisable()
    {
        // Stop pause menu music
        if (SoundManager.Instance != null && SoundManager.Instance.audioSource != null)
        {
            SoundManager.Instance.audioSource.Stop();
            SoundManager.Instance.audioSource.loop = false;
        }
        // Resume the level music from where it was paused
        SoundManager.Instance?.ResumeLevelMusic();
        SoundManager.Instance?.ResumeAmbientLoops();

        dispalyText.Hide();
    }

    /// Return to main menu
    public void OnReturnToMainMenuClicked()
    {
        int menuIndex = GameManager.LastMenuIndex;
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);

        // stop level music and any one-shot sources
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllActiveAudio();
            if (SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.Stop();
        }

      
        Time.timeScale = 1f;


        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(menuIndex, "CrossFade", SoundType.MainMenuMusic);
        }
      
    }

    /// Retry current level
    public void OnTryAgainClicked()
    {
      
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);

    
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.StopAllActiveAudio();
            if (SoundManager.Instance.audioSource != null)
                SoundManager.Instance.audioSource.Stop();
        }

      
        Time.timeScale = 1f;

        int id = SceneManager.GetActiveScene().buildIndex;

    
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.LoadScene(id, "CrossFade", GetMusicTypeForLevel(id));
        }
       
    }

    public void OnSettingsClicked()
    {
   
        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.5f);

     
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ShowSettings();
            SettingsManager.Instance.settingsUIHandler?.OpenSettingsPanel();
            Debug.Log("OnSettingsClicked: Opened settings via SettingsManager.");
        }
        else
        {
            Debug.LogWarning("OnSettingsClicked: SettingsManager.Instance is null. Ensure SettingsManager is present in the scene and its Awake sets Instance.");
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
