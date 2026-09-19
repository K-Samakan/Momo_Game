using UnityEngine;
using MOMO.Player;

/// <summary>
/// รางวัลภารกิจรอง Map2 — "โล่ป้องกันโอเอซิส"
/// ไม่มีต้นแบบตรงจาก Map1 (TigerAllyEffect เป็นบัฟโจมตี ส่วนนี้เป็นบัฟป้องกัน)
///
/// เงื่อนไขที่ยืนยันแล้ว:
///   • ทำงานเฉพาะตอน RockQuestManager.IsQuestComplete == true และอยู่ในห้องสู้บอส Map2
///   • HP ผู้เล่น <= 2 (และยังไม่ตาย) → เปิดโล่อัตโนมัติ
///   • โล่พังเมื่อ: บล็อกดาเมจครบ 3 ครั้ง หรือผ่านไป 45 วินาที (แล้วแต่อย่างไหนถึงก่อน)
///   • หลังโล่พัง คูลดาวน์ 60 วินาที ก่อน trigger ใหม่ได้อีกครั้ง
///   • ไม่มีระบบตกแพลตฟอร์ม (ตัดออกจากทุกด่านแล้ว) — ไม่ต้อง handle
///
/// ⚠️ ต้องแก้ PlayerHealth.cs เพิ่ม field `isShielded` + event `OnShieldHit`
///    (ดูคำแนะนำ patch แยกต่างหาก) ไม่งั้นสคริปต์นี้ compile ไม่ผ่าน
/// </summary>
public class OasisShieldEffect : MonoBehaviour
{
    [Header("Shield Settings")]
    public int maxHitsAbsorbed = 3;
    public float shieldDuration = 45f;
    public float shieldCooldown = 60f;

    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("VFX/UI")]
    [Tooltip("โมเดล/Particle โล่รอบตัวผู้เล่น — เปิด/ปิดตาม SetActive")]
    public GameObject shieldVFX;

    [Tooltip("Popup UI ตอนโล่เปิด (ใช้ class TigerAssistPopupUI เดิมจาก Map1 — เป็น popup กลางๆ ใช้ซ้ำได้ ไม่ผูกกับเสือ)")]
    public TigerAssistPopupUI shieldPopup;

    [Header("🧪 Debug / Test")]
    [Tooltip("เปิดถ้าอยากใช้ปุ่มคีย์บอร์ดลัดเทส — บังคับเปิด/ปิดโล่ได้ทันที ไม่ต้องอยู่ในห้องบอสหรือทำเควสก่อน")]
    public bool enableDebugKey = true;
    public KeyCode debugActivateKey = KeyCode.F8;
    public KeyCode debugDeactivateKey = KeyCode.F7;

    private bool bossFightActive = false;
    private bool shieldCapabilityUnlocked = false; // = RockQuestManager สำเร็จแล้ว
    private bool isShieldActive = false;
    private int hitsAbsorbed = 0;
    private float shieldTimer = 0f;
    private float cooldownTimer = 0f;

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHPChanged += HandleHPChanged;
            playerHealth.OnShieldHit += HandleShieldHit;
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHPChanged -= HandleHPChanged;
            playerHealth.OnShieldHit -= HandleShieldHit;
        }
    }

    void Update()
    {
        if (isShieldActive)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f) DeactivateShield();
        }
        else if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (enableDebugKey)
        {
            if (Input.GetKeyDown(debugActivateKey)) TEST_ForceActivateShield();
            if (Input.GetKeyDown(debugDeactivateKey)) TEST_ForceDeactivateShield();
        }
    }

    /// <summary>เรียกจาก RockQuestManager ตอนภารกิจรองสำเร็จ (ปลดล็อกความสามารถ ยังไม่เปิดโล่ทันที)</summary>
    public void EnableShieldCapability()
    {
        shieldCapabilityUnlocked = true;
        Debug.Log("[OasisShield] ✅ ปลดล็อกความสามารถโล่ป้องกันแล้ว (จะทำงานตอนสู้บอสและ HP<=2)");
    }

    /// <summary>เรียกจาก BossMap2RoomTrigger ตอนผู้เล่นเข้า/ออกห้องบอส</summary>
    public void SetBossFightActive(bool active)
    {
        bossFightActive = active;
    }

    private void HandleHPChanged(int current, int max)
    {
        if (!shieldCapabilityUnlocked || !bossFightActive) return;
        if (playerHealth == null || !playerHealth.IsAlive()) return;
        if (isShieldActive || cooldownTimer > 0f) return;

        // ✅ HP <= 2 (ไม่ใช่ ==2 พอดี) ตามที่ยืนยันไว้
        if (current <= 2)
        {
            ActivateShield();
        }
    }

    private void ActivateShield()
    {
        isShieldActive = true;
        hitsAbsorbed = 0;
        shieldTimer = shieldDuration;
        playerHealth.isShielded = true;

        if (shieldVFX) shieldVFX.SetActive(true);
        shieldPopup?.ShowPopup();
        HapticManager.Instance?.Vibrate(0.4f, 0.4f, 0.2f);

        Debug.Log("[OasisShield] 🛡️ โล่เปิดใช้งาน (HP <= 2)");
    }

    private void HandleShieldHit()
    {
        if (!isShieldActive) return;

        hitsAbsorbed++;
        Debug.Log($"[OasisShield] 🛡️ บล็อกดาเมจ {hitsAbsorbed}/{maxHitsAbsorbed}");

        if (hitsAbsorbed >= maxHitsAbsorbed)
            DeactivateShield();
    }

    private void DeactivateShield()
    {
        isShieldActive = false;
        playerHealth.isShielded = false;
        cooldownTimer = shieldCooldown;

        if (shieldVFX) shieldVFX.SetActive(false);

        Debug.Log("[OasisShield] 💥 โล่พัง — เริ่มคูลดาวน์ 60 วินาที");
    }

    // ════════════════════════════════════════════════════════════
    #region 🧪 Debug / Test
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// 🧪 TEST ONLY — บังคับเปิดโล่ทันที ข้ามทุกเงื่อนไข
    /// (ไม่ต้องทำเควสสำเร็จ, ไม่ต้องอยู่ในห้องบอส, ไม่ต้อง HP<=2)
    /// เรียกได้ 2 ทาง: กดปุ่ม debugActivateKey (F8) ตอน Runtime
    ///                  หรือคลิกขวา component นี้ตอน Play Mode > เลือกเมนูนี้
    /// </summary>
    [ContextMenu("🧪 TEST - Force Activate Shield")]
    public void TEST_ForceActivateShield()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[OasisShield][TEST] ⚠️ ยังไม่ได้ผูก Player Health ใน Inspector");
            return;
        }

        if (isShieldActive)
        {
            Debug.Log("[OasisShield][TEST] โล่เปิดอยู่แล้ว ไม่ต้องเปิดซ้ำ");
            return;
        }

        Debug.Log("[OasisShield][TEST] ⚡ บังคับเปิดโล่ (ข้ามเงื่อนไขเควส/ห้องบอส/HP ทั้งหมด)");
        ActivateShield();
    }

    /// <summary>
    /// 🧪 TEST ONLY — บังคับปิดโล่ทันที (จำลองว่าโล่พังแล้ว รวมคูลดาวน์ด้วย)
    /// </summary>
    [ContextMenu("🧪 TEST - Force Deactivate Shield")]
    public void TEST_ForceDeactivateShield()
    {
        if (!isShieldActive)
        {
            Debug.Log("[OasisShield][TEST] โล่ไม่ได้เปิดอยู่ ไม่มีอะไรให้ปิด");
            return;
        }

        Debug.Log("[OasisShield][TEST] ⚡ บังคับปิดโล่");
        DeactivateShield();
    }

    #endregion
}