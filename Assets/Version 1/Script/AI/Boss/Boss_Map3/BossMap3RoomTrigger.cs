using UnityEngine;

public class BossMap3RoomTrigger : MonoBehaviour
{
    [Header("References")]
    public BossMap3BT bossController;
    public BossMap3UI bossUI;
    public GameObject sideQuestUI;
    public SoundEmitter bossSoundEmitter;
    public float bossRoomVSERadius = 50f;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;
        if (sideQuestUI != null) sideQuestUI.SetActive(false);
        if (bossSoundEmitter != null) bossSoundEmitter.maxHearingRadius = bossRoomVSERadius;
        bossUI.ShowBossUI();
        bossController.OnPlayerEnterBossRoom();
    }
}