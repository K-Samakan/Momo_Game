using UnityEngine;

/// <summary>
/// PlayerReference — Singleton ที่แก้ปัญหา "Player ผี" ค้างจาก DontDestroyOnLoad
/// 
/// ปัญหาที่แก้:
///   GameObject.FindGameObjectWithTag("Player") อาจเจอ instance เก่าที่ค้างจาก
///   Scene ก่อนหน้า (ถ้า Player Prefab เรียก DontDestroyOnLoad โดยไม่เช็ค duplicate)
///   ทำให้ Boss ไล่ตาม "ผี" ที่ลอยอยู่ตำแหน่งเก่าแทนตัวจริง
/// 
/// วิธีใช้:
///   1) ติด Component นี้บน root ของ Player Prefab (ตัวเดียวกับที่เรียก DontDestroyOnLoad)
///   2) ลบโค้ด DontDestroyOnLoad เดิมที่กระจัดกระจายอยู่ทุกที่ออก ให้ script นี้จัดการแทน
///   3) Boss เรียก PlayerReference.Instance.transform แทน FindGameObjectWithTag("Player")
/// 
/// กลไกป้องกัน Duplicate:
///   ถ้ามี Instance เก่าอยู่แล้วตอน Awake() → ทำลายตัวใหม่ทิ้งทันที (ตัวเก่าคือของจริงเสมอ)
///   ถ้าไม่มี Instance เก่า → ตัวเองกลายเป็น Instance ใหม่ + DontDestroyOnLoad
/// </summary>
public class PlayerReference : MonoBehaviour
{
    public static PlayerReference Instance { get; private set; }

    [Header("Debug")]
    [Tooltip("แสดง log ตอน duplicate ถูกทำลาย — เปิดไว้ช่วยเช็คว่าเคยมีผีเกิดขึ้นไหม")]
    public bool logDuplicateDestruction = true;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (logDuplicateDestruction)
                Debug.LogWarning(
                    $"[PlayerReference] 👻 พบ Player ซ้ำซ้อน! ทำลาย duplicate '{gameObject.name}' ทิ้ง " +
                    $"(instance เดิมคือ '{Instance.gameObject.name}' ที่ position {Instance.transform.position})");

            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[PlayerReference] ✅ ตั้งเป็น Singleton instance ที่ position {transform.position}");
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// เรียกใช้แทน GameObject.FindGameObjectWithTag("Player") ทุกที่ในโปรเจกต์
    /// รับประกันว่าได้ Player ตัวจริงเสมอ ไม่มีทางได้ผี
    /// </summary>
    public static Transform GetPlayerTransform()
    {
        if (Instance != null) return Instance.transform;

        // Fallback เผื่อกรณียังไม่ได้ติด PlayerReference (backward compatibility)
        Debug.LogWarning("[PlayerReference] ⚠️ Instance เป็น null! ใช้ FindGameObjectWithTag แทนชั่วคราว — ควรติด PlayerReference บน Player Prefab");
        var go = GameObject.FindGameObjectWithTag("Player");
        return go != null ? go.transform : null;
    }
}