using UnityEngine;

/// <summary>
/// Trigger เข้าห้องบอสด่าน 3 (Snow Man)
/// ✅ [Map3 Side Quest] เพิ่ม WhaleGuardianEffect — แพทเทิร์นเดียวกับ OasisShieldEffect ใน BossMap2RoomTrigger
/// </summary>
public class BossMap3RoomTrigger : MonoBehaviour
{
    [Header("References")]
    public BossMap3BT bossController;
    public BossMap3UI bossUI;
    public GameObject sideQuestUI;
    public SoundEmitter bossSoundEmitter;
    public float bossRoomVSERadius = 50f;

    [Header("Whale Guardian (Side Quest Reward)")]
    public WhaleGuardianEffect whaleGuardianEffect;

    private bool _triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;

        if (sideQuestUI != null) sideQuestUI.SetActive(false);
        if (bossSoundEmitter != null) bossSoundEmitter.maxHearingRadius = bossRoomVSERadius;

        bossUI.ShowBossUI();
        bossController.OnPlayerEnterBossRoom();

        if (whaleGuardianEffect != null)
            whaleGuardianEffect.SetBossFightActive(true);
    }
}