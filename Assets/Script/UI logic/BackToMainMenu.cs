using UnityEngine;

public class BackToMainMenu : MonoBehaviour
{
    public void GoToMainMenu()
    {
        if (UIManagerMainMenu.Instance != null)
            UIManagerMainMenu.Instance.ShowUI(UIType.MainMenu);
    }
}