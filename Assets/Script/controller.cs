using Assets.script;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class controller : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private bool isGrounded;
    [SerializeField] private Transform interactOrigin;

    [SerializeField] private Material playerMaterial;
    [SerializeField] private Material slowMaterial;
    [SerializeField] private Material hitMaterial;
    [SerializeField] private Material stunnedMaterial;
    [SerializeField] private Material speedMaterial;
    [SerializeField] private Material strongMaterial;
    [SerializeField] private string parameterName = "HITEFFECT_ON";

    [SerializeField] private SpriteRenderer playerRenderer;

    [SerializeField] private ParticleSystem playerDust;


    private AnimatorHolder animatorHolder;

    public int maxCarry = 3;
    public float speedPenalty = 0.7f;
    public Transform carryPoint;
    public TextMeshProUGUI text;

    private Rigidbody2D _rb;
    public CarryUI carryUI;
    public Stack<GameObject> cargoStack = new Stack<GameObject>();

    [Header("减速参数")]
    public float slowMultiplier = 0.3f;
    public float dropletSlowDuration = 5f;
    private bool isDropletSlowed = false;
    private bool isZoneSlowed = false;

    [Header("滑倒参数")]
    public float slipDisableDuration = 2f;
    private bool isSlipping = false;

    public bool isStunning = false;

    [Header("滑倒推进（水平）")]
    public bool slipUseVelocity = true;
    public float slipSpeed = 6f;

    [Header("Throw Settings")]
    public Vector2 throwVelocity = new Vector2(5f, 8f);

    [Header("Interaction")]
    public float interactRange = 1.5f;
    public LayerMask interactableMask;
    [SerializeField] private bool canControl = true;

    [SerializeField] private float currentSpeed;
    private IInteractable currentInteractable;

    [Header("控制锁看门狗")]
    public float controlLockTimeout = 2.0f;
    private Coroutine controlWatchdogCo;

    [Header("Flash Effect Settings")]
    [SerializeField] private float hitFlashDuration = 2f;
    [SerializeField] private float hitFlashInterval = 0.1f;

    [Header("加速参数")]
    public float boostMultiplier = 1.5f;
    public float boostDuration = 3f;
    private bool isBoosting = false;
    private Coroutine boostCo;

    [Header("强壮状态")]
    public float strongDuration = 5f;
    private bool isStrong = false;
    private Coroutine strongCo;

    // === Material Management System ===
    private Coroutine materialCo;
    private Material currentActiveMaterial;

    /// <summary>外部只读</summary>
    public bool IsStrong => isStrong;
    public bool IsBoosting => isBoosting;

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        animatorHolder = GetComponent<AnimatorHolder>();

        currentSpeed = _moveSpeed;

        carryUI = carryPoint.GetComponent<CarryUI>();
        if (carryUI == null)
        {
            carryUI = carryPoint.gameObject.AddComponent<CarryUI>();
            carryUI.capacity = maxCarry;
            carryUI.followTarget = carryPoint;
        }

        currentActiveMaterial = playerMaterial;
        ApplyNormalMaterial();
    }

    void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentState != GameState.Playing)
            return;

        if (canControl)
        {
            if (!isSlipping)
            {
                HandleMovement();
                HandleJump();
            }
        }
        else
        {
            if (!isStunning && !isSlipping)
                _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
        }

        animatorHolder.animator.SetBool("isHolding", carryUI.Count > 0);

        if (canControl)
        {
            if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.X))
            {
                var station = FindNearestStation();
                if (station != null)
                {
                    station.Interact(this, true);
                    SoundManager.Instance.PlaySound(SoundType.Interact, null, 0.25f);
                }
            }

            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Z))
            {
                var cargo = FindNearestCargo();
                if (cargo != null && cargo.state == CargoState.Active)
                    TryPickCargo(cargo);
                SoundManager.Instance.PlaySound(SoundType.Pickup, null, 0.3f);
            }

            if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.C))
            {
                ThrowCargo();
                SoundManager.Instance.PlaySound(SoundType.Throw, null, 0.3f);
            }
        }
    }

    void HandleMovement()
    {
        int moveInput = 0;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) moveInput += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) moveInput -= 1;

        if (moveInput > 0) transform.localScale = new Vector3(1, 1, 1);
        if (moveInput < 0) transform.localScale = new Vector3(-1, 1, 1);

        float prevScaleX = transform.localScale.x;
        if (isGrounded && Mathf.Sign(prevScaleX) != Mathf.Sign(transform.localScale.x))
        {
            PlayDustPS();
        }

        if (isGrounded)
            animatorHolder.animator.SetFloat("moveX", Mathf.Abs(moveInput));
        else
            animatorHolder.animator.SetFloat("moveX", 0);

        _rb.linearVelocity = new Vector2(moveInput * currentSpeed, _rb.linearVelocityY);

        HandleDustEffect(moveInput != 0);
    }

    void HandleJump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocityX, _jumpForce);
            SoundManager.Instance.PlaySound(SoundType.Jump, null, 0.3f);
            playerDust.Play();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
            isGrounded = true;
        SoundManager.Instance.PlaySound(SoundType.Jumpland, null, 0.3f);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("ground"))
            isGrounded = false;
    }

    public void PlayDustPS()
    {
        if (playerDust != null && isGrounded)
        {
            if (!isBoosting)
            {
                playerDust.Play();
            }
        }
    }

    private void HandleDustEffect(bool isMoving)
    {
        if (isBoosting && isGrounded && isMoving)
        {
            if (!playerDust.isPlaying)
            {
                playerDust.Play();
            }
        }
        else
        {
            if (playerDust.isPlaying)
            {
                playerDust.Stop();
            }
        }
    }

    private Vector3 GetInteractOrigin() => interactOrigin ? interactOrigin.position : transform.position;

    private Cargo FindNearestCargo()
    {
        Vector3 origin = GetInteractOrigin();
        Collider2D[] cols = Physics2D.OverlapCircleAll(origin, interactRange, interactableMask);
        float minDist = float.MaxValue;
        Cargo best = null;
        foreach (var col in cols)
        {
            var cargo = col.GetComponentInParent<Cargo>();
            if (cargo == null) continue;
            float d = Vector2.Distance(origin, col.transform.position);
            if (d < minDist) { minDist = d; best = cargo; }
        }
        return best;
    }

    private IInteractable FindNearestStation()
    {
        Vector3 origin = GetInteractOrigin();
        Collider2D[] cols = Physics2D.OverlapCircleAll(origin, interactRange, interactableMask);
        float minDist = float.MaxValue;
        IInteractable best = null;
        foreach (var col in cols)
        {
            var ia = col.GetComponentInParent<IInteractable>();
            if (ia == null || ia is Cargo) continue;
            float d = Vector2.Distance(origin, col.transform.position);
            if (d < minDist) { minDist = d; best = ia; }
        }
        return best;
    }

    public bool TryPickCargo(Cargo cargo)
    {
        if (cargo == null) return false;
        if (cargoStack.Count >= maxCarry) return false;

        GameObject cargoGO = cargo.gameObject;
        TweenUtils.ScaleInPopOutEffect(cargoGO, 0.5f, 0f);
        cargo.SetState(CargoState.Carried);

        carryUI.Push(cargoGO);
        cargoStack.Push(cargoGO);

        Rigidbody2D rb = cargoGO.GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = false;

        Collider2D col = cargoGO.GetComponent<Collider2D>();
        if (col) col.enabled = false;

        animatorHolder.animator.SetBool("isHolding", carryUI.Count > 0);
        return true;
    }

    public void ThrowCargo()
    {
        if (cargoStack.Count == 0) return;

        GameObject cargoGO = cargoStack.Pop();
        Cargo cargo = cargoGO.GetComponent<Cargo>();

        cargoGO.transform.SetParent(null);
        cargoGO.transform.position = carryPoint.position + Vector3.up * 1f;

        Rigidbody2D rb = cargoGO.GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = true;

        Collider2D col = cargoGO.GetComponent<Collider2D>();
        if (col) col.enabled = true;

        cargo.SetState(CargoState.Active);

        Rigidbody2D rbCargo = cargoGO.GetComponent<Rigidbody2D>();
        if (rbCargo != null)
        {
            bool facingRight = transform.localScale.x >= 0f;
            float vx = throwVelocity.x * (facingRight ? 1 : -1);
            float vy = throwVelocity.y;
            rbCargo.linearVelocity = new Vector2(vx, vy);
        }

        carryUI.Pop();
    }



    /// <summary>
    ///  material application with priority system
    /// higher priority materials override lower ones
    /// </summary>
    private void SetMaterialWithDuration(Material material, float duration)
    {
        // Stop any existing material coroutine
        if (materialCo != null)
        {
            StopCoroutine(materialCo);
            materialCo = null;
        }

        // Apply the new material
        currentActiveMaterial = material;
        SetPlayerMaterial(material);

        // Start timer to restore normal material
        if (duration > 0)
        {
            materialCo = StartCoroutine(RestoreMaterialAfterDelay(duration));
        }
    }

    private IEnumerator RestoreMaterialAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Check current state priorities and apply appropriate material
        UpdateMaterialBasedOnState();
        materialCo = null;
    }

    /// <summary>
    /// Updates material based on current active states (priority order)
    /// Priority: Strong > Slip > Speed > Slow > Normal
    /// (Slip overrides speed to give clear visual feedback of control loss)
    /// </summary>
    private void UpdateMaterialBasedOnState()
    {
        if (isStrong)
        {
            currentActiveMaterial = strongMaterial;
        }
        else if (isSlipping)
        {
            
            currentActiveMaterial = slowMaterial;
        }
        else if (isBoosting)
        {
            currentActiveMaterial = speedMaterial;
        }
        else if (isDropletSlowed || isZoneSlowed)
        {
            currentActiveMaterial = slowMaterial;
        }
        else
        {
            currentActiveMaterial = playerMaterial;
        }

        SetPlayerMaterial(currentActiveMaterial);
    }

    private void SetPlayerMaterial(Material material)
    {
        if (playerRenderer != null && material != null)
        {
            playerRenderer.material = material;
        }
    }



    public void ApplyNormalMaterial()
    {
        currentActiveMaterial = playerMaterial;
        SetPlayerMaterial(playerMaterial);
    }

    public void ApplySlowMaterial()
    {
        // Only apply if not in higher priority state
        if (!isStrong && !isBoosting)
        {
            SetMaterialWithDuration(slowMaterial, dropletSlowDuration);
        }
    }

    public void ApplyHitMaterial()
    {
        TweenUtils.ApplyMaterialWithFlash(playerRenderer, hitMaterial, currentActiveMaterial, parameterName, hitFlashDuration, hitFlashInterval);
    }

    public void ApplyStunnedMaterial(float duration)
    {
        SetMaterialWithDuration(stunnedMaterial, duration);
    }

    public void ApplySpeedMaterial(float duration)
    {
        // Only apply if not in strong state
        if (!isStrong && speedMaterial != null && duration > 0f)
        {
            SetMaterialWithDuration(speedMaterial, duration);
        }
    }

    public void ApplyStrongMaterial(float duration)
    {
        if (strongMaterial != null && duration > 0f)
        {
            SetMaterialWithDuration(strongMaterial, duration);
        }
    }



    public void ApplyDropletSlow()
    {
        if (isStrong) return;

        // Slow removes speed boost
        if (isBoosting)
        {
            if (boostCo != null) StopCoroutine(boostCo);
            boostCo = null;
            isBoosting = false;
            if (playerDust.isPlaying) playerDust.Stop();
            TweenUtils.ShowStatusText("Speed --", transform.position, 0.6f, GradientType.Hit);
        }

        if (!isDropletSlowed)
        {
            isDropletSlowed = true;
            UpdateSpeed();
            StartCoroutine(DropletSlowRoutine());
        }
    }

    IEnumerator DropletSlowRoutine()
    {
        UpdateMaterialBasedOnState();
        yield return new WaitForSeconds(dropletSlowDuration);
        isDropletSlowed = false;
        UpdateSpeed();
        UpdateMaterialBasedOnState();
    }

    public void EnterDangerZone()
    {
        if (isStrong) return;

        // Slow zone removes speed boost
        if (isBoosting)
        {
            if (boostCo != null) StopCoroutine(boostCo);
            boostCo = null;
            isBoosting = false;
            if (playerDust.isPlaying) playerDust.Stop();
            TweenUtils.ShowStatusText("Speed --", transform.position, 0.6f, GradientType.Hit);
        }

        isZoneSlowed = true;
        UpdateSpeed();
        UpdateMaterialBasedOnState();
    }

    public void ExitDangerZone()
    {
        isZoneSlowed = false;
        UpdateSpeed();
        UpdateMaterialBasedOnState();
    }

    public void Slip(float dirX = 0f)
    {
        if (isStrong) return;

        // Cancel speed boost if slipping
        if (isBoosting)
        {
            if (boostCo != null) StopCoroutine(boostCo);
            boostCo = null;
            isBoosting = false;
            if (playerDust.isPlaying) playerDust.Stop();
            TweenUtils.ShowStatusText("Speed --", transform.position, 0.6f, GradientType.Hit);
        }

        float dir = dirX == 0f ? Mathf.Sign(transform.localScale.x) : Mathf.Sign(dirX);
        if (!isSlipping) StartCoroutine(SlipRoutine(dir));
    }

    IEnumerator SlipRoutine(float dirX)
    {
        isSlipping = true;
        SetControl(false);

        if (animatorHolder != null && animatorHolder.animator != null)
        {
            animatorHolder.animator.SetTrigger("Slip");
            animatorHolder.animator.SetBool("isSlipping", true);
        }

        // Update material based on current state
        UpdateMaterialBasedOnState();

        string[] message = new string[3] { "Slipped!", "Ooops!", "Tripped!" };
        string selectedMessage = message[Random.Range(0, message.Length)];
        TweenUtils.ShowStatusText(selectedMessage, transform.position, 0.6f, GradientType.Slow);

        float t = 0f;
        while (t < slipDisableDuration)
        {
            t += Time.deltaTime;
            if (slipUseVelocity)
                _rb.linearVelocity = new Vector2(dirX * slipSpeed, _rb.linearVelocity.y);
            yield return null;
        }

        isSlipping = false;
        if (animatorHolder != null && animatorHolder.animator != null)
            animatorHolder.animator.SetBool("isSlipping", false);

        SetControl(true);
        UpdateMaterialBasedOnState();
    }

    public void ApplySpeedBoost(float multiplier = -1f, float duration = -1f, bool refreshIfActive = true)
    {
        if (multiplier > 0f) boostMultiplier = multiplier;
        if (duration > 0f) boostDuration = duration;

        // Cannot speed up while slipping
        if (isSlipping)
        {
            TweenUtils.ShowStatusText("Cannot Speed Up While Slipping!", transform.position + Vector3.up * 4.5f, 0.6f, GradientType.Hit);
            //TweenUtils.ShowText("Cannot Speed Up While Slipping!", transform.position, 0.6f, GradientType.Hit,FonT);
            return;
        }

        // Speed boost removes slow effects
        if (isDropletSlowed || isZoneSlowed)
        {
            isDropletSlowed = false;
            isZoneSlowed = false;
            TweenUtils.ShowStatusText("Slow Removed", transform.position, 0.6f, GradientType.Hit);
        }

        if (isBoosting && !refreshIfActive) return;

        if (boostCo != null) StopCoroutine(boostCo);
        boostCo = StartCoroutine(SpeedBoostRoutine());
    }

    private IEnumerator SpeedBoostRoutine()
    {
        isBoosting = true;
        UpdateMaterialBasedOnState();
        UpdateSpeed();

        float t = 0f;
        while (t < boostDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isBoosting = false;
        UpdateSpeed();
        UpdateMaterialBasedOnState();

        if (playerDust.isPlaying)
        {
            playerDust.Stop();
        }

        TweenUtils.ShowStatusText("Speed --", transform.position, 0.6f, GradientType.Hit);
        boostCo = null;
    }

    public void ApplyStrong(float duration = -1f, bool refreshIfActive = true)
    {
        if (duration > 0f) strongDuration = duration;
        if (isStrong && !refreshIfActive) return;

        if (strongCo != null) StopCoroutine(strongCo);
        strongCo = StartCoroutine(StrongRoutine());
    }

    private IEnumerator StrongRoutine()
    {
        isStrong = true;

        // Clear debuffs and update material
        isDropletSlowed = false;
        isZoneSlowed = false;
        UpdateSpeed();
        UpdateMaterialBasedOnState();

        float t = 0f;
        while (t < strongDuration)
        {
            t += Time.deltaTime;
            yield return null;
        }

        isStrong = false;
        UpdateSpeed();
        UpdateMaterialBasedOnState();

        TweenUtils.ShowStatusText("Immunity Removed", transform.position, 0.6f, GradientType.Hit);
        strongCo = null;
    }

    void UpdateSpeed(float overrideSpeed = -1f)
    {
        if (overrideSpeed >= 0)
        {
            currentSpeed = overrideSpeed;
            return;
        }

        float speed = _moveSpeed;

        if (isBoosting) speed *= boostMultiplier;

        if (!isStrong)
        {
            if (isDropletSlowed || isZoneSlowed) speed *= slowMultiplier;
        }
        else
        {
            isDropletSlowed = false;
            isZoneSlowed = false;
        }

        currentSpeed = speed;
    }

    public void playerReset()
    {
        cargoStack.Clear();
        animatorHolder.animator.SetBool("isHolding", false);
        if (carryUI != null) carryUI.Clear();
    }

    public void SetControl(bool value)
    {
        canControl = value;
        if (animatorHolder != null && animatorHolder.animator != null)
            animatorHolder.animator.SetBool("canControl", value);

        if (!value)
        {
            UpdateSpeed(0);
            StartControlWatchdog();
        }
        else
        {
            StopControlWatchdog();
            UpdateSpeed();
        }
    }

    private void StartControlWatchdog()
    {
        if (controlWatchdogCo != null) StopCoroutine(controlWatchdogCo);
        controlWatchdogCo = StartCoroutine(ControlWatchdog());
    }

    private void StopControlWatchdog()
    {
        if (controlWatchdogCo != null)
        {
            StopCoroutine(controlWatchdogCo);
            controlWatchdogCo = null;
        }
    }

    private IEnumerator ControlWatchdog()
    {
        float t = 0f;
        while (t < controlLockTimeout)
        {
            if (canControl) { controlWatchdogCo = null; yield break; }
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        if (!canControl && !isSlipping && !isStunning)
        {
            Debug.LogWarning("[controller] Control lock timeout reached. Force unlock.");
            canControl = true;
            if (animatorHolder != null && animatorHolder.animator != null)
                animatorHolder.animator.SetBool("canControl", true);
            UpdateSpeed();
        }
        controlWatchdogCo = null;
    }
}