using System.Collections;
using UnityEngine;

/// <summary>
/// 3D third-person character controller (WASD movement + Space to jump)
/// Features: Slow (droplet & zone) and Slip states
/// Uses CharacterController (can be converted to Rigidbody later)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class Controller3D : MonoBehaviour
{
    [Header("Movement / Jump")]
    public float moveSpeed = 6f;           // Base movement speed
    public float acceleration = 18f;       // How fast velocity changes
    public float airControl = 0.5f;        // Control while in air (0~1)
    public float gravity = -20f;
    public float jumpHeight = 1.2f;
    public bool enableJump = true;
    private bool wasGrounded;

    [Header("Camera (used for movement direction)")]
    public Transform cameraTransform;      // Usually assign MainCamera

    [Header("State: Slow (movement debuff)")]
    public float slowMultiplier = 0.3f;    // Slow multiplier applied to speed
    public float dropletSlowDuration = 5f; // Duration for temporary droplet slow
    private bool isDropletSlowed = false;  // Is currently droplet-slowed
    private bool isZoneSlowed = false;     // Is currently inside a slow zone

    [Header("State: Slip")]
    public float slipDisableDuration = 2f; // Duration of slip (controls disabled)
    public bool slipUseConstantSpeed = true;
    public float slipSpeed = 6f;
    private bool isSlipping = false;

    [Header("Visuals / Materials")]
    public Renderer playerRenderer;        // MeshRenderer or SkinnedMeshRenderer on the player
    public Material normalMaterial;
    public Material slowMaterial;

    [Header("Control lock & watchdog")]
    public float controlLockTimeout = 2f;

    // Runtime state
    private CharacterController cc;
    private Vector3 planarVelocity;  // Horizontal velocity (x,z)
    private float verticalVelocity;  // Vertical velocity (y)
    private float currentSpeed;      // Effective movement speed (takes slow into account)
    private bool canControl = true;
    private Coroutine controlWatchdogCo;

    private AnimatorHolder animator;

    private float inputMagnitude = 0;
    private float animVelocity = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        wasGrounded = cc.isGrounded;
        animator = GetComponent<AnimatorHolder>();
        if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
        UpdateSpeed(); // initialize currentSpeed
        ApplyMaterial(false);
    }

    void Update()
    {
        // ---- 1) Read input (camera-relative) ----
        Vector3 moveInput = Vector3.zero;

        if (canControl && !isSlipping)
        {
            float h = Input.GetAxisRaw("Horizontal"); // A/D or left/right
            float v = Input.GetAxisRaw("Vertical");   // W/S or up/down
            inputMagnitude = Mathf.Clamp01(new Vector2(h, v).magnitude);

            // Convert input to camera-relative planar direction
            Vector3 camFwd = cameraTransform ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight = cameraTransform ? cameraTransform.right : Vector3.right;
            camFwd.y = 0; camRight.y = 0;
            camFwd.Normalize(); camRight.Normalize();

            Vector3 desiredPlanar = (camFwd * v + camRight * h);
            if (desiredPlanar.sqrMagnitude > 1f) desiredPlanar.Normalize();
            desiredPlanar *= currentSpeed;

            float control = cc.isGrounded ? 1f : airControl;
            planarVelocity = Vector3.MoveTowards(
                planarVelocity,
                desiredPlanar,
                acceleration * control * Time.deltaTime
            );

            // Rotate to face movement direction
            Vector3 face = planarVelocity; face.y = 0f;
            if (face.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(face), 12f * Time.deltaTime);
        }

        // ---- 2) Jump / gravity ----
        if (cc.isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -2f; // small grounded downward velocity
            if (enableJump && canControl && !isSlipping && Input.GetKeyDown(KeyCode.Space)) 
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.animator.SetTrigger("Jump");
                animator.animator.SetBool("isGrounded", false);
            }
                
        }
        verticalVelocity += gravity * Time.deltaTime;

        // ---- 3) Slip (movement behavior while slipping) ----
        if (isSlipping)
        {
            // While slipping, planarVelocity is typically driven by slip logic (handled in SlipRoutine)
        }

        // ---- 4) Apply movement ----
        Vector3 velocity;
        if (inputMagnitude > 0.01f)
        {
            // 正常移动
            velocity = planarVelocity + Vector3.up * verticalVelocity;
        }
        else
        {
            // 没输入则速度清零（防止惯性）
            planarVelocity = Vector3.zero;
            velocity = Vector3.up * verticalVelocity; // 仍保留重力
        }


        // ---- Animator parameters ----
        if (animator != null && animator.animator != null)
        {
            if (inputMagnitude < 0.01f)
            {
                // 无输入时，缓慢减速至0（可调整衰减速度）
                animVelocity = Mathf.MoveTowards(animVelocity, 0f, 2f * Time.deltaTime);
            }
            else
            {
                // 有输入时，缓慢靠近目标inputMagnitude
                animVelocity = Mathf.MoveTowards(animVelocity, inputMagnitude, 3f * Time.deltaTime);
            }

            // 更新动画参数
            animator.animator.SetFloat("velocityX", animVelocity);
            animator.animator.SetBool("isGrounded", cc.isGrounded);
        }


        cc.Move(velocity * Time.deltaTime);
    }



    /// <summary>Apply a temporary droplet slow (if not already slowed)</summary>
    public void ApplyDropletSlow()
    {
        if (isDropletSlowed) return;
        isDropletSlowed = true;
        UpdateSpeed();
        ApplyMaterial(true);
        StartCoroutine(DropletSlowRoutine());
    }

    private IEnumerator DropletSlowRoutine()
    {
        yield return new WaitForSeconds(dropletSlowDuration);
        isDropletSlowed = false;
        UpdateSpeed();
        ApplyMaterial(true);
    }

  
    public void EnterSlowZone()
    {
        isZoneSlowed = true;
        UpdateSpeed();
        ApplyMaterial(true);
    }

    public void ExitSlowZone()
    {
        isZoneSlowed = false;
        UpdateSpeed();
        ApplyMaterial(true);
    }

    private void UpdateSpeed()
    {
        float speed = moveSpeed;
        if (isDropletSlowed || isZoneSlowed) speed *= slowMultiplier;
        currentSpeed = speed;
    }

    private void ApplyMaterial(bool animated)
    {
        if (!playerRenderer) return;
        var target = (isDropletSlowed || isZoneSlowed) && slowMaterial ? slowMaterial : normalMaterial;
        if (target) playerRenderer.material = target; // assign material if available
    }

  

    /// <summary>
    /// Trigger a slip. dirHint: optional direction hint (use current forward if omitted).
    /// </summary>
    public void Slip(Vector3 dirHint = default)
    {
        if (isSlipping) return;

        // Default to forward direction if no hint provided
        Vector3 dir = (dirHint == default) ? transform.forward : dirHint;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;
        dir.Normalize();

        StartCoroutine(SlipRoutine(dir));
    }

    private IEnumerator SlipRoutine(Vector3 slipDir)
    {
        Debug.Log($"slipped corotinue called");
        isSlipping = true;
        SetControl(false);              // disable player control
        ApplyMaterial(true);

        float t = 0f;
        while (t < slipDisableDuration)
        {
            t += Time.deltaTime;

            // Optionally enforce a constant slip speed
            if (slipUseConstantSpeed)
                planarVelocity = slipDir * slipSpeed;

            // Rotate to face slip direction
            if (slipDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(slipDir), 10f * Time.deltaTime);
            Debug.Log($"slipped");

            yield return null;
        }

        isSlipping = false;
        SetControl(true);               // re-enable control
        ApplyMaterial(true);
    }

    /* ===================== Control lock & watchdog ===================== */

    public void SetControl(bool value)
    {
        canControl = value;
        if (!value)
        {
            // Stop movement when control is disabled (e.g. during slip)
            planarVelocity = Vector3.zero;
            StartControlWatchdog();
        }
        else
        {
            StopControlWatchdog();
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
            if (canControl) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        // Timeout reached: force re-enable control
        canControl = true;
    }
}

