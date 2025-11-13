using UnityEngine;
using UnityEngine.UI;

public class SettingsUIHandler : MonoBehaviour
{
    [Header("Volume Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider ambientVolumeSlider;
    
    [Header("Mute Toggle")]
    public Toggle muteToggle;
    
    [Header("Navigation")]
    public Button backButton;

    [Header("Section Buttons")]
    public Button settingsButton;
    public Button howToPlayButton;
    public Button creditsButton;

    [Header("Panels")]
    public GameObject settingsPanel;   
    public GameObject howToPlayPanel;
    public GameObject creditsPanel;

    private enum PanelType { Settings, HowToPlay, Credits }

    private void Start()
    {
        SetupUI();
        LoadCurrentSettings();


       // ShowPanel(PanelType.Settings);
    }

    private void SetupUI()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
        if (ambientVolumeSlider != null)
            ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);
            
        if (muteToggle != null)
            muteToggle.onValueChanged.AddListener(OnMuteToggled);
            
        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);

     
        if (settingsButton != null)
            settingsButton.onClick.AddListener(() => OpenSettingsPanel());
        if (howToPlayButton != null)
            howToPlayButton.onClick.AddListener(() => OpenHowToPlayPanel());
        if (creditsButton != null)
            creditsButton.onClick.AddListener(() => OpenCreditsPanel());
    }

    private void LoadCurrentSettings()
    {
        if (SoundManager.Instance == null) return;
        
        var settings = SoundManager.Instance.soundSettings;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.SetValueWithoutNotify(settings.musicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(settings.sfxVolume);

        if (ambientVolumeSlider != null)
            ambientVolumeSlider.SetValueWithoutNotify(settings.ambientVolume);

        if (muteToggle != null)
            muteToggle.SetIsOnWithoutNotify(settings.isMuted);
    }


    public void OpenSettingsPanel()
    {
        Debug.Log("open panel called");
        SettingsManager.Instance?.ShowSettings();
        ShowPanel(PanelType.Settings);
    }

    public void OpenHowToPlayPanel()
    {

        Debug.Log("OpenHowToPlayPanel called");
        SettingsManager.Instance?.ShowSettings();
        ShowPanel(PanelType.HowToPlay);
    }

    public void OpenCreditsPanel()
    {
        
       // SettingsManager.Instance?.ShowSettings();
        ShowPanel(PanelType.Credits);
    }

    private void ShowPanel(PanelType panel)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);

        Debug.Log($"ShowPanel: {panel} (settingsPanel assigned: {(settingsPanel != null)})");
        //SettingsManager.Instance?.ShowSettings();

        // toggles pecific child panels (only the selected one is active)
        if (settingsPanel != null)
            settingsPanel.SetActive(panel == PanelType.Settings);
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(panel == PanelType.HowToPlay);
        if (creditsPanel != null)
            creditsPanel.SetActive(panel == PanelType.Credits);

       
        if (settingsButton != null) settingsButton.interactable = (panel != PanelType.Settings);
        if (howToPlayButton != null) howToPlayButton.interactable = (panel != PanelType.HowToPlay);
        if (creditsButton != null) creditsButton.interactable = (panel != PanelType.Credits);
    }

    public void OnMasterVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetMasterVolume(value);
    }

    public void OnMusicVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetMusicVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetSFXVolume(value);
    }

    public void OnAmbientVolumeChanged(float value)
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.SetAmbientVolume(value);
    }

    public void OnMuteToggled(bool isMuted)
    {
        Debug.Log($"Mute toggled to: {isMuted}");
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.soundSettings.isMuted = isMuted;
            SoundManager.Instance.SetMuted(isMuted);
        }
    }

    public void OnBackButtonClicked()
    {
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
            
        // Close settings instead of going to main menu
        SettingsManager.Instance?.HideSettings();
    }
}