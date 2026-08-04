using UnityEngine;

/// <summary>
/// Trigger เข้าห้องบอสด่าน 4 (หุ่นยนต์หมึกยักษ์)
/// โครงสร้างเหมือน BossRoomTrigger (Map1) / BossMap2RoomTrigger / BossMap3RoomTrigger ทุกประการ
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

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

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
}