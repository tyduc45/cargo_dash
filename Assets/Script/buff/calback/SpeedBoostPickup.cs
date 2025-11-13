using UnityEngine;
using System.Collections;  // ✅ 关键

[RequireComponent(typeof(Collider2D))]
public class SpeedBoostPickup : MonoBehaviour
{
    public float lifetime = 10f;
    public GameObject pickupVFX;
    public AudioClip pickupSFX;
    [Range(0.75f, 1.2f)] public float sfxVolume = 1f;

    private bool consumed;
    private Coroutine lifeRoutine;

    void OnEnable()
    {
        consumed = false;
        if (lifeRoutine != null) StopCoroutine(lifeRoutine);
        lifeRoutine = StartCoroutine(AutoDespawnAfter(lifetime));
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        var player = other.GetComponentInParent<controller>() ?? other.GetComponent<controller>();
        if (!player) return;

        consumed = true;

        // 直接用玩家里的 boost 配置（你已在 controller 里设置过）
        player.ApplySpeedBoost(player.boostMultiplier, player.boostDuration, refreshIfActive: true);

        if (pickupVFX) Instantiate(pickupVFX, transform.position, Quaternion.identity);
        if (pickupSFX) AudioSource.PlayClipAtPoint(pickupSFX, transform.position, sfxVolume);
        TweenUtils.ShowStatusText("Speed ++", transform.position, 0.6f, GradientType.Buff);
        if (lifeRoutine != null) StopCoroutine(lifeRoutine);
        StartCoroutine(Disappear());
    }

    private IEnumerator AutoDespawnAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (!consumed) StartCoroutine(Disappear());
    }

    private IEnumerator Disappear()
    {
        float time = 0f;
        Vector3 start = transform.localScale;
        while (time < 0.2f)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, time / 0.2f);
            yield return null;
        }
        gameObject.SetActive(false);
        transform.localScale = start;
    }
}
