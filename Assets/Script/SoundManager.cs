using System.Collections.Generic;
using UnityEngine;

public enum SoundCategory
{
    SFX,
    Music,
    Ambient
}

[RequireComponent(typeof(AudioSource))]
public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] soundlist;
    public static SoundManager Instance { get; private set; }
    
    [Header("Audio Sources")]
    public AudioSource audioSource;
    [SerializeField] private AudioSource footStepSource;
    [SerializeField] private AudioSource warningAudioSource;
    [SerializeField] private AudioSource birdAudioSource;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioSource alarmAudioSource;

    [SerializeField] private bool alarmWasPlayingBeforePause;
    [SerializeField] private bool warningWasPlayingBeforePause;

    [Header("Sound Settings")]
    public SoundSettings soundSettings = new SoundSettings();

    private bool musicWasPlayingBeforePause = false;
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            soundSettings.LoadSettings();
            ApplyVolumeSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void ApplyVolumeSettings()
    {
        float masterVol = soundSettings.isMuted ? 0f : soundSettings.masterVolume;
        
        if (musicAudioSource != null)
            musicAudioSource.volume = masterVol * soundSettings.musicVolume;
        
        if (audioSource != null)
            audioSource.volume = masterVol * soundSettings.sfxVolume;
        
        if (footStepSource != null)
            footStepSource.volume = masterVol * soundSettings.sfxVolume;
            
        if (warningAudioSource != null)
            warningAudioSource.volume = masterVol * soundSettings.ambientVolume;
            
        if (birdAudioSource != null)
            birdAudioSource.volume = masterVol * soundSettings.sfxVolume;
            
        if (alarmAudioSource != null)
            alarmAudioSource.volume = masterVol * soundSettings.ambientVolume;
    }

    public void PlaySound(SoundType soundType, AudioClip clip = null, float volume = 1.0f)
    {
        if (soundSettings.isMuted) return;

        AudioClip toPlay = clip;
        int index = (int)soundType;

        if (toPlay == null)
        {
            if (soundlist == null || index < 0 || index >= soundlist.Length) return;
            toPlay = soundlist[index];
            if (toPlay == null) return;
        }

        AudioSource target = audioSource;
        SoundCategory category = GetCategory(soundType);
        float categoryVol = soundSettings.sfxVolume;

        switch (category)
        {
            // ADDITION: log stack trace so you can find who requested music


            case SoundCategory.Music:
                if (musicAudioSource == null) return;
                Debug.Log($"PlaySound: Music requested -> SoundType={soundType} (index={index}), Clip={(toPlay != null ? toPlay.name : "null")}");
                Debug.Log("PlaySound: caller stack:\n" + System.Environment.StackTrace);
                musicAudioSource.Stop();
                musicAudioSource.clip = toPlay;
                musicAudioSource.volume = soundSettings.isMuted ? 0f : volume * soundSettings.masterVolume * soundSettings.musicVolume;
                musicAudioSource.loop = true;
                musicAudioSource.Play();
                return;

            case SoundCategory.Ambient:
                target = warningAudioSource ?? alarmAudioSource ?? audioSource;
                categoryVol = soundSettings.ambientVolume;
                break;

            case SoundCategory.SFX:
            default:
                if (soundType == SoundType.Footstep && footStepSource != null) target = footStepSource;
                else if ((soundType == SoundType.BirdMove || soundType == SoundType.BirdExplode) && birdAudioSource != null) target = birdAudioSource;
                categoryVol = soundSettings.sfxVolume;
                break;
        }

        if (target == null) return;

        float finalVolume = soundSettings.isMuted ? 0f : volume * soundSettings.masterVolume * categoryVol;
        target.PlayOneShot(toPlay, finalVolume);
    }

    

    public void StartAlarmLoop(SoundType soundType, float volume = 1.0f)
    {
        if (alarmAudioSource != null && soundlist[(int)soundType] != null)
        {
            alarmAudioSource.clip = soundlist[(int)soundType];
            float finalVolume = soundSettings.isMuted ? 0f : volume * soundSettings.masterVolume * soundSettings.ambientVolume;
            alarmAudioSource.volume = finalVolume;
            alarmAudioSource.loop = true;
            alarmAudioSource.Play();
        }
    }

    public void StartWarningLoop(SoundType soundType, float volume = 1.0f)
    {
        if (warningAudioSource != null && soundlist[(int)soundType] != null)
        {
            warningAudioSource.clip = soundlist[(int)soundType];
            float finalVolume = soundSettings.isMuted ? 0f : volume * soundSettings.masterVolume * soundSettings.ambientVolume;
            warningAudioSource.volume = finalVolume;
            warningAudioSource.loop = true;
            warningAudioSource.Play();
        }
    }

    public void SetMasterVolume(float volume)
    {
        soundSettings.masterVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        soundSettings.SaveSettings();
    }

    public void SetMusicVolume(float volume)
    {
        soundSettings.musicVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        soundSettings.SaveSettings();
    }

    public void SetSFXVolume(float volume)
    {
        soundSettings.sfxVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        soundSettings.SaveSettings();
    }

    public void SetAmbientVolume(float volume)
    {
        soundSettings.ambientVolume = Mathf.Clamp01(volume);
        ApplyVolumeSettings();
        soundSettings.SaveSettings();
    }

    public void ToggleMute()
    {
        soundSettings.isMuted = !soundSettings.isMuted;
        Debug.Log($"Sound muted: {soundSettings.isMuted}");
        ApplyVolumeSettings();
        soundSettings.SaveSettings();
    }
    
    public void SetMuted(bool muted)
    {
        soundSettings.isMuted = muted;
        Debug.Log($"Sound muted set to: {soundSettings.isMuted}");
        ApplyVolumeSettings();
        soundSettings.SaveSettings();
    }


    public void StopWarningLoop()
    {
        if (warningAudioSource != null)
        {
            warningAudioSource.Stop();
            warningAudioSource.loop = false;
        }
    }

    public void StopAlarmLoop()
    {
        if (alarmAudioSource != null)
        {
            alarmAudioSource.Stop();
            alarmAudioSource.loop = false;
        }
    }

    public void StopLevelMusic()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
            musicAudioSource.loop = false;
        }
    }

    public void FadeOutMusic(float duration)
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            float startVolume = musicAudioSource.volume;
            LeanTween.value(startVolume, 0f, duration)
                .setOnUpdate((float vol) => {
                    if (musicAudioSource != null)
                        musicAudioSource.volume = vol;
                })
                .setIgnoreTimeScale(true)
                .setOnComplete(() => {
                    if (musicAudioSource != null)
                        musicAudioSource.Stop();
                });
        }
    }

    public void PauseAmbientLoops()
    {
        if (alarmAudioSource != null)
        {
            alarmWasPlayingBeforePause = alarmAudioSource.isPlaying;
            if (alarmWasPlayingBeforePause)
                alarmAudioSource.Pause();
        }

        if (warningAudioSource != null)
        {
            warningWasPlayingBeforePause = warningAudioSource.isPlaying;
            if (warningWasPlayingBeforePause)
                warningAudioSource.Pause();
        }
    }

    public void ResumeAmbientLoops()
    {
        if (alarmAudioSource != null)
        {
            if (alarmWasPlayingBeforePause && alarmAudioSource.clip != null && !alarmAudioSource.isPlaying)
                alarmAudioSource.UnPause();
            alarmWasPlayingBeforePause = false;
        }

        if (warningAudioSource != null)
        {
            if (warningWasPlayingBeforePause && warningAudioSource.clip != null && !warningAudioSource.isPlaying)
                warningAudioSource.UnPause();
            warningWasPlayingBeforePause = false;
        }
    }


    public void PlayFootstepSound(AudioClip clip, float volume = 1.0f)
    {
        if (soundSettings.isMuted) return;
        float finalVolume = volume * soundSettings.masterVolume * soundSettings.sfxVolume;
        footStepSource.PlayOneShot(clip, finalVolume);
    }

    public void PlayWarningSound(AudioClip clip, float volume = 1.0f)
    {
        if (soundSettings.isMuted) return;
        float finalVolume = volume * soundSettings.masterVolume * soundSettings.ambientVolume;
        warningAudioSource.PlayOneShot(clip, finalVolume);
    }

    public void PlayBirdSound(AudioClip clip, float volume = 1.0f)
    {
        if (soundSettings.isMuted) return;
        float finalVolume = volume * soundSettings.masterVolume * soundSettings.sfxVolume;
        birdAudioSource.PlayOneShot(clip, finalVolume);
    }

    public void PauseLevelMusic()
    {
        if (musicAudioSource == null) return;

        // remember whether music was playing and pause (preserves position)
        musicWasPlayingBeforePause = musicAudioSource.isPlaying;
        if (musicWasPlayingBeforePause)
        {
            musicAudioSource.Pause();
        }
    }

    public void ResumeLevelMusic()
    {
        if (musicAudioSource == null) return;

        // if there is a clip and it was playing before pause, unpause it to resume from same position
        if (musicAudioSource.clip != null && musicWasPlayingBeforePause && !musicAudioSource.isPlaying)
        {
            musicAudioSource.UnPause();
        }

        // reset flag
        musicWasPlayingBeforePause = false;
    }

    public AudioSource FoostepAudioSource() => footStepSource;
    public AudioSource BirdAudioSource() => birdAudioSource;

    public void DebugPrintPlayingClips()
    {
        Debug.Log("====== 当前正在播放的音频 ======");
        PrintIfPlaying(audioSource, "audioSource");
        PrintIfPlaying(musicAudioSource, "musicAudioSource");
        PrintIfPlaying(warningAudioSource, "warningAudioSource");
        PrintIfPlaying(alarmAudioSource, "alarmAudioSource");
        PrintIfPlaying(footStepSource, "footStepSource");
        PrintIfPlaying(birdAudioSource, "birdAudioSource");
        Debug.Log("============================");
    }

    private void PrintIfPlaying(AudioSource src, string name)
    {
        if (src != null && src.isPlaying)
        {
            string clipName = src.clip != null ? src.clip.name : "(无 clip)";
            Debug.Log($"[{name}] 正在播放: {clipName}");
        }
    }

    public void StopAllActiveAudio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
        }

        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
            musicAudioSource.loop = false;
        }

        if (alarmAudioSource != null)
        {
            alarmAudioSource.Stop();
            alarmAudioSource.loop = false;
        }

        if (warningAudioSource != null)
        {
            warningAudioSource.Stop();
            warningAudioSource.loop = false;
        }

        if (footStepSource != null)
        {
            footStepSource.Stop();
        }

        if (birdAudioSource != null)
        {
            birdAudioSource.Stop();
        }
    }

    private static readonly Dictionary<SoundType, SoundCategory> soundTypeToCategory = new Dictionary<SoundType, SoundCategory>
    {

        { SoundType.Jump, SoundCategory.SFX },
        { SoundType.Jumpland, SoundCategory.SFX },
        { SoundType.Footstep, SoundCategory.SFX },
        { SoundType.Pickup, SoundCategory.SFX },
        { SoundType.Throw, SoundCategory.SFX },
        { SoundType.Interact, SoundCategory.SFX },
        { SoundType.WrongReciever, SoundCategory.SFX },
        { SoundType.RightReciever, SoundCategory.SFX },
        { SoundType.CargoSpawn, SoundCategory.SFX },
        { SoundType.CargoBounce, SoundCategory.SFX },
        { SoundType.CargoBreak, SoundCategory.SFX },
        { SoundType.WaterDropletSpawn, SoundCategory.SFX },
        { SoundType.WaterDropletLand, SoundCategory.SFX },
        { SoundType.WaterSplashPlayer, SoundCategory.SFX },
        { SoundType.BirdMove, SoundCategory.SFX },
        { SoundType.PlayerHurt, SoundCategory.SFX },
        { SoundType.BirdExplode, SoundCategory.SFX },
        { SoundType.UIClick, SoundCategory.SFX },
        { SoundType.LevelSuccess, SoundCategory.SFX },
        { SoundType.LevelFailed, SoundCategory.SFX },
        { SoundType.IsolationWallWarning, SoundCategory.SFX },
        { SoundType.IsolationWallDrop, SoundCategory.SFX},

        // Ambient / looping warnings
        { SoundType.BackLogAlarm, SoundCategory.Ambient },

        // Music
        { SoundType.MainMenuMusic, SoundCategory.Music },
        { SoundType.Level1Music, SoundCategory.Music },
        { SoundType.Level2Music, SoundCategory.Music },
        { SoundType.Level3Music, SoundCategory.Music },
        { SoundType.Level3DMusic, SoundCategory.Music},
        { SoundType.LevelWinMusic, SoundCategory.Music },
        { SoundType.LevelFailedMusic, SoundCategory.Music },
        { SoundType.PauseMusic, SoundCategory.Music }
    };

    public SoundCategory GetCategory(SoundType type)
    {
        return soundTypeToCategory.TryGetValue(type, out var cat) ? cat : SoundCategory.SFX;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            DebugPrintPlayingClips();
        }
    }

    public AudioClip GetSoundClip(SoundType type)
    {
        int index = (int)type;
        if (soundlist != null && index >= 0 && index < soundlist.Length)
            return soundlist[index];
        return null;
    }
}


public enum SoundType
{
    Jump,
    Jumpland,
    Footstep,
    Pickup,
    Throw,
    Interact,
    WrongReciever,
    RightReciever,
    CargoSpawn,        // For cargo instantiation
    CargoBounce,       // For bouncy cargo
    CargoBreak,
    WaterDropletSpawn,
    WaterDropletLand,
    WaterSplashPlayer,
    IsolationWallWarning,
    IsolationWallDrop,
    BirdMove,
    PlayerHurt,
    BackLogAlarm,
    UIClick,
    MainMenuMusic,
    Level1Music,
    Level2Music,
    Level3Music,
    BirdExplode,
    LevelSuccess,
    LevelFailed,
    LevelFailedMusic,
    LevelWinMusic,
    PauseMusic,
    Level3DMusic
}