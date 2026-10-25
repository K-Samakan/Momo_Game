// ============================================================
// FILE: FireBowPickup.cs
// PATH: Assets/Version 1/Script/Map3/Item3/FireBowPickup.cs
// อ้างอิงโครงสร้างจาก GlovePickup.cs (Map1) — คง pattern เดิมของโปรเจกต์
// ============================================================
// ✅ CHANGELOG
//   [NEW] itemData — ItemData สำหรับ Item Detail Popup (เหมือน GlovePickup)
//   [NEW] EquipFireBow() ขั้นที่ 7 — เรียก TutorialManager.ShowItemDetail(itemData)
//         (ไม่แสดงตอน ForceEquipOnRespawn — ตอนโหลด save ไม่ต้องเด้ง popup ซ้ำ)
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.Player;
using Momo.UITutorial.Data;
using Momo.UITutorial.Managers;

public class FireBowPickup : MonoBehaviour
{
    [Header("References")]
    public FireBowController fireBowController;

    // ✅ NEW
    [Header("Item Data (สำหรับ Item Detail Popup)")]
    [Tooltip("ลาก ItemData ของธนูไฟ (Create → Momo → Item → Item Data)")]
    public ItemData itemData;

    [Header("Pickup Settings")]
    public int hpBonus = 2;
    public int atkBonus = 1;

    [Header("UI Prompt")]
    public GameObject promptUI;

    [Header("Bow Visual — ลาก mesh_node (ผูกกับมือผู้เล่นไว้ล่วงหน้าใน Hierarchy, ปิดไว้ก่อน)")]
    public GameObject bowMesh;

    [Header("Pedestal — จะปรากฏก็ต่อเมื่อ Puzzle3 สำเร็จแล้วเท่านั้น")]
    [Tooltip("ลาก ItemT (ธนูไฟบนแท่น) มาใส่ — หายไปตอน Equip")]
    public GameObject pedestalItem;

    [Header("Doors (เปิดหลังเก็บไอเทมสำเร็จ — 2 บาน)")]
    public GameObject doorBack;
    public GameObject doorBack2;

    [Header("VFX (Optional)")]
    public GameObject pickupEffect;

    // ── Private ──
    private bool _playerInRange = false;
    private bool _pickedUp = false;
    private PlayerInput _playerInput;
    private InputAction _interactAction;

    private PlayerHealth _playerHealth;
    private SmartAttackSystem _smartAttack;

    // ✅ เพิ่มใหม่ — ใช้ติดตามว่าแท่นถูกเปิดให้เก็บแล้วหรือยัง (กันเช็คซ้ำทุกเฟรมหลังเปิดแล้ว)
    private bool _pedestalRevealed = false;

    // ─────────────────────────────────────────────
    void Start()
    {
        if (promptUI != null) promptUI.SetActive(false);
        HideBowVisual();

        bool alreadyHasBow = SaveManager.Instance != null && SaveManager.Instance.CurrentSave.hasFireBow;

        if (alreadyHasBow)
        {
            // เคยเก็บแล้ว (respawn/reload) → เก็บถาวรทันที ไม่ต้องกดใหม่
            ForceEquipOnRespawn();
            return;
        }

        // ✅ ซ่อนแท่นไว้ก่อนเป็นค่าเริ่มต้นเสมอ — Update() จะเป็นคนเปิดให้เองตอน puzzle solved
        if (pedestalItem != null) pedestalItem.SetActive(false);

        // ✅ Setup input ไว้ล่วงหน้าเลย (ไม่ปิด component) — เผื่อ puzzle solved ทีหลังจะกดเก็บได้ทันที
        _playerInput = FindFirstObjectByType<PlayerInput>();
        if (_playerInput == null)
        {
            Debug.LogError("[FireBowPickup] ❌ ไม่พบ PlayerInput!");
            return;
        }

        var playerMap = _playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
            _interactAction = playerMap.FindAction("Attack");
            if (_interactAction != null)
                _interactAction.performed += OnInteract;
            else
                Debug.LogError("[FireBowPickup] ❌ ไม่พบ Action 'Attack'!");
        }
        else
        {
            Debug.LogError("[FireBowPickup] ❌ ไม่พบ ActionMap 'Player'!");
        }

        // ✅ NEW: เตือนถ้ายังไม่ได้ลาก ItemData
        if (itemData == null)
            Debug.LogWarning("[FireBowPickup] ⚠️ ยังไม่ได้ลาก ItemData — เก็บธนูแล้วจะไม่มี Item Detail Popup");
    }

    // ✅ เพิ่มใหม่ — เช็คทุกเฟรมว่า puzzle solved หรือยัง (แทนการเช็คครั้งเดียวใน Start)
    void Update()
    {
        if (_pickedUp || _pedestalRevealed) return;

        bool puzzleSolved = Inventory.Instance != null && Inventory.Instance.CorrectPuzzle3;
        if (puzzleSolved)
        {
            _pedestalRevealed = true;
            if (pedestalItem != null) pedestalItem.SetActive(true);
            Debug.Log("[FireBowPickup] ✅ Puzzle3 สำเร็จแล้ว — แท่นธนูไฟปรากฏขึ้น!");
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

        if (promptUI != null) promptUI.SetActive(true);
        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║  🏹 Fire Bow — Press □ to equip       ║");
        Debug.Log("╚══════════════════════════════════════╝");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        if (!_playerInRange || _pickedUp) return;
        EquipFireBow();
    }

    // ─────────────────────────────────────────────
    void EquipFireBow()
    {
        _pickedUp = true;

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentSave.hasFireBow = true;
            SaveManager.Instance.ForceSaveCurrentState();
        }

        // ── 1) HP +2 ──
        if (_playerHealth != null)
        {
            int oldMax = _playerHealth.maxHealth;
            _playerHealth.maxHealth += hpBonus;
            _playerHealth.Heal(hpBonus);
            Debug.Log($"[FireBowPickup] ❤️ HP: {oldMax} → {_playerHealth.maxHealth}");
        }

        // ── 2) ATK +1 ──
        if (_smartAttack != null)
        {
            int old = _smartAttack.punchDamage;
            _smartAttack.punchDamage += atkBonus;
            _smartAttack.punchDamage2 += atkBonus;
            _smartAttack.kickDamage += atkBonus;
            _smartAttack.kickDamage2 += atkBonus;
            Debug.Log($"[FireBowPickup] ⚔️ ATK: {old} → {_smartAttack.punchDamage}");
        }

        // ── 3) แสดง Mesh ธนู ──
        ShowBowVisual();

        // ── 4) เปิด FireBowController ──
        if (fireBowController != null)
        {
            fireBowController.enabled = true;
            fireBowController.OnEquip(atkBonus == 0 ? 1 : 1); // baseArrowDamage อ้างอิงจาก inspector
        }

        // ── 5) เปิดประตูของห้อง Puzzle ทั้ง 2 บาน ──
        if (doorBack != null) doorBack.SetActive(false);
        if (doorBack2 != null) doorBack2.SetActive(false);
        Debug.Log("[FireBowPickup] 🚪 ประตู Puzzle Room เปิดแล้ว 2 บาน!");

        // ── 6) Haptic + VFX ──
        HapticManager.Instance?.PlayPattern(HapticPatternType.Success);
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);

        // ── 7) ✅ NEW: แสดง Item Detail Popup (เหมือน GlovePickup) ──
        if (itemData != null)
        {
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.ShowItemDetail(itemData);
            else
                Debug.LogWarning("[FireBowPickup] ⚠️ ไม่พบ TutorialManager ใน Scene Map 3 — Popup แสดงไม่ได้");
        }
        else
        {
            Debug.LogWarning("[FireBowPickup] ⚠️ ไม่พบ ItemData! ลืมลาก reference ใน Inspector หรือเปล่า?");
        }

        // ── 8) ซ่อนแท่น ──
        if (promptUI != null) promptUI.SetActive(false);
        if (pedestalItem != null) pedestalItem.SetActive(false);
        this.enabled = false;

        Debug.Log("[FireBowPickup] ✅ Fire Bow equipped!");
    }

    // ─────────────────────────────────────────────
    // เรียกจาก SaveManager ตอน Respawn / Reload Scene
    // (ไม่แสดง Item Detail Popup — ผู้เล่นเคยเห็นแล้วตอนเก็บครั้งแรก)
    // ─────────────────────────────────────────────
    public void ForceEquipOnRespawn()
    {
        if (_pickedUp) return;
        _pickedUp = true;

        var smartAttack = FindFirstObjectByType<SmartAttackSystem>();
        if (smartAttack != null)
        {
            smartAttack.punchDamage += atkBonus;
            smartAttack.punchDamage2 += atkBonus;
            smartAttack.kickDamage += atkBonus;
            smartAttack.kickDamage2 += atkBonus;
        }

        ShowBowVisual();

        if (fireBowController != null)
        {
            fireBowController.enabled = true;
            fireBowController.OnEquip(1);
        }

        if (doorBack != null) doorBack.SetActive(false);
        if (doorBack2 != null) doorBack2.SetActive(false);
        if (pedestalItem != null) pedestalItem.SetActive(false);
        this.enabled = false;

        Debug.Log("[FireBowPickup] 🏹 Fire Bow restored from save!");
    }

    // ─────────────────────────────────────────────
    void ShowBowVisual()
    {
        if (bowMesh != null) bowMesh.SetActive(true);
    }

    void HideBowVisual()
    {
        if (bowMesh != null) bowMesh.SetActive(false);
    }
}