using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.Player;

public class FishingRodPickup : MonoBehaviour
{
    [Header("=== Item Object ===")]
    [Tooltip("GameObject ของโมเดลเบ็ดยืดที่จะถูก 'เปิด' ออกมาเมื่อเก็บ (เช่น อยู่ใน Chest หรือแท่นวาง)")]
    public GameObject fishingRodItemObject;

    [Header("=== UI Prompt ===")]
    public GameObject promptUI;

    [Header("=== Stat Bonuses (ให้ตอนสวมใส่) ===")]
    public int hpBonus = 2;

    [Header("=== References ===")]
    public PlayerInput _playerInput;

    private bool _pickedUp = false;
    private bool _playerInRange = false;
    private bool _pedestalRevealed = false;
    private InputAction _interactAction;

    private PlayerHealth _playerHealth;
    private SmartAttackSystem _smartAttack;
    private FishingRodController _rodController;
    private MonoBehaviour _swimmingController; // ใช้ SendMessage เพื่อไม่ต้อง reference type ตรง ๆ

    void Start()
    {
        if (fishingRodItemObject != null)
            fishingRodItemObject.SetActive(false);

        if (promptUI != null) promptUI.SetActive(false);

        if (_playerInput == null)
            _playerInput = FindFirstObjectByType<PlayerInput>();

        if (_playerInput == null)
        {
            Debug.LogError("[FishingRodPickup] ❌ ไม่พบ PlayerInput!");
            return;
        }

        var playerMap = _playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
            _interactAction = playerMap.FindAction("Attack");
            if (_interactAction != null)
                _interactAction.performed += OnInteract;
            else
                Debug.LogError("[FishingRodPickup] ❌ ไม่พบ Action 'Attack'!");
        }
        else
        {
            Debug.LogError("[FishingRodPickup] ❌ ไม่พบ ActionMap 'Player'!");
        }
    }

    void Update()
    {
        if (_pickedUp || _pedestalRevealed) return;

        bool puzzleSolved = Inventory.Instance != null && Inventory.Instance.CorrectPuzzle4;
        if (puzzleSolved)
        {
            _pedestalRevealed = true;
            if (fishingRodItemObject != null) fishingRodItemObject.SetActive(true);
            Debug.Log("[FishingRodPickup] ✅ Puzzle4 สำเร็จแล้ว — เบ็ดยืดปรากฏขึ้น!");
        }
    }

    void OnDestroy()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteract;
    }

    // ─────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_pickedUp || !_pedestalRevealed || !other.CompareTag("Player")) return;

        _playerInRange = true;
        _playerHealth = other.GetComponent<PlayerHealth>();
        _smartAttack = other.GetComponent<SmartAttackSystem>();
        _rodController = other.GetComponentInChildren<FishingRodController>(true);
        _swimmingController = other.GetComponent<SwimmingControllerHybrid>();

        if (promptUI != null) promptUI.SetActive(true);

        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║  🎣 เบ็ดยืด — กด □ เพื่อเก็บ           ║");
        Debug.Log("╚══════════════════════════════════════╝");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    // ─────────────────────────────────────────────
    void OnInteract(InputAction.CallbackContext context)
    {
        if (!_playerInRange || _pickedUp) return;
        PickUpFishingRod();
    }

    // ─────────────────────────────────────────────
    // เรียกโดยตรงได้จากปุ่ม Test เพื่อเปิดใช้งานทันที
    // ─────────────────────────────────────────────
    public void PickUpFishingRod()
    {
        _pickedUp = true;

        // ── 1) เปิด GameObject item ที่ถูกซ่อนไว้ในห้อง puzzle ──
        if (fishingRodItemObject != null)
            fishingRodItemObject.SetActive(true);

        // ✅ บันทึก flag ลง disk ทันที
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentSave.hasFishingRod = true; // เพิ่ม field นี้ใน SaveData
            SaveManager.Instance.ForceSaveCurrentState();
        }

        // ── 2) HP Max + Heal ──
        if (_playerHealth != null)
        {
            int oldMax = _playerHealth.maxHealth;
            int oldCurrent = _playerHealth.currentHealth;

            _playerHealth.maxHealth += hpBonus;
            _playerHealth.Heal(hpBonus);

            Debug.Log($"[FishingRodPickup] ❤️ HP STAT CHANGED | maxHP: {oldMax} → {_playerHealth.maxHealth} | current: {oldCurrent} → {_playerHealth.currentHealth}");
        }
        else Debug.LogWarning("[FishingRodPickup] ⚠️ ไม่พบ PlayerHealth!");

        // ── 3) Equip ระบบต่อสู้ + oxygen bonus ──
        if (_rodController != null)
        {
            _rodController.OnEquip();
        }
        else
        {
            Debug.LogWarning("[FishingRodPickup] ⚠️ ไม่พบ FishingRodController บนตัวผู้เล่น! (ต้องเพิ่ม component นี้ใต้ Player)");
        }

        if (promptUI != null) promptUI.SetActive(false);

        if (SaveManager.Instance != null)
            SaveManager.Instance.ForceSaveCurrentState();

        Debug.Log("[FishingRodPickup] 🎣 เก็บเบ็ดยืดสำเร็จ! พร้อมใช้งาน");
    }
}