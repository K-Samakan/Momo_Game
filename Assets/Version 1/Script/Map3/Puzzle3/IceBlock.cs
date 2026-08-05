using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ✅ IceBlock - บล็อกน้ำแข็งที่ผู้เล่นสามารถจับและผลักได้
/// ✅ รองรับ Haptic Feedback, Push Counter, Fall Detection
/// ✅ ไม่มี UI Logic - ให้ IcePuzzleManager จัดการแทน
/// </summary>
public class IceBlock : MonoBehaviour
{
    #region Serialized Fields

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float gridSize = 2f;
    public LayerMask wallLayer;
    public LayerMask blockLayer;

    [Header("Player Interaction")]
    public string playerTag = "Player";
    public float grabRange = 1.8f;
    public float pushDetectionRange = 5f;
    public PlayerInput playerInput;

    [Header("Push Counter System")]
    public int maxPushCount = 5;
    public int currentPushCount = 0;
    public bool showCounterUI = true;

    [Header("Fall Detection System")]
    public float fallCheckDelay = 1f;
    public float fallYThreshold = -10f;
    public LayerMask validGroundLayers;

    [Header("HP System")]
    public static int currentHP = 3;
    public static int maxHP = 3;

    [Header("Haptic Feedback")]
    public bool enableBlockHaptics = true;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    #endregion

    #region Events

    // Events
    public event Action OnBlockMoved;
    public event Action<IceBlock> OnBlockDestroyed;
    public static event Action<int> OnHPChanged;
    public static event Action OnGameRestart;

    #endregion

    #region Static Variables

    // Static Counter Management
    private static IceBlock lastPushedBlock = null;
    private static Dictionary<IceBlock, int> blockCounters = new Dictionary<IceBlock, int>();
    private static bool inputManagerInitialized = false;
    private static IceBlockInputManager inputManager;

    // Goal Management
    private static PuzzleGoalManager goalManager;
    private static bool goalManagerInitialized = false;

    #endregion

    #region Private Variables

    // Movement
    private bool isMoving = false;
    private bool isGrabbed = false;
    private Vector3 targetPosition;
    private Vector3 lastMoveDirection = Vector3.zero;

    // References
    private Transform playerTransform;
    private Renderer blockRenderer;
    private Color originalColor = Color.white;
    private TextMesh counterText;
    private Rigidbody blockRigidbody;

    // Fall Detection
    private bool isFalling = false;
    private bool hasValidGround = true;
    private Coroutine fallCheckCoroutine;

    // Haptic Feedback
    private bool wasTriggersPressed = false;
    private HapticManager hapticManager;
    private bool isContinuousHapticActive = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        targetPosition = transform.position;
        Debug.Log($"🧊 IceBlock '{gameObject.name}' Starting at position: {transform.position}");

        // Setup Components
        SetupRenderer();
        SetupRigidbody();

        if (showCounterUI)
        {
            CreateCounterUI();
        }

        FindPlayer();
        FindPlayerInput();
        FindHapticManager();

        InitializeCounter();
        SetupInputManager();
        SetupGoalManager();
        StartFallDetection();

        Debug.Log($"📊 Setup Complete - Block: {gameObject.name} | HP: {currentHP}/{maxHP}");
    }

    void Update()
    {
        // ตรวจสอบว่า HapticManager ยังคงมีอยู่
        if (hapticManager == null)
        {
            FindHapticManager();
        }

        HandleMovement();
        CheckPlayerDistance();
        HandleGrabState();

        // Billboard Counter Text
        if (counterText != null && Camera.main != null)
        {
            Vector3 directionToCamera = Camera.main.transform.position - counterText.transform.position;
            counterText.transform.rotation = Quaternion.LookRotation(-directionToCamera);
        }
    }

    void OnDestroy()
    {
        // หยุด Haptic Feedback เมื่อทำลาย Object
        if (enableBlockHaptics && hapticManager != null && isContinuousHapticActive)
        {
            hapticManager.StopContinuousPushFeedback();
            isContinuousHapticActive = false;
            Debug.Log($"🎮 Stopped continuous haptic on destroy - Block: {gameObject.name}");
        }

        if (fallCheckCoroutine != null)
        {
            StopCoroutine(fallCheckCoroutine);
        }
    }

    #endregion

    #region Setup Methods

    void SetupRenderer()
    {
        blockRenderer = GetComponent<Renderer>();
        if (blockRenderer != null)
        {
            try
            {
                originalColor = blockRenderer.material.color;
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"⚠️ Material ของ {gameObject.name} ไม่มี Color property - ใช้สีขาว");
                originalColor = Color.white;
            }
        }
    }

    void SetupRigidbody()
    {
        blockRigidbody = GetComponent<Rigidbody>();
        if (blockRigidbody == null)
        {
            blockRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        blockRigidbody.useGravity = true;
        blockRigidbody.mass = 1f;
        blockRigidbody.drag = 2f;
        blockRigidbody.angularDrag = 5f;
        blockRigidbody.freezeRotation = true;

        Debug.Log($"✅ Rigidbody setup complete for {gameObject.name}");
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            Debug.Log($"✅ Player found: {playerObj.name} | Distance: {GetPlayerDistance():F1}");
        }
        else
        {
            Debug.LogError($"❌ Player GameObject with tag '{playerTag}' not found!");
        }
    }

    void FindPlayerInput()
    {
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null)
            {
                Debug.Log($"✅ PlayerInput found automatically: {playerInput.name}");
            }
            else
            {
                Debug.LogError("❌ PlayerInput component not found!");
            }
        }
        else
        {
            Debug.Log($"✅ PlayerInput assigned: {playerInput.name}");
        }
    }

    void FindHapticManager()
    {
        hapticManager = HapticManager.Instance;
        if (hapticManager != null)
        {
            Debug.Log($"✅ HapticManager found for block: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"⚠️ HapticManager not found for block: {gameObject.name}");
        }
    }

    void InitializeCounter()
    {
        if (!blockCounters.ContainsKey(this))
        {
            blockCounters[this] = 0;
        }
        currentPushCount = blockCounters[this];
        UpdateCounterDisplay();
    }

    void SetupInputManager()
    {
        if (!inputManagerInitialized && playerInput != null)
        {
            // ลบ InputManager เก่าก่อน (ถ้ามี)
            if (inputManager != null)
            {
                Destroy(inputManager.gameObject);
            }

            GameObject managerObj = new GameObject("IceBlockInputManager");
            inputManager = managerObj.AddComponent<IceBlockInputManager>();
            inputManager.Initialize(playerInput);
            inputManagerInitialized = true;

            Debug.Log("✅ IceBlock Input Manager created/recreated");
        }
    }

    void SetupGoalManager()
    {
        if (!goalManagerInitialized)
        {
            GameObject managerObj = GameObject.Find("IceBlockGoalManager");
            if (managerObj == null)
            {
                managerObj = new GameObject("IceBlockGoalManager");
            }

            goalManager = managerObj.GetComponent<PuzzleGoalManager>();
            if (goalManager == null)
            {
                goalManager = managerObj.AddComponent<PuzzleGoalManager>();
            }

            goalManager.Initialize(playerInput);
            goalManagerInitialized = true;

            Debug.Log("✅ Puzzle Goal Manager created");
        }
    }

    void CreateCounterUI()
    {
        GameObject counterObj = new GameObject("CounterText");
        counterObj.transform.SetParent(transform);
        counterObj.transform.localPosition = Vector3.up * 1.2f;
        counterObj.transform.localRotation = Quaternion.identity;

        counterText = counterObj.AddComponent<TextMesh>();
        counterText.text = $"{currentPushCount}/{maxPushCount}";
        counterText.fontSize = 20;
        counterText.color = Color.white;
        counterText.anchor = TextAnchor.MiddleCenter;
        counterText.alignment = TextAlignment.Center;
        counterText.characterSize = 0.1f;
    }

    #endregion

    #region Fall Detection

    void StartFallDetection()
    {
        if (fallCheckCoroutine != null)
        {
            StopCoroutine(fallCheckCoroutine);
        }
        fallCheckCoroutine = StartCoroutine(FallDetectionCoroutine());
    }

    IEnumerator FallDetectionCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(fallCheckDelay);

            // เช็คว่าตกลงไปต่ำกว่า Threshold หรือไม่
            if (transform.position.y < fallYThreshold)
            {
                Debug.Log($"💀 Block {gameObject.name} ตกลงไปแล้ว! Y: {transform.position.y}");
                HandleBlockFall();
                yield break;
            }

            // เช็คว่าอยู่บนพื้นที่ถูกต้องหรือไม่
            if (!isMoving && !IsOnValidGround())
            {
                if (hasValidGround)
                {
                    hasValidGround = false;
                    Debug.Log($"⚠️ Block {gameObject.name} ไม่อยู่บนพื้นที่ที่ถูกต้อง - เริ่มตก");

                    if (blockRigidbody != null)
                    {
                        blockRigidbody.useGravity = true;
                        blockRigidbody.isKinematic = false;
                    }
                }
            }
            else
            {
                hasValidGround = true;
            }
        }
    }

    bool IsOnValidGround()
    {
        Collider[] groundColliders = Physics.OverlapSphere(transform.position, 1.0f);

        foreach (Collider col in groundColliders)
        {
            if (col != null && col.gameObject != gameObject)
            {
                if (col.CompareTag("Grid") || col.CompareTag("Goal") || col.CompareTag("Ice"))
                {
                    return true;
                }
            }
        }

        return false;
    }

    void HandleBlockFall()
    {
        Debug.Log($"💥 Block {gameObject.name} หลุดจากพื้นที่เล่น!");
        DecreaseHP();
        DestroyBlockFromFall();
    }

    void DestroyBlockFromFall()
    {
        // หยุด Haptic Feedback
        if (enableBlockHaptics && hapticManager != null)
        {
            hapticManager.StopContinuousPushFeedback();
            Debug.Log($"🎮 Stopped continuous haptic for falling block: {gameObject.name}");
        }

        if (blockCounters.ContainsKey(this))
        {
            blockCounters.Remove(this);
        }

        if (lastPushedBlock == this)
        {
            lastPushedBlock = null;
        }

        if (fallCheckCoroutine != null)
        {
            StopCoroutine(fallCheckCoroutine);
        }

        OnBlockDestroyed?.Invoke(this);

        Debug.Log($"🗑️ Block {gameObject.name} ถูกทำลายเพราะตก");
        Destroy(gameObject);
    }

    #endregion

    #region HP System

    /// <summary>
    /// ✅ ลด HP และแจ้ง Event
    /// </summary>
    public static void DecreaseHP()
    {
        currentHP--;
        Debug.Log($"💔 HP ลดลง: {currentHP}/{maxHP}");

        // แจ้ง Event ให้ IcePuzzleManager รับไป
        OnHPChanged?.Invoke(currentHP);

        if (currentHP <= 0)
        {
            Debug.Log("💀 HP หมด! เริ่มเกมใหม่...");
            RestartGame();
        }
    }

    /// <summary>
    /// ✅ รีสตาร์ทเกม
    /// </summary>
    public static void RestartGame()
    {
        Debug.Log("🔄 เริ่มเกมใหม่...");

        // รีเซ็ต static variables
        currentHP = maxHP;
        inputManagerInitialized = false;
        goalManagerInitialized = false;
        lastPushedBlock = null;
        blockCounters.Clear();

        // แจ้ง Event ให้ IcePuzzleManager รับไป
        OnGameRestart?.Invoke();

        // รีโลด Scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    #endregion

    #region Movement

    void HandleMovement()
    {
        if (isMoving)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;

                if (blockRigidbody != null)
                {
                    blockRigidbody.isKinematic = false;
                    blockRigidbody.useGravity = true;
                }

                DebugNearbyIceObjects();
                CheckIceSliding();
            }
        }
    }

    void CheckPlayerDistance()
    {
        if (playerTransform == null) return;

        float distance = GetPlayerDistance();
        if (distance > grabRange && isGrabbed)
        {
            Debug.Log($"📏❌ Player ไกลเกินไป! Distance: {distance:F1} > {grabRange}");
            ReleaseGrab();
        }
    }

    #endregion

    #region Grab & Push System

    void HandleGrabState()
    {
        if (inputManager == null) return;

        bool shouldGrab = inputManager.IsBothTriggersPressed() && IsPlayerNearForGrab();

        // ระบบ Continuous Haptic Feedback
        if (enableBlockHaptics && hapticManager != null)
        {
            if (shouldGrab != wasTriggersPressed)
            {
                wasTriggersPressed = shouldGrab;

                if (shouldGrab && IsPlayerNearForGrab())
                {
                    // เริ่มการสั่นแบบต่อเนื่อง
                    hapticManager.StartContinuousPushFeedback(currentPushCount);
                    isContinuousHapticActive = true;
                    Debug.Log($"🎮 เริ่มการสั่นต่อเนื่อง - Block: {gameObject.name} | Push Count: {currentPushCount}");
                }
                else
                {
                    // หยุดการสั่นแบบต่อเนื่อง
                    hapticManager.StopContinuousPushFeedback();
                    isContinuousHapticActive = false;
                    Debug.Log($"🎮 หยุดการสั่นต่อเนื่อง - Block: {gameObject.name}");
                }
            }

            // อัปเดต push count สำหรับการสั่นต่อเนื่อง
            if (isContinuousHapticActive && shouldGrab)
            {
                hapticManager.UpdateContinuousPushCount(currentPushCount);
            }
        }

        // จับบล็อก
        if (shouldGrab && !isGrabbed && !isMoving)
        {
            Debug.Log($"📈 Grab Both L2+R2: Block={gameObject.name}");
            StartGrab();
        }
        else if (!shouldGrab && isGrabbed)
        {
            Debug.Log($"📉 Release: Block={gameObject.name}");
            ReleaseGrab();
        }

        // ผลักบล็อก
        if (isGrabbed && !isMoving && inputManager.HasMoveInput())
        {
            Vector2 moveInput = inputManager.GetMoveInput();
            if (moveInput.magnitude > 0.5f && IsPlayerPushingThisBlockDirectly())
            {
                Vector3 pushDirection = CalculatePushDirection();
                Debug.Log($"🎯 Calculated Push Direction: {pushDirection} | Input: {moveInput} | Block: {gameObject.name}");

                if (TryPush(pushDirection))
                {
                    IncrementPushCounter();
                }
            }
        }
    }

    Vector3 CalculatePushDirection()
    {
        if (playerTransform == null) return Vector3.zero;

        Vector3 directionFromPlayer = (transform.position - playerTransform.position).normalized;

        Vector3 gridDirection;
        if (Mathf.Abs(directionFromPlayer.x) > Mathf.Abs(directionFromPlayer.z))
        {
            gridDirection = directionFromPlayer.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            gridDirection = directionFromPlayer.z > 0 ? Vector3.forward : Vector3.back;
        }

        Debug.Log($"🧮 Push Calculation: PlayerPos={playerTransform.position} | BlockPos={transform.position} | Direction={gridDirection}");
        return gridDirection;
    }

    bool IsPlayerNearForGrab()
    {
        if (playerTransform == null) return false;
        return GetPlayerDistance() <= grabRange;
    }

    bool IsPlayerPushingThisBlockDirectly()
    {
        if (playerTransform == null || !isGrabbed) return false;

        float distance = GetPlayerDistance();
        if (distance > pushDetectionRange) return false;

        Vector3 directionFromPlayer = (transform.position - playerTransform.position).normalized;
        Vector3[] cardinalDirections = {
            Vector3.right, Vector3.left, Vector3.forward, Vector3.back
        };

        foreach (Vector3 cardinal in cardinalDirections)
        {
            float dot = Vector3.Dot(directionFromPlayer, cardinal);
            if (dot > 0.7f) return true;
        }

        return false;
    }

    float GetPlayerDistance()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    void StartGrab()
    {
        isGrabbed = true;
        Debug.Log($"🤏✅ จับบล็อค {gameObject.name} สำเร็จ! | Count: {currentPushCount}/{maxPushCount}");
        ChangeGrabVisual(true);
    }

    void ReleaseGrab()
    {
        isGrabbed = false;
        Debug.Log($"👐⚫ ปล่อยบล็อค {gameObject.name}");
        ChangeGrabVisual(false);

        // หยุด Haptic Feedback
        if (enableBlockHaptics && hapticManager != null && isContinuousHapticActive)
        {
            hapticManager.StopContinuousPushFeedback();
            isContinuousHapticActive = false;
            wasTriggersPressed = false;
            Debug.Log($"🎮 หยุดการสั่นต่อเนื่องเมื่อปล่อย - Block: {gameObject.name}");
        }
    }

    void ChangeGrabVisual(bool grabbed)
    {
        if (blockRenderer == null) return;

        try
        {
            if (grabbed)
            {
                blockRenderer.material.color = Color.yellow;
            }
            else
            {
                float ratio = (float)currentPushCount / maxPushCount;
                blockRenderer.material.color = Color.Lerp(originalColor, Color.red, ratio);
            }
        }
        catch (System.Exception)
        {
            Debug.LogWarning($"⚠️ Cannot change color of {gameObject.name} material");
        }
    }

    public bool TryPush(Vector3 direction)
    {
        if (isMoving) return false;

        Vector3 gridDirection = direction * gridSize;
        Vector3 newPos = transform.position + gridDirection;

        if (IsPositionBlocked(newPos))
        {
            Debug.Log($"❌ ไม่สามารถผลักได้ - มีสิ่งกีดขวาง | Block: {gameObject.name}");
            ShowBlockedEffect();
            return false;
        }

        if (blockRigidbody != null)
        {
            blockRigidbody.useGravity = false;
            blockRigidbody.isKinematic = true;
        }

        lastMoveDirection = direction.normalized;
        targetPosition = newPos;
        isMoving = true;

        Debug.Log($"💪 ผลักบล็อค {gameObject.name} จาก {transform.position} ไป {newPos}");
        Debug.Log($"📍 เก็บทิศทางการเคลื่อนที่: {lastMoveDirection}");

        return true;
    }

    void ShowBlockedEffect()
    {
        Debug.Log($"🚫 Block Push Effect - Block: {gameObject.name}");
        // เพิ่ม Effect หรือ Sound ได้ที่นี่
    }

    bool IsPositionBlocked(Vector3 position)
    {
        // เช็คกำแพง
        Collider[] wallHits = Physics.OverlapSphere(position, 0.8f, wallLayer);
        if (wallHits.Length > 0)
        {
            Debug.Log($"🧱 ติดกำแพง | Block: {gameObject.name}");
            return true;
        }

        // เช็คบล็อกอื่น
        Collider[] blockHits = Physics.OverlapSphere(position, 0.8f, blockLayer);
        foreach (Collider hit in blockHits)
        {
            if (hit != null && hit.gameObject != gameObject)
            {
                Debug.Log($"📦 ติดบล็อคอื่น | Block: {gameObject.name}");
                return true;
            }
        }

        return false;
    }

    #endregion

    #region Push Counter System

    void IncrementPushCounter()
    {
        // ลด Counter ของบล็อกอื่น
        var blockList = new List<IceBlock>(blockCounters.Keys);
        foreach (var block in blockList)
        {
            if (block != null && block != this && block.currentPushCount > 0)
            {
                block.DecrementCounter();
                Debug.Log($"📉 ลด Counter ของ Block อื่น: {block.name} | Counter: {block.currentPushCount}/{block.maxPushCount}");
            }
        }

        currentPushCount++;
        blockCounters[this] = currentPushCount;
        lastPushedBlock = this;

        Debug.Log($"📈 เพิ่ม Counter: {gameObject.name} | Count: {currentPushCount}/{maxPushCount}");

        // Haptic Feedback
        if (enableBlockHaptics && hapticManager != null)
        {
            hapticManager.PlayPushCountIncrementFeedback(currentPushCount);

            if (isContinuousHapticActive)
            {
                hapticManager.UpdateContinuousPushCount(currentPushCount);
            }

            Debug.Log($"🎮 Played push count increment feedback: Level {currentPushCount}");
        }

        UpdateCounterDisplay();
        ChangeGrabVisual(false);

        // ถ้าผลักครบ → ทำลายบล็อก
        if (currentPushCount >= maxPushCount)
        {
            DestroyBlock();
        }
    }

    void DecrementCounter()
    {
        if (currentPushCount > 0)
        {
            currentPushCount--;
            blockCounters[this] = currentPushCount;
            UpdateCounterDisplay();
            ChangeGrabVisual(false);

            if (enableBlockHaptics && hapticManager != null && isContinuousHapticActive)
            {
                hapticManager.UpdateContinuousPushCount(currentPushCount);
            }

            Debug.Log($"📉 ลด Counter: {gameObject.name} | Count: {currentPushCount}/{maxPushCount}");
        }
    }

    void UpdateCounterDisplay()
    {
        if (counterText != null)
        {
            counterText.text = $"{currentPushCount}/{maxPushCount}";
            float ratio = (float)currentPushCount / maxPushCount;
            counterText.color = Color.Lerp(Color.white, Color.red, ratio);
        }
    }

    void DestroyBlock()
    {
        Debug.Log($"💥 Block {gameObject.name} ถูกทำลาย! (ผลักครบ {maxPushCount} ครั้ง)");

        // Haptic Feedback
        if (enableBlockHaptics && hapticManager != null)
        {
            if (isContinuousHapticActive)
            {
                hapticManager.StopContinuousPushFeedback();
                isContinuousHapticActive = false;
            }

            hapticManager.PlayBlockExplosionFeedback();
            Debug.Log($"💥 Played explosion haptic feedback for block: {gameObject.name}");
        }

        DecreaseHP();

        if (blockCounters.ContainsKey(this))
        {
            blockCounters.Remove(this);
        }

        if (lastPushedBlock == this)
        {
            lastPushedBlock = null;
        }

        if (fallCheckCoroutine != null)
        {
            StopCoroutine(fallCheckCoroutine);
        }

        OnBlockDestroyed?.Invoke(this);
        ShowDestroyEffect();
        Destroy(gameObject);
    }

    void ShowDestroyEffect()
    {
        Debug.Log("💥 เอฟเฟกต์การทำลาย Block");
        // เพิ่ม Particle Effects ได้ที่นี่
    }

    #endregion

    #region Ice Sliding System

    void CheckIceSliding()
    {
        bool onIce = IsOnIceSurface(transform.position);

        Debug.Log($"🔍 CheckIceSliding: Block {gameObject.name} | Position: {transform.position} | OnIce: {onIce}");

        if (onIce)
        {
            Debug.Log($"🧊 Block {gameObject.name} อยู่บนพื้นน้ำแข็ง! เริ่มการเลื่อน...");

            Vector3 slideDirection = GetLastMoveDirection();

            Debug.Log($"📍 ทิศทางที่เก็บไว้: {slideDirection}");

            if (slideDirection != Vector3.zero)
            {
                Debug.Log($"⚡ เลื่อนต่อในทิศทาง: {slideDirection}");
                StartCoroutine(IceSlideSequence(slideDirection));
                return;
            }
            else
            {
                Debug.Log("❌ ไม่พบทิศทางการเคลื่อนที่");
            }
        }
        else
        {
            Debug.Log($"❄️ Block {gameObject.name} ไม่อยู่บนน้ำแข็ง");
        }

        OnBlockMoved?.Invoke();
        CheckGoalsWhenStopped();
    }

    bool IsOnIceSurface(Vector3 position)
    {
        Collider[] iceColliders = Physics.OverlapSphere(position, 1.0f);

        Debug.Log($"🔍 IsOnIceSurface: Position {position} | Found {iceColliders.Length} colliders");

        foreach (Collider col in iceColliders)
        {
            if (col != null)
            {
                Debug.Log($"   - Collider: {col.name} | Tag: {col.tag}");

                if (col.CompareTag("Ice"))
                {
                    Debug.Log($"🧊 ✅ ตรวจพบพื้นน้ำแข็งที่ {position} | Object: {col.name}");
                    return true;
                }
            }
        }

        Debug.Log($"❄️ ❌ ไม่พบพื้นน้ำแข็งที่ {position}");
        return false;
    }

    IEnumerator IceSlideSequence(Vector3 direction)
    {
        yield return new WaitForSeconds(0.1f);

        Vector3 slideDirection = direction * gridSize;
        Vector3 nextPosition = transform.position + slideDirection;

        Debug.Log($"🎯 กำลังเช็คการเลื่อนจาก {transform.position} ไป {nextPosition}");

        if (!IsPositionBlocked(nextPosition))
        {
            Debug.Log($"✅ เลื่อนได้! Block {gameObject.name} เลื่อนไป {nextPosition}");

            if (blockRigidbody != null)
            {
                blockRigidbody.useGravity = false;
                blockRigidbody.isKinematic = true;
            }

            lastMoveDirection = direction;
            targetPosition = nextPosition;
            isMoving = true;
        }
        else
        {
            Debug.Log($"🛑 การเลื่อนหยุด - มีสิ่งกีดขวางที่ {nextPosition}");

            lastMoveDirection = Vector3.zero;
            OnBlockMoved?.Invoke();
            CheckGoalsWhenStopped();
        }
    }

    void CheckGoalsWhenStopped()
    {
        lastMoveDirection = Vector3.zero;

        if (goalManager != null)
        {
            Debug.Log($"🎯 Block {gameObject.name} หยุดเคลื่อนที่ - เช็คเป้าหมาย");
            goalManager.CheckGoals();
        }
    }

    Vector3 GetLastMoveDirection()
    {
        Debug.Log($"📍 ทิศทางการเคลื่อนที่ล่าสุด: {lastMoveDirection}");
        return lastMoveDirection;
    }

    #endregion

    #region Debug & Utility

    void DebugNearbyIceObjects()
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 3f);

        Debug.Log($"🔍 Debug: Nearby objects within 3f radius of {gameObject.name}:");

        foreach (Collider col in nearbyColliders)
        {
            if (col != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                Debug.Log($"   - {col.name} | Tag: {col.tag} | Distance: {distance:F2}");

                if (col.CompareTag("Ice"))
                {
                    Debug.Log($"     🧊 Ice object found!");
                }
            }
        }
    }

    public bool IsOnGoal()
    {
        Collider[] goalColliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (Collider col in goalColliders)
        {
            if (col != null && col.CompareTag("Goal"))
                return true;
        }
        return false;
    }

    #endregion

    #region Haptic Control Methods

    /// <summary>
    /// เปิด/ปิด Block Haptics
    /// </summary>
    public void SetBlockHapticsEnabled(bool enabled)
    {
        enableBlockHaptics = enabled;
        Debug.Log($"🔧 Block Haptics {gameObject.name}: {(enabled ? "Enabled" : "Disabled")}");

        if (!enabled && hapticManager != null && isContinuousHapticActive)
        {
            hapticManager.StopContinuousPushFeedback();
            isContinuousHapticActive = false;
        }
    }

    /// <summary>
    /// ตรวจสอบสถานะการสั่นต่อเนื่อง
    /// </summary>
    public bool IsContinuousHapticActive()
    {
        return isContinuousHapticActive;
    }

    /// <summary>
    /// บังคับหยุดการสั่นต่อเนื่อง
    /// </summary>
    public void ForceStopContinuousHaptic()
    {
        if (hapticManager != null && isContinuousHapticActive)
        {
            hapticManager.StopContinuousPushFeedback();
            isContinuousHapticActive = false;
            wasTriggersPressed = false;
            Debug.Log($"🎮 Force stopped continuous haptic for block: {gameObject.name}");
        }
    }

    #endregion

    #region Context Menu (Debug)

    [ContextMenu("Test Push Count Haptics")]
    void TestPushCountHaptics()
    {
        if (hapticManager != null)
        {
            Debug.Log($"🧪 Testing haptic feedback for push count: {currentPushCount}");
            hapticManager.PlayPushCountIncrementFeedback(currentPushCount);
        }
        else
        {
            Debug.LogWarning("❌ HapticManager not found!");
        }
    }

    [ContextMenu("Test Continuous Haptic")]
    void TestContinuousHaptic()
    {
        if (hapticManager != null)
        {
            if (!isContinuousHapticActive)
            {
                Debug.Log($"🧪 Testing continuous haptic feedback - Push Count: {currentPushCount}");
                hapticManager.StartContinuousPushFeedback(currentPushCount);
                isContinuousHapticActive = true;
            }
            else
            {
                Debug.Log("🧪 Stopping continuous haptic feedback");
                hapticManager.StopContinuousPushFeedback();
                isContinuousHapticActive = false;
            }
        }
        else
        {
            Debug.LogWarning("❌ HapticManager not found!");
        }
    }

    [ContextMenu("Test Explosion Haptic")]
    void TestExplosionHaptic()
    {
        if (hapticManager != null)
        {
            Debug.Log("🧪 Testing explosion haptic feedback");
            hapticManager.PlayBlockExplosionFeedback();
        }
        else
        {
            Debug.LogWarning("❌ HapticManager not found!");
        }
    }

    [ContextMenu("Test Ice Detection")]
    void TestIceDetection()
    {
        bool onIce = IsOnIceSurface(transform.position);
        Debug.Log($"Manual Test: OnIce = {onIce}");
        DebugNearbyIceObjects();
    }

    [ContextMenu("Reset HP")]
    public static void ResetHP()
    {
        currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP);
        Debug.Log($"🔄 HP รีเซ็ต: {currentHP}/{maxHP}");
    }

    [ContextMenu("Decrease HP (Test)")]
    public static void TestDecreaseHP()
    {
        DecreaseHP();
    }

    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // บล็อก
        Gizmos.color = isGrabbed ? Color.yellow : (isMoving ? Color.green : Color.white);
        Gizmos.DrawWireCube(transform.position, Vector3.one);

        // Grab Range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, grabRange);

        // Push Detection Range
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, pushDetectionRange);

        // Ground Detection
        Gizmos.color = hasValidGround ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 1.0f);

        // Fall Threshold
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 2f, fallYThreshold, transform.position.z),
            new Vector3(transform.position.x + 2f, fallYThreshold, transform.position.z)
        );

        // Player Connection
        if (playerTransform != null && IsPlayerNearForGrab())
        {
            Gizmos.color = isGrabbed ? Color.yellow : Color.gray;
            Gizmos.DrawLine(transform.position, playerTransform.position);

            if (isGrabbed)
            {
                Vector3 pushDir = CalculatePushDirection() * gridSize;
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, pushDir);
            }
        }

        // Ice Sliding Preview
        if (IsOnIceSurface(transform.position) && lastMoveDirection != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, lastMoveDirection * gridSize);

            Vector3 nextSlidePos = transform.position + (lastMoveDirection * gridSize);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(nextSlidePos, Vector3.one * 0.8f);
        }

        // Continuous Haptic Indicator
        if (isContinuousHapticActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.3f);
        }

#if UNITY_EDITOR
        // Label
        if (Application.isPlaying)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f,
                $"HP: {currentHP}/{maxHP}\n" +
                $"Count: {currentPushCount}/{maxPushCount}\n" +
                $"LastDir: {lastMoveDirection}\n" +
                $"OnIce: {IsOnIceSurface(transform.position)}\n" +
                $"ValidGround: {hasValidGround}\n" +
                $"Y: {transform.position.y:F1}\n" +
                $"Haptic: {isContinuousHapticActive}\n" +
                $"Triggers: {wasTriggersPressed}");
        }
#endif
    }

    #endregion
}