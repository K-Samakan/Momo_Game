using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets; // ← สำหรับ disable ThirdPersonController

[RequireComponent(typeof(CharacterController))]
public class ClimbingSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────
    [Header("Detection")]
    public float grabRange = 2.5f;
    public LayerMask climbableLayer;

    [Header("Climbing Movement")]
    public float traverseSpeed = 2f;
    public float grabLerpTime  = 0.3f;
    public float vaultLerpTime = 0.5f;

    [Header("Input")]
    public InputActionAsset inputActions;

    [Header("References")]
    [Tooltip("ลาก Animator จาก PlayerArmature ใส่ที่นี่ (optional)")]
    public Animator animator;

    [Tooltip("ลาก ThirdPersonController script ใส่ที่นี่เพื่อ disable ตอนปีน")]
    public MonoBehaviour thirdPersonController;

    // ─────────────────────────────────────────────
    //  PRIVATE STATE
    // ─────────────────────────────────────────────
    private enum ClimbState { Normal, Grabbing, Transitioning, Vaulting }
    private ClimbState state = ClimbState.Normal;

    private CharacterController cc;
    private ClimbableRock currentRock;
    private float currentRockOffset = 0f;

    private InputAction jumpAction;
    private InputAction rightStickAction;  // Right Stick → เลือกทิศ/ไต่
    private InputAction moveAction;        // Left Stick  → เดินปกติ (ThirdPersonController ใช้)

    private Vector3 velocity;

    private Vector3    transitionStart;
    private Vector3    transitionEnd;
    private float      transitionTimer;
    private float      transitionDuration;
    private ClimbState transitionNextState;

    private float jumpCooldown = 0f;
    private const float JUMP_COOLDOWN_TIME = 0.3f;

    // ─────────────────────────────────────────────
    //  UNITY LIFECYCLE
    // ─────────────────────────────────────────────
    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        SetupInput();
    }

    private void SetupInput()
    {
        var playerMap    = inputActions.FindActionMap("Player");
        jumpAction       = playerMap.FindAction("Jump");
        rightStickAction = playerMap.FindAction("Look"); // Right Analog
        moveAction       = playerMap.FindAction("Move"); // Left Analog
    }

    private void OnEnable()  => jumpAction.performed += OnJumpPressed;
    private void OnDisable() => jumpAction.performed -= OnJumpPressed;

    private void Update()
    {
        jumpCooldown -= Time.deltaTime;

        switch (state)
        {
            case ClimbState.Normal:        /* ThirdPersonController จัดการ */ break;
            case ClimbState.Grabbing:      HandleGrabbing();   break;
            case ClimbState.Transitioning: HandleTransition(); break;
            case ClimbState.Vaulting:      HandleVaulting();   break;
        }
    }

    // ─────────────────────────────────────────────
    //  JUMP INPUT
    // ─────────────────────────────────────────────
    private void OnJumpPressed(InputAction.CallbackContext ctx)
    {
        if (jumpCooldown > 0f) return;
        jumpCooldown = JUMP_COOLDOWN_TIME;

        switch (state)
        {
            case ClimbState.Normal:   TryGrabNearestRock(); break;
            case ClimbState.Grabbing: TryJumpFromRock();    break;
        }
    }

    // ─────────────────────────────────────────────
    //  TRY GRAB NEAREST ROCK
    // ─────────────────────────────────────────────
    private void TryGrabNearestRock()
{
    // ✅ เพิ่ม QueryTriggerInteraction.Collide เพื่อให้ detect Trigger Collider ได้
    Collider[] hits = Physics.OverlapSphere(
        transform.position, 
        grabRange, 
        climbableLayer,
        QueryTriggerInteraction.Collide  // ← เพิ่มบรรทัดนี้
    );
    
    Debug.Log($"[Climb] OverlapSphere พบ {hits.Length} colliders | grabRange={grabRange}");

    ClimbableRock nearest     = null;
    float         nearestDist = float.MaxValue;

    foreach (var hit in hits)
    {
        ClimbableRock rock = hit.GetComponent<ClimbableRock>();
        Debug.Log($"[Climb] hit: {hit.name} | hasClimbableRock: {rock != null}");
        if (rock == null) continue;

        float dist = Vector3.Distance(transform.position, rock.transform.position);
        if (dist < nearestDist) { nearestDist = dist; nearest = rock; }
    }

    if (nearest != null)
    {
        Debug.Log($"[Climb] เกาะ: {nearest.name}");
        GrabRock(nearest, 0f);
    }
    else
    {
        Debug.Log("[Climb] ไม่พบหินในระยะ!");
    }
}

    // ─────────────────────────────────────────────
    //  TRY JUMP FROM ROCK
    // ─────────────────────────────────────────────
    private void TryJumpFromRock()
    {
        // Top Rock → Vault ขึ้น Platform
        if (currentRock != null && currentRock.isTopRock)
        {
            StartVault();
            return;
        }

        Vector2 stickDir = rightStickAction.ReadValue<Vector2>();

        if (stickDir.magnitude < 0.3f)
        {
            ReleaseRock();
            return;
        }

        ClimbableRock neighbor = currentRock.FindNeighborInDirection(stickDir);
        if (neighbor != null) GrabRock(neighbor, 0f);
        else                  ReleaseRock();
    }

    // ─────────────────────────────────────────────
    //  GRAB ROCK
    // ─────────────────────────────────────────────
    private void GrabRock(ClimbableRock rock, float offset)
    {
        currentRock       = rock;
        currentRockOffset = offset;
        velocity          = Vector3.zero;

        // ✅ Disable ThirdPersonController ป้องกัน conflict
        if (thirdPersonController != null)
            thirdPersonController.enabled = false;

        StartTransition(transform.position, GetGrabPosition(rock, offset), grabLerpTime, ClimbState.Grabbing);

        SetAnimatorBool("IsClimbing", true);
        PlayHaptic(0.2f, 0.6f, 0.2f);
    }

    // ─────────────────────────────────────────────
    //  GRABBING STATE
    //  Right Stick X → ไต่ซ้ายขวาบนหินเดิม
    //  Right Stick + Jump → เปลี่ยนหิน
    // ─────────────────────────────────────────────
    private void HandleGrabbing()
    {
        if (currentRock == null) { ReleaseRock(); return; }

        Vector2 stick      = rightStickAction.ReadValue<Vector2>();
        float   halfWidth  = currentRock.rockWidth / 2f;

        currentRockOffset += stick.x * traverseSpeed * Time.deltaTime;
        currentRockOffset  = Mathf.Clamp(currentRockOffset, -halfWidth, halfWidth);

        Vector3 targetPos  = GetGrabPosition(currentRock, currentRockOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, 15f * Time.deltaTime);

        // หมุน Player หันออกจากหิน
        Vector3 facingDir = transform.position - currentRock.transform.position;
        facingDir.y = 0f;
        if (facingDir.magnitude > 0.1f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(facingDir),
                8f * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  VAULT
    // ─────────────────────────────────────────────
    private void StartVault()
    {
        if (currentRock.vaultTarget == null)
        {
            Debug.LogWarning($"[ClimbingSystem] {currentRock.name}: isTopRock=true แต่ไม่มี vaultTarget!");
            ReleaseRock();
            return;
        }

        StartTransition(transform.position, currentRock.vaultTarget.position, vaultLerpTime, ClimbState.Normal);
        state = ClimbState.Vaulting;

        SetAnimatorTrigger("Vault");
        PlayHaptic(0.8f, 0.8f, 0.4f);
    }

    private void HandleVaulting() => HandleTransition();

    // ─────────────────────────────────────────────
    //  RELEASE ROCK
    // ─────────────────────────────────────────────
    private void ReleaseRock()
    {
        currentRock = null;
        state       = ClimbState.Normal;
        velocity    = Vector3.zero;
        cc.enabled  = true;

        // ✅ Re-enable ThirdPersonController
        if (thirdPersonController != null)
            thirdPersonController.enabled = true;

        SetAnimatorBool("IsClimbing", false);
    }

    // ─────────────────────────────────────────────
    //  LERP TRANSITION
    // ─────────────────────────────────────────────
    private void StartTransition(Vector3 from, Vector3 to, float duration, ClimbState nextState)
    {
        transitionStart     = from;
        transitionEnd       = to;
        transitionTimer     = 0f;
        transitionDuration  = duration;
        transitionNextState = nextState;
        cc.enabled          = false;
        state               = ClimbState.Transitioning;
    }

    private void HandleTransition()
    {
        transitionTimer   += Time.deltaTime;
        float t            = Mathf.Clamp01(transitionTimer / transitionDuration);
        transform.position = Vector3.Lerp(transitionStart, transitionEnd, Mathf.SmoothStep(0f, 1f, t));

        if (t >= 1f)
        {
            transform.position = transitionEnd;
            cc.enabled         = true;
            state              = transitionNextState;

            if (transitionNextState == ClimbState.Normal)
            {
                currentRock = null;

                // ✅ Re-enable ThirdPersonController หลัง Vault
                if (thirdPersonController != null)
                    thirdPersonController.enabled = true;

                SetAnimatorBool("IsClimbing", false);
            }
        }
    }

    // ─────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────
    private Vector3 GetGrabPosition(ClimbableRock rock, float offset)
    {
        Vector3 grabPos = rock.grabPoint != null ? rock.grabPoint.position : rock.transform.position;
        return grabPos + rock.GetTraverseDir() * offset;
    }

    // ✅ Safe Animator — ไม่ error ถ้า animator = null
    private void SetAnimatorBool(string param, bool value)
    {
        if (animator != null) animator.SetBool(param, value);
    }

    private void SetAnimatorTrigger(string param)
    {
        if (animator != null) animator.SetTrigger(param);
    }

    // ─────────────────────────────────────────────
    //  HAPTIC
    // ─────────────────────────────────────────────
    private void PlayHaptic(float low, float high, float duration)
    {
        var gp = Gamepad.current;
        if (gp == null) return;
        gp.SetMotorSpeeds(low, high);
        Invoke(nameof(StopHaptic), duration);
    }

    private void StopHaptic() => Gamepad.current?.SetMotorSpeeds(0f, 0f);

    // ─────────────────────────────────────────────
    //  GIZMOS
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, grabRange);
    }
}