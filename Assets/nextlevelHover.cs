using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class nextLevelHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Level Info")]
    public int levelIndex;                  // 1=第一关，2=第二关...
    public TextMeshProUGUI hoverText;



    [Header("Unlock Requirements")]
    public int level2RequiredScore = 200;
    public int level3RequiredScore = 500;

    int getSceneIndex(int levelindex)
    {
        int sceneIndex = 0;
        switch (levelIndex)
        {
            case 1: sceneIndex = 3; break; // Level1
            case 2: sceneIndex = 4; break; // Level2
            case 3: sceneIndex = 5; break; // Level3
            case 4: sceneIndex = 6; break; // NightShift
            case 5: sceneIndex = 7; break; // Level3D
        }
        return sceneIndex;
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverText == null) return;

        // 旁路已开：给明确提示，避免误以为“锁不回去了”
        if (PlayerPrefs.GetInt(LevelSelectorUI.DevBypassKey, 0) == 1)
        {
            hoverText.text = "<color=#00C853>Developer bypass ON — all levels unlocked</color>";
            return;
        }

        int sceneIndex = 0;

        sceneIndex = getSceneIndex(levelIndex);

        int totalThis = PlayerPrefs.GetInt($"TotalScore_{sceneIndex}", 0);

        // 第一关永远开放
      

        int required = 0;

        if (levelIndex == 1) required = level2RequiredScore;
        else if (levelIndex == 2) required = level3RequiredScore;
        else
        {
            required = 0;
        }

        if (totalThis < required)
        {
            hoverText.text = $"Complete  <color=red>Level {levelIndex}</color>  with {required} total points";
        }
        else if (levelIndex == 3)
        {
            hoverText.text = $"We refuse to work overtime, your score: {totalThis}";
        }
        else
        {
            hoverText.text = $"Your Total Score On This Level: {totalThis}";
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverText != null) hoverText.text = "";
    }
}
