using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;

public class PuzzleManager : MonoBehaviour
{
    public HapticManager hapticManager;

    [SerializeField] private GameObject selectedIconsContainer;
    [SerializeField] private GameObject shortIconPrefab;
    [SerializeField] private GameObject longIconPrefab;

    private HapticPatternType[] targetPattern = new HapticPatternType[5];
    private HapticPatternType[] playerPattern = new HapticPatternType[5];
    private int currentIndex = 0;
    private GameObject[] iconInstances = new GameObject[5];

    [SerializeField] private GameObject _puzzleCanvas;
    [SerializeField] private PlayerInput playerInput;

    [Header("Doors")]
    [SerializeField] private GameObject Door;      // ประตูหน้า → ห้อง Item (เปิดตอนแก้ Puzzle สำเร็จ)
    [SerializeField] private GameObject DoorBack;  // ประตูหลัง → เปิดตอนรับถุงมือ (ไม่ต้องทำอะไรตรงนี้)

    [Header("Hearts UI")]
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite redHeartSprite;
    [SerializeField] private Sprite blackHeartSprite;
    private int failCount = 0;

    [SerializeField] private GameObject unsolvedCanvas;

    [Header("UI")]
    [SerializeField] private IntroScreenUI introScreenUI;
    [SerializeField] private PuzzleClearUI puzzleClearUI;

    private bool isPuzzleActive = false;

    // ─────────────────────────────────────────────
    void Start()
    {
        if (hapticManager == null)
        {
            Debug.LogError("[PuzzleManager] ❌ HapticManager not assigned!");
            return;
        }
        if (selectedIconsContainer == null || shortIconPrefab == null || longIconPrefab == null)
        {
            Debug.LogError("[PuzzleManager] ❌ SelectedIconsContainer, ShortIconPrefab, or LongIconPrefab not assigned!");
            return;
        }

        UpdateHeartsUI();
        StartCoroutine(InitializePuzzle()); // ✅ เรียกแค่นี้อย่างเดียว
    }

    // ─────────────────────────────────────────────
    // Initialize
    // ─────────────────────────────────────────────
    IEnumerator InitializePuzzle()
    {
        // 1. แสดง Intro Screen
        if (introScreenUI != null)
        {
            introScreenUI.Show();
            yield return new WaitForSeconds(4f); // รอ Intro จบ (3s display + 1s buffer)
        }

        // 2. สร้าง Pattern
        GenerateRandomPattern();

        // 3. หน่วง 10 วินาที หลัง PuzzleCanvas แสดง
        yield return new WaitForSeconds(10f);

        // 4. เล่น Pattern ครั้งเดียว
        yield return StartCoroutine(PlayPatternWithDelay());

        // 5. เปิดให้ผู้เล่นเล่นได้
        isPuzzleActive = true;
        Debug.Log("[PuzzleManager] ✅ Puzzle is now active!");
    }

    // ─────────────────────────────────────────────
    // Pattern
    // ─────────────────────────────────────────────
    void GenerateRandomPattern()
    {
        for (int i = 0; i < 5; i++)
        {
            targetPattern[i] = UnityEngine.Random.value > 0.5f
                ? HapticPatternType.Short
                : HapticPatternType.Long;
        }
    }

    private IEnumerator PlayPatternWithDelay()
    {
        string patternLog = "[PuzzleManager] Target Pattern: ";
        for (int i = 0; i < 5; i++)
        {
            patternLog += targetPattern[i] == HapticPatternType.Short ? "S " : "L ";
            hapticManager.PlayPattern(targetPattern[i]);
            yield return new WaitForSeconds(1f);
        }
        Debug.Log(patternLog);
    }

    // ─────────────────────────────────────────────
    // Player Input
    // ─────────────────────────────────────────────
    public void OnShortVibration()
    {
        if (!isPuzzleActive) return;
        if (currentIndex < 5)
        {
            playerPattern[currentIndex] = HapticPatternType.Short;
            CreateIcon(HapticPatternType.Short);
            currentIndex++;
            CheckPattern();
        }
    }

    public void OnLongVibration()
    {
        if (!isPuzzleActive) return;
        if (currentIndex < 5)
        {
            playerPattern[currentIndex] = HapticPatternType.Long;
            CreateIcon(HapticPatternType.Long);
            currentIndex++;
            CheckPattern();
        }
    }

    // ─────────────────────────────────────────────
    // Icon
    // ─────────────────────────────────────────────
    void CreateIcon(HapticPatternType type)
    {
        if (currentIndex < 5 && iconInstances[currentIndex] == null)
        {
            GameObject prefab = type == HapticPatternType.Short ? shortIconPrefab : longIconPrefab;
            iconInstances[currentIndex] = Instantiate(prefab, selectedIconsContainer.transform);
            iconInstances[currentIndex].SetActive(true);
        }
    }

    void ClearIcons()
    {
        for (int i = 0; i < 5; i++)
        {
            if (iconInstances[i] != null)
            {
                Destroy(iconInstances[i]);
                iconInstances[i] = null;
            }
        }
    }

    // ─────────────────────────────────────────────
    // Hearts UI
    // ─────────────────────────────────────────────
    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            heartImages[i].sprite = i < failCount ? blackHeartSprite : redHeartSprite;
        }
    }

    // ─────────────────────────────────────────────
    // Check Pattern
    // ─────────────────────────────────────────────
    void CheckPattern()
    {
        if (currentIndex < 5) return;

        bool isCorrect = true;
        for (int i = 0; i < 5; i++)
        {
            if (playerPattern[i] != targetPattern[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            StartCoroutine(OnPuzzleSolved());
        }
        else
        {
            failCount++;
            UpdateHeartsUI();
            Debug.Log($"[PuzzleManager] ❌ Puzzle failed {failCount} times.");

            if (failCount >= 5)
            {
                Debug.Log("[PuzzleManager] ❌ ล้มเหลว 5 ครั้ง!");
                StartCoroutine(ShowUnsolvedAndLoadScene());
            }

            hapticManager.PlayPattern(HapticPatternType.Fail);
            currentIndex = 0;
            Array.Clear(playerPattern, 0, 5);
            ClearIcons();
        }
    }

    // ─────────────────────────────────────────────
    // Puzzle Solved
    // ─────────────────────────────────────────────
    IEnumerator OnPuzzleSolved()
    {
        isPuzzleActive = false;

        // 1. Haptic Success
        hapticManager.PlayPattern(HapticPatternType.Success);

        // 2. บันทึกสถานะ
        if (Inventory.Instance != null)
        {
            Inventory.Instance.CorrectPuzzle = true;
            Debug.Log("[PuzzleManager] ✅ Puzzle solved! CorrectPuzzle = true");
        }

        // 3. แสดง Clear UI
        if (puzzleClearUI != null)
        {
            puzzleClearUI.Show();
            yield return new WaitForSeconds(3.5f);
        }

        // 4. ปิด Puzzle Canvas
        _puzzleCanvas.SetActive(false);

        // 5. เปิดประตูหน้า (ไปห้อง Item) เท่านั้น
        //    ประตูหลัง (DoorBack) จะเปิดตอนรับถุงมือใน GlovePickup
        if (Door != null)
        {
            Door.SetActive(false);
            Debug.Log("[PuzzleManager] 🚪 ประตูหน้าเปิดแล้ว! (ไปห้อง Item)");
        }
        else
        {
            Debug.LogWarning("[PuzzleManager] ⚠️ ไม่พบ Door!");
        }

        // DoorBack ไม่เปิดที่นี่ — เปิดตอนรับถุงมือใน GlovePickup
        Debug.Log("[PuzzleManager] ℹ️ DoorBack จะเปิดหลังรับถุงมือลิง");

        // 6. คืน Control ให้ผู้เล่น
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
            Debug.Log("[PuzzleManager] ✅ คืน Control ให้ผู้เล่นแล้ว");
        }
    }

    // ─────────────────────────────────────────────
    // Puzzle Failed — Load Scene
    // ─────────────────────────────────────────────
    private IEnumerator ShowUnsolvedAndLoadScene()
    {
        isPuzzleActive = false;

        if (unsolvedCanvas != null) unsolvedCanvas.SetActive(true);
        yield return new WaitForSeconds(5f);
        if (unsolvedCanvas != null) unsolvedCanvas.SetActive(false);

        Debug.Log("[PuzzleManager] 🔄 โหลด Map1 ใหม่...");
        SceneManager.LoadScene("Map1");
    }

    // ─────────────────────────────────────────────
    // Public Methods
    // ─────────────────────────────────────────────
    public void ReplayPattern()
    {
        if (!isPuzzleActive) return;
        StartCoroutine(PlayPatternWithDelay());
    }

    public void CancelLast()
    {
        if (currentIndex <= 0) return;

        currentIndex--;
        playerPattern[currentIndex] = 0;
        if (iconInstances[currentIndex] != null)
        {
            Destroy(iconInstances[currentIndex]);
            iconInstances[currentIndex] = null;
        }
    }

    public void ExitToScene()
    {
        _puzzleCanvas.SetActive(false);
        playerInput.SwitchCurrentActionMap("Player");
    }

    // ─────────────────────────────────────────────
    // Enable / Disable
    // ─────────────────────────────────────────────
    private void OnEnable()
    {
        if (playerInput == null) return;

        playerInput.actions["Short"].performed += OnShortVibrationAction;
        playerInput.actions["Long"].performed += OnLongVibrationAction;
        playerInput.actions["Cancel"].performed += OnCancelLastAction;
        playerInput.actions["Exit"].performed += OnExitToSceneAction;
        playerInput.actions["Replay"].performed += OnReplayPatternAction;
        playerInput.SwitchCurrentActionMap("PuzzleUI1");
    }

    private void OnDisable()
    {
        if (playerInput == null) return;

        playerInput.actions["Short"].performed -= OnShortVibrationAction;
        playerInput.actions["Long"].performed -= OnLongVibrationAction;
        playerInput.actions["Cancel"].performed -= OnCancelLastAction;
        playerInput.actions["Exit"].performed -= OnExitToSceneAction;
        playerInput.actions["Replay"].performed -= OnReplayPatternAction;
        playerInput.SwitchCurrentActionMap("Player");
    }

    private void OnShortVibrationAction(InputAction.CallbackContext ctx) => OnShortVibration();
    private void OnLongVibrationAction(InputAction.CallbackContext ctx) => OnLongVibration();
    private void OnCancelLastAction(InputAction.CallbackContext ctx) => CancelLast();
    private void OnExitToSceneAction(InputAction.CallbackContext ctx) => ExitToScene();
    private void OnReplayPatternAction(InputAction.CallbackContext ctx) => ReplayPattern();
}