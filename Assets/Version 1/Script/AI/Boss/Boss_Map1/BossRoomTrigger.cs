using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("References")]
    public BossMap1BT bossController;
    public BossMap1UI bossUI;
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

        // ✅ ขยายรัศมี VSE ของบอสให้กว้างทั้งห้อง
        if (bossSoundEmitter != null)
            bossSoundEmitter.maxHearingRadius = bossRoomVSERadius;

        bossUI.ShowBossUI();
        bossController.OnPlayerEnterBossRoom();

        // ✅ เปลี่ยน BGM เป็นเพลงบอสตอนผู้เล่นเข้าห้อง
        MusicManager.Instance?.PlayBossMusic();
    }
}