using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.Player;

/// <summary>
/// แท่นวางถุงมือลิง
/// ผู้เล่นเข้าใกล้แล้วกด □ (Square/Attack) เพื่อรับ
/// ถอดไม่ได้ — Equip ครั้งเดียวถาวร
/// หลัง Equip:
///   - HP Max +2, Heal +2
///   - ATK +1
///   - Swap Model เป็น Spaceboy_item1Ver2
///   - เปิดประตูด้านหลัง (DoorBack)
///   - เปิด GloveController
/// </summary>
public class GlovePickup : MonoBehaviour
{
    [Header("References")]
    [Tooltip("ลาก GloveController จาก GloveSystem มาใส่")]
    public GloveController gloveController;

    [Header("Pickup Settings")]
    public int hpBonus = 2;
    public int atkBonus = 1;

    [Header("UI Prompt")]
    [Tooltip("GameObject UI ที่แสดงข้อความ 'กด □ เพื่อรับถุงมือ'")]
    public GameObject promptUI;

    [Header("Model Swap")]
    [Tooltip("Model Momo ปกติ (Spaceboy)")]
    public GameObject modelDefault;
    [Tooltip("Model Momo สวมถุงมือ (Spaceboy_item1Ver2)")]
    public GameObject modelWithGlove;

    [Header("Door")]
    [Tooltip("ประตูด้านหลังที่จะเปิดหลังรับถุงมือ")]
    public GameObject doorBack;

    [Header("Cutscene (เตรียมไว้ก่อน ยังไม่ต้องทำ)")]
    [Tooltip("Cutscene Object (ยังไม่ต้องทำ — เตรียม Reference ไว้ก่อน)")]
    public GameObject cutsceneObject;

    [Header("VFX (Optional)")]
    public GameObject pickupEffect;

    // ── Private ──
    private bool _playerInRange = false;
    private bool _pickedUp = false;
    private PlayerInput _playerInput;
    private InputAction _interactAction;

    // Cache
    private PlayerHealth _playerHealth;
    private SmartAttackSystem _smartAttack;

    // ─────────────────────────────────────────────
    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);

        // ตรวจสอบ Model ตอนเริ่ม
        if (modelWithGlove != null) modelWithGlove.SetActive(false);

        _playerInput = FindFirstObjectByType<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("[GlovePickup] ❌ ไม่พบ PlayerInput!");
            return;
        }

        // ใช้ปุ่ม Attack (□) รับไอเทม
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
    // Trigger Zone
    // ─────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (_pickedUp || !other.CompareTag("Player")) return;

        _playerInRange = true;
        _playerHealth = other.GetComponent<PlayerHealth>();
        _smartAttack = other.GetComponent<SmartAttackSystem>();

        if (promptUI != null) promptUI.SetActive(true);

        Debug.Log("╔══════════════════════════════════╗");
        Debug.Log("║  🥊 ถุงมือลิง — กด □ เพื่อรับไอเทม ║");
        Debug.Log("╚══════════════════════════════════╝");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // Input Callback
    // ─────────────────────────────────────────────
    void OnInteract(InputAction.CallbackContext context)
    {
        if (!_playerInRange || _pickedUp) return;
        EquipGlove();
    }

    // ─────────────────────────────────────────────
    // Equip Logic
    // ─────────────────────────────────────────────
    void EquipGlove()
    {
        _pickedUp = true;

        // ── 1) HP Max + Heal ──
        if (_playerHealth != null)
        {
            _playerHealth.maxHealth += hpBonus;
            _playerHealth.Heal(hpBonus);
            Debug.Log($"[GlovePickup] ❤️ maxHP +{hpBonus} → maxHP = {_playerHealth.maxHealth}");
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ PlayerHealth!");

        // ── 2) ATK ──
        if (_smartAttack != null)
        {
            _smartAttack.punchDamage += atkBonus;
            _smartAttack.punchDamage2 += atkBonus;
            _smartAttack.kickDamage += atkBonus;
            _smartAttack.kickDamage2 += atkBonus;
            Debug.Log($"[GlovePickup] ⚔️ ATK +{atkBonus} | punch={_smartAttack.punchDamage} kick={_smartAttack.kickDamage}");
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ SmartAttackSystem!");

        // ── 3) Cutscene (เตรียมไว้ก่อน ยังไม่ต้องทำ) ──
        if (cutsceneObject != null)
        {
            cutsceneObject.SetActive(true);
            Debug.Log("[GlovePickup] 🎬 Cutscene เริ่ม!");
            // TODO: รอ Cutscene จบแล้วค่อย SwapModel
        }

        // ── 4) Swap Model ──
        SwapModel();

        // ── 5) GloveController ──
        if (gloveController != null)
        {
            gloveController.enabled = true;
            gloveController.OnEquip();
            Debug.Log("[GlovePickup] 🥊 GloveController พร้อมใช้งาน!");
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ GloveController!");

        // ── 6) เปิดประตูหลัง ──
        if (doorBack != null)
        {
            doorBack.SetActive(false);
            Debug.Log("[GlovePickup] 🚪 ประตูด้านหลังเปิดแล้ว!");
        }
        else Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ DoorBack!");

        // ── 7) Haptic ──
        HapticManager.Instance?.PlayPattern(HapticPatternType.Success);

        // ── 8) VFX ──
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        // ── 9) ซ่อน UI + Disable แท่นวาง ──
        if (promptUI != null) promptUI.SetActive(false);

        Debug.Log("[GlovePickup] ✅ รับถุงมือลิงสำเร็จ! ถอดไม่ได้แล้ว");
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // Swap Model
    // ─────────────────────────────────────────────
    void SwapModel()
    {
        if (modelDefault != null)
        {
            modelDefault.SetActive(false);
            Debug.Log("[GlovePickup] 🔄 Hide Default Model");
        }

        if (modelWithGlove != null)
        {
            modelWithGlove.SetActive(true);
            Debug.Log("[GlovePickup] 🔄 Show Glove Model");

            // ✅ หา Animator จาก Model ใหม่
            Animator newAnimator = modelWithGlove
                .GetComponentInChildren<Animator>();

            if (newAnimator != null)
            {
                // ✅ อัพเดท ThirdPersonController
                var tpc = FindFirstObjectByType
                    <StarterAssets.ThirdPersonController>();
                if (tpc != null)
                    tpc._animator = newAnimator;

                // ✅ อัพเดท MOMOCharacterBridge
                var bridge = FindFirstObjectByType
                    <MOMOCharacterBridge>();
                if (bridge != null)
                    bridge.Animator = newAnimator;

                Debug.Log("[GlovePickup] ✅ Animator Swapped!");
            }
            else
            {
                Debug.LogWarning("[GlovePickup] ⚠️ ไม่พบ Animator ใน Model ใหม่!");
            }
        }
    }
}