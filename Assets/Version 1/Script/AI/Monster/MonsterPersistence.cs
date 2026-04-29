using UnityEngine;

public class MonsterPersistence : MonoBehaviour
{
    [Header("=== Monster ID ===")]
    [Tooltip("ID ห้ามซ้ำกันทั้งเกม แนะนำ: SceneName_MonsterType_Index เช่น Forest_Goblin_01")]
    public string monsterID;

    private void Start()
    {
        // Auto ตั้ง ID จากชื่อ GameObject ถ้าไม่ได้กำหนดเอง
        if (string.IsNullOrEmpty(monsterID))
            monsterID = gameObject.name;

        // ตรวจกับ SaveManager ว่า Monster นี้เคยตายแล้วหรือยัง
        if (SaveManager.Instance != null && SaveManager.Instance.IsMonsterDead(monsterID))
        {
            Debug.Log($"☠️ [MonsterPersistence] {monsterID} ตายไปแล้ว → Deactivate ทันที");
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// เรียกจาก MonsterHealth.cs ใน Die() ก่อน Destroy
    ///   GetComponent<MonsterPersistence>()?.RegisterDeath();
    /// </summary>
    public void RegisterDeath()
    {
        SaveManager.Instance?.RegisterMonsterKill(monsterID);
        Debug.Log($"☠️ [MonsterPersistence] {monsterID} บันทึกการตายแล้ว");
    }
}