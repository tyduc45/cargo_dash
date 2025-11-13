using UnityEngine;

public class LightningEffect : MonoBehaviour
{
    public Renderer[] renderers;
    public float flashDuration = 0.1f;
    public float timeBetweenFlashes = 0.1f;

    [Header("Auto Flash Settings")]
    public bool autoFlash = true;
    public float minFlashInterval = 2f;
    public float maxFlashInterval = 5f;
    public int minFlashCount = 2;
    public int maxFlashCount = 4;

    private string keywordName = "OVERLAY_ON";
    private bool isFlashing = false;
    private float nextFlashTime;

    private void Awake()
    {
        // If renderers not assigned, try to get from children
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        foreach (var rend in renderers)
        {
            if (rend != null && rend.material != null)
            {
                rend.material.DisableKeyword(keywordName);
            }
        }

        // Set first flash time
        nextFlashTime = Time.time + Random.Range(minFlashInterval, maxFlashInterval);
    }

    private void Update()
    {
        if (autoFlash && !isFlashing && Time.time >= nextFlashTime)
        {
            int flashCount = Random.Range(minFlashCount, maxFlashCount + 1);
            TriggerFlash(flashCount);
            nextFlashTime = Time.time + Random.Range(minFlashInterval, maxFlashInterval);
        }
    }

    public void TriggerFlash(int flashCount = 3)
    {
        if (renderers == null || renderers.Length == 0)
        {
            Debug.LogWarning("No renderers assigned!");
            return;
        }

        // Prevent multiple coroutines from running at once
        if (!isFlashing)
        {
            StartCoroutine(FlashCoroutine(flashCount));
        }
    }

    private System.Collections.IEnumerator FlashCoroutine(int flashCount)
    {
        isFlashing = true;

        for (int i = 0; i < flashCount; i++)
        {
            // Flash on all renderers
            foreach (var rend in renderers)
            {
                if (rend != null && rend.material != null)
                {
                    rend.material.EnableKeyword(keywordName);
                }
            }
            yield return new WaitForSeconds(flashDuration);

            // Flash off all renderers
            foreach (var rend in renderers)
            {
                if (rend != null && rend.material != null)
                {
                    rend.material.DisableKeyword(keywordName);
                }
            }

            // Wait before next flash (except on last flash)
            if (i < flashCount - 1)
            {
                yield return new WaitForSeconds(timeBetweenFlashes);
            }
        }

        isFlashing = false;
    }

    public void SetFlashDuration(float duration)
    {
        flashDuration = duration;
    }
}
