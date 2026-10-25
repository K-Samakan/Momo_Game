// ============================================================
// FILE: IcePuzzleManager.cs
// PATH: Assets/Version 1/Script/Puzzle3/Com/IcePuzzleManager.cs
// ============================================================
// ✅ CHANGELOG
//   [NEW] overworldHUD — HUD หลัก (HP ผู้เล่น ฯลฯ) ซ่อนตอนเข้าห้อง Puzzle / เปิดคืนตอนออก
//         (แบบเดียวกับ Map 4: WaterValvePuzzleManager)
//   [NEW] EnterPuzzle() → ซ่อน overworldHUD ทันทีตอนวาปเข้าห้อง
//   [NEW] ExitPuzzle()  → เปิด overworldHUD กลับ (ทั้งตอนแก้สำเร็จ และตอน Fail ก่อนวาปกลับ)
//   [NEW] puzzleFailUI / failRespawnDelay — ตั้งใน Inspector ได้เหมือน Map 2 / Map 4
//   [NEW] HandlePuzzleFail() — IceBlock.RestartGame() เรียกตัวนี้แทนการเรียก PuzzleFailRespawn เอง
//   [NEW] Intro แบบ Map 2 — lockInputDuringScreens + สั่นสั้น + รอเท่า IntroScreenUI.TotalDuration
//         + isEntering กันกดแท่นซ้ำระหว่าง Intro
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ✅ จัดการ Puzzle Flow, UI, และ HP System สำหรับ Ice Puzzle
/// </summary>
public class IcePuzzleManager : MonoBehaviour
{
    public static IcePuzzleManager Instance { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform puzzleSpawnPoint;
    [SerializeField] private PlayerInput playerInput;

    [Header("Puzzle State")]
    public bool isPuzzleActive = false;
    public bool isPuzzleSolved = false;
    private static bool puzzleInitialized = false;

    [Header("UI References")]
    [SerializeField] private IntroScreenUI introScreenUI;
    [SerializeField] private PuzzleClearUI puzzleClearUI;
    [SerializeField] private InGameHUD inGameHUD;

    // ✅ NEW: HUD หลักของเกม (ซ่อนระหว่างเล่น Puzzle)
    [Header("Main HUD (ซ่อนระหว่างเล่น Puzzle)")]
    [Tooltip("ลาก GameObject ของ HUD หลัก (HP ผู้เล่น / ไอเทม ฯลฯ) — ห้ามลาก Canvas ที่มี InGameHUD ของ Puzzle อยู่ข้างใน")]
    [SerializeField] private GameObject overworldHUD;

    // ✅ NEW: Fail → Respawn (เหมือน Map 2 / Map 4)
    [Header("Fail → Respawn (กลับ SavePoint หน้าห้อง)")]
    [Tooltip("UI 'ปริศนาล้มเหลว' (ปิดไว้ก่อน) — ไม่ใส่ก็ได้")]
    [SerializeField] private GameObject puzzleFailUI;

    [Tooltip("เวลารอก่อนวาปกลับ (วินาที)")]
    [SerializeField] private float failRespawnDelay = 2.5f;

    [Header("Puzzle Door")]
    [SerializeField] private GameObject puzzleDoor;

    [Header("HP Settings")]
    [SerializeField] private int maxHP = 3;
    private int currentHP;

    [Header("Settings")]
    [SerializeField] private bool showIntroOnStart = true;

    // ✅ NEW: เหมือน Map 2
    [Tooltip("ล็อกการควบคุมผู้เล่นระหว่างหน้าจอ Intro / Clear")]
    [SerializeField] private bool lockInputDuringScreens = true;

    [Tooltip("รอ Intro กี่วินาที (0 = ใช้เวลาจริงของ IntroScreenUI = fadeIn + display + fadeOut)")]
    [SerializeField] private float introWaitTime = 0f;

    private bool isEntering = false;   // ✅ NEW: กันกดแท่นซ้ำระหว่าง Intro

    // ✅ แก้แล้ว: ปิด auto-start เป็นค่า default
    //    ผู้เล่นต้องกดแท่น warp trigger เอง (ผ่าน PuzzlePedestalTrigger.cs)
    //    ถึงจะเรียก EnterPuzzle() — ไม่ auto เข้าเองตอน Start()
    [SerializeField] private bool autoStartOnSceneLoad = false;

    private void Awake()
    {
        // Singleton Pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        currentHP = maxHP;

        // ✅ หา Player อัตโนมัติ
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log($"✅ Found Player: {playerObj.name}");
            }
        }

        // ✅ หา PlayerInput อัตโนมัติ
        if (playerInput == null && player != null)
        {
            playerInput = player.GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                Debug.Log("✅ Found PlayerInput");
            }
        }

        // ✅ ตั้งค่า Door
        if (puzzleDoor != null)
        {
            puzzleDoor.SetActive(true);
        }

        // ✅ NEW: ปิด Fail UI ไว้ก่อน
        if (puzzleFailUI != null)
        {
            puzzleFailUI.SetActive(false);
        }

        // ✅ NEW: เตือนถ้ายังไม่ได้ลาก HUD หลัก
        if (overworldHUD == null)
        {
            Debug.LogWarning("⚠️ [IcePuzzleManager] ยังไม่ได้ลาก Overworld HUD — HUD หลักจะไม่ถูกซ่อนตอนเล่น Puzzle");
        }

        // ✅ Subscribe Events
        IceBlock.OnHPChanged += OnHPChanged;
        IceBlock.OnGameRestart += OnGameRestart;

        // ✅ เริ่ม Puzzle อัตโนมัติ (เฉพาะถ้าเปิด autoStartOnSceneLoad ไว้ — ปกติจะปิด)
        if (autoStartOnSceneLoad && !puzzleInitialized)
        {
            EnterPuzzle();
        }
    }

    #region Puzzle Entry/Exit

    /// <summary>
    /// ✅ เข้าสู่โหมด Ice Puzzle — เรียกจาก PuzzlePedestalTrigger.cs ตอนผู้เล่นกดแท่น
    /// </summary>
    public void EnterPuzzle()
    {
        if (isPuzzleActive || isEntering)
        {
            Debug.LogWarning("⚠️ Puzzle already active / entering!");
            return;
        }
        isEntering = true;

        Debug.Log("🧊 Entering Ice Puzzle...");

        // 1. Warp Player (ถ้ามี Spawn Point)
        if (player != null && puzzleSpawnPoint != null)
        {
            WarpPlayer();
        }

        // ✅ NEW: ซ่อน HUD หลักทันทีตอนเข้าห้อง (ก่อน Intro Screen)
        SetOverworldHUDVisible(false);

        // 2. เริ่ม Puzzle Sequence
        StartCoroutine(PuzzleStartSequence());
    }

    void WarpPlayer()
    {
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.position = puzzleSpawnPoint.position;
            player.rotation = puzzleSpawnPoint.rotation;
            cc.enabled = true;

            Debug.Log($"✅ Warped Player to {puzzleSpawnPoint.name}");
        }
    }

    /// <summary>
    /// ✅ CHANGED: Intro แบบเดียวกับ Map 2 — ล็อก input + สั่นสั้นบอกว่าเข้าห้องแล้ว + รอ Intro จบพอดี
    /// </summary>
    IEnumerator PuzzleStartSequence()
    {
        // 1. แสดง Intro Screen
        if (showIntroOnStart)
        {
            if (introScreenUI != null)
            {
                if (lockInputDuringScreens && playerInput != null)
                    playerInput.DeactivateInput();

                introScreenUI.Show();

                // สั่นสั้น ๆ บอกผู้เล่นว่า "เข้าห้องปริศนาแล้ว" (สำคัญสำหรับผู้เล่นที่ไม่ได้ยินเสียง)
                if (HapticManager.Instance != null)
                    HapticManager.Instance.PlayPattern(HapticPatternType.Short);

                float wait = introWaitTime > 0f ? introWaitTime : introScreenUI.TotalDuration;
                yield return new WaitForSeconds(wait); // รอ Intro จบ

                if (lockInputDuringScreens && playerInput != null)
                    playerInput.ActivateInput();
            }
            else
            {
                Debug.LogWarning("⚠️ [IcePuzzleManager] ยังไม่ได้ลาก Intro Screen UI ใน Inspector — ข้าม Intro");
            }
        }

        // 2. เปิดโหมด Puzzle
        ActivatePuzzleMode();
        isEntering = false;
    }

    void ActivatePuzzleMode()
    {
        // Switch to Puzzle3 ActionMap
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Puzzle3");
            Debug.Log("🎮 Switched to Puzzle3 ActionMap");
        }

        isPuzzleActive = true;
        isPuzzleSolved = false;
        puzzleInitialized = true;

        // แสดง InGame HUD
        if (inGameHUD != null)
        {
            inGameHUD.Show(currentHP, maxHP);
            inGameHUD.UpdateObjective("นำบล็อกน้ำแข็งไปยังเป้าหมายทั้ง 3 จุด");
        }

        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║     🧊 ICE PUZZLE MODE ACTIVE 🧊         ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log("║  Controls:                                ║");
        Debug.Log("║  L2 + R2    = Grab Block                 ║");
        Debug.Log("║  Left Stick = Push Block                 ║");
        Debug.Log("╠═══════════════════════════════════════════╣");
        Debug.Log($"║  HP: ❤️  {currentHP}/{maxHP}                             ║");
        Debug.Log("╚═══════════════════════════════════════════╝");
    }

    /// <summary>
    /// ✅ ออกจากโหมด Puzzle
    /// </summary>
    public void ExitPuzzle()
    {
        isPuzzleActive = false;

        // ซ่อน InGame HUD
        if (inGameHUD != null)
        {
            inGameHUD.HideImmediate();
        }

        // ✅ NEW: เปิด HUD หลักกลับมา
        SetOverworldHUDVisible(true);

        // Switch back to Player ActionMap
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
            Debug.Log("🎮 Switched to Player ActionMap");
        }

        Debug.Log("╔═══════════════════════════════════════════╗");
        Debug.Log("║     ✅ EXITED PUZZLE MODE                 ║");
        Debug.Log("╚═══════════════════════════════════════════╝");
    }

    /// <summary>
    /// ✅ NEW: เปิด/ปิด HUD หลัก
    /// </summary>
    void SetOverworldHUDVisible(bool visible)
    {
        if (overworldHUD == null) return;

        overworldHUD.SetActive(visible);
        Debug.Log($"🖥️ [IcePuzzleManager] Overworld HUD = {(visible ? "ON" : "OFF")}");
    }

    #endregion

    #region Puzzle Complete

    /// <summary>
    /// ✅ เรียกเมื่อแก้ปริศนาสำเร็จ (จาก PuzzleGoalManager)
    /// </summary>
    public void OnPuzzleComplete()
    {
        if (isPuzzleSolved) return;

        isPuzzleSolved = true;
        isPuzzleActive = false;

        Debug.Log("🎉 PUZZLE COMPLETED! 🎉");

        StartCoroutine(PuzzleCompleteSequence());
    }

    IEnumerator PuzzleCompleteSequence()
    {
        // 1. ซ่อน InGame HUD
        if (inGameHUD != null)
        {
            inGameHUD.HideImmediate();
        }

        // 2. แสดง Clear UI
        if (puzzleClearUI != null)
        {
            puzzleClearUI.Show();
            yield return new WaitForSeconds(3.5f); // รอ ClearUI จบ
        }

        // 3. ปิด Door (บานที่ 1 — เปิดทางไปห้อง Item)
        if (puzzleDoor != null)
        {
            puzzleDoor.SetActive(false);
            Debug.Log("🚪 Door removed! (ทางไปห้อง Item เปิดแล้ว)");
        }

        // 4. ออกจาก Puzzle Mode (เปิด HUD หลักกลับด้วย)
        ExitPuzzle();

        Debug.Log("✅ Puzzle Complete Sequence Finished!");
    }

    #endregion

    #region HP System

    /// <summary>
    /// ✅ Event Handler เมื่อ HP เปลี่ยน
    /// </summary>
    void OnHPChanged(int newHP)
    {
        currentHP = newHP;

        // อัปเดต InGame HUD
        if (inGameHUD != null)
        {
            inGameHUD.UpdateHearts(currentHP);
        }

        Debug.Log($"💗 HP Changed: {currentHP}/{maxHP}");
    }

    /// <summary>
    /// ✅ Event Handler เมื่อเกมรีสตาร์ท
    /// </summary>
    void OnGameRestart()
    {
        currentHP = maxHP;
        isPuzzleActive = false;
        isPuzzleSolved = false;
        isEntering = false;
        puzzleInitialized = false;

        // รีเซ็ต UI
        if (inGameHUD != null)
        {
            inGameHUD.HideImmediate();
        }

        if (puzzleDoor != null)
        {
            puzzleDoor.SetActive(true);
        }

        Debug.Log("🔄 Puzzle Manager Reset");
    }

    /// <summary>
    /// ✅ NEW: HP หมด → ซ่อน Puzzle HUD → Fail UI + รอ → สลับ ActionMap เป็น Player → วาปกลับ SavePoint หน้าห้อง
    /// เรียกจาก IceBlock.RestartGame()
    /// </summary>
    public void HandlePuzzleFail()
    {
        Debug.Log("💀 [IcePuzzleManager] Puzzle Fail → กลับ SavePoint หน้าห้อง");

        isPuzzleActive = false;

        if (inGameHUD != null)
        {
            inGameHUD.HideImmediate();
        }

        // ExitPuzzle() จะถูกเรียกตอนก่อนโหลดฉาก (สลับ ActionMap กลับ Player + เปิด HUD หลัก)
        PuzzleFailRespawn.Trigger(this, puzzleFailUI, ExitPuzzle, failRespawnDelay);
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        IceBlock.OnHPChanged -= OnHPChanged;
        IceBlock.OnGameRestart -= OnGameRestart;

        // ✅ NEW: เคลียร์ Instance ให้ตัวใหม่ในฉากใหม่ขึ้นแทนได้ (หลัง Fail → โหลดฉาก)
        if (Instance == this) Instance = null;
    }

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (puzzleSpawnPoint == null) return;

#if UNITY_EDITOR
        // แสดง Spawn Point
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(puzzleSpawnPoint.position, 0.5f);
        UnityEditor.Handles.Label(
            puzzleSpawnPoint.position + Vector3.up,
            "🧊 ICE PUZZLE SPAWN"
        );

        // แสดงเส้นจาก Player ไป Spawn Point (ถ้ากำลังเล่น)
        if (Application.isPlaying && player != null)
        {
            float distance = Vector3.Distance(player.position, puzzleSpawnPoint.position);
            if (distance > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(player.position, puzzleSpawnPoint.position);
            }
        }
#endif
    }

    #endregion
}