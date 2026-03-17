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

    [Header("Puzzle Door")]
    [SerializeField] private GameObject puzzleDoor;

    [Header("HP Settings")]
    [SerializeField] private int maxHP = 3;
    private int currentHP;

    [Header("Settings")]
    [SerializeField] private bool showIntroOnStart = true;
    [SerializeField] private bool autoStartOnSceneLoad = true;

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

        // ✅ Subscribe Events
        IceBlock.OnHPChanged += OnHPChanged;
        IceBlock.OnGameRestart += OnGameRestart;

        // ✅ เริ่ม Puzzle อัตโนมัติ (ถ้าต้องการ)
        if (autoStartOnSceneLoad && !puzzleInitialized)
        {
            EnterPuzzle();
        }
    }

    #region Puzzle Entry/Exit

    /// <summary>
    /// ✅ เข้าสู่โหมด Ice Puzzle
    /// </summary>
    public void EnterPuzzle()
    {
        if (isPuzzleActive)
        {
            Debug.LogWarning("⚠️ Puzzle already active!");
            return;
        }

        Debug.Log("🧊 Entering Ice Puzzle...");

        // 1. Warp Player (ถ้ามี Spawn Point)
        if (player != null && puzzleSpawnPoint != null)
        {
            WarpPlayer();
        }

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

    IEnumerator PuzzleStartSequence()
    {
        // 1. แสดง Intro Screen
        if (introScreenUI != null && showIntroOnStart)
        {
            introScreenUI.Show();
            yield return new WaitForSeconds(3.5f); // รอ Intro จบ
        }

        // 2. เปิดโหมด Puzzle
        ActivatePuzzleMode();
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

        // 3. ปิด Door
        if (puzzleDoor != null)
        {
            puzzleDoor.SetActive(false);
            Debug.Log("🚪 Door removed!");
        }

        // 4. ออกจาก Puzzle Mode
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

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        IceBlock.OnHPChanged -= OnHPChanged;
        IceBlock.OnGameRestart -= OnGameRestart;
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