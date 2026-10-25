// ============================================================
// FILE: BossMap4RoomTrigger.cs  (แทนที่ไฟล์เดิม)
// PATH: Assets/Version 1/Script/AI/Boss/Boss_Map4/BossMap4RoomTrigger.cs
// ============================================================
using UnityEngine;

/// <summary>
/// Trigger เข้าห้องบอสด่าน 4 (หุ่นยนต์หมึกยักษ์)
/// โครงสร้างเหมือน BossMap2RoomTrigger — เพิ่มการเปิด TempleBlessingEffect (รางวัล Side Quest)
///
/// ติดบน Collider (Is Trigger = true) ที่วางกั้นทางเข้าห้องบอส
/// </summary>
public class BossMap4RoomTrigger : MonoBehaviour
{
    [Header("References")]
    public BossMap4BT bossController;
    public BossMap4UI bossUI;
    public GameObject sideQuestUI;

    [Header("VSE (Visual Sound Effect)")]
    [Tooltip("ตัวปล่อยเสียง/การสั่นของบอส — ขยายรัศมีให้ผู้เล่นรู้ตำแหน่งบอสทั้งห้องตอนเข้าฉากสู้")]
    public SoundEmitter bossSoundEmitter;
    public float bossRoomVSERadius = 50f;

    [Header("Temple Blessing (Side Quest Reward)")]
    public TempleBlessingEffect templeBlessingEffect;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // ✅ [FIX v2] เปิดพรวิหาร "ทุกครั้ง" ที่เดินเข้าห้อง (ไม่ใช่แค่ครั้งแรก)
        //    เดิมอยู่หลัง if (_triggered) return → ตายแล้ว respawn เดินกลับมา บัฟไม่กลับมาอีกเลย
        ArmTempleBlessing();

        if (_triggered) return;

        _triggered = true;

        if (sideQuestUI != null)
            sideQuestUI.SetActive(false);

        if (bossSoundEmitter != null)
            bossSoundEmitter.maxHearingRadius = bossRoomVSERadius;

        if (bossUI != null)
            bossUI.ShowBossUI();

        if (bossController != null)
            bossController.OnPlayerEnterBossRoom();

        Debug.Log("[BossMap4RoomTrigger] 🐙 ผู้เล่นเข้าห้องบอส — เริ่มการต่อสู้!");
    }

    void ArmTempleBlessing()
    {
        if (templeBlessingEffect == null)
            templeBlessingEffect = FindFirstObjectByType<TempleBlessingEffect>();
        if (templeBlessingEffect == null)
        {
            Debug.LogWarning("[BossMap4RoomTrigger] ⚠️ ไม่พบ TempleBlessingEffect ในฉาก — รางวัลเควสสมอจะไม่ทำงาน");
            return;
        }

        // ✅ กันเคส AnchorQuestManager ลากคนละ object / ลืมลาก → ถ้าเควสจบแล้ว ปลดล็อกตรงนี้ซ้ำได้ (idempotent)
        if (AnchorQuestManager.Instance != null && AnchorQuestManager.Instance.IsQuestComplete)
            templeBlessingEffect.EnableTempleBlessing();

        templeBlessingEffect.SetBossFightActive(true); // เริ่มเฉพาะเมื่อยังไม่มีรอบที่วิ่งอยู่
    }
}