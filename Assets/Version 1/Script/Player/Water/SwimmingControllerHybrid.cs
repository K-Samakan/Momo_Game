using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class SwimmingControllerHybrid : MonoBehaviour
{
    #region === SETTINGS ===

    [Header("=== Swimming Settings ===")]
    [SerializeField] private float swimSpeed = 5f;
    [SerializeField] private float diveSpeed = 3f;
    [SerializeField] private float surfaceSpeed = 4f;
    [SerializeField] private float swimRotationSpeed = 10f;

    [Header("=== Physics Settings ===")]
    [SerializeField] private float buoyancyForce = 0f;
    [SerializeField] private float waterDrag = 8f;
    [Range(0f, 1f)]
    [SerializeField] private float underwaterGravityScale = 0.3f;

    [Header("=== Camera Settings ===")]
    [SerializeField] private float cameraSensitivityX = 2f;
    [SerializeField] private float cameraSensitivityY = 2f;
    [SerializeField] private Transform playerCameraRoot;
    [SerializeField] private Transform playerBody;
    [SerializeField] private Transform headBone;

    [Header("=== Oxygen Settings ===")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float oxygenDepletionRate = 10f;
    [SerializeField] private float oxygenRecoveryRate = 20f;
    [SerializeField] private float damageInterval = 1f;
    [SerializeField] private int damageCountPerHealth = 5;
    [SerializeField] private float surfaceThreshold = 0.1f;

    [Header("=== Drowning Haptic Settings ===")]
    [SerializeField] private bool enableDrowningHaptics = true;
    [SerializeField] private float drowningHitIntensity = 0.3f;
    [SerializeField] private float drowningHitDuration = 0.1f;

    [Header("=== Animation Parameters ===")]
    [SerializeField] private string swimSpeedParam = "SwimSpeed";
    [SerializeField] private string verticalSpeedParam = "VerticalSpeed";
    [SerializeField] private string isSwimmingParam = "IsSwimming";
    [SerializeField] private string groundedParam = "Grounded";

    [Header("=== References ===")]
    [SerializeField] private Transform cameraTransform;

    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region === COMPONENTS ===

    private CharacterController characterController;
    private Animator animator;
    private PlayerInput playerInput;
    private MonoBehaviour thirdPersonController;
    private MOMO.Player.PlayerHealth playerHealth;

    #endregion

    #region === STATE ===

    private bool isInWater = false;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 moveDirection = Vector3.zero;

    // Camera State
    private float cameraRotationX = 0f;
    private float cameraRotationY = 0f;

    // Oxygen State
    private float currentOxygen = 100f;
    private bool isUnderwater = false;
    private float waterSurfaceY = 0f;

    // Drowning Damage State
    private float damageTimer = 0f;
    private int damageCount = 0;

    // Air Bubble State
    private bool isInAirBubble = false;
    private float airBubbleRecoveryMultiplier = 1f;
    private bool airBubbleInstantRecovery = false;

    // Whirlpool State
    private bool isInWhirlpool = false;
    private float whirlpoolSpeedMultiplier = 1f;
    private Vector3 whirlpoolForce = Vector3.zero;

    #endregion

    #region === INPUT ===

    private Vector2 moveInput = Vector2.zero;
    private bool diveInput = false;
    private bool surfaceInput = false;

    #endregion

    #region === UNITY LIFECYCLE ===

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        playerInput = GetComponent<PlayerInput>();

        playerHealth = GetComponent<MOMO.Player.PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth component not found! Please add PlayerHealth to player.");
        }
        else
        {
            if (showDebugLogs) Debug.Log("✅ Found PlayerHealth component");
        }

        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
                if (showDebugLogs) Debug.Log("📷 Found Main Camera");
            }
        }

        if (playerCameraRoot == null)
        {
            Transform armature = transform.Find("PlayerArmature");
            if (armature != null)
            {
                playerCameraRoot = armature.Find("PlayerCameraRoot");
                if (showDebugLogs && playerCameraRoot != null)
                    Debug.Log("✅ Found PlayerCameraRoot");
            }
        }

        if (playerBody == null)
        {
            playerBody = transform;
        }

        if (headBone == null && animator != null)
        {
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone != null)
            {
                if (showDebugLogs) Debug.Log($"✅ Auto-found Head Bone: {headBone.name}");
            }
            else
            {
                Debug.LogWarning("⚠️ Could not auto-find Head Bone. Please assign manually.");
            }
        }

        Component[] components = GetComponents<MonoBehaviour>();
        foreach (Component comp in components)
        {
            if (comp.GetType().Name.Contains("Controller") && comp != this)
            {
                thirdPersonController = comp as MonoBehaviour;
                if (showDebugLogs) Debug.Log($"✅ Found controller: {comp.GetType().Name}");
                break;
            }
        }

        if (showDebugLogs) Debug.Log("✅ SwimmingControllerHybrid initialized");
    }

    void OnEnable()
    {
        SubscribeToInputs();
    }

    void OnDisable()
    {
        UnsubscribeFromInputs();
    }

    void Update()
    {
        CheckWaterOverlap();

        if (!isInWater) return;

        UpdateCamera();
        CheckUnderwaterStatus();
        UpdateOxygen();
        CalculateMovement();
        ApplyMovement();
        UpdateAnimations();
    }

    #endregion

    #region === INPUT SYSTEM ===

    private void SubscribeToInputs()
    {
        if (playerInput == null)
        {
            if (showDebugLogs) Debug.LogWarning("⚠️ PlayerInput component not found!");
            return;
        }

        InputAction moveAction = playerInput.actions.FindAction("Move");
        if (moveAction != null)
        {
            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
        }

        InputAction jumpSurfaceAction = playerInput.actions.FindAction("Jump");
        if (jumpSurfaceAction != null)
        {
            jumpSurfaceAction.performed += OnJumpSurfacePerformed;
            jumpSurfaceAction.canceled += OnJumpSurfaceCanceled;
            if (showDebugLogs) Debug.Log("✅ Jump/Surface action subscribed");
        }

        InputAction diveAction = playerInput.actions.FindAction("Dodge");
        if (diveAction != null)
        {
            diveAction.performed += OnDivePerformed;
            diveAction.canceled += OnDiveCanceled;
            if (showDebugLogs) Debug.Log("✅ Dive mapped to Dodge (Circle)");
        }
        else
        {
            Debug.LogWarning("⚠️ Dodge action not found!");
        }

        if (showDebugLogs) Debug.Log("✅ Input actions subscribed");
    }

    private void UnsubscribeFromInputs()
    {
        if (playerInput == null) return;

        InputAction moveAction = playerInput.actions.FindAction("Move");
        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
        }

        InputAction jumpSurfaceAction = playerInput.actions.FindAction("Jump");
        if (jumpSurfaceAction != null)
        {
            jumpSurfaceAction.performed -= OnJumpSurfacePerformed;
            jumpSurfaceAction.canceled -= OnJumpSurfaceCanceled;
        }

        InputAction diveAction = playerInput.actions.FindAction("Dodge");
        if (diveAction != null)
        {
            diveAction.performed -= OnDivePerformed;
            diveAction.canceled -= OnDiveCanceled;
        }
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    private void OnDivePerformed(InputAction.CallbackContext context)
    {
        diveInput = true;
        if (showDebugLogs && isInWater) Debug.Log("⬇️ <color=red>DIVE BUTTON PRESSED</color>");
    }

    private void OnDiveCanceled(InputAction.CallbackContext context)
    {
        diveInput = false;
        if (showDebugLogs && isInWater) Debug.Log("⬆️ <color=yellow>DIVE BUTTON RELEASED</color>");
    }

    private void OnJumpSurfacePerformed(InputAction.CallbackContext context)
    {
        if (isInWater)
        {
            surfaceInput = true;
            if (showDebugLogs) Debug.Log("⬆️ <color=cyan>SURFACE (in water)</color>");
        }
        else
        {
            if (showDebugLogs) Debug.Log("🦘 <color=yellow>JUMP (on ground)</color>");
        }
    }

    private void OnJumpSurfaceCanceled(InputAction.CallbackContext context)
    {
        if (isInWater)
        {
            surfaceInput = false;
            if (showDebugLogs) Debug.Log("⬇️ <color=yellow>SURFACE RELEASED</color>");
        }
    }

    #endregion

    #region === SWIMMING LOGIC ===

    private void CheckWaterOverlap()
    {
        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            characterController.radius,
            LayerMask.GetMask("Water")
        );

        bool foundWater = hitColliders.Length > 0;

        if (foundWater && !isInWater)
        {
            WaterVolume water = hitColliders[0].GetComponent<WaterVolume>();
            if (water != null)
            {
                EnterWater();
            }
        }
        else if (!foundWater && isInWater)
        {
            ExitWater();
        }
    }

    private void UpdateCamera()
    {
        if (!isInWater || playerCameraRoot == null) return;

        float mouseX = 0f;
        float mouseY = 0f;

        if (playerInput != null)
        {
            InputAction lookAction = playerInput.actions.FindAction("Look");
            if (lookAction != null)
            {
                Vector2 lookValue = lookAction.ReadValue<Vector2>();

                bool isMouseDelta = Mathf.Abs(lookValue.x) > 5f || Mathf.Abs(lookValue.y) > 5f;

                if (isMouseDelta)
                {
                    mouseX = lookValue.x * cameraSensitivityX * 0.02f;
                    mouseY = lookValue.y * cameraSensitivityY * 0.02f;
                }
                else if (lookValue.magnitude > 0.01f)
                {
                    mouseX = lookValue.x * cameraSensitivityX * Time.deltaTime;
                    mouseY = lookValue.y * cameraSensitivityY * Time.deltaTime;
                }
            }
        }

        cameraRotationX += mouseY;
        cameraRotationX = Mathf.Clamp(cameraRotationX, -80f, 80f);
        cameraRotationY += mouseX;

        playerCameraRoot.localRotation = Quaternion.Euler(cameraRotationX, 0f, 0f);

        if (playerBody != null)
        {
            playerBody.rotation = Quaternion.Euler(0f, cameraRotationY, 0f);
        }
    }

    private void CheckUnderwaterStatus()
    {
        if (!isInWater)
        {
            isUnderwater = false;
            return;
        }

        float headHeight;

        if (headBone != null)
        {
            headHeight = headBone.position.y;
        }
        else
        {
            headHeight = transform.position.y + characterController.height * 0.8f;
        }

        RaycastHit hit;
        if (Physics.Raycast(headBone.position + Vector3.up * 10f, Vector3.down, out hit, 20f, LayerMask.GetMask("Water")))
        {
            float realWaterSurface = hit.point.y;
            isUnderwater = headHeight < (realWaterSurface - surfaceThreshold);

            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"💧 Head: {headHeight:F2} | Real Surface: {realWaterSurface:F2} | Calculated Surface: {waterSurfaceY:F2} | Underwater: {isUnderwater}");
            }
        }
        else
        {
            isUnderwater = headHeight < (waterSurfaceY - surfaceThreshold);

            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"💧 Head: {headHeight:F2} | Surface: {waterSurfaceY:F2} | Underwater: {isUnderwater}");
            }
        }
    }

    private void UpdateOxygen()
    {
        if (isInAirBubble)
        {
            currentOxygen += oxygenRecoveryRate * airBubbleRecoveryMultiplier * Time.deltaTime;
            currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);

            damageTimer = 0f;
            damageCount = 0;

            if (showDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"💨 Oxygen in Air Bubble: {currentOxygen:F1}/{maxOxygen} (+{oxygenRecoveryRate * airBubbleRecoveryMultiplier * Time.deltaTime:F2}/frame)");
            }

            return;
        }

        if (!isInWater)
        {
            currentOxygen += oxygenRecoveryRate * Time.deltaTime;
            currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);
            damageTimer = 0f;
            damageCount = 0;
            return;
        }

        if (isUnderwater)
        {
            currentOxygen -= oxygenDepletionRate * Time.deltaTime;
            currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);

            if (showDebugLogs && Time.frameCount % 30 == 0)
            {
                Debug.Log($"🫁 Oxygen: {currentOxygen:F1}/{maxOxygen} (-{oxygenDepletionRate * Time.deltaTime:F2}/frame)");
            }
        }
        else
        {
            currentOxygen += oxygenRecoveryRate * Time.deltaTime;
            currentOxygen = Mathf.Clamp(currentOxygen, 0f, maxOxygen);

            damageTimer = 0f;
            damageCount = 0;

            if (showDebugLogs && Time.frameCount % 30 == 0 && currentOxygen < maxOxygen)
            {
                Debug.Log($"🫁 Oxygen: {currentOxygen:F1}/{maxOxygen} (+{oxygenRecoveryRate * Time.deltaTime:F2}/frame)");
            }
        }

        if (currentOxygen <= 0f)
        {
            ProcessDrowningDamage();
        }
    }

    private void ProcessDrowningDamage()
    {
        if (playerHealth != null && playerHealth.currentHealth <= 0) return;

        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;
            damageCount++;

            Debug.LogWarning($"💀 DROWNING HIT #{damageCount} (Every {damageInterval}s)");

            if (enableDrowningHaptics)
            {
                VibrateController(drowningHitIntensity, drowningHitDuration);
            }

            if (damageCount >= damageCountPerHealth)
            {
                damageCount = 0;
                TakeHealthDamage(1);
            }
        }
    }

    private void TakeHealthDamage(int damage)
    {
        if (playerHealth == null)
        {
            Debug.LogError("❌ PlayerHealth not found! Cannot take damage.");
            return;
        }

        playerHealth.TakeDamage(damage);

        if (playerHealth.currentHealth <= 0)
        {
            OnPlayerDeath();
        }
    }

    private void OnPlayerDeath()
    {
        Debug.LogError("☠️ ========================================");
        Debug.LogError("☠️ PLAYER DIED FROM DROWNING!");
        Debug.LogError("☠️ ========================================");

        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool(isSwimmingParam, false);
        }
    }

    private void CalculateMovement()
    {
        moveDirection = Vector3.zero;

        if (moveInput.magnitude >= 0.1f && cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        }

        float verticalVelocity = 0f;

        if (diveInput)
        {
            verticalVelocity = -diveSpeed;
        }
        else if (surfaceInput)
        {
            verticalVelocity = surfaceSpeed;
        }
        else
        {
            verticalVelocity = buoyancyForce;
        }

        float currentSwimSpeed = swimSpeed * whirlpoolSpeedMultiplier;

        Vector3 targetVelocity = moveDirection * currentSwimSpeed;
        targetVelocity.y = verticalVelocity;

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * waterDrag);
    }

    private void ApplyMovement()
    {
        Vector3 totalMovement = currentVelocity + whirlpoolForce;
        characterController.Move(totalMovement * Time.deltaTime);

        if (moveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * swimRotationSpeed);
        }

        whirlpoolForce = Vector3.zero;
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        float horizontalSpeed = new Vector3(currentVelocity.x, 0f, currentVelocity.z).magnitude;
        float normalizedSpeed = Mathf.Clamp01(horizontalSpeed / swimSpeed);
        float normalizedVertical = Mathf.Clamp(currentVelocity.y / surfaceSpeed, -1f, 1f);

        animator.SetFloat(swimSpeedParam, normalizedSpeed);
        animator.SetFloat(verticalSpeedParam, normalizedVertical);
        animator.SetBool(isSwimmingParam, isInWater);

        if (animator.GetBool(groundedParam))
        {
            animator.SetBool(groundedParam, false);
        }
    }

    #endregion

    #region === AIR BUBBLE ===

    public void EnterAirBubble(float recoveryMultiplier, bool instantRecovery)
    {
        isInAirBubble = true;
        airBubbleRecoveryMultiplier = recoveryMultiplier;
        airBubbleInstantRecovery = instantRecovery;

        if (instantRecovery)
        {
            currentOxygen = maxOxygen;

            if (showDebugLogs)
            {
                Debug.Log($"💨 Oxygen instantly recovered to 100%!");
            }
        }

        damageTimer = 0f;
        damageCount = 0;

        if (animator != null)
        {
            animator.SetBool(isSwimmingParam, false);
            animator.SetBool(groundedParam, true);
        }

        if (showDebugLogs)
        {
            Debug.Log($"🫧 Entered Air Bubble | Recovery: x{recoveryMultiplier} | Instant: {instantRecovery}");
        }
    }

    public void ExitAirBubble()
    {
        isInAirBubble = false;
        airBubbleRecoveryMultiplier = 1f;
        airBubbleInstantRecovery = false;

        if (animator != null)
        {
            animator.SetBool(isSwimmingParam, true);
            animator.SetBool(groundedParam, false);
        }

        if (showDebugLogs)
        {
            Debug.Log($"💧 Left Air Bubble");
        }
    }

    public void RecoverOxygenInAirBubble(float deltaTime)
    {
        if (!isInAirBubble || airBubbleInstantRecovery) return;

        float recoveryAmount = oxygenRecoveryRate * airBubbleRecoveryMultiplier * deltaTime;
        currentOxygen += recoveryAmount;
        currentOxygen = Mathf.Min(currentOxygen, maxOxygen);

        if (showDebugLogs && Time.frameCount % 30 == 0)
        {
            Debug.Log($"💨 Oxygen recovering in Air Bubble: {currentOxygen:F1}/{maxOxygen} (+{recoveryAmount:F2}/frame)");
        }
    }

    public void ForceEnterWater()
    {
        if (!isInWater)
        {
            EnterWater();
        }

        isUnderwater = true;

        if (showDebugLogs)
        {
            Debug.Log($"🏊 Forced into Water Mode (from Air Bubble exit)");
        }
    }

    public bool IsInAirBubble()
    {
        return isInAirBubble;
    }

    #endregion

    #region === HAPTIC FEEDBACK (DROWNING ONLY) ===

    private void VibrateController(float intensity, float duration)
    {
        if (playerHealth == null) return;
        playerHealth.TestVibration(intensity, duration);
    }

    #endregion

    #region === PUBLIC API ===

    public void EnterWater()
    {
        if (isInWater) return;

        if (showDebugLogs) Debug.Log("🌊 <color=cyan>Player entered water</color>");

        isInWater = true;

        Collider[] hitColliders = Physics.OverlapSphere(
            transform.position,
            characterController.radius * 2f,
            LayerMask.GetMask("Water")
        );

        if (hitColliders.Length > 0)
        {
            WaterVolume water = hitColliders[0].GetComponent<WaterVolume>();
            if (water != null)
            {
                BoxCollider waterCollider = water.GetComponent<BoxCollider>();
                float waterHeight = waterCollider.size.y * water.transform.lossyScale.y;
                waterSurfaceY = water.transform.position.y + (waterHeight / 2f);

                if (showDebugLogs)
                {
                    Debug.Log($"🌊 ========== WATER DEBUG ==========");
                    Debug.Log($"🌊 Water GameObject: {water.name}");
                    Debug.Log($"🌊 Water Position Y: {water.transform.position.y:F2}");
                    Debug.Log($"🌊 Water Collider Size Y: {waterCollider.size.y:F2}");
                    Debug.Log($"🌊 Water Scale Y: {water.transform.lossyScale.y:F2}");
                    Debug.Log($"🌊 Water Height: {waterHeight:F2}");
                    Debug.Log($"🌊 CALCULATED Surface Y: {waterSurfaceY:F2}");
                    Debug.Log($"🌊 Player Position Y: {transform.position.y:F2}");
                    Debug.Log($"🌊 ==================================");
                }
            }
        }

        damageTimer = 0f;
        damageCount = 0;

        if (playerCameraRoot != null)
        {
            Vector3 currentRotation = playerCameraRoot.localEulerAngles;
            cameraRotationX = currentRotation.x;
            if (cameraRotationX > 180f) cameraRotationX -= 360f;

            cameraRotationY = playerBody.eulerAngles.y;

            if (showDebugLogs) Debug.Log($"📷 Saved camera rotation: X={cameraRotationX:F1}, Y={cameraRotationY:F1}");
        }

        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = false;
            if (showDebugLogs) Debug.Log($"Disabled {thirdPersonController.GetType().Name}");
        }

        currentVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(isSwimmingParam, true);
        }
    }

    public void ExitWater()
    {
        if (!isInWater) return;

        if (showDebugLogs) Debug.Log("🏖️ <color=yellow>Player exited water</color>");

        isInWater = false;
        isUnderwater = false;

        damageTimer = 0f;
        damageCount = 0;

        if (thirdPersonController != null)
        {
            thirdPersonController.enabled = true;
            if (showDebugLogs) Debug.Log($"Enabled {thirdPersonController.GetType().Name}");
        }

        currentVelocity = Vector3.zero;
        moveInput = Vector2.zero;
        diveInput = false;
        surfaceInput = false;

        if (animator != null)
        {
            animator.SetBool(isSwimmingParam, false);
        }
    }

    public void ResetHealth()
    {
        if (playerHealth == null) return;

        playerHealth.currentHealth = playerHealth.maxHealth;
        playerHealth.UpdateHealthUI();

        currentOxygen = maxOxygen;
        damageTimer = 0f;
        damageCount = 0;

        if (playerInput != null)
        {
            playerInput.enabled = true;
        }

        Debug.Log("💚 Health and Oxygen reset!");
    }

    public float CurrentOxygen => currentOxygen;
    public float MaxOxygen => maxOxygen;
    public float OxygenPercentage => (currentOxygen / maxOxygen) * 100f;
    public bool IsInWater => isInWater;
    public bool IsUnderwaterState => isUnderwater;

    public float GetOxygen() => currentOxygen;
    public float GetMaxOxygen() => maxOxygen;
    public float GetOxygenPercentage() => currentOxygen / maxOxygen;
    public bool IsUnderwater() => isUnderwater;
    public bool IsDrowning() => currentOxygen <= 0f && isUnderwater;

    public int GetCurrentHealth() => playerHealth != null ? playerHealth.currentHealth : 0;
    public int GetMaxHealth() => playerHealth != null ? playerHealth.maxHealth : 0;
    public int GetDamageCount() => damageCount;
    public bool IsDead() => playerHealth != null ? playerHealth.currentHealth <= 0 : false;

    public bool IsSwimming() => isInWater;
    public Vector3 GetVelocity() => currentVelocity;

    public void ForceExitWater()
    {
        if (isInWater)
        {
            ExitWater();
            Debug.Log("🏖️ [SwimmingController] Force Exit Water (Called Externally)");
            StartCoroutine(RecoverOxygenWhileDisabled());
        }
    }

    private IEnumerator RecoverOxygenWhileDisabled()
    {
        Debug.Log("🫁 [SwimmingController] Started Oxygen Recovery Coroutine");

        float waterCheckInterval = 0.1f;
        float waterCheckTimer = 0f;

        while (true)
        {
            waterCheckTimer += Time.deltaTime;
            if (waterCheckTimer >= waterCheckInterval)
            {
                waterCheckTimer = 0f;

                Collider[] waterColliders = Physics.OverlapSphere(
                    transform.position + Vector3.up * (characterController.height * 0.5f),
                    characterController.radius * 2f,
                    LayerMask.GetMask("Water")
                );

                if (waterColliders.Length > 0 && !enabled)
                {
                    Debug.Log($"🌊 [SwimmingController] Water detected - Enabling script");
                    enabled = true;
                    yield break;
                }
            }

            if (!enabled && currentOxygen < maxOxygen)
            {
                currentOxygen += oxygenRecoveryRate * Time.deltaTime;
                currentOxygen = Mathf.Min(currentOxygen, maxOxygen);

                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"🫁 Oxygen: {currentOxygen:F1}/{maxOxygen}");
                }
            }

            yield return null;

            if (enabled)
            {
                Debug.Log("✅ Coroutine stopped (script enabled)");
                yield break;
            }
        }
    }

    #endregion

    #region === WHIRLPOOL ===

    public void EnterWhirlpool(float speedMultiplier)
    {
        isInWhirlpool = true;
        whirlpoolSpeedMultiplier = speedMultiplier;

        if (showDebugLogs)
        {
            Debug.Log($"🌀 [SwimmingController] Entered whirlpool | Speed: x{speedMultiplier}");
        }
    }

    public void ExitWhirlpool()
    {
        isInWhirlpool = false;
        whirlpoolSpeedMultiplier = 1f;
        whirlpoolForce = Vector3.zero;

        if (showDebugLogs)
        {
            Debug.Log($"🏊 [SwimmingController] Exited whirlpool");
        }
    }

    public void ApplyWhirlpoolForce(Vector3 force)
    {
        whirlpoolForce = force;
    }

    public bool IsInWhirlpool()
    {
        return isInWhirlpool;
    }

    #endregion

    #region === DEBUG ===

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        if (isInWater)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 1f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, currentVelocity);

            if (moveDirection.magnitude > 0.1f)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, moveDirection * 2f);
            }

            if (waterSurfaceY > 0)
            {
                Gizmos.color = Color.blue;
                Vector3 surfacePos = transform.position;
                surfacePos.y = waterSurfaceY;
                Gizmos.DrawLine(surfacePos + Vector3.left * 2f, surfacePos + Vector3.right * 2f);

                if (headBone != null)
                {
                    Gizmos.color = isUnderwater ? Color.red : Color.green;
                    Gizmos.DrawWireSphere(headBone.position, 0.3f);

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(transform.position, headBone.position);
                }
                else
                {
                    Gizmos.color = isUnderwater ? Color.red : Color.green;
                    Gizmos.DrawWireSphere(transform.position, 0.3f);
                }
            }
        }

        if (isInAirBubble)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 1.5f);
        }
    }

    #endregion
}