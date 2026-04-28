using UnityEngine;
using MOMO.Player;

public class FallDeathTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tag ของผู้เล่น")]
    public string playerTag = "Player";

    [Tooltip("หน่วงก่อนตาย (วินาที) เพื่อให้รู้สึกว่าตกก่อน")]
    public float deathDelay = 0.3f;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph != null)
            StartCoroutine(DelayedDeath(ph));
    }

    System.Collections.IEnumerator DelayedDeath(PlayerHealth ph)
    {
        yield return new WaitForSeconds(deathDelay);

        if (ph != null && ph.IsAlive())
            ph.FallDeath(); // เรียก Die โดยตรง ไม่หัก HP ทีละจุด
    }
}
