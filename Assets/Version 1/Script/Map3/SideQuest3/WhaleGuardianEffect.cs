// ============================================================
// FILE: WhaleGuardianEffect.cs
// PATH: Assets/Version 1/Script/Map3/SideQuest3/WhaleGuardianEffect.cs
// ============================================================
using UnityEngine;

/// <summary>
/// รางวัลภารกิจรอง Map3 — "WHALE GUARDIAN!" (วาฬโบราณช่วยตอนสู้ Snow Man)
///
/// ยึด Map2 เป็นหลัก:
///   • ทำงานเฉพาะเมื่อ IceCrystalQuestManager สำเร็จ + เข้าห้องบอส Map3
///   • ATK +1 ถาวร (หมัด/เตะ/ธนู) — บวกครั้งเดียว ไม่ถอดคืน
///   • กันสถานะเดินช้าจากก้อนหิมะของบอส — ถาวร
///   • โล่: ใช้ OasisShieldEffect ตัวเดิมของ Map2 ตรงๆ (HP≤2, 3 ฮิต/45 วิ, คูลดาวน์ 60 วิ)
///     → ไม่ต้องเขียน logic โล่ซ้ำ แค่วาง component ไว้ในฉาก Map3
///   • ไม่มีกำแพงหิมะ (ตัดออกแล้ว)
/// </summary>
public class WhaleGuardianEffect : MonoBehaviour
{
    [Header("ATK Bonus (ถาวร)")]
    public int atkBonus = 1;
    public SmartAttackSystem smartAttackSystem;
    public FireBowController fireBowController;

    [Tooltip("ปิด (ค่าเริ่มต้น) = บวก ATK ตอนเข้าห้องบอส / เปิด = บวกทันทีที่เควสสำเร็จ")]
    public bool applyAtkOnQuestComplete = false;

    [Header("Shield (ใช้ระบบเดียวกับ Map2)")]
    [Tooltip("ลาก OasisShieldEffect ที่วางไว้ในฉาก Map3 มาใส่ (ตั้งค่าเหมือน Map2 ได้เลย)")]
    public OasisShieldEffect shieldEffect;

    [Header("UI")]
    [Tooltip("Popup WHALE GUARDIAN! (ใช้ TigerAssistPopupUI เดิม เปลี่ยนแค่รูป)")]
    public TigerAssistPopupUI guardianPopup;

    [Header("🧪 Debug / Test")]
    public bool enableDebugKey = true;
    public KeyCode debugForceBossKey = KeyCode.F10;

    private bool _unlocked = false;
    private bool _bossFightActive = false;
    private bool _atkApplied = false;

    public bool IsUnlocked => _unlocked;

    void Awake()
    {
        if (smartAttackSystem == null) smartAttackSystem = FindFirstObjectByType<SmartAttackSystem>();
        if (fireBowController == null) fireBowController = FindFirstObjectByType<FireBowController>(FindObjectsInactive.Include);

        // ✅ [FIX] OasisShieldEffect ถูกปิด (checkbox) ไว้ในฉาก → Update ไม่ทำงาน โล่ไม่มีวันเปิด
        if (shieldEffect != null && !shieldEffect.enabled)
        {
            shieldEffect.enabled = true;
            Debug.Log("[WhaleGuardian] 🛡️ เปิด OasisShieldEffect ที่ถูกปิดไว้ให้อัตโนมัติ");
        }
    }

    void Update()
    {
        if (enableDebugKey && Input.GetKeyDown(debugForceBossKey))
            TEST_ForceGuardianInBossFight();
    }

    /// <summary>เรียกจาก IceCrystalQuestManager ตอนเควสสำเร็จ</summary>
    public void EnableGuardian()
    {
        if (_unlocked) return;
        _unlocked = true;

        if (shieldEffect != null) shieldEffect.EnableShieldCapability();
        if (applyAtkOnQuestComplete) ApplyAtkBonus();

        // ถ้าเควสจบตอนอยู่ในห้องบอสแล้ว (กรณีเทส) → เปิดบัฟทันที
        if (_bossFightActive) ActivateBossBuffs();

        Debug.Log("[WhaleGuardian] ✅ ปลดล็อกวาฬผู้พิทักษ์แล้ว");
    }

    /// <summary>เรียกจาก BossMap3RoomTrigger ตอนผู้เล่นเข้าห้องบอส</summary>
    public void SetBossFightActive(bool active)
    {
        // ✅ [FIX] สำรอง: ถ้า IceCrystalQuestManager ไม่ได้ลากช่อง whaleGuardianEffect ไว้
        //    EnableGuardian() จะไม่เคยถูกเรียก (F10 ทำงานเพราะเรียกตรง) → เช็คสถานะเควสเองตอนเข้าห้องบอส
        if (active && !_unlocked)
        {
            var quest = IceCrystalQuestManager.Instance;
            if (quest != null && quest.IsQuestComplete)
            {
                Debug.Log("[WhaleGuardian] 🔗 เควสผลึกสำเร็จแล้วแต่ยังไม่ปลดล็อก → ปลดล็อกจากห้องบอสแทน");
                EnableGuardian();
            }
            else
                Debug.Log($"[WhaleGuardian] ℹ️ เข้าห้องบอสแล้ว แต่เควสผลึกยังไม่ครบ (QuestManager={(quest != null ? "พบ" : "ไม่พบ")}) → ไม่มีรางวัล");
        }

        _bossFightActive = active;
        if (shieldEffect != null) shieldEffect.SetBossFightActive(active);

        if (active && _unlocked) ActivateBossBuffs();
    }

    void ActivateBossBuffs()
    {
        ApplyAtkBonus();

        // ❄️ กันเดินช้า ถาวร + ล้าง slow ที่ค้างอยู่ (ถ้ามี)
        SnowballSlowEffect.SlowImmune = true;
        var player = GameObject.FindWithTag("Player");
        player?.GetComponent<SnowballSlowEffect>()?.ClearSlow();

        guardianPopup?.ShowPopup();
        HapticManager.Instance?.PlayPattern(HapticPatternType.Success);

        Debug.Log("[WhaleGuardian] 🐋 WHALE GUARDIAN! — ATK+1, กันสโลว์, โล่พร้อมทำงาน");
    }

    void ApplyAtkBonus()
    {
        if (_atkApplied) return;
        _atkApplied = true;

        if (smartAttackSystem != null)
        {
            smartAttackSystem.punchDamage += atkBonus;
            smartAttackSystem.punchDamage2 += atkBonus;
            smartAttackSystem.kickDamage += atkBonus;
            smartAttackSystem.kickDamage2 += atkBonus;
        }
        else Debug.LogWarning("[WhaleGuardian] ⚠️ ไม่พบ SmartAttackSystem");

        if (fireBowController != null)
            fireBowController.baseArrowDamage += atkBonus;
        else Debug.LogWarning("[WhaleGuardian] ⚠️ ไม่พบ FireBowController (ธนูจะไม่ได้ ATK+1)");

        Debug.Log($"[WhaleGuardian] ⚔️ ATK +{atkBonus} (ถาวร)");
    }

    void OnDestroy()
    {
        // static flag ไม่หายเองตอนเปลี่ยนฉาก — รีเซ็ตกันค้างไป Map4
        SnowballSlowEffect.SlowImmune = false;
    }

    /// <summary>🧪 TEST — ข้ามเควส + จำลองเข้าห้องบอสทันที</summary>
    [ContextMenu("🧪 TEST - Force Guardian In Boss Fight")]
    public void TEST_ForceGuardianInBossFight()
    {
        Debug.Log("[WhaleGuardian][TEST] ⚡ บังคับเปิดวาฬผู้พิทักษ์ + สถานะสู้บอส");
        _bossFightActive = true;
        if (shieldEffect != null) shieldEffect.SetBossFightActive(true);
        if (!_unlocked) EnableGuardian();
        else ActivateBossBuffs();
    }
}