using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class BirdController : MonoBehaviour
{
    [Header("Movement Parameters")]
    public float speed = 6f;
    public float lifetime = 10f;
    public Vector2 direction = Vector2.right;

    [Header("Scene Bounds (World Coordinates)")]
    public Rect sceneBounds;

    [Header("Player Interaction")]
    [Tooltip("Upward knockback force")]
    public float knockbackForce = 20f;
    [Tooltip("Maximum time to disable player control (seconds, not affected by timeScale)")]
    public float maxDisableDuration = 3f;
    [Tooltip("Minimum interval between hits on the same player (seconds, not affected by timeScale)")]
    public float hitCooldown = 0.5f;

    [Header("Collision Handling After Impact")]
    [Tooltip("Prevent multiple triggers from the same bird after hitting a player")]
    public bool preventMultiHitAfterImpact = true;
    [Tooltip("Use layer swap instead of disabling collider; requires the layer to exist")]
    public bool useLayerSwap = false;
    [Tooltip("Target layer to switch to after impact (e.g. IgnorePlayer)")]
    public string ignorePlayerLayerName = "IgnorePlayer";
    [Tooltip("Duration (seconds) to disable collider or switch layer after impact")]
    public float colliderDisableDuration = 0.2f;

    [Header("Explode Animation")]
    private Animator animator;
    private string explodeAnimationName = "BirdExplode"; // Name of your explosion animation
 

    private Rigidbody2D rb;
    private Collider2D hitCol;

    private bool isStunned = false;
    private float lastHitRealtime = -999f;
    private bool isExploding = false; // Prevent multiple explode calls

    private int originalLayer;
    private bool layerWasSwapped = false;

    [Header("Hit Text")]
    [SerializeField]
    private string[] hitMessages = { "Hit!", "Ouch!", "Drone Strike!", "Swoosh!" };

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitCol = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();

        rb.gravityScale = 0;
        rb.isKinematic = true;
        originalLayer = gameObject.layer;

      
    }

    void Start()
    {
        StartCoroutine(AutoDestroy());
    }

    void FixedUpdate()
    {
        // Don't move if exploding
        if (isExploding) return;

        // Movement
        Vector2 pos = rb.position + direction * speed * Time.fixedDeltaTime;

        // Bounce at bounds
        if (pos.x < sceneBounds.xMin || pos.x > sceneBounds.xMax)
        {
            direction.x *= -1;
            pos.x = Mathf.Clamp(pos.x, sceneBounds.xMin, sceneBounds.xMax);
        }
        if (pos.y < sceneBounds.yMin || pos.y > sceneBounds.yMax)
        {
            direction.y *= -1;
            pos.y = Mathf.Clamp(pos.y, sceneBounds.yMin, sceneBounds.yMax);
        }

        rb.MovePosition(pos);
    }

    IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(lifetime);
        // Use explode animation instead of direct destruction
        ExplodeBird();
    }

    // Method to handle explode animation before destruction
    public void ExplodeBird()
    {
        if (isExploding) return; // Prevent multiple calls
        
        StartCoroutine(ExplodeSequence());
    }

    private IEnumerator ExplodeSequence()
    {
        isExploding = true;

       
        if (hitCol != null)
            hitCol.enabled = false;

      
        if (animator != null)
        {
            animator.Play(explodeAnimationName);
        }

       
        SoundManager.Instance?.PlaySound(SoundType.BirdExplode, null, 0.15f);

        
        float animationDuration = GetAnimationLength(explodeAnimationName);
        
        // Add fade-out effect using animation duration
        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Start fade-out during the last 30% of the animation
            float fadeStartDelay = animationDuration * 0.7f;
            float fadeDuration = animationDuration * 0.3f;
            
            // Wait for fade start delay
            yield return new WaitForSeconds(fadeStartDelay);
            
            // Apply fade-out tween
            LeanTween.value(gameObject, 0.5f, 0f, fadeDuration)
                .setOnUpdate((float alpha) => {
                    if (spriteRenderer != null)
                    {
                        var color = spriteRenderer.color;
                        color.a = alpha;
                        spriteRenderer.color = color;
                    }
                })
                .setEaseInQuad();
            
            // Wait for fade to complete
            yield return new WaitForSeconds(fadeDuration);
        }
        else
        {
            
            yield return new WaitForSeconds(animationDuration);
        }

      
        Destroy(gameObject);
    }

    private float GetAnimationLength(string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
            return 0.5f; // fallback duration

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
                return clip.length;
        }
        return 0.5f; // fallback duration
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.CompareTag("player")) return;

        var p = col.GetComponent<controller>();
        if (p == null) return;

        // === 强壮：不伤害玩家，鸟直接消失 ===
        if (p.IsStrong)
        {
            // 可选的反馈
            TweenUtils.ShowStatusText("BONK!", p.transform.position, 0.8f, GradientType.Hit);
            // 如有适合的音效可换：SoundType.EnemyDown
            SoundManager.Instance.PlaySound(SoundType.PlayerHurt, null, 0.2f);
            // Use explode animation instead of direct destruction
            ExplodeBird();
            return;
        }

        // === 原有受击效果（仅非强壮） ===
        string randomMessage = hitMessages[Random.Range(0, hitMessages.Length)];
        TweenUtils.ShowStatusText(randomMessage, p.transform.position, 1f, GradientType.Hit);
        p.ApplyHitMaterial();
        TweenUtils.TriggerVignetteEffect();
        SoundManager.Instance.PlaySound(SoundType.PlayerHurt, null, 0.3f);

        float now = Time.realtimeSinceStartup;
        if (now - lastHitRealtime < hitCooldown) return;
        lastHitRealtime = now;
        if (isStunned) return;

        if (preventMultiHitAfterImpact)
            StartCoroutine(TemporarilyDisableCollision());

        StartCoroutine(KnockbackAndDisable(p));
    }

    private IEnumerator KnockbackAndDisable(controller p)
    {
        isStunned = true;

        // Disable player control
        p.SetControl(false);

        // Apply upward knockback
        Rigidbody2D prb = p.GetComponent<Rigidbody2D>();
        if (prb != null)
        {
            var v = prb.linearVelocity;
            v.y = knockbackForce;
            prb.linearVelocity = v;
        }

        // Drop items and reset
        while (p.cargoStack.Count != 0)
            p.ThrowCargo();
        p.playerReset();

        // Recovery condition: grounded or timeout
        float deadline = Time.realtimeSinceStartup + maxDisableDuration;
        while (Time.realtimeSinceStartup < deadline)
        {
            if (prb != null && Mathf.Abs(prb.linearVelocity.y) < 0.01f)
                break;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.2f);

        p.SetControl(true);
        p.ApplyNormalMaterial();
        isStunned = false;
    }

    private IEnumerator TemporarilyDisableCollision()
    {

        if (hitCol != null)
        {
            bool prev = hitCol.enabled;
            hitCol.enabled = false;
            yield return new WaitForSecondsRealtime(colliderDisableDuration);
            if (this != null && hitCol != null)
                hitCol.enabled = prev;
        }
    }
}
