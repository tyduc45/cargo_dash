using System.Collections;
using UnityEngine;

public class CrossFade : SceneTransition
{
    public CanvasGroup crossfade;

    public override IEnumerator AnimatorTransitionIn()
    {
        var tween = crossfade.LeanAlpha(1f, 1f).setIgnoreTimeScale(true);
        yield return new WaitWhile(() => LeanTween.isTweening(tween.id));
    }
    public override IEnumerator AnimatorTransitionOut()
    {
        var tween =crossfade.LeanAlpha(0f, 1f).setIgnoreTimeScale(true);
        yield return new WaitWhile(() => LeanTween.isTweening(tween.id));
    }
   
}
