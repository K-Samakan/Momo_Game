// ============================================================
// FILE: GlovePickup.cs
// PATH: Assets/Version 1/Script/Map1/Puzzle1/GlovePickup.cs
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.Player;

public class GlovePickup : MonoBehaviour
{
    [Header("References")]
    public GloveController gloveController;

    [Header("Pickup Settings")]
    public int hpBonus = 2;
    public int atkBonus = 1;

    [Header("UI Prompt")]
    public GameObject promptUI;

    [Header("Glove Visual — ลาก Mesh จาก Spaceboy Model")]
    public GameObject gloveSleeveR;
    public GameObject gloveSleeveL;
    public GameObject gloveFistR;
    public GameObject gloveFistL;

    [Header("Pedestal")]
    [Tooltip("ลาก ItemT (ถุงมือบนแท่น) มาใส่ — จะหายไปตอน Equip")]
    public GameObject pedestalItem;

    [Header("Door")]
    public GameObject doorBack;

    [Header("Cutscene (เตรียมไว้ก่อน)")]
    public GameObject cutsceneObject;

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
    private SmartAttackSystem _smartAttack;

    // ─────────────────────────────────────────────
    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
        HideGloveVisual();

        bool shouldHide = SaveManager.Instance != null
                       && SaveManager.Instance.CurrentSave.hasMonkeyGlove;

#if UNITY_EDITOR
        if (_debugIgnoreSave) shouldHide = false;
#endif

        if (shouldHide)
        {
            // ✅ ซ่อนแค่ถุงมือบนแท่น ไม่ซ่อนทั้งแท่น
            if (pedestalItem != null) pedestalItem.SetActive(false);
            this.enabled = false;
            return;
        }

        _playerInput = FindFirstObjectByType<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("[GlovePickup] ❌ ไม่พบ PlayerInput!");
            return;
        }

        var playerMap = _playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
            _interactAction = playerMap.FindAction("Attack");
            if (_interactAction != null)
                _interactAction.performed += OnInteract;
            else
                Debug.LogError("[GlovePickup] ❌ ไม่พบ Action 'Attack'!");
        }
        else
        {
            Debug.LogError("[GlovePickup] ❌ ไม่พบ ActionMap 'Player'!");
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
        if (_pickedUp || !other.CompareTag("Player")) return;

        _playerInRange = true;
        _playerHealth = other.GetComponent<PlayerHealth>();
        _smartAttack = other.GetComponent<SmartAttackSystem>();

        if (promptUI != null) promptUI.SetActive(true);

        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║  🥊 Monkey Glove — Press □ to equip  ║");
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
        EquipGlove();
    }

    // ─────────────────────────────────────────────
    void EquipGlove()
    {
        _pickedUp = true;

        // ✅ บันทึก flag ลง disk ทันที
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentSave.hasMonkeyGlove = true;
            SaveManager.Instance.ForceSaveCurrentState();
        }

        // ── 1) HP Max + Heal ──
        if (_playerHealth != null)
        {
            int oldMax = _playerHealth.maxHealth;
            int oldCurrent = _playerHealth.currentHealth;

            _playerHealth.maxHealth += hpBonus;
            _playerHealth.Heal(hpBonus);

            Debug.Log($"[GlovePickup] ❤️ HP STAT CHANGED | maxHP: {oldMax} → {_playerHealth.maxHealth} | current: {oldCurrent} → {_playerHealth.currentHealth}");

            if (SaveManager.Instance != null)
                SaveManager.Instance.ForceSaveCurrentState();
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ PlayerHealth!");

        // ── 2) ATK ──
        if (_smartAttack != null)
        {
            int oldPunch = _smartAttack.punchDamage;
            int oldKick = _smartAttack.kickDamage;

            _smartAttack.punchDamage += atkBonus;
            _smartAttack.punchDamage2 += atkBonus;
            _smartAttack.kickDamage += atkBonus;
            _smartAttack.kickDamage2 += atkBonus;

            Debug.Log($"[GlovePickup] ⚔️ ATK STAT CHANGED | punch: {oldPunch} → {_smartAttack.punchDamage} | kick: {oldKick} → {_smartAttack.kickDamage}");
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ SmartAttackSystem!");

        // ── 3) แสดง Mesh ถุงมือ ──
        ShowGloveVisual();

        // ── 4) เปิด GloveController ──
        if (gloveController != null)
        {
            gloveController.enabled = true;
            gloveController.OnEquip();
            Debug.Log("[GlovePickup] 🥊 GloveController พร้อมใช้งาน!");
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ GloveController!");

        // ── 5) เปิดประตูหลัง ──
        if (doorBack != null)
        {
            doorBack.SetActive(false);
            Debug.Log("[GlovePickup] 🚪 ประตูด้านหลังเปิดแล้ว!");
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ DoorBack!");

        // ── 6) Cutscene ──
        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(true);
            Debug.Log("[GlovePickup] 🎬 Cutscene เริ่ม!");
        }

        // ── 7) Haptic ──
        HapticManager.Instance?.PlayPattern(HapticPatternType.Success);

        // ── 8) VFX ──
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        // ── 9) ซ่อนแค่ถุงมือบนแท่น ──
        if (promptUI != null) promptUI.SetActive(false);
        if (pedestalItem != null) pedestalItem.SetActive(false);
        this.enabled = false;

        Debug.Log("[GlovePickup] ✅ Monkey Glove equipped! Cannot be removed.");
    }

    // ─────────────────────────────────────────────
    // ✅ เรียกจาก SaveManager ตอน Respawn
    // ─────────────────────────────────────────────
    public void ForceEquipOnRespawn()
    {
        if (_pickedUp) return;
        _pickedUp = true;

        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║  🔄 RESPAWN — Restoring Glove Stats  ║");
        Debug.Log("╚══════════════════════════════════════╝");

        var smartAttack = FindFirstObjectByType<SmartAttackSystem>();
        if (smartAttack != null)
        {
            int oldPunch = smartAttack.punchDamage;
            int oldKick = smartAttack.kickDamage;

            smartAttack.punchDamage += atkBonus;
            smartAttack.punchDamage2 += atkBonus;
            smartAttack.kickDamage += atkBonus;
            smartAttack.kickDamage2 += atkBonus;

            Debug.Log($"[GlovePickup] ⚔️ ATK RESTORED | punch: {oldPunch} → {smartAttack.punchDamage} | kick: {oldKick} → {smartAttack.kickDamage}");
        }

        ShowGloveVisual();

        if (gloveController != null)
        {
            gloveController.enabled = true;
            gloveController.OnEquip();
        }

        if (doorBack != null)
            doorBack.SetActive(false);

        // ✅ ซ่อนแค่ถุงมือบนแท่น
        if (pedestalItem != null) pedestalItem.SetActive(false);
        this.enabled = false;

        Debug.Log("[GlovePickup] 🥊 Glove restored from save!");
    }

    // ─────────────────────────────────────────────
    void ShowGloveVisual()
    {
        if (gloveSleeveR != null) gloveSleeveR.SetActive(true);
        if (gloveSleeveL != null) gloveSleeveL.SetActive(true);
        if (gloveFistR != null) gloveFistR.SetActive(true);
        if (gloveFistL != null) gloveFistL.SetActive(true);
        Debug.Log("[GlovePickup] 🥊 Glove Visual แสดงแล้ว!");
    }

    void HideGloveVisual()
    {
        if (gloveSleeveR != null) gloveSleeveR.SetActive(false);
        if (gloveSleeveL != null) gloveSleeveL.SetActive(false);
        if (gloveFistR != null) gloveFistR.SetActive(false);
        if (gloveFistL != null) gloveFistL.SetActive(false);
    }
}