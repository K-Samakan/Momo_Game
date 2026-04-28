using UnityEngine;

public class TigerAllyEffect : MonoBehaviour
{
    [Header("Tiger Ally Settings")]
    public int tigerBonusDamage = 3;
    public int comboRequirement = 5;

    private bool isActive = false;
    private int currentCombo = 0;

    [Header("Visual")]
    public GameObject tigerAttackVFX;
    public GameObject tigerAllyObject;

    public void EnableTigerAlly()
    {
        isActive = true;
        if (tigerAllyObject) tigerAllyObject.SetActive(true);
        Debug.Log("[TigerAlly] เสือพร้อมช่วยโจมตีบอสแล้ว!");
    }

    /// <summary>
    /// เรียกทุกครั้งที่ผู้เล่นโจมตีบอสสำเร็จ
    /// ✅ แก้: เปลี่ยน BossHealth → MonsterHealth (ตาม Project จริง)
    /// </summary>
    public void OnPlayerAttackBoss(MonsterHealth bossHealth)
    {
        if (!isActive) return;

        currentCombo++;
        Debug.Log($"[TigerAlly] Combo: {currentCombo}/{comboRequirement}");

        if (currentCombo >= comboRequirement)
        {
            TigerAssistAttack(bossHealth);
            currentCombo = 0;
        }
    }

    void TigerAssistAttack(MonsterHealth bossHealth)
    {
        // ✅ MonsterHealth.TakeDamage() ต้องการ (int damage, Vector3 attackerPosition)
        bossHealth.TakeDamage(tigerBonusDamage, transform.position);

        if (tigerAttackVFX)
            Instantiate(tigerAttackVFX, bossHealth.transform.position, Quaternion.identity);

        // ✅ แก้: ใช้ PlayPattern(Short) แทน PlayTigerAssist()
        // สั่นกระชับ = เสือพุ่งโจมตี
        HapticManager.Instance?.PlayPattern(HapticPatternType.Short);

        Debug.Log($"[TigerAlly] เสือช่วยโจมตี! บอสรับ +{tigerBonusDamage} damage");
    }
}