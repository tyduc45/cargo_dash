using UnityEngine;
using Febucci.UI.Core;
using Febucci.UI.Effects;
using Febucci.UI;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;

public class MainMenuVisuals : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI playText, instructionsText, settingsText;

    [SerializeField] private TextMeshProUGUI level1Text, level2Text, level3Text, specialText, level3DText;

    [SerializeField] private TextMeshProUGUI backToMainMenu;

    [SerializeField]
    private List<TextAnimator_TMP> mainMenuItems;

    private List<GameObject> allTextElements = new List<GameObject>();

    private void Awake()
    {
         mainMenuItems = new List<TextAnimator_TMP>();
    }

    void Start()
    {
        
        SetupHoverEffects();
    }

    private void SetupHoverEffects()
    {
        AddHoverEffect(playText?.gameObject, 1.3f);
        AddHoverEffect(instructionsText?.gameObject, 1.3f);
        AddHoverEffect(settingsText?.gameObject, 1.3f);

        AddHoverEffect(level1Text?.gameObject, 1.3f);
        AddHoverEffect(level2Text?.gameObject, 1.3f);
        AddHoverEffect(level3Text?.gameObject, 1.3f);
        AddHoverEffect(specialText?.gameObject, 1.3f);
        AddHoverEffect(level3DText?.gameObject, 1.3f);

        AddHoverEffect(backToMainMenu?.gameObject, 1.1f);
    }



    private void AddHoverEffect(GameObject textObject, float hoverScale)
    {
        if (textObject == null) return;

        var hoverHandler = textObject.GetComponent<UIHoverHandler>();
        if (hoverHandler == null)
        {
            hoverHandler = textObject.AddComponent<UIHoverHandler>();
        }

        hoverHandler.hoverScale = hoverScale;
        hoverHandler.originalScale = textObject.transform.localScale;
    }

}
