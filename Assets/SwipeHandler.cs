using System;
using UnityEngine;
using UnityEngine.UI;

public class SwipeHandler : MonoBehaviour
{
    [SerializeField]  int maxPage;
    int currentPage;

    Vector3 targetPos;

    [SerializeField] RectTransform levelPagesRect;
    [SerializeField] Vector3 pageStep;
    [SerializeField] float tweenTime;
    [SerializeField] LeanTweenType tweenType;

    [SerializeField]
    private Button nextbutton, prevbutton;


    private void Start()
    {
        targetPos = levelPagesRect.localPosition;
        currentPage = 1;
        UpdateButtons();
    }


    public void Next()
    {
        if(currentPage < maxPage)
        {
            Debug.Log(" next button pressed");
            currentPage++;
            targetPos += pageStep;
            MovePage();
            UpdateButtons();
            SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
        }

    }

    public void Previous()
    {
        if(currentPage > 1)
        { 
            currentPage--;
            targetPos -= pageStep;
            MovePage();
            UpdateButtons();
            SoundManager.Instance.PlaySound(SoundType.UIClick, null, 0.5f);
        }
    }

    private void MovePage()
    {
       levelPagesRect.LeanMoveLocal(targetPos, tweenTime).setEase(tweenType).setIgnoreTimeScale(true);
    }

    private void UpdateButtons()
    {
        if (prevbutton != null)
            prevbutton.interactable = (currentPage > 1);

        if (nextbutton != null)
            nextbutton.interactable = (currentPage < maxPage);
    }
}
