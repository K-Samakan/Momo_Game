// ============================================================
// FILE: FishingRodController.cs
// PATH: Assets/Version 1/Script/Map4/Item4/FishingRodController.cs
// วางไว้เป็นลูกของ Player (เหมือน GloveController.cs ของ Map1)
// ============================================================
using UnityEngine;

public class FishingRodController : MonoBehaviour
{
    [Header("=== Combat Bonus ===")]
    [Tooltip("ระยะตรวจจับศัตรูที่เพิ่มขึ้นเมื่อสวมเบ็ดยืด (แทน punchDetectRadius ปกติ)")]
    public float extendedAttackRange = 4.5f;

    [Tooltip("แนวโจมตีทั่วไป (SmartAttackSystem.attackRange) ที่เพิ่มขึ้นเมื่อสวมเบ็ดยืด")]
    public float extendedDetectionRange = 5f;

    [Tooltip("โบนัสพลังโจมตี (หน่วย) ที่บวกเพิ่มจากค่าปกติ เช่น ปกติ 1 → รวมบวก 2 = 3")]
    public int attackDamageBonus = 2;

    [Header("=== Oxygen Bonus (อยู่ใต้น้ำได้นานขึ้น) ===")]
    [Tooltip("เพิ่ม Max Oxygen กี่ % จากค่าเดิม")]
    [Range(0f, 2f)]
    public float extraOxygenPercent = 0.5f; // +50%

    [Tooltip("ลดอัตราการใช้ออกซิเจนกี่ % จากค่าเดิม")]
    [Range(0f, 1f)]
    public float oxygenDepletionReductionPercent = 0.3f; // -30%

    [Header("=== References ===")]
    public SmartAttackSystem smartAttackSystem;
    public SwimmingControllerHybrid swimmingController;

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
    }

    // ─────────────────────────────────────────────
    // เรียกจาก FishingRodPickup ตอนเก็บไอเทม
    // ─────────────────────────────────────────────
    public void OnEquip()
    {
        if (_isEquipped) return;
        _isEquipped = true;

        ApplyCombatBonus();
        ApplyOxygenBonus();

        Debug.Log("[FishingRodController] 🎣 Equipped! ระยะโจมตี+พลังโจมตี+ออกซิเจน อัปเดตแล้ว");
    }

    public void OnUnequip()
    {
        if (!_isEquipped) return;
        _isEquipped = false;

        RevertCombatBonus();
        RevertOxygenBonus();
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

        // เก็บค่าดั้งเดิมไว้ก่อน (เผื่อถอดเบ็ดออก)
        _originalPunchDetectRadius = smartAttackSystem.glovePunchDetectRadius;
        _originalAttackRange = smartAttackSystem.attackRange;
        _originalPunchDamage = smartAttackSystem.punchDamage;
        _originalPunchDamage2 = smartAttackSystem.punchDamage2;

        // ✅ เพิ่มระยะโจมตี
        smartAttackSystem.glovePunchDetectRadius = extendedAttackRange;
        smartAttackSystem.attackRange = Mathf.Max(smartAttackSystem.attackRange, extendedDetectionRange);

        // ✅ เพิ่มพลังโจมตี (1 → 3 เมื่อ bonus = 2)
        smartAttackSystem.punchDamage = _originalPunchDamage + attackDamageBonus;
        smartAttackSystem.punchDamage2 = _originalPunchDamage2 + attackDamageBonus;

        // ✅ ให้ระบบคอมโบต่อยรับรู้ว่ามีอุปกรณ์สวมใส่อยู่ (ใช้พาธเดียวกับถุงมือ)
        //    ถ้า SmartAttackSystem อ้างอิง GloveController อยู่แล้ว ให้ปล่อยผ่านได้ตามปกติ
        //    เบ็ดยืดใช้ท่าต่อย 3 คอมโบเดียวกัน (PunchRight → PunchLeft → PunchRight2)
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
    // Oxygen Bonus — ต้องมี method เสริมใน SwimmingControllerHybrid (ดูหมายเหตุด้านล่าง)
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
    // 🧪 TEST — เปิดใช้งานทันทีจากปุ่มทดสอบ (Editor Button / Debug Key)
    // ─────────────────────────────────────────────
    [ContextMenu("TEST: Equip Fishing Rod Now")]
    public void TestEquipNow()
    {
        OnEquip();
        Debug.Log("[FishingRodController] 🧪 TEST EQUIP เรียกใช้งานแล้ว");
    }

#if UNITY_EDITOR
    void Update()
    {
        // กด F9 เพื่อทดสอบเปิดใช้งานเบ็ดยืดทันทีตอนเล่นใน Editor
        if (Input.GetKeyDown(KeyCode.F9))
        {
            TestEquipNow();
        }
    }
#endif
}