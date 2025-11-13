using UnityEngine;

public static class UITweens
{
    public static LTDescr ScaleOnHover(GameObject target, float hoverScale = 1.2f, float duration = 0.3f)
    {
        if (target == null) return null;
        
        Vector3 targetScale = target.transform.localScale * hoverScale;
        return LeanTween.scale(target, targetScale, duration)
            .setEaseOutBack()
            .setUseEstimatedTime(true);  
    }

    public static LTDescr ScaleOnHoverExit(GameObject target, Vector3 originalScale, float duration = 0.3f)
    {
        if (target == null) return null;

        return LeanTween.scale(target, originalScale, duration)
            .setEaseOutBack()
            .setUseEstimatedTime(true);
    }

    public static LTDescr ScaleIn(GameObject go, float duration = 0.35f, float startScaleMultiplier = 0f, float delay = 0f, bool useEstimatedTime = true, bool useFade = true)
    {
        if (go == null) return null;

        bool wasInactive = !go.activeSelf;
        if (wasInactive) go.SetActive(true);

        Vector3 originalScale = go.transform.localScale;

        if (originalScale.sqrMagnitude <= 1e-6f)
        {
            originalScale = Vector3.one;
            go.transform.localScale = originalScale;
        }

        Vector3 startScale = originalScale * startScaleMultiplier;
        go.transform.localScale = startScale;

        CanvasGroup cg = null;
        if (useFade)
        {
            cg = go.GetComponent<CanvasGroup>() ?? go.GetComponentInChildren<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
            }
        }

        var descr = LeanTween.scale(go, originalScale, duration)
            .setEaseOutBack()
            .setDelay(delay)
            .setUseEstimatedTime(useEstimatedTime)
            .setOnComplete(() =>
            {
                if (go != null) go.transform.localScale = originalScale;
                if (cg != null) cg.alpha = 1f;
            });

        if (cg != null)
        {
            LeanTween.alphaCanvas(cg, 1f, duration)
                .setDelay(delay)
                .setUseEstimatedTime(useEstimatedTime);
        }

        return descr;
    }


    public static LTDescr ScaleOut(GameObject go, float duration = 0.25f, float endScaleMultiplier = 0f, float delay = 0f, bool useEstimatedTime = true, bool deactivateOnComplete = true, bool useFade = true)
    {
        if (go == null) return null;

        Vector3 originalScale = go.transform.localScale;
        Vector3 endScale = originalScale * endScaleMultiplier;

        CanvasGroup cg = null;
        if (useFade)
        {
            cg = go.GetComponent<CanvasGroup>() ?? go.GetComponentInChildren<CanvasGroup>();
        }

        var descr = LeanTween.scale(go, endScale, duration)
            .setEaseInBack()
            .setDelay(delay)
            .setUseEstimatedTime(useEstimatedTime)
            .setOnComplete(() =>
            {
                if (go == null) return;

                go.transform.localScale = originalScale;
                if (deactivateOnComplete) go.SetActive(false);
                if (cg != null) cg.alpha = 1f;
            });

        if (cg != null)
        {
            LeanTween.alphaCanvas(cg, 0f, duration)
                .setDelay(delay)
                .setUseEstimatedTime(useEstimatedTime);
        }

        return descr;
    }


    public static void CancelTweens(GameObject target)
    {
        if (target == null) return;
        LeanTween.cancel(target);
    }
}