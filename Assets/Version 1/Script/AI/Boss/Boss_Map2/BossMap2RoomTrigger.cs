using UnityEngine;

/// <summary>
/// Trigger เข้าห้องบอสด่าน 2
/// โครงสร้างเหมือน BossRoomTrigger ของ Map1
/// </summary>
public class BossMap2RoomTrigger : MonoBehaviour
{
    [Header("References")]
    public BossMap2BT bossController;
    public BossMap2UI bossUI;
    public GameObject sideQuestUI;

    [Header("VSE")]
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

        bossUI.ShowBossUI();
        bossController.OnPlayerEnterBossRoom();
    }
}