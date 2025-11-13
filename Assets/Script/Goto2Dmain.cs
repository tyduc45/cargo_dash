using UnityEngine;

public class Goto2Dmain : MonoBehaviour
{
    private void OnMouseDown()
    {
        if (LevelManager.Instance == null)
        {
            Debug.LogError("LevelManager not found!");
            return;
        }

        SoundManager.Instance?.PlaySound(SoundType.UIClick, null, 0.45f);
        SoundManager.Instance?.FadeOutMusic(0.3f);

        LevelManager.Instance.LoadScene(2, "CrossFade", SoundType.MainMenuMusic);
    }
}