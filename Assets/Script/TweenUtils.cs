using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;


public static class TweenUtils
{
    public static LTSeq ScaleInPopOutEffect(GameObject go, float duration = 0.3f, float delay = 0f)
    {
        if (go == null) return null;
        go.transform.localScale = Vector3.zero;
        var seq = LeanTween.sequence();
        seq.append(delay);
        seq.append(LeanTween.scale(go, Vector3.one, duration).setEaseOutBack());
        return seq;
    }

    // Add complete pop-in-pop-out effect for deliveries
    public static LTSeq PopInPopOutEffect(GameObject go, float scaleInDuration = 0.3f, float holdDuration = 0.5f, float scaleOutDuration = 0.3f, float delay = 0f)
    {
        if (go == null) return null;
        go.transform.localScale = Vector3.zero;
        var seq = LeanTween.sequence();
        seq.append(delay);
        seq.append(LeanTween.scale(go, Vector3.one * 1.2f, scaleInDuration).setEaseOutBack());
        seq.append(holdDuration);
        seq.append(LeanTween.scale(go, Vector3.zero, scaleOutDuration).setEaseInBack());
        return seq;
    }

    //  Success delivery effect - scale up and fade
    public static LTSeq SuccessDeliveryEffect(GameObject go, float duration = 0.6f, float delay = 0f)
    {
        if (go == null) return null;
        go.transform.localScale = Vector3.one;
        var seq = LeanTween.sequence();
        seq.append(delay);
        seq.append(LeanTween.scale(go, Vector3.one * 1.5f, duration * 0.4f).setEaseOutBack());
        seq.append(LeanTween.alpha(go, 0f, duration * 0.6f).setEaseInQuad());
        return seq;
    }

    //  Combo delivery effect - bouncy scale 
    public static LTSeq ComboDeliveryEffect(GameObject go, float bounceHeight = 1f, float duration = 0.8f, float delay = 0f)
    {
        if (go == null) return null;
        Vector3 startPos = go.transform.position;
        var seq = LeanTween.sequence();
        seq.append(delay);
        seq.append(LeanTween.moveY(go, startPos.y + bounceHeight, duration * 0.5f).setEaseOutQuart());
        seq.append(LeanTween.scale(go, Vector3.zero, duration * 0.5f).setEaseInBack());
        return seq;
    }

    //  Wrong delivery shake effect
    public static LTSeq WrongDeliveryShake(GameObject go, float intensity = 0.5f, float duration = 0.5f)
    {
        if (go == null) return null;
        Vector3 originalPos = go.transform.position;
        var seq = LeanTween.sequence();
        seq.append(LeanTween.moveX(go, originalPos.x + intensity, duration * 0.1f).setEaseShake().setLoopPingPong(5));
        seq.append(() => go.transform.position = originalPos); // Reset position
        return seq;
    }

    public static LTSeq ReceiverPopEffect(GameObject go, float squashOffset = -0.15f, float squashDuration = 0.1f, float bounceOffset = 0.25f, float bounceDuration = 0.15f, float returnDuration = 0.1f)
    {
        if (go == null) return null;

        Vector3 originalPosition = go.transform.position;
        Vector3 squashPosition = new Vector3(originalPosition.x, originalPosition.y + squashOffset, originalPosition.z);
        Vector3 bouncePosition = new Vector3(originalPosition.x, originalPosition.y + bounceOffset, originalPosition.z);

        var seq = LeanTween.sequence();

        // Move down on Y-axis (squash effect)
        seq.append(LeanTween.moveY(go, squashPosition.y, squashDuration).setEaseOutQuart());

        // Move up beyond original position (bounce effect)
        seq.append(LeanTween.moveY(go, bouncePosition.y, bounceDuration).setEaseOutBack());

        // Return to original position
        seq.append(LeanTween.moveY(go, originalPosition.y, returnDuration).setEaseOutQuart());

        return seq;
    }

    //  Fade and scale effect for item disappearance
    public static LTSeq FadeOutEffect(GameObject go, float duration = 0.4f, float delay = 0f)
    {
        if (go == null) return null;
        var seq = LeanTween.sequence();
        seq.append(delay);
        seq.append(LeanTween.alpha(go, 0f, duration).setEaseInQuad());
        seq.append(LeanTween.scale(go, Vector3.one * 0.1f, duration * 0.5f).setEaseInBack());
        return seq;
    }

    public static LTSeq BreakEffect(GameObject go, float duration = 0.4f, float scaleUp = 1.3f, float fadeDelay = 0.1f)
    {
        if (go == null) return null;

        // 找 SpriteRenderer 做透明度动画
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        if (sr == null) return null;

        var seq = LeanTween.sequence();
        // 放大
        seq.append(LeanTween.scale(go, Vector3.one * scaleUp, duration * 0.4f).setEaseOutBack());
        // 延迟后淡出并缩小
        seq.append(fadeDelay);
        seq.append(LeanTween.value(go, 1f, 0f, duration * 0.6f).setOnUpdate((float a) =>
        {
            if (sr != null)
            {
                var c = sr.color;
                c.a = a;
                sr.color = c;
            }
        }));
        seq.append(LeanTween.scale(go, Vector3.zero, duration * 0.6f).setEaseInBack());
        return seq;
    }

    public static void ShowStatusText(string text, Vector3 worldPosition, float duration = 1.5f, GradientType gradientType = GradientType.Hit)
    {
        //Debug.Log($"ShowStatusText called with text: '{text}'");
        // Create temporary text object
        GameObject textGO = new GameObject("StatusText");
        var textMeshPro = textGO.AddComponent<TextMeshPro>();
       
        textMeshPro.fontSize = 15;
        textMeshPro.alignment = TextAlignmentOptions.Center;

        textMeshPro.text = text;


        textMeshPro.ForceMeshUpdate();

        if (EventFont.Instance != null)
        {
            
            if (EventFont.Instance.eventFont != null)
            {
                textMeshPro.font = EventFont.Instance.eventFont;
            }

           
            if (EventFont.Instance.GetGradient(gradientType) != null)
            {
                textMeshPro.enableVertexGradient = true;
            }
            EventFont.Instance.ApplyGradient(textMeshPro, gradientType);
            EventFont.Instance.ApplyFont(textMeshPro, gradientType);
            textMeshPro.ForceMeshUpdate();
        }
        else
        {
           
            textMeshPro.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        }

        textMeshPro.text = text;

        // Position above the target
        textGO.transform.position = worldPosition + Vector3.up * 1.5f;
        textGO.transform.localScale = Vector3.zero;

        // Animate the text
        var seq = LeanTween.sequence();
        // Scale in
        seq.append(LeanTween.scale(textGO, Vector3.one, 0.3f).setEaseOutBack());
        // Move up while holding
        seq.append(LeanTween.moveY(textGO, textGO.transform.position.y + 0.5f, duration * 0.8f).setEaseOutQuart());
        // Fade out
        seq.append(LeanTween.value(textGO, 1f, 0f, duration * 0.2f).setOnUpdate((float a) =>
        {
            if (textMeshPro != null)
            {
                var c = textMeshPro.color;
                c.a = a;
                textMeshPro.color = c;
            }
        }));
        
        seq.append(() => { if (textGO != null) Object.Destroy(textGO); });
    }

    public static void ShowStunText(string text, Transform playerTransform, float duration)
    {
        GameObject textGO = new GameObject("StunText");
        var textMeshPro = textGO.AddComponent<TextMeshPro>();

        textMeshPro.fontSize = 10f;
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.text = text;

        if (EventFont.Instance != null)
        {
            if (EventFont.Instance.eventFont != null)
            {
                textMeshPro.font = EventFont.Instance.eventFont;
            }
            textMeshPro.enableVertexGradient = true;
            EventFont.Instance.ApplyGradient(textMeshPro, GradientType.Stun);
            EventFont.Instance.ApplyFont(textMeshPro, GradientType.Stun);
        }
        else
        {
            textMeshPro.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
            textMeshPro.color = Color.red;
        }

        // Initial position above player
        textGO.transform.position = playerTransform.position + Vector3.up * 2.5f;
        textGO.transform.localScale = Vector3.zero;

        var seq = LeanTween.sequence();
        
        // Scale in with bounce
        seq.append(LeanTween.scale(textGO, Vector3.one * 1.5f, 0.4f).setEaseOutBounce());
        
        // Follow player with pulsing effect for the duration
        seq.append(LeanTween.value(textGO, 0f, duration, duration).setOnUpdate((float t) =>
        {
            if (textGO != null && playerTransform != null)
            {
                // Follow player position
                textGO.transform.position = playerTransform.position + Vector3.up * 2.5f;
                
                // Pulsing effect
                float pulseScale = 1.5f + Mathf.Sin(Time.time * 4f) * 0.2f;
                textGO.transform.localScale = Vector3.one * pulseScale;
            }
        }));
        
        // Fade out
        seq.append(LeanTween.value(textGO, 1f, 0f, 0.5f).setOnUpdate((float a) =>
        {
            if (textMeshPro != null)
            {
                var c = textMeshPro.color;
                c.a = a;
                textMeshPro.color = c;
            }
        }));

        seq.append(() => { if (textGO != null) Object.Destroy(textGO); });
    }

    public static void ShowText(string text, Vector3 worldPosition, float duration, GradientType gradientType, FontType fontType)
    {
        GameObject textGO = new GameObject("GenericText");
        var textMeshPro = textGO.AddComponent<TextMeshPro>();

        textMeshPro.fontSize = 10f;
        textMeshPro.alignment = TextAlignmentOptions.Center;
        textMeshPro.text = text;

        if (EventFont.Instance != null)
        {
            EventFont.Instance.ApplyFont(textMeshPro, fontType);
            textMeshPro.enableVertexGradient = true;
            EventFont.Instance.ApplyGradient(textMeshPro, gradientType);
        }
        else
        {
            textMeshPro.font = Resources.Load<TMP_FontAsset>("LiberationSans SDF");
        }

        textGO.transform.position = worldPosition + Vector3.up * 1.5f;
        textGO.transform.localScale = Vector3.zero;

        var seq = LeanTween.sequence();
        seq.append(LeanTween.scale(textGO, Vector3.one, 0.3f).setEaseOutBack());
        seq.append(LeanTween.moveY(textGO, textGO.transform.position.y + 0.5f, duration * 0.8f).setEaseOutQuart());
        seq.append(LeanTween.value(textGO, 1f, 0f, duration * 0.2f).setOnUpdate((float a) =>
        {
            if (textMeshPro != null)
            {
                var c = textMeshPro.color;
                c.a = a;
                textMeshPro.color = c;
            }
        }));

        seq.append(() => { if (textGO != null) Object.Destroy(textGO); });
    }

    public static LTSeq ApplyMaterialForDuration(GameObject go, Material mat, float duration)
    {
        if (go == null || mat == null) return null;
        var renderers = go.GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length == 0) return null;
        // Store original materials
        var originalMats = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMats[i] = renderers[i].material;
            renderers[i].material = mat;
        }
        var seq = LeanTween.sequence();
        seq.append(duration);
        seq.append(() =>
        {
            // Restore original materials
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {   
                    renderers[i].material = originalMats[i];
                }
            }
        });
        return seq;
    }


    public static LTSeq ShakeCamera()
    {
        var mainCam = Camera.main;
        if (mainCam == null) return null;
        Vector3 originalPos = mainCam.transform.position;
        var seq = LeanTween.sequence();
        seq.append(LeanTween.moveX(mainCam.gameObject, originalPos.x + 0.5f, 0.1f).setEaseShake().setLoopPingPong(5));
        seq.append(() => mainCam.transform.position = originalPos); // Reset position
        return seq;
    }   

    public static LTSeq ApplyMaterialWithFlash(Renderer renderer, Material flashMaterial, Material originalMaterial, string keywordName, float duration, float flashInterval = 0.1f)
    {
        if (renderer == null || flashMaterial == null || originalMaterial == null)
            return null;

        var seq = LeanTween.sequence();

       
        bool flashState = true;

      
        seq.append(() => {
            if (renderer != null)
                renderer.material = flashMaterial;
        });

        
        int flashCount = Mathf.FloorToInt(duration / flashInterval);

        for (int i = 0; i < flashCount; i++)
        {
           
            bool currentFlashState = flashState;
            
            seq.append(() => {
                if (renderer != null && renderer.material != null)
                {
                    if (currentFlashState)
                        renderer.material.EnableKeyword(keywordName);
                    else
                        renderer.material.DisableKeyword(keywordName);
                }
            });
            seq.append(flashInterval);
            
            
            flashState = !flashState;
        }

        // Restore original material and disable keyword
        seq.append(() => {
            if (renderer != null)
            {
                if (renderer.material != null)
                    //renderer.material.DisableKeyword(keywordName);
                renderer.material = originalMaterial;
            }
        });

        return seq;
    }


    public static LTSeq TriggerVignetteEffect()
    {
        var vignetteController = Object.FindFirstObjectByType<VignetteController>();
        if (vignetteController == null)
        {
            Debug.LogWarning("VignetteController not found in scene!");
            return null;
        }

        if (vignetteController.volume == null)
        {
            Debug.LogWarning("Volume not assigned on VignetteController!");
            return null;
        }

        var profile = vignetteController.volume.profile;
        if (profile == null)
        {
            Debug.LogWarning("Volume profile is null!");
            return null;
        }

        if (!profile.TryGet<Vignette>(out var vignette))
        {
            Debug.LogWarning("Vignette effect not found in volume profile! Please add Vignette override to the Volume Profile.");
            return null;
        }

        var seq = LeanTween.sequence();

        // Enable and set initial intensity
        seq.append(() => {
            vignette.active = true;
            vignette.intensity.Override(0f);
        });

        // Fade in to target intensity
        seq.append(LeanTween.value(0f, vignetteController.vignetteIntensity, 0.4f)
            .setOnUpdate((float val) => {
                if (vignette != null)
                    vignette.intensity.Override(val);
            }));

        // Hold at peak
        seq.append(0.4f);

        // Fade out
        seq.append(LeanTween.value(vignetteController.vignetteIntensity, 0f, 0.3f)
            .setOnUpdate((float val) => {
                if (vignette != null)
                    vignette.intensity.Override(val);
            }));

        // Reset and disable
        seq.append(() => {
            if (vignette != null)
            {
                vignette.intensity.Override(0f);
                vignette.active = false;
            }
        });

        return seq;
    }

    public static LTSeq TriggerChromaticAberration(float duration = 0.8f, float fadeOutDuration = 0.3f)
    {
        var chromaticController = Object.FindFirstObjectByType<ChromaticAbberation>();
        if (chromaticController == null)
        {
            Debug.LogWarning("ChromaticAbberation controller not found in scene!");
            return null;
        }

        if (chromaticController.chromaticProfile == null)
        {
            Debug.LogWarning("Chromatic Profile not assigned on ChromaticAbberation controller!");
            return null;
        }

        var profile = chromaticController.chromaticProfile.profile;
        if (profile == null)
        {
            Debug.LogWarning("Chromatic Profile is null!");
            return null;
        }

        if (!profile.TryGet<ChromaticAberration>(out var chromaticAberration))
        {
            Debug.LogWarning("ChromaticAberration effect not found in volume profile!");
            return null;
        }

        var seq = LeanTween.sequence();

        // Enable and fade in
        seq.append(() => {
            chromaticAberration.active = true;
            chromaticAberration.intensity.Override(0f);
        });

        seq.append(LeanTween.value(0f, chromaticController.intensity, 0.2f)
            .setOnUpdate((float val) => {
                if (chromaticAberration != null)
                    chromaticAberration.intensity.Override(val);
            }));

        // Hold at peak
        seq.append(duration);

       
        seq.append(LeanTween.value(chromaticController.intensity, 0f, fadeOutDuration)
            .setOnUpdate((float val) => {
                if (chromaticAberration != null)
                    chromaticAberration.intensity.Override(val);
            }));

        // Reset and disable
        seq.append(() => {
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.Override(0f);
                chromaticAberration.active = false;
            }
        });

        return seq;
    }

    public static LTDescr PulseLoop(GameObject go, float minScaleMultiplier = 1.5f, float maxScaleMultiplier = 1.2f, float duration = 1f)
    {
        if (go == null) return null;

        Vector3 originalScale = go.transform.localScale;

        return LeanTween.scale(go, originalScale * maxScaleMultiplier, duration)
            .setEaseInOutSine()
            .setLoopPingPong()
            .setFrom(originalScale * minScaleMultiplier);
    }

    public static LTSeq ScaleYTemporary(GameObject go, float peakMultiplier = 1.3f, float totalDuration = 0.4f, float delay = 0f)
    {
        if (go == null) return null;

        Vector3 original = go.transform.localScale;
        Vector3 peak = new Vector3(original.x, original.y * peakMultiplier, original.z);

        // Split totalDuration into up / hold / down phases
        float up = totalDuration * 0.4f;
        float hold = totalDuration * 0.2f;
        float down = Mathf.Max(0f, totalDuration - up - hold);

        var seq = LeanTween.sequence();
        seq.append(delay);
        seq.append(LeanTween.scale(go, peak, up).setEaseOutBounce());
        seq.append(hold);
        seq.append(LeanTween.scale(go, original, down).setEaseInBounce());
        return seq;
    }

    public static LTSeq ShrinkAndDisable(GameObject go, float duration = 0.35f, float delay = 0f, System.Action onComplete = null)
    {
        if (go == null) return null;

      
        var seq = LeanTween.sequence();
        seq.append(delay);

        var descr = LeanTween.scale(go, Vector3.zero, duration).setEaseInBack();
        descr.setOnComplete(() =>
        {
            try { onComplete?.Invoke(); } catch { }
            if (go != null)
                go.SetActive(false);
        });

        seq.append(descr);
        return seq;
    }


}
