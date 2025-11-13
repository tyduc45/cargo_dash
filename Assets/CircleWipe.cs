using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CircleWipe : SceneTransition
{
    public Image circle;



    public override IEnumerator AnimatorTransitionIn()
    {
        circle.rectTransform.anchoredPosition = new Vector2(-1000f, 0f);
        LTDescr tweener = LeanTween.moveX(circle.rectTransform, 0f, 1f);
        while (LeanTween.isTweening(tweener.id))
        {
            yield return null;
        }
    }

    public override IEnumerator AnimatorTransitionOut()
    {
        LTDescr tweener = LeanTween.moveX(circle.rectTransform, 1000f, 1f);
        while (LeanTween.isTweening(tweener.id))
        {
            yield return null;
        }
    }
}
