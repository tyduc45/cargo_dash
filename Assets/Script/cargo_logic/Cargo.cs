using Assets.script;
using System;
using TMPro;
using UnityEngine;

public enum CargoType { Common, Elasticity, Frangibility, Fuzziness, Universality }

public class Cargo : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    public string cargoName;
    public CargoType cargoType;
    public Sprite icon;
    public Sprite cargoIcon;

    [HideInInspector] public ObjectSpawnerWithPool ownerSpawner;
    [HideInInspector] public bool hasCollided;
    public CargoState state = CargoState.InPool;

    [Header("UI")]
    public TextMeshPro nameLabel;

    [Header("Scoring / Combo Bridge")]
    [SerializeField] private DeliveryReceiver receiver;

    // ==== 弹性货物 ====
    [Header("Elasticity Bounce Settings")]
    public int maxGroundBounce = 2;
    public float firstBounceImpulse = 8f;
    public float bounceDamping = 0.6f;
    private int groundBounceCount = 0;

    // ==== 易碎货标记 ====
    [Header("Frangibility Flags")]
    public bool willBreak = false;

    [Header("Frangibility Tween")]
    public float breakAnimDuration = 0.4f;
    public float breakScale = 1.3f;
    public float breakFadeDelay = 0.1f;

    // ==== Fuzziness ====
    private bool categoryHidden = false;

    // ==== 缓存的组件 ====
    private Rigidbody2D rb;
    private Collider2D col;
    private Camera mainCam;
    public SpriteRenderer categorySR;
    public SpriteRenderer typeSR;

    // Events
    public static event Action<Cargo> OnCargoPickedUp;
    public static event Action<Cargo> OnCargoBroken;
    public static event Action<Cargo> OnCargoDelivered;

    public void Interact(controller player, bool isHold)
    {
        // 仅在可被拾取的阶段响应（避免已被拿起/已交付时误触）
        if (state != CargoState.Active) return;

        // 向玩家请求拾取（见下文 controller 新增的公共方法）
        player.TryPickCargo(this);
    }

    // 3) 供 UI 使用的提示文案（可接入你的交互提示系统）
    public string GetInteractPrompt()
    {
        // 可根据 cargoName 或类型定制
        return $"按 F 拾取 {cargoName}";
    }

    void Awake()
    {
        mainCam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // 🔧 一次性缓存分类与类型图标
        var cat = FindDeep(transform, "cargoCatagory");
        if (cat) categorySR = cat.GetComponent<SpriteRenderer>();

        var type = FindDeep(transform, "cargoType");
        if (type) typeSR = type.GetComponent<SpriteRenderer>();

        if (!receiver) receiver = FindReceiverSafe();
    }

    void OnEnable()
    {
        hasCollided = false;
        groundBounceCount = 0;
        // 每次重新启用时默认恢复图标
        if (categorySR) categorySR.enabled = true;
        if (typeSR) typeSR.enabled = true;
    }

    void Update()
    {
        if ((state == CargoState.Active || state == CargoState.Carried) && nameLabel && mainCam)
        {
            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position + Vector3.up * 0.5f);
            nameLabel.transform.position = screenPos;
            nameLabel.text = cargoName;
            nameLabel.enabled = true;
        }
    }

    public void SetState(CargoState newState)
    {
        var prev = state;
        state = newState;

        switch (newState)
        {
            case CargoState.InPool:
                gameObject.SetActive(false);
                willBreak = false;
                categoryHidden = false;
                if (categorySR) categorySR.enabled = true;
                if (typeSR) typeSR.enabled = true;
                if (nameLabel) nameLabel.enabled = false;
                break;

            case CargoState.Active:
                gameObject.SetActive(true);
                if (rb) rb.simulated = true;
                if (nameLabel) { nameLabel.text = cargoName; nameLabel.enabled = true; }

                if (cargoType == CargoType.Frangibility && prev == CargoState.Carried)
                    willBreak = true;
                break;

            case CargoState.Carried:
                if (rb) rb.simulated = false;
                groundBounceCount = 0;
                willBreak = false;

                if (cargoType == CargoType.Fuzziness && !categoryHidden)
                {
                    if (categorySR) categorySR.enabled = false;
                    categoryHidden = true;
                }
                TriggerPickedUp();
                break;

            case CargoState.Delivered:
                TriggerDelivered();
                Recycle();
                break;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (state != CargoState.Active) return;
        if (collision.collider.CompareTag("player")) return;

        // ✅ 所有类型：第一次碰到地面或其他货物就统计 backlog
        if (!hasCollided && (collision.collider.CompareTag("ground") || collision.collider.CompareTag("cargos")))
        {
            GameManager.Instance.OnBacklogAdd();
            hasCollided = true;
        }

        // ✅ 弹性货物落地后弹跳
        if (cargoType == CargoType.Elasticity
            && collision.collider.CompareTag("ground")
            && groundBounceCount < maxGroundBounce)
        {
            SoundManager.Instance.PlaySound(SoundType.CargoBounce, null, 0.10f);
            float impulse = firstBounceImpulse * Mathf.Pow(bounceDamping, groundBounceCount);
            rb.AddForce(Vector2.up * impulse, ForceMode2D.Impulse);
            groundBounceCount++;
        }

        // ✅ 易碎货物：落地后破碎（以前只有被拿起后才会）
        if (cargoType == CargoType.Frangibility && willBreak)
        {
            BreakWithTween();
            return; // 破碎后就不再继续后续逻辑
        }
    }

    void BreakWithTween()
    {
        willBreak = false;
        GameManager.Instance.OnBacklogAdd();

        SoundManager.Instance.PlaySound(SoundType.CargoBreak, null, 0.3f);


        TriggerBroken();
        receiver?.OnCargoBroken();

        if (rb) rb.simulated = false;
        if (col) col.enabled = false;
        if (nameLabel) nameLabel.enabled = false;

        TweenUtils.BreakEffect(gameObject, breakAnimDuration, breakScale, breakFadeDelay);
        TweenUtils.ShowText("BROKEN CARGO!", transform.position, 1.5f, GradientType.Message, FontType.Message);
        Destroy(gameObject, breakAnimDuration + 0.2f);
    }

    Transform FindDeep(Transform root, string target)
    {
        if (!root) return null;
        if (root.name == target) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, target);
            if (r) return r;
        }
        return null;
    }

    void Recycle()
    {
        SetState(CargoState.InPool);
        Debug.Log($"[Cargo] Recycled: {cargoName}");
    }

    void TriggerPickedUp() => OnCargoPickedUp?.Invoke(this);
    void TriggerBroken() => OnCargoBroken?.Invoke(this);
    void TriggerDelivered() => OnCargoDelivered?.Invoke(this);

    DeliveryReceiver FindReceiverSafe()
    {
#if UNITY_2023_1_OR_NEWER
        var r = FindFirstObjectByType<DeliveryReceiver>();
        if (!r) r = FindAnyObjectByType<DeliveryReceiver>();
        return r;
#else
        return FindObjectOfType<DeliveryReceiver>();
#endif
    }
}
