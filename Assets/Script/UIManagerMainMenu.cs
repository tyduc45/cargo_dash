using UnityEngine;
using System.Collections.Generic;

public enum UIType
{
    None,
    GameOverUI,
    PauseUI,
    MainMenu,
    LevelSelectorUI,
    CreditUI,
    SettingsUI
}

public class UIManagerMainMenu : MonoBehaviour
{
    public static UIManagerMainMenu Instance;

    private Dictionary<UIType, GameObject> uiPanels = new Dictionary<UIType, GameObject>();
    private UIType currentUIType = UIType.None;
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // find all UI component , add them into dictionary in advance.
        foreach (Transform child in transform)
        {
            if (System.Enum.TryParse(child.name, out UIType type))
            {
                if (type != UIType.None && !uiPanels.ContainsKey(type))
                {
                    uiPanels.Add(type, child.gameObject);
                    child.gameObject.SetActive(false); //close UI script
                }
            }
        }
        // ✅ 启动场景默认显示主菜单
        ShowUI(UIType.MainMenu);
    }

    public void ShowUI(UIType type)
    {
        currentUIType = type;
        foreach (var kvp in uiPanels)
        {
            var panel = kvp.Value;
            // Cancel any existing tweens on this panel
            UITweens.CancelTweens(panel);

            if (kvp.Key == type)
            {
                // Activate and scale-in the requested panel
                if (!panel.activeSelf) panel.SetActive(true);
                UITweens.ScaleIn(panel, duration: 0.45f, startScaleMultiplier: 0f, delay: 0f);
            }
            else
            {
                // Scale out and deactivate other panels (if currently active)
                if (panel.activeSelf)
                {
                    UITweens.ScaleOut(panel, duration: 0.25f, endScaleMultiplier: 0f, delay: 0f);
                }
            }
        }
    }


    public UIType GetCurrentUIType()
    {
        return currentUIType;
    }
    public void HideAll()
    {
        foreach (var kvp in uiPanels)
        {
            var panel = kvp.Value;
            UITweens.CancelTweens(panel);
            if (panel.activeSelf)
                UITweens.ScaleOut(panel, duration: 0.25f, endScaleMultiplier: 0f, delay: 0f);
        }
    }



}
