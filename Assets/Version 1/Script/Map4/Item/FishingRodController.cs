// ============================================================
// FILE: FishingRodController.cs  (v4 — ตีเหมือนหมัด + ระยะไกลขึ้น + แรงขึ้น + ท่า RodAttack)
// PATH: Assets/Version 1/Script/Map4/Item/FishingRodController.cs
// วางไว้เป็นลูกของ Player (เหมือน GloveController.cs ของ Map1)
//
// ท่าฟาดเบ็ด: SmartAttackSystem จะยิง Trigger "RodAttack" + Int "RodStep" (1/2/3)
//             เองเมื่อ IsEquipped = true → เข้า State RodAttack1/2/3 ใน Animator
// ดาเมจ: ใช้ระบบเดียวกับหมัด (DoDetection ที่ punchPoint) แต่ radius = extendedAttackRange
// ============================================================
using UnityEngine;

public class FishingRodController : MonoBehaviour
{
    [Header("=== Combat Bonus ===")]
    [Tooltip("ระยะ hitbox ของหมัดตอนสวมเบ็ด (ปกติหมัด = 1.5)")]
    public float extendedAttackRange = 4.5f;

    [Tooltip("ระยะล็อกเป้า (SmartAttackSystem.attackRange) เมื่อสวมเบ็ดยืด — ควร ≥ extendedAttackRange")]
    public float extendedDetectionRange = 5f;

    [Tooltip("โบนัสพลังโจมตี (หน่วย) เช่น ปกติ 1 → บวก 2 = 3")]
    public int attackDamageBonus = 2;

    [Header("=== Oxygen Bonus (อยู่ใต้น้ำได้นานขึ้น) ===")]
    [Range(0f, 2f)] public float extraOxygenPercent = 0.5f;               // +50%
    [Range(0f, 1f)] public float oxygenDepletionReductionPercent = 0.3f;  // -30%

    [Header("=== Rod Model ===")]
    [Tooltip("โมเดลเบ็ดที่ติดอยู่ในมือ (Mesh_0 ใต้ hand.R) — เปิดตอนสวม ปิดตอนถอด")]
    public GameObject rodHandModel;

    [Header("=== References ===")]
    public SmartAttackSystem smartAttackSystem;
    public SwimmingControllerHybrid swimmingController;

    // ── Private ──
    private bool _isEquipped = false;
    private float _originalPunchDetectRadius;
    private float _originalAttackRange;
    private int _originalPunchDamage;
    private int _originalPunchDamage2;

    public bool IsEquipped => _isEquipped;

    void Awake()
    {
        if (smartAttackSystem == null)
            smartAttackSystem = GetComponentInParent<SmartAttackSystem>();
        if (swimmingController == null)
            swimmingController = GetComponentInParent<SwimmingControllerHybrid>();

        if (rodHandModel != null) rodHandModel.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // Equip / Unequip
    // ─────────────────────────────────────────────
    public void OnEquip()
    {
        if (_isEquipped) return;
        _isEquipped = true;

        ApplyCombatBonus();
        ApplyOxygenBonus();
        if (rodHandModel != null) rodHandModel.SetActive(true);

        Debug.Log($"[FishingRodController] 🎣 Equipped! ระยะตี {extendedAttackRange} | DMG +{attackDamageBonus} | ออกซิเจนเพิ่ม");
    }

    public void OnUnequip()
    {
        if (!_isEquipped) return;
        _isEquipped = false;

        RevertCombatBonus();
        RevertOxygenBonus();
        if (rodHandModel != null) rodHandModel.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // Combat Bonus
    // ─────────────────────────────────────────────
    void ApplyCombatBonus()
    {
        if (smartAttackSystem == null)
        {
            Debug.LogWarning("[FishingRodController] ⚠️ ไม่พบ SmartAttackSystem!");
            return;
        }

        _originalPunchDetectRadius = smartAttackSystem.glovePunchDetectRadius;
        _originalAttackRange = smartAttackSystem.attackRange;
        _originalPunchDamage = smartAttackSystem.punchDamage;
        _originalPunchDamage2 = smartAttackSystem.punchDamage2;

        // ✅ ระยะหมัด (SmartAttackSystem ใช้ค่านี้ตอนสวมเบ็ด)
        smartAttackSystem.glovePunchDetectRadius = extendedAttackRange;
        // ✅ ระยะล็อกเป้า + ระยะตี Cage/Rock/Anchor
        smartAttackSystem.attackRange = Mathf.Max(smartAttackSystem.attackRange, extendedDetectionRange);

        // ✅ ความแรง (1 → 3, 2 → 4)
        smartAttackSystem.punchDamage = _originalPunchDamage + attackDamageBonus;
        smartAttackSystem.punchDamage2 = _originalPunchDamage2 + attackDamageBonus;
    }

    void RevertCombatBonus()
    {
        if (smartAttackSystem == null) return;

        smartAttackSystem.glovePunchDetectRadius = _originalPunchDetectRadius;
        smartAttackSystem.attackRange = _originalAttackRange;
        smartAttackSystem.punchDamage = _originalPunchDamage;
        smartAttackSystem.punchDamage2 = _originalPunchDamage2;
    }

    // ─────────────────────────────────────────────
    // Oxygen Bonus
    // ─────────────────────────────────────────────
    void ApplyOxygenBonus()
    {
        if (swimmingController == null)
        {
            Debug.LogWarning("[FishingRodController] ⚠️ ไม่พบ SwimmingControllerHybrid!");
            return;
        }
        swimmingController.ApplyOxygenBonus(extraOxygenPercent, oxygenDepletionReductionPercent);
    }

    void RevertOxygenBonus()
    {
        if (swimmingController == null) return;
        swimmingController.RemoveOxygenBonus();
    }

    // ─────────────────────────────────────────────
    // 🧪 TEST — F6 สลับสวม/ถอด
    // ─────────────────────────────────────────────
    [Header("=== Debug / Test ===")]
    public KeyCode testToggleKey = KeyCode.F6;

    [ContextMenu("TEST: Equip Fishing Rod Now")]
    public void TestEquipNow() { OnEquip(); Debug.Log("[FishingRodController] 🧪 TEST EQUIP"); }

    [ContextMenu("TEST: Unequip Fishing Rod Now")]
    public void TestUnequipNow() { OnUnequip(); Debug.Log("[FishingRodController] 🧪 TEST UNEQUIP"); }

    [ContextMenu("TEST: Toggle Equip Now")]
    public void TestToggleNow() { if (_isEquipped) TestUnequipNow(); else TestEquipNow(); }

#if UNITY_EDITOR
    void Update()
    {
        if (Input.GetKeyDown(testToggleKey)) TestToggleNow();
    }
#endif
}