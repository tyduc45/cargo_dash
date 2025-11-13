using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class DangerArea : MonoBehaviour
{
    [Header("Íæ¼ÒÍ£Áô´¥·¢Ê±¼ä")]
    public float stayTime = 0.3f;

    [Header("´æÔÚÊ±¼ä(Ãë)")]
    public float lifetime = 3f;

    private float timer;
    private controller playerInside;
    private SpriteRenderer sr;
    private Vector3 initialScale;

    [SerializeField]
    private Renderer[] allRenderers;
    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        StartCoroutine(AutoDestroy());
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("player"))
        {
            var pc = col.GetComponent<controller>();
            if (pc == null) return;

            if (!pc.IsStrong)     // ← 强壮时不进入减速
            {
                playerInside = pc;
                playerInside.EnterDangerZone();
                timer = 0f;
            }
            else
            {
                playerInside = null; // 强壮：不记录、不计时
            }
        }
    }

    void OnTriggerStay2D(Collider2D col)
    {
        if (col.CompareTag("player"))
        {
            var pc = col.GetComponent<controller>();
            if (pc == null || pc.IsStrong) return;  // ← 强壮：不触发掉落/打滑

            timer += Time.deltaTime;
            if (timer > stayTime && playerInside)
            {
                while (playerInside.cargoStack.Count != 0)
                {
                    Debug.Log($"how many cargo in player's hand? {playerInside.cargoStack.Count}");
                    playerInside.ThrowCargo();
                }
                playerInside.playerReset();
                playerInside.Slip();
                timer = -999f;
            }
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (playerInside != null) playerInside.ExitDangerZone();
        if (col.CompareTag("player")) playerInside = null;
    }

    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(lifetime - 0.3f); // Ô¤Áô0.3Ãë¶¯»­

        float t = 0f;
        float duration = 0.3f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = 1f - (t / duration); // Start at 1, fade to 0

            // ✅ Fade out all renderers
            foreach (var renderer in allRenderers)
            {
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    color.a = alpha;
                    renderer.material.color = color;
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
