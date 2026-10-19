// Assets/Version 1/Script/VSE/ProjectileShootEmitter.cs
using UnityEngine;

/// <summary>
/// ติดกับ "สิ่งที่บอสยิงออกมา" เท่านั้น (หมัด / ก้อนหิมะ / เลเซอร์-หนวด)
/// ตัวบอสยังใช้ SoundEmitter tag "Enemy" เหมือนเดิม — สคริปต์นี้ไม่ไปยุ่งกับบอสเลย
///
/// การทำงาน:
///   1) Awake  → บังคับ SoundEmitter บน object นี้ให้เป็น tag "Shoot"
///   2) Start  → Emit() ทันที 1 ครั้งตอนเกิด (เตือนว่ามีของพุ่งมา)
///   3) Update → (เลือกได้) Emit ซ้ำระหว่างบิน ไอคอนจะขยับทิศตามกระสุน
///               ยิ่งใกล้ผู้เล่น ไอคอนยิ่งชัด/สั่นแรงขึ้น
///
/// ใช้ได้ 2 แบบ:
///   • Prefab  : Add Component ใน Inspector (หมัด, หิมะ)
///   • Runtime : ProjectileShootEmitter.Attach(go)  (เลเซอร์/หนวดที่สร้างด้วยโค้ด)
/// </summary>
[RequireComponent(typeof(SoundEmitter))]
public class ProjectileShootEmitter : MonoBehaviour
{
    [Header("VSE Tag")]
    [Tooltip("ต้องตรงกับ Tag Name ใน VSE Manager > Visual Types")]
    public string shootTag = "Shoot";

    [Range(0f, 1f)]
    public float spawnIntensity = 1f;

    [Header("ระหว่างบิน (Tracking)")]
    [Tooltip("Emit ซ้ำระหว่างบินให้ไอคอนชี้ตามตำแหน่งกระสุน")]
    public bool emitWhileFlying = true;

    [Tooltip("ทุกกี่วินาทีจะ Emit ซ้ำ")]
    public float flyingEmitInterval = 0.4f;

    [Tooltip("รัศมีที่ผู้เล่นจะเห็น/รู้สึกกระสุนนี้")]
    public float hearingRadius = 25f;

    private SoundEmitter _emitter;
    private Transform _player;
    private float _nextEmitTime;

    /// <summary>ติด VSE Shoot ให้ GameObject ที่สร้างด้วยโค้ด (ถ้ามีอยู่แล้วจะไม่ติดซ้ำ)</summary>
    public static ProjectileShootEmitter Attach(GameObject go)
    {
        if (go == null) return null;
        var pse = go.GetComponent<ProjectileShootEmitter>();
        if (pse == null) pse = go.AddComponent<ProjectileShootEmitter>(); // RequireComponent เพิ่ม SoundEmitter ให้เอง
        return pse;
    }

    void Awake()
    {
        _emitter = GetComponent<SoundEmitter>();

        // ✅ บังคับ tag เป็น Shoot เสมอ — กันกรณี SoundEmitter ถูกเพิ่มอัตโนมัติด้วยค่าดีฟอลต์ "Enemy"
        _emitter.soundTag = shootTag;
        _emitter.baseIntensity = spawnIntensity;
        _emitter.maxHearingRadius = hearingRadius;
        _emitter.emitCooldown = Mathf.Min(_emitter.emitCooldown, flyingEmitInterval * 0.5f);
    }

    void Start()
    {
        _player = PlayerReference.GetPlayerTransform();
        if (_player == null)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }

        // ✅ เตือนทันทีตอนกระสุนเกิด
        _emitter.Emit();
        _nextEmitTime = Time.time + flyingEmitInterval;
        Debug.Log($"[ShootVSE] 🎯 Emit Shoot ตอนเกิด — {gameObject.name}");
    }

    void Update()
    {
        if (!emitWhileFlying || _player == null) return;
        if (Time.time < _nextEmitTime) return;

        _nextEmitTime = Time.time + flyingEmitInterval;
        _emitter.EmitByDistance(_player); // ไกล = จาง, ใกล้ = ชัด (ออกนอกรัศมีจะไม่ขึ้นเอง)
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.63f, 0.25f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, hearingRadius);
    }
}