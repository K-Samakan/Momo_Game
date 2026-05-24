using UnityEngine;

public class BossRoomTrigger : MonoBehaviour
{
    [Header("References")]
    public BossMap1Controller bossController;
    public BossMap1UI bossUI;
    public GameObject sideQuestUI;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;

        _triggered = true;

        if (sideQuestUI != null)
            sideQuestUI.SetActive(false);

        bossUI.ShowBossUI();
        bossController.OnPlayerEnterBossRoom();
    }
}