using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.Player;

public class JetpackPickup : MonoBehaviour
{
    [Header("References")]
    public JetpackController jetpackController;

    [Header("Pickup Settings")]
    public int hpBonus = 2;
    public int atkBonus = 1; // ATK ของพลังเวทย์ที่ยิงลงล่าง

    [Header("UI Prompt")]
    public GameObject promptUI;

    [Header("Jetpack Visual — ลาก Mesh จาก Player Model")]
    public GameObject jetpackMesh; // Mesh ที่หลังผู้เล่น

    [Header("Pedestal")]
    [Tooltip("ลาก Jetpack Item บนแท่น — จะหายไปตอน Equip")]
    public GameObject pedestalItem;

    [Header("Door")]
    [Tooltip("ประตูทางเข้า Puzzle2 — จะเปิดหลังสวม Jetpack")]
    public GameObject entranceDoor;

    [Header("VFX (Optional)")]
    public GameObject pickupEffect;

    [Header("Debug (Editor Only)")]
    [SerializeField] private bool _debugIgnoreSave = false;

    // ── Private ──
    private bool _playerInRange = false;
    private bool _pickedUp = false;
    private PlayerInput _playerInput;
    private InputAction _interactAction;

    private PlayerHealth _playerHealth;

    // ─────────────────────────────────────────────
    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
        HideJetpackVisual();

        bool shouldHide = SaveManager.Instance != null
                       && SaveManager.Instance.CurrentSave.hasJetpack;

#if UNITY_EDITOR
        if (_debugIgnoreSave) shouldHide = false;
#endif

        if (shouldHide)
        {
            // ✅ ซ่อนแค่ Jetpack บนแท่น
            if (pedestalItem != null) pedestalItem.SetActive(false);
            if (entranceDoor != null) entranceDoor.SetActive(false);
            this.enabled = false;
            return;
        }

        // เริ่มต้น: ซ่อน Jetpack + ล็อกประตู
        if (pedestalItem != null) pedestalItem.SetActive(false);
        if (entranceDoor != null) entranceDoor.SetActive(true);

        _playerInput = FindFirstObjectByType<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("[JetpackPickup] ❌ ไม่พบ PlayerInput!");
            return;
        }

        var playerMap = _playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
            _interactAction = playerMap.FindAction("Attack");
            if (_interactAction != null)
                _interactAction.performed += OnInteract;
            else
                Debug.LogError("[JetpackPickup] ❌ ไม่พบ Action 'Attack'!");
        }
    }

    void OnDestroy()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteract;
    }

    // ─────────────────────────────────────────────
    // ✅ เรียกจาก MirrorInteractionManager.OnPuzzleComplete()
    // ─────────────────────────────────────────────
    public void ShowJetpackAfterPuzzle()
    {
        if (pedestalItem != null)
        {
            pedestalItem.SetActive(true);
            Debug.Log("[JetpackPickup] 🚀 Jetpack ปรากฏบนแท่น!");
        }
    }

    // ─────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_pickedUp || !other.CompareTag("Player")) return;

        _playerInRange = true;
        _playerHealth = other.GetComponent<PlayerHealth>();

        if (promptUI != null) promptUI.SetActive(true);

        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║  🚀 Jetpack — Press □ to equip       ║");
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
        EquipJetpack();
    }

    // ─────────────────────────────────────────────
    void EquipJetpack()
    {
        _pickedUp = true;

        // ✅ บันทึก flag
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentSave.hasJetpack = true;
            SaveManager.Instance.ForceSaveCurrentState();
        }

        // ── 1) HP Max + Heal ──
        if (_playerHealth != null)
        {
            int oldMax = _playerHealth.maxHealth;
            int oldCurrent = _playerHealth.currentHealth;

            _playerHealth.maxHealth += hpBonus;
            _playerHealth.Heal(hpBonus);

            Debug.Log($"[JetpackPickup] ❤️ HP CHANGED | maxHP: {oldMax} → {_playerHealth.maxHealth} | current: {oldCurrent} → {_playerHealth.currentHealth}");

            if (SaveManager.Instance != null)
                SaveManager.Instance.ForceSaveCurrentState();
        }

        // ── 2) แสดง Mesh Jetpack ──
        ShowJetpackVisual();

        // ── 3) เปิด JetpackController ──
        if (jetpackController != null)
        {
            jetpackController.enabled = true;
            jetpackController.OnEquip(atkBonus);
            Debug.Log("[JetpackPickup] 🚀 JetpackController พร้อมใช้งาน!");
        }

        // ── 4) เปิดประตูทางเข้า ──
        if (entranceDoor != null)
        {
            entranceDoor.SetActive(false);
            Debug.Log("[JetpackPickup] 🚪 ประตูทางเข้าเปิดแล้ว!");
        }

        // ── 5) Haptic ──
        HapticManager.Instance?.PlayPattern(HapticPatternType.Success);

        // ── 6) VFX ──
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        // ── 7) ซ่อน Jetpack บนแท่น ──
        if (promptUI != null) promptUI.SetActive(false);
        if (pedestalItem != null) pedestalItem.SetActive(false);
        this.enabled = false;

        Debug.Log("[JetpackPickup] ✅ Jetpack equipped!");
    }

    // ─────────────────────────────────────────────
    public void ForceEquipOnRespawn()
    {
        if (_pickedUp) return;
        _pickedUp = true;

        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║  🔄 RESPAWN — Restoring Jetpack      ║");
        Debug.Log("╚══════════════════════════════════════╝");

        ShowJetpackVisual();

        if (jetpackController != null)
        {
            jetpackController.enabled = true;
            jetpackController.OnEquip(atkBonus);
        }

        if (entranceDoor != null)
            entranceDoor.SetActive(false);

        if (pedestalItem != null)
            pedestalItem.SetActive(false);

        this.enabled = false;
        Debug.Log("[JetpackPickup] 🚀 Jetpack restored from save!");
    }

    // ─────────────────────────────────────────────
    void ShowJetpackVisual()
    {
        if (jetpackMesh != null)
        {
            jetpackMesh.SetActive(true);
            Debug.Log("[JetpackPickup] 🚀 Jetpack Mesh แสดงแล้ว!");
        }
    }

    void HideJetpackVisual()
    {
        if (jetpackMesh != null)
            jetpackMesh.SetActive(false);
    }
}