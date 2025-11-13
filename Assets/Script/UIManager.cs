using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

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
    }

    public void ShowUI(UIType type)
    {
        currentUIType = type;
        foreach (var kvp in uiPanels)
        {
            var panel = kvp.Value;

            UITweens.CancelTweens(panel);

            if (kvp.Key == type)
            {
   
                if (!panel.activeSelf) panel.SetActive(true);
                UITweens.ScaleIn(panel, duration: 0.45f, startScaleMultiplier: 0f, delay: 0f);
            }
            else
            {
                // Scale out and deactivate other panels (if currently active)
                if (panel.activeSelf)
                {
                    UITweens.ScaleOut(panel, duration: 0.25f, endScaleMultiplier: 0f, delay: 0f,true);
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
                UITweens.ScaleOut(panel, duration: 0.25f, endScaleMultiplier: 0f, delay: 0f,true);
        }
    }
}
