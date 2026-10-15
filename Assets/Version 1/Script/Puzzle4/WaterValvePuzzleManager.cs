using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

public class WaterValvePuzzleManager : MonoBehaviour
{
    [Header("Puzzle Components")]
    public List<Valve> valves = new List<Valve>();
    public List<WaterTile> waterTiles = new List<WaterTile>();
    public Transform waterSource;
    public Transform symbolTarget;
    public PuzzleTargetTrigger targetTrigger;
    public GameObject puzzleDoor;

    [Header("Player Settings")]
    public Transform player;
    public Transform puzzleSpawnPoint;
    public PlayerInput playerInput;

    [Header("Puzzle State")]
    public int currentHearts = 3;
    public int maxHearts = 3;
    public bool isPuzzleActive = false;
    public bool isPuzzleSolved = false;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationCompleteDelay = 0.5f;
    [SerializeField] private float valveBrokenDelay = 1.0f;

    [Header("Puzzle Check Settings")]
    [SerializeField] private float puzzleCheckInterval = 1f;
    private Coroutine puzzleCheckCoroutine;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("UI")]
    [SerializeField] private IntroScreenUI introScreenUI;
    [SerializeField] private InGameHUD inGameHUD;
    [SerializeField] private PuzzleClearUI puzzleClearUI;
    [SerializeField] private GameObject overworldHUD;

    private Valve currentInteractingValve;
    private InputAction interactAction;
    private InputAction exitInteractAction;
    private InputAction rotateAction;
    private InputAction lookAction;
    private InputAction jumpAction;

    // Rotation detection
    private enum RotationState { Neutral, Right, Up, Left, Down }
    private RotationState currentRotationState = RotationState.Neutral;
    private RotationState startRotationState = RotationState.Neutral;
    private List<RotationState> rotationPath = new List<RotationState>();
    private bool isRotating = false;
    private float rotationCooldown = 0.3f;
    private float lastRotationTime = 0f;

    #region Initialization

    void Start()
    {
        // Setup Input Actions from Puzzle4 ActionMap
        var puzzle4Map = playerInput.actions.FindActionMap("Puzzle4");
        interactAction = puzzle4Map.FindAction("InteractValve");
        exitInteractAction = puzzle4Map.FindAction("ExitInteract");
        rotateAction = puzzle4Map.FindAction("RotateValve");
        lookAction = puzzle4Map.FindAction("Look");
        jumpAction = puzzle4Map.FindAction("Jump");

        interactAction.performed += OnInteract;
        exitInteractAction.performed += OnExitInteract;
        rotateAction.performed += OnRotateValve;

        if (puzzleDoor != null)
        {
            puzzleDoor.SetActive(true);
        }

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"╔═══════════════════════════════════════════╗");
            Debug.Log($"║  🌊 PUZZLE INITIALIZATION                ║");
            Debug.Log($"╠═══════════════════════════════════════════╣");
            Debug.Log($"║  Hearts: {currentHearts}/{maxHearts}                             ║");
            Debug.Log($"║  Valves: {valves.Count}                                ║");
            Debug.Log($"║  Tiles: {waterTiles.Count}                                ║");
            Debug.Log($"╚═══════════════════════════════════════════╝");
        }
    }

    void OnDestroy()
    {
        if (interactAction != null) interactAction.performed -= OnInteract;
        if (exitInteractAction != null) exitInteractAction.performed -= OnExitInteract;
        if (rotateAction != null) rotateAction.performed -= OnRotateValve;

        StopPuzzleCheck();
    }

    #endregion

    #region Puzzle Entry/Exit

    public void EnterPuzzle()
    {
        if (isPuzzleActive)
        {
            Debug.LogWarning("⚠️ Puzzle already active!");
            return;
        }

        // Warp player to puzzle area
        if (player != null && puzzleSpawnPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.position = puzzleSpawnPoint.position;
                player.rotation = puzzleSpawnPoint.rotation;
                cc.enabled = true;
            }
        }

        // ✅ ซ่อน HUD หลักทันทีตอนเข้าห้อง
        if (overworldHUD != null)
            overworldHUD.SetActive(false);

        if (introScreenUI != null)
        {
            introScreenUI.Show();
        }

        float switchDelay = introScreenUI != null ? 3.5f : 0f;
        Invoke(nameof(ActivatePuzzleMode), switchDelay);
    }
    void ActivatePuzzleMode()
    {
        // Switch to Puzzle4 ActionMap
        playerInput.SwitchCurrentActionMap("Puzzle4");
        isPuzzleActive = true;

        if (inGameHUD != null)
        {
            inGameHUD.Show(currentHearts, maxHearts);
        }

        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║     🌊 WATER VALVE PUZZLE MODE 🌊        ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log("║  Controls:                                ║");
        Debug.Log("║  ⭕ Circle  = Select Valve                ║");
        Debug.Log("║  ✕ X       = Deselect Valve              ║");
        Debug.Log("║  Right Stick → = Rotate +90°             ║");
        Debug.Log("║  Right Stick ← = Rotate -90°             ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log($"║  Hearts: ❤️  {currentHearts}/{maxHearts}                           ║");
        Debug.Log("╚═══════════════════════════════════════════╝");

        // ✅ เริ่ม Source น้ำ
        //InitializeWaterSource();

        // ✅ เริ่มตรวจสอบ Puzzle
        StartPuzzleCheck();
    }

    public void ExitPuzzle()
    {
        EnableLookAndJump();

        if (inGameHUD != null)
        {
            inGameHUD.HideImmediate();
        }

        // ✅ เปิด HUD หลักกลับมาตอนออกจากห้อง
        if (overworldHUD != null)
            overworldHUD.SetActive(true);

        playerInput.SwitchCurrentActionMap("Player");
        isPuzzleActive = false;
        currentInteractingValve = null;

        StopPuzzleCheck();

        if (showDebugLogs)
        {
            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║     ✅ EXITED PUZZLE MODE                 ║");
            Debug.Log("╚═══════════════════════════════════════════╝");
        }
    }

    #endregion


    #region Puzzle Check (Auto)

    /// <summary>
    /// ✅ เริ่มตรวจสอบ Puzzle อัตโนมัติ
    /// </summary>
    void StartPuzzleCheck()
    {
        if (puzzleCheckCoroutine != null)
        {
            StopCoroutine(puzzleCheckCoroutine);
        }

        puzzleCheckCoroutine = StartCoroutine(PuzzleCheckRoutine());

        if (showDebugLogs)
        {
            Debug.Log($"[Puzzle Check] ✅ Started auto-check (Interval: {puzzleCheckInterval}s)");
        }
    }

    /// <summary>
    /// ✅ หยุดตรวจสอบ Puzzle
    /// </summary>
    void StopPuzzleCheck()
    {
        if (puzzleCheckCoroutine != null)
        {
            StopCoroutine(puzzleCheckCoroutine);
            puzzleCheckCoroutine = null;

            if (showDebugLogs)
            {
                Debug.Log("[Puzzle Check] ⏹️ Stopped auto-check");
            }
        }
    }

    /// <summary>
    /// ✅ Coroutine ตรวจสอบทุกช่วงเวลา
    /// </summary>
    IEnumerator PuzzleCheckRoutine()
    {
        while (isPuzzleActive && !isPuzzleSolved)
        {
            CheckPuzzleComplete();
            yield return new WaitForSeconds(puzzleCheckInterval);
        }
    }

    /// <summary>
    /// ✅ ตรวจสอบว่า Puzzle แก้สำเร็จหรือไม่
    /// </summary>
    void CheckPuzzleComplete()
    {
        if (targetTrigger == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning("[Puzzle Check] ⚠️ Target Trigger is NULL!");
            }
            return;
        }

        // ⭐ เช็คว่า Target มีน้ำหรือไม่
        if (targetTrigger.HasWater())
        {
            if (showDebugLogs)
            {
                Debug.Log("╔══════════════════════════════════════════╗");
                Debug.Log("║ [Puzzle Check] ✅ TARGET HAS WATER!      ║");
                Debug.Log("╠══════════════════════════════════════════╣");
                Debug.Log($"║  Connected Point: {targetTrigger.connectedPoint.gameObject.name}");
                Debug.Log($"║  Has Water: {targetTrigger.connectedPoint.hasWater}");
                Debug.Log("╚══════════════════════════════════════════╝");
            }

            PuzzleSolved();
        }
    }

    #endregion

    #region Input Handlers

    void OnInteract(InputAction.CallbackContext context)
    {
        if (!isPuzzleActive || isPuzzleSolved) return;

        Valve targetValve = DetectValveInRange();

        if (targetValve != null && !targetValve.isBroken)
        {
            currentInteractingValve = targetValve;
            DisableLookAndJump();

            if (showDebugLogs)
            {
                Debug.Log($"[⭕ Interact] ✅ Selected: {targetValve.gameObject.name}");
            }
        }
        else if (targetValve != null && targetValve.isBroken)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[⭕ Interact] ❌ {targetValve.gameObject.name} is BROKEN!");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("[⭕ Interact] ⚠️ No valve nearby");
            }
        }
    }

    void OnExitInteract(InputAction.CallbackContext context)
    {
        if (currentInteractingValve != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[✕ Exit] Deselected {currentInteractingValve.gameObject.name}");
            }

            currentInteractingValve = null;
            EnableLookAndJump();
        }
    }

    void OnRotateValve(InputAction.CallbackContext context)
    {
        if (currentInteractingValve == null)
        {
            if (showDebugLogs)
            {
                Debug.Log("[Right Stick] ⚠️ No valve selected");
            }
            return;
        }

        if (Time.time - lastRotationTime < rotationCooldown)
        {
            return;
        }

        Vector2 stickInput = context.ReadValue<Vector2>();
        DetectCircularMotion(stickInput);
    }

    #endregion

    #region Rotation Detection

    void DetectCircularMotion(Vector2 stickInput)
    {
        float threshold = 0.7f;
        float angle = Mathf.Atan2(stickInput.y, stickInput.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        RotationState newState = GetRotationZone(stickInput, angle, threshold);

        if (newState == RotationState.Neutral)
        {
            if (isRotating)
            {
                ResetRotation();
            }
            return;
        }

        if (!isRotating)
        {
            StartRotation(newState);
            return;
        }

        if (newState != currentRotationState)
        {
            ProcessRotationStep(newState);
        }
    }

    void StartRotation(RotationState state)
    {
        isRotating = true;
        startRotationState = state;
        currentRotationState = state;
        rotationPath.Clear();
        rotationPath.Add(state);
    }

    void ProcessRotationStep(RotationState newState)
    {
        if (!IsValidTransition(currentRotationState, newState))
        {
            ResetRotation();
            return;
        }

        rotationPath.Add(newState);

        if (rotationPath.Count >= 5 && newState == startRotationState)
        {
            CheckRotationDirection();
            ResetRotation();
            return;
        }

        currentRotationState = newState;

        if (rotationPath.Count > 8)
        {
            ResetRotation();
        }
    }

    bool IsValidTransition(RotationState from, RotationState to)
    {
        // Clockwise transitions
        if (from == RotationState.Right && to == RotationState.Down) return true;
        if (from == RotationState.Down && to == RotationState.Left) return true;
        if (from == RotationState.Left && to == RotationState.Up) return true;
        if (from == RotationState.Up && to == RotationState.Right) return true;

        // Counter-Clockwise transitions
        if (from == RotationState.Right && to == RotationState.Up) return true;
        if (from == RotationState.Up && to == RotationState.Left) return true;
        if (from == RotationState.Left && to == RotationState.Down) return true;
        if (from == RotationState.Down && to == RotationState.Right) return true;

        return false;
    }

    void CheckRotationDirection()
    {
        if (rotationPath.Count < 5) return;

        bool isClockwise = IsClockwiseRotation(rotationPath);

        if (isClockwise)
        {
            ExecuteClockwiseRotation();
        }
        else
        {
            ExecuteCounterClockwiseRotation();
        }
    }

    bool IsClockwiseRotation(List<RotationState> path)
    {
        if (path.Count < 2) return true;

        RotationState first = path[0];
        RotationState second = path[1];

        if (first == RotationState.Right && second == RotationState.Down) return true;
        if (first == RotationState.Down && second == RotationState.Left) return true;
        if (first == RotationState.Left && second == RotationState.Up) return true;
        if (first == RotationState.Up && second == RotationState.Right) return true;

        return false;
    }

    void ResetRotation()
    {
        isRotating = false;
        rotationPath.Clear();
        startRotationState = RotationState.Neutral;
        currentRotationState = RotationState.Neutral;
    }

    RotationState GetRotationZone(Vector2 stick, float angle, float threshold)
    {
        float magnitude = stick.magnitude;

        if (magnitude < threshold)
        {
            return RotationState.Neutral;
        }

        if (angle >= 315f || angle < 45f) return RotationState.Right;
        else if (angle >= 45f && angle < 135f) return RotationState.Up;
        else if (angle >= 135f && angle < 225f) return RotationState.Left;
        else if (angle >= 225f && angle < 315f) return RotationState.Down;

        return RotationState.Neutral;
    }

    void ExecuteClockwiseRotation()
    {
        if (currentInteractingValve != null)
        {
            currentInteractingValve.RotateClockwise();
            lastRotationTime = Time.time;

            Invoke(nameof(RestoreControlsAfterRotation), rotationCompleteDelay);
        }
    }

    void ExecuteCounterClockwiseRotation()
    {
        if (currentInteractingValve != null)
        {
            currentInteractingValve.RotateCounterClockwise();
            lastRotationTime = Time.time;

            Invoke(nameof(RestoreControlsAfterRotation), rotationCompleteDelay);
        }
    }

    void RestoreControlsAfterRotation()
    {
        EnableLookAndJump();
        currentInteractingValve = null;
    }

    #endregion

    #region Valve Events

    public void OnValveBroken(Valve brokenValve)
    {
        if (currentInteractingValve == brokenValve)
        {
            Invoke(nameof(RestoreControlsAfterValveBreak), valveBrokenDelay);
        }
    }

    void RestoreControlsAfterValveBreak()
    {
        EnableLookAndJump();
        currentInteractingValve = null;
    }

    #endregion

    #region Valve Detection

    Valve DetectValveInRange()
    {
        if (player == null) return null;

        float detectionRadius = 3f;
        Collider[] hits = Physics.OverlapSphere(player.position, detectionRadius);

        Valve nearestValve = null;
        float nearestDistance = float.MaxValue;

        foreach (Collider hit in hits)
        {
            Valve valve = hit.GetComponent<Valve>();
            if (valve != null)
            {
                float distance = Vector3.Distance(player.position, valve.transform.position);

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestValve = valve;
                }
            }
        }

        return nearestValve;
    }

    #endregion

    #region Puzzle Complete

    void PuzzleSolved()
    {
        isPuzzleSolved = true;

        if (Inventory.Instance != null)
        {
            Inventory.Instance.CorrectPuzzle4 = true;
            Debug.Log("[WaterValvePuzzleManager] ✅ CorrectPuzzle4 = true");
        }
        else
        {
            Debug.LogWarning("⚠️ Inventory.Instance not found! ไม่สามารถบันทึก CorrectPuzzle4 ได้");
        }

        if (puzzleDoor != null)
        {
            puzzleDoor.SetActive(false);
        }
        if (inGameHUD != null)
            inGameHUD.HideImmediate();
        if (puzzleClearUI != null)
        {
            puzzleClearUI.Show();
        }

        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0.5f, 0.5f);
            Invoke(nameof(StopHaptics), 1f);
        }

        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║     🎉 PUZZLE SOLVED! 🎉                 ║");
        Debug.Log("╚═══════════════════════════════════════════╝");

        Invoke(nameof(ExitPuzzle), 2f);
    }

    #endregion

    #region Hearts System

    public void LoseHeart()
    {
        if (currentHearts > 0)
        {
            currentHearts--;

            if (inGameHUD != null)
            {
                inGameHUD.UpdateHearts(currentHearts);
            }

            Debug.Log($"╔═══════════════════════════════════════════╗");
            Debug.Log($"║     💔 LOST A HEART!                      ║");
            Debug.Log($"║     Remaining: ❤️  {currentHearts}/{maxHearts}                    ║");
            Debug.Log($"╚═══════════════════════════════════════════╝");

            if (Gamepad.current != null)
            {
                Gamepad.current.SetMotorSpeeds(1f, 1f);
                Invoke(nameof(StopHaptics), 2f);
            }

            if (currentHearts <= 0)
            {
                GameOver();
            }
        }
    }

    void GameOver()
    {
        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║     ☠️  GAME OVER                         ║");
        Debug.Log("╚═══════════════════════════════════════════╝");

        if (player != null && puzzleSpawnPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.position = puzzleSpawnPoint.position;
                cc.enabled = true;
            }
        }

        Invoke(nameof(FullResetPuzzle), 3f);
        if (inGameHUD != null)
        {
            // ถ้ายังไม่โชว์ HUD ก็ให้โชว์ด้วย
            inGameHUD.Show(currentHearts, maxHearts);
        }

    }

    #endregion

    #region Reset

    public void FullResetPuzzle()
    {
        currentHearts = maxHearts;

        // ✅ รีเซ็ตทุก Tile ก่อน
        foreach (WaterTile tile in waterTiles)
        {
            if (tile != null)
            {
                tile.ResetTile();
            }
        }

        // ✅ รีเซ็ตทุก Valve
        foreach (Valve valve in valves)
        {
            if (valve != null)
            {
                valve.ResetValve();
            }
        }

        isPuzzleSolved = false;

        RestartWaterSource();

        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0.3f, 0.8f);
            Invoke(nameof(StopHaptics), 1f);
        }

        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║     🔄 FULL RESET                         ║");
        Debug.Log("╚═══════════════════════════════════════════╝");

    }
    void RestartWaterSource()
    {
        // ⭐ หา Source Tile จาก waterTiles List
        WaterTile sourceTile = null;

        foreach (WaterTile tile in waterTiles)
        {
            if (tile == null) continue;

            PipeStateController state = tile.GetComponent<PipeStateController>();
            if (state != null && state.IsSource)
            {
                sourceTile = tile;
                break;
            }
        }

        // ⭐ ถ้าไม่เจอใน List → หาทั้ง Scene
        if (sourceTile == null)
        {
            WaterTile[] allTiles = FindObjectsOfType<WaterTile>();
            foreach (WaterTile tile in allTiles)
            {
                PipeStateController state = tile.GetComponent<PipeStateController>();
                if (state != null && state.IsSource)
                {
                    sourceTile = tile;
                    break;
                }
            }
        }

        // ⭐ เริ่มน้ำ
        if (sourceTile != null)
        {
            sourceTile.SetWaterStatus(true);

            if (showDebugLogs)
            {
                Debug.Log($"[Reset] ✅ Restarted water at {sourceTile.gameObject.name}");
            }
        }
        else
        {
            Debug.LogError("[Reset] ❌ No Source Tile found!");
        }
    }

    public void RepairAllBrokenValves()
    {
        int repairedCount = 0;

        foreach (Valve valve in valves)
        {
            if (valve.isBroken)
            {
                valve.RepairValve();
                repairedCount++;
            }
        }

        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0.5f, 0.2f);
            Invoke(nameof(StopHaptics), 0.5f);
        }

        Debug.Log($"╔═══════════════════════════════════════════╗");
        Debug.Log($"║     🔧 REPAIRED {repairedCount} VALVES                  ║");
        Debug.Log($"╚═══════════════════════════════════════════╝");
    }

    #endregion

    #region Helpers

    void DisableLookAndJump()
    {
        if (lookAction != null) lookAction.Disable();
        if (jumpAction != null) jumpAction.Disable();
    }

    void EnableLookAndJump()
    {
        if (lookAction != null) lookAction.Enable();
        if (jumpAction != null) jumpAction.Enable();
    }

    void StopHaptics()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    /// <summary>
    /// ✅ หา Tile ที่ใกล้ตำแหน่งที่สุด
    /// </summary>
    WaterTile FindNearestTile(Vector3 position)
    {
        WaterTile nearestTile = null;
        float minDistance = float.MaxValue;

        foreach (WaterTile tile in waterTiles)
        {
            if (tile == null) continue;

            float distance = Vector3.Distance(position, tile.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestTile = tile;
            }
        }

        if (showDebugLogs && nearestTile != null)
        {
            Debug.Log($"[Find Tile] Nearest to {position}: {nearestTile.gameObject.name} (Distance: {minDistance:F2})");
        }

        return nearestTile;
    }

    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        if (player == null) return;

        // วงกลมแสดงระยะตรวจจับ Valve
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, 3f);

#if UNITY_EDITOR
        foreach (Valve valve in valves)
        {
            if (valve != null)
            {
                float distance = Vector3.Distance(player.position, valve.transform.position);
                Gizmos.color = valve.isBroken ? Color.red : Color.green;
                Gizmos.DrawWireSphere(valve.transform.position, 0.5f);

                UnityEditor.Handles.Label(
                    valve.transform.position + Vector3.up,
                    $"{valve.gameObject.name}\n{distance:F1}m"
                );
            }
        }

        // ✅ แสดง Water Source
        if (waterSource != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(waterSource.position, 0.5f);
            UnityEditor.Handles.Label(waterSource.position + Vector3.up, "🌊 WATER SOURCE");
        }

        // ✅ แสดง Symbol Target
        if (symbolTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(symbolTarget.position, 0.5f);
            UnityEditor.Handles.Label(symbolTarget.position + Vector3.up, "🎯 TARGET");
        }
#endif
    }

    #endregion
}