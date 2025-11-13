using System.Collections;  // for IEnumerator
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StrongPickup : MonoBehaviour
{
    [Header("Lifetime")]
    [Tooltip("无人拾取时，多少秒后自动消失")]
    public float lifetime = 12f;

    [Header("Strong Config")]
    [Tooltip(">0 则用这个值覆盖玩家默认 strongDuration；<=0 时读取玩家的 strongDuration")]
    public float durationOverride = -1f;
    [Tooltip("重复拾取时是否刷新持续时间")]
    public bool refreshIfActive = true;

    [Header("VFX & SFX (optional)")]
    public GameObject pickupVFX;
    public AudioClip pickupSFX;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private bool consumed = false;
    private Coroutine lifeRoutine;

    void Reset()
    {
        // 建议把 Collider2D 设为 Trigger，并把物体放到 Pickup Layer
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    void OnEnable()
    {
        consumed = false;
        if (lifeRoutine != null) StopCoroutine(lifeRoutine);
        lifeRoutine = StartCoroutine(AutoDespawnAfter(lifetime));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;

        // 你的玩家脚本名为 controller，Tag 通常为 "player"
        if (!other.CompareTag("player")) return;

        var player = other.GetComponentInParent<controller>() ?? other.GetComponent<controller>();
        if (!player) return;

        consumed = true;

        // 计算持续时间：优先用道具自定义，否则读玩家默认 strongDuration
        float dur = (durationOverride > 0f) ? durationOverride : player.strongDuration;

        // 施加强壮
        player.ApplyStrong(dur, refreshIfActive);

        // 反馈
        if (pickupVFX) Instantiate(pickupVFX, transform.position, Quaternion.identity);
        if (pickupSFX) AudioSource.PlayClipAtPoint(pickupSFX, transform.position, sfxVolume);

        string[] statusText = new string[2] { "Imnunity", "For Kazmodon!" };
        int randomText = Random.Range(0, statusText.Length);
        TweenUtils.ShowStatusText(statusText[randomText], transform.position, 0.6f, GradientType.Buff);

        // 消失（适配对象池，使用 SetActive(false) 而非 Destroy）
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
        // 简单缩放渐隐；如不需要可直接 SetActive(false)
        float time = 0f;
        Vector3 start = transform.localScale;
        while (time < 0.2f)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(start, Vector3.zero, time / 0.2f);
            yield return null;
        }
        gameObject.SetActive(false);
        transform.localScale = start; // 复位以便对象池下次复用
    }
}