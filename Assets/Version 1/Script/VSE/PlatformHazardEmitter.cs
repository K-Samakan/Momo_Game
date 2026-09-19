using UnityEngine;

/// <summary>
/// เช็คระยะห่างผู้เล่นเองทุกเฟรม แล้วสั่ง SoundEmitter ยิง VSE
/// ไม่ต้องใช้ Trigger Collider — ตัดปัญหาเรื่อง scale/rigidbody ทิ้งทั้งหมด
/// </summary>
[RequireComponent(typeof(SoundEmitter))]
public class HazardProximityEmitter : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("ลาก GameObject ผู้เล่นใส่ตรงนี้ (ถ้าเว้นว่างจะหาเองจาก Tag)")]
    public Transform player;
    public string playerTag = "Player";

    [Header("Emit Settings")]
    [Tooltip("ระยะที่เริ่มยิง VSE (ควร <= Max Hearing Radius ของ SoundEmitter)")]
    public float triggerRadius = 15f;

    [Tooltip("เว้นกี่วินาทีก่อนเช็คใหม่ (ไม่ต้องเช็คทุกเฟรมก็ได้)")]
    public float checkInterval = 0.2f;

    [Header("Debug")]
    public bool showDebugLog = true;

    private SoundEmitter _emitter;
    private float _nextCheckTime;

    void Awake()
    {
        _emitter = GetComponent<SoundEmitter>();
    }

    void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null)
            {
                player = found.transform;
                if (showDebugLog)
                    Debug.Log($"[Hazard] เจอผู้เล่นอัตโนมัติ: {found.name}");
            }
            else
            {
                Debug.LogError($"[Hazard] ❌ หาผู้เล่นไม่เจอ! ไม่มี GameObject ที่ Tag = {playerTag} " +
                               $"→ ให้ลาก Transform ผู้เล่นใส่ช่อง Player ใน Inspector โดยตรง");
            }
        }
    }

    void Update()
    {
        if (player == null || _emitter == null) return;
        if (Time.time < _nextCheckTime) return;

        _nextCheckTime = Time.time + checkInterval;

        float dist = Vector3.Distance(transform.position, player.position);

        if (showDebugLog)
            Debug.Log($"[Hazard] ระยะห่าง = {dist:F2} / triggerRadius = {triggerRadius}");

        if (dist <= triggerRadius)
            _emitter.EmitByDistance(player);
    }

    // วาดวงระยะให้เห็นใน Scene View ตอนเลือก object นี้
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}