using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.InputSystem;
using System.Collections; // เพิ่มสำหรับ Coroutine
using UnityEngine.UI;

public class PuzzleManager : MonoBehaviour
{
    public HapticManager hapticManager;
    [SerializeField] private GameObject selectedIconsContainer; // Container สำหรับไอคอน
    [SerializeField] private GameObject shortIconPrefab; // Prefab ของ ShortIcon
    [SerializeField] private GameObject longIconPrefab; // Prefab ของ LongIcon
    private HapticPatternType[] targetPattern = new HapticPatternType[5]; // การสั่นที่กำหนด
    private HapticPatternType[] playerPattern = new HapticPatternType[5]; // การสั่นที่เลือก
    private int currentIndex = 0;
    private GameObject[] iconInstances = new GameObject[5]; // อาร์เรย์เก็บไอคอนที่สร้าง
    [SerializeField] private GameObject _puzzleCanvas;
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject Door;

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


    void Start()
    {

        if (hapticManager == null)
        {
            Debug.LogError("HapticManager not assigned!");
            return;
        }
        if (selectedIconsContainer == null || shortIconPrefab == null || longIconPrefab == null)
        {
            Debug.LogError("SelectedIconsContainer, ShortIconPrefab, or LongIconPrefab not assigned!");
            return;
        }
        GenerateRandomPattern();
        StartCoroutine(PlayPatternWithDelay()); // ใช้ Coroutine สำหรับการหน่วงเวลา

        UpdateHeartsUI();

        StartCoroutine(InitializePuzzle());
    }
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

        // 3. เล่น Pattern
        yield return StartCoroutine(PlayPatternWithDelay());

        // 4. เปิดให้ผู้เล่นเล่นได้
        isPuzzleActive = true;
        Debug.Log("✅ Puzzle is now active!");
    }
    void GenerateRandomPattern()
    {
        for (int i = 0; i < 5; i++)
        {
            targetPattern[i] = UnityEngine.Random.value > 0.5f ? HapticPatternType.Short : HapticPatternType.Long;
        }
    }

    private IEnumerator PlayPatternWithDelay()
    {
        string patternLog = "Target Pattern: ";
        for (int i = 0; i < 5; i++)
        {
            patternLog += targetPattern[i] == HapticPatternType.Short ? "S " : "L ";
            hapticManager.PlayPattern(targetPattern[i]);
            yield return new WaitForSeconds(1f); // หน่วงเวลา 0.3 วินาทีระหว่างการสั่น
        }
        Debug.Log(patternLog); // แสดงลำดับการสั่นใน Console หลังจากเล่นเสร็จ
    }

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

    void CreateIcon(HapticPatternType type)
    {
        if (currentIndex < 5 && iconInstances[currentIndex] == null)
        {
            GameObject prefab = type == HapticPatternType.Short ? shortIconPrefab : longIconPrefab;
            iconInstances[currentIndex] = Instantiate(prefab, selectedIconsContainer.transform);
            iconInstances[currentIndex].SetActive(true);
        }
    }

    void UpdateHeartsUI()
    {
        // หัวใจแดง = เหลือโอกาส, หัวใจดำ = โอกาสหมด
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < failCount)
            {
                heartImages[i].sprite = blackHeartSprite; // ผิด แสดงหัวใจดำ
            }
            else
            {
                heartImages[i].sprite = redHeartSprite; // ยังโอเค แสดงหัวใจแดง
            }
        }
    }

    void CheckPattern()
    {
        if (currentIndex == 5)
        {
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
                /*hapticManager.PlayPattern(HapticPatternType.Success);

                if (Inventory.Instance != null)
                {
                    Inventory.Instance.CorrectPuzzle = true;
                    Debug.Log("Puzzle solved! CorrectPuzzle set to true.");
                }
                _puzzleCanvas.SetActive(false);
                Door.SetActive(false);
*/
            }
            else
            {
                failCount++;
                UpdateHeartsUI();

                Debug.Log("failCount: " + failCount);
                if (failCount >= 5)
                {
                    Debug.Log("ล้มเหลว");
                    Debug.Log("ถึงจุดจะ Load Scene!");
                    // ตรงนี้จะเพิ่มฟังก์ชัน GameOver หรือปิด puzzle ก็ได้
                    StartCoroutine(ShowUnsolvedAndLoadScene());

                }
                else
                {
                    Debug.Log("Puzzle failed " + failCount + " times.");
                }

                hapticManager.PlayPattern(HapticPatternType.Fail);
                currentIndex = 0;
                Array.Clear(playerPattern, 0, 5);
                ClearIcons();
            }
        }
    }

    IEnumerator OnPuzzleSolved()
    {
        isPuzzleActive = false; // ล็อคการกดปุ่ม

        // 1. เล่น Success Haptic
        hapticManager.PlayPattern(HapticPatternType.Success);

        // 2. บันทึกสถานะ
        if (Inventory.Instance != null)
        {
            Inventory.Instance.CorrectPuzzle = true;
            Debug.Log("Puzzle solved! CorrectPuzzle set to true.");
        }

        // 3. แสดง Clear UI
        if (puzzleClearUI != null)
        {
            puzzleClearUI.Show();
            yield return new WaitForSeconds(3.5f); // รอ ClearUI จบ (2s display + 1s fade + 0.5s buffer)
        }

        // 4. ปิด Puzzle Canvas
        _puzzleCanvas.SetActive(false);

        // 5. ปิด/ทำลาย Door
        if (Door != null)
        {
            Door.SetActive(false);
            Debug.Log("🚪 Door removed!");
        }

        // 6. คืน Control ให้ผู้เล่น
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("Player");
        }
    }

    private IEnumerator ShowUnsolvedAndLoadScene()
    {
        isPuzzleActive = false;
        Debug.Log("เริ่ม coroutine");
        if (unsolvedCanvas != null) unsolvedCanvas.SetActive(true);

        yield return new WaitForSeconds(5f);
        if (unsolvedCanvas != null) unsolvedCanvas.SetActive(false);

        Debug.Log("กำลังโหลด map1");
        SceneManager.LoadScene("Map1");
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

    public void ReplayPattern()
    {
        if (!isPuzzleActive) return;
        StartCoroutine(PlayPatternWithDelay()); // ใช้ Coroutine เมื่อกด Replay
    }

    public void CancelLast()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            playerPattern[currentIndex] = 0;
            if (iconInstances[currentIndex] != null)
            {
                Destroy(iconInstances[currentIndex]);
                iconInstances[currentIndex] = null;
            }
        }
    }

    private void OnEnable()
    {
        if (playerInput != null)
        {
            playerInput.actions["Short"].performed += OnShortVibrationAction;
            playerInput.actions["Long"].performed += OnLongVibrationAction;
            playerInput.actions["Cancel"].performed += OnCancelLastAction;
            playerInput.actions["Exit"].performed += OnExitToSceneAction;
            playerInput.actions["Replay"].performed += OnReplayPatternAction;
            playerInput.SwitchCurrentActionMap("PuzzleUI1");
        }
    }
    private void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.actions["Short"].performed -= OnShortVibrationAction;
            playerInput.actions["Long"].performed -= OnLongVibrationAction;
            playerInput.actions["Cancel"].performed -= OnCancelLastAction;
            playerInput.actions["Exit"].performed -= OnExitToSceneAction;
            playerInput.actions["Replay"].performed -= OnReplayPatternAction;
            playerInput.SwitchCurrentActionMap("Player");
        }
    }

    private void OnShortVibrationAction(InputAction.CallbackContext ctx) => OnShortVibration();
    private void OnLongVibrationAction(InputAction.CallbackContext ctx) => OnLongVibration();
    private void OnCancelLastAction(InputAction.CallbackContext ctx) => CancelLast();
    private void OnExitToSceneAction(InputAction.CallbackContext ctx) => ExitToScene();
    private void OnReplayPatternAction(InputAction.CallbackContext ctx) => ReplayPattern();


    public void ExitToScene()
    {
        _puzzleCanvas.SetActive(false);
        playerInput.SwitchCurrentActionMap("Player");
    }


}