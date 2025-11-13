using TMPro;
using UnityEngine;

public class GameUIVisuals : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI restartText, nextLevelText, settingsText, restart2Text, mainMenuText, tryAgainText, settings2Text, mainMenu2Text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupHoverEffects();
    }

   private void SetupHoverEffects()
   {
        AddHoverEffect(restartText?.gameObject,1.35f);
        AddHoverEffect(nextLevelText?.gameObject, 1.35f);
        AddHoverEffect(settingsText?.gameObject, 1.35f);
        
        AddHoverEffect(restart2Text?.gameObject, 1.2f);
        AddHoverEffect(mainMenuText?.gameObject, 1.2f);

        AddHoverEffect(mainMenu2Text?.gameObject, 1.35f);
        AddHoverEffect(tryAgainText?.gameObject, 1.35f);
        AddHoverEffect(settings2Text?.gameObject, 1.35f);

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
