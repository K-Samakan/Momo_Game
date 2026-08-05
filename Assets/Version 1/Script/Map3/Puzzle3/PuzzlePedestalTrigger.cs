// ============================================================
// FILE: PuzzlePedestalTrigger.cs
// PATH: Assets/Version 1/Script/Puzzle3/Com/PuzzlePedestalTrigger.cs
// แปะที่แท่น (มี Collider isTrigger) ในโลกปกติ
// ผู้เล่นเดินเข้าไปในระยะ → กดปุ่ม Interact → ค่อยวาปเข้าห้อง Puzzle3
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzlePedestalTrigger : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject promptUI; // "Press □ to solve puzzle"

    [Header("Input")]
    [Tooltip("ชื่อ Action ใน ActionMap 'Player' ที่ใช้กดเข้า puzzle (แนะนำใช้ตัวเดียวกับปุ่ม Interact/Attack)")]
    public string interactActionName = "Attack";

    // ── Private ──
    private bool _playerInRange = false;
    private PlayerInput _playerInput;
    private InputAction _interactAction;

    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);

        _playerInput = FindFirstObjectByType<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("[PuzzlePedestalTrigger] ❌ ไม่พบ PlayerInput!");
            return;
        }

        var playerMap = _playerInput.actions.FindActionMap("Player");
        _interactAction = playerMap?.FindAction(interactActionName);
        if (_interactAction != null)
            _interactAction.performed += OnInteract;
        else
            Debug.LogError($"[PuzzlePedestalTrigger] ❌ ไม่พบ Action '{interactActionName}'!");
    }

    void OnDestroy()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteract;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // ✅ ถ้า puzzle เคยแก้สำเร็จไปแล้ว ไม่ต้องโชว์ prompt ให้กดซ้ำ
        if (Inventory.Instance != null && Inventory.Instance.CorrectPuzzle3) return;

        _playerInRange = true;
        if (promptUI != null) promptUI.SetActive(true);
        Debug.Log("[PuzzlePedestalTrigger] 🧩 กดปุ่มเพื่อเข้าสู่ห้อง Puzzle3");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void OnInteract(InputAction.CallbackContext ctx)
    {
        if (!_playerInRange) return;
        if (Inventory.Instance != null && Inventory.Instance.CorrectPuzzle3) return;

        if (IcePuzzleManager.Instance != null)
        {
            IcePuzzleManager.Instance.EnterPuzzle(); // ✅ ตรงนี้เองที่ warp + switch action map
            Debug.Log("[PuzzlePedestalTrigger] 🌀 เข้าสู่ห้อง Puzzle3!");
        }
        else
        {
            Debug.LogError("[PuzzlePedestalTrigger] ❌ ไม่พบ IcePuzzleManager.Instance!");
        }

        if (promptUI != null) promptUI.SetActive(false);
        _playerInRange = false; // กันกดซ้ำระหว่างกำลัง warp
    }
}