using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.Player;

/// <summary>
/// แท่นวางถุงมือลิง
/// ผู้เล่นเข้าใกล้แล้วกด □ (Attack) เพื่อรับ
/// ถอดไม่ได้ — Equip ครั้งเดียวถาวร
/// 
/// วิธีนี้ใช้ SetActive แสดง/ซ่อน Mesh ถุงมือ
/// แทนการ Swap Model ทั้งตัว
/// → Animation ทำงานปกติ 100%
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
    [Tooltip("GameObject UI 'Press □ to equip Monkey Glove'")]
    public GameObject promptUI;

    [Header("Glove Visual — ลาก Mesh จาก Spaceboy Model")]
    [Tooltip("ปลอกแขนขวา (Mesh_0.001)")]
    public GameObject gloveSleeveR;
    [Tooltip("ปลอกแขนซ้าย (Mesh_0)")]
    public GameObject gloveSleeveL;
    [Tooltip("หมัดขวา (Mesh_0.002)")]
    public GameObject gloveFistR;
    [Tooltip("หมัดซ้าย (Mesh_0.003)")]
    public GameObject gloveFistL;

    [Header("Door")]
    [Tooltip("ประตูด้านหลังที่เปิดหลังรับถุงมือ")]
    public GameObject doorBack;

    [Header("Cutscene (เตรียมไว้ก่อน)")]
    [Tooltip("Cutscene Object — ยังไม่ต้องทำ")]
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
        // ซ่อน Prompt UI ตอนเริ่ม
        if (promptUI != null) promptUI.SetActive(false);

        // ซ่อน Mesh ถุงมือตอนเริ่ม
        HideGloveVisual();

        // หา PlayerInput
        _playerInput = FindFirstObjectByType<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("[GlovePickup] ❌ ไม่พบ PlayerInput!");
            return;
        }

        // Subscribe Attack (□) เพื่อรับไอเทม
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
    // Input
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
            Debug.Log($"[GlovePickup] ⚔️ ATK +{atkBonus} | punch={_smartAttack.punchDamage}");
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

        // ── 6) Cutscene (TODO) ──
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

        // ── 9) ซ่อน UI + Disable แท่นวาง ──
        if (promptUI != null) promptUI.SetActive(false);

        Debug.Log("[GlovePickup] ✅ Monkey Glove equipped! Cannot be removed.");
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // Glove Visual
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