// ============================================================
// FILE: OasisShieldEffect.cs
// PATH: Assets/Version 1/Script/Map2/SideQuest2/OasisShieldEffect.cs
// ============================================================
using UnityEngine;
using MOMO.Player;

/// <summary>
/// รางวัลภารกิจรอง Map2 — "โล่ป้องกันโอเอซิส"
///
/// โล่จะเปิดเองเมื่อครบ 3 เงื่อนไขพร้อมกัน:
///   1) Quest Unlocked  = ทุบหินครบ 5 ก้อน (RockQuestManager เรียก EnableShieldCapability)
///   2) Boss Fight Active = เดินเข้าห้องบอส (BossMap2RoomTrigger เรียก SetBossFightActive(true))
///   3) HP ผู้เล่น <= 2 และยังไม่ตาย และไม่ติดคูลดาวน์
///
/// ✅ v2: เช็คเงื่อนไขทุกเฟรมใน Update (ไม่พึ่ง event OnHPChanged อย่างเดียว)
/// ✅ v2: โชว์สถานะทุกเงื่อนไขใน Inspector ตอน Play — ดูได้เลยว่าข้อไหนไม่ผ่าน
/// </summary>
public class OasisShieldEffect : MonoBehaviour
{
    [Header("Shield Settings")]
    public int maxHitsAbsorbed = 3;
    public float shieldDuration = 45f;
    public float shieldCooldown = 60f;
    [Tooltip("โล่เปิดเมื่อ HP <= ค่านี้")]
    public int hpThreshold = 2;

    [Header("References")]
    public PlayerHealth playerHealth;

    [Header("VFX/UI")]
    [Tooltip("โมเดล/Particle โล่รอบตัวผู้เล่น — เปิด/ปิดตาม SetActive")]
    public GameObject shieldVFX;

    [Tooltip("Popup UI ตอนโล่เปิด (ใช้ class TigerAssistPopupUI เดิมจาก Map1)")]
    public TigerAssistPopupUI shieldPopup;

    [Header("📊 Status (ดูตอน Play — ต้องติ๊กครบ 2 ช่องแรก)")]
    [SerializeField] private bool questUnlocked = false;     // เงื่อนไข 1
    [SerializeField] private bool bossFightActive = false;   // เงื่อนไข 2
    [SerializeField] private int currentHP = 0;              // เงื่อนไข 3
    [SerializeField] private bool isShieldActive = false;
    [SerializeField] private float cooldownLeft = 0f;

    [Header("🧪 Debug / Test")]
    public bool enableDebugKey = true;
    public KeyCode debugActivateKey = KeyCode.F8;
    public KeyCode debugDeactivateKey = KeyCode.F7;

    private int hitsAbsorbed = 0;
    private float shieldTimer = 0f;
    private bool warnedNotAlive = false;

    void Awake()
    {
        // ✅ v3: ถ้าช่อง Player Health ว่าง หรือลาก Prefab จาก Project มาใส่ (ไม่ใช่ตัวใน Scene)
        //        → หา PlayerHealth ตัวจริงใน Scene จาก Tag "Player" ให้อัตโนมัติ
        if (playerHealth == null || !playerHealth.gameObject.scene.IsValid())
        {
            if (playerHealth != null)
                Debug.LogWarning("[OasisShield] ⚠️ ช่อง Player Health เป็น Prefab ใน Project ไม่ใช่ตัวใน Scene — กำลังหาตัวจริงให้");

            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                PlayerHealth found = p.GetComponentInParent<PlayerHealth>();
                if (found == null) found = p.GetComponentInChildren<PlayerHealth>();
                playerHealth = found;
            }
        }
    }

    void OnEnable()
    {
        if (playerHealth == null)
        {
            Debug.LogError("[OasisShield] ❌ ยังไม่ได้ลาก Player Health ใส่ใน Inspector — โล่จะไม่ทำงาน");
            return;
        }
        playerHealth.OnShieldHit += HandleShieldHit;
    }

    void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnShieldHit -= HandleShieldHit;
    }

    void Update()
    {
        if (playerHealth != null) currentHP = playerHealth.currentHealth;

        if (isShieldActive)
        {
            shieldTimer -= Time.deltaTime;
            if (shieldTimer <= 0f) DeactivateShield();
        }
        else
        {
            if (cooldownLeft > 0f) cooldownLeft -= Time.deltaTime;
            TryAutoActivate();
        }

        if (enableDebugKey)
        {
            if (Input.GetKeyDown(debugActivateKey)) TEST_ForceActivateShield();
            if (Input.GetKeyDown(debugDeactivateKey)) TEST_ForceDeactivateShield();
        }
    }

    // เช็คทุกเฟรม — ถ้าครบเงื่อนไขก็เปิดโล่ทันที
    private void TryAutoActivate()
    {
        if (!questUnlocked || !bossFightActive) return;
        if (playerHealth == null) return;

        if (playerHealth.currentHealth <= hpThreshold && !playerHealth.IsAlive())
        {
            if (!warnedNotAlive)
            {
                Debug.LogWarning($"[OasisShield] ⚠️ HP = {playerHealth.currentHealth} แต่ IsAlive() = false — เช็ค IsAlive() ใน PlayerHealth");
                warnedNotAlive = true;
            }
            return;
        }
        if (cooldownLeft > 0f) return;

        if (playerHealth.currentHealth <= hpThreshold)
            ActivateShield();
    }

    /// <summary>เรียกจาก RockQuestManager ตอนภารกิจรองสำเร็จ</summary>
    public void EnableShieldCapability()
    {
        questUnlocked = true;
        Debug.Log("[OasisShield] ✅ เงื่อนไข 1 ผ่าน: ปลดล็อกโล่แล้ว (เควสหินสำเร็จ)");
    }

    /// <summary>เรียกจาก BossMap2RoomTrigger ตอนผู้เล่นเข้า/ออกห้องบอส</summary>
    public void SetBossFightActive(bool active)
    {
        bossFightActive = active;
        Debug.Log($"[OasisShield] {(active ? "✅" : "⛔")} เงื่อนไข 2: Boss Fight Active = {active}");

        if (active && !questUnlocked)
            Debug.LogWarning("[OasisShield] ⚠️ เข้าห้องบอสแล้ว แต่ยังไม่ได้ปลดล็อกโล่ (เควสหินยังไม่สำเร็จ หรือ RockQuestManager ไม่ได้ผูกช่อง Oasis Shield Effect)");
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

        Debug.Log($"[OasisShield] 🛡️ โล่เปิดใช้งาน (HP = {playerHealth.currentHealth})");
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
        cooldownLeft = shieldCooldown;

        if (shieldVFX) shieldVFX.SetActive(false);

        Debug.Log($"[OasisShield] 💥 โล่พัง — คูลดาวน์ {shieldCooldown} วินาที");
    }

    // ════════════════════════════════════════════════════════════
    #region 🧪 Debug / Test
    // ════════════════════════════════════════════════════════════

    /// <summary>บังคับเปิดโล่ทันที ข้ามทุกเงื่อนไข (F8) — ใช้เช็คว่า VFX/Popup ทำงาน</summary>
    [ContextMenu("🧪 TEST - Force Activate Shield")]
    public void TEST_ForceActivateShield()
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("[OasisShield][TEST] ⚠️ ยังไม่ได้ผูก Player Health ใน Inspector");
            return;
        }
        if (isShieldActive) return;

        Debug.Log("[OasisShield][TEST] ⚡ บังคับเปิดโล่");
        ActivateShield();
    }

    /// <summary>บังคับปิดโล่ทันที (F7)</summary>
    [ContextMenu("🧪 TEST - Force Deactivate Shield")]
    public void TEST_ForceDeactivateShield()
    {
        if (!isShieldActive) return;

        Debug.Log("[OasisShield][TEST] ⚡ บังคับปิดโล่");
        DeactivateShield();
    }

    /// <summary>จำลองเงื่อนไข 1+2 ผ่าน — แล้วลองโดนตีจน HP <= 2 เพื่อเทส flow จริง</summary>
    [ContextMenu("🧪 TEST - Unlock + Boss Active (เทสเงื่อนไข HP)")]
    public void TEST_UnlockAndBossActive()
    {
        EnableShieldCapability();
        SetBossFightActive(true);
        cooldownLeft = 0f;
    }

    #endregion
}