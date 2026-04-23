using System.Collections;
using UnityEngine;
using MOMO.Combat;

/// <summary>
/// Smart Attack System
/// - หาเป้าอัตโนมัติ
/// - หันหน้าหาเป้าก่อนโจมตี
/// - เลือก Punch/Kick ตามขนาด Monster
/// </summary>
public class SmartAttackSystem : MonoBehaviour
{
    // ────────────────────────────────────────
    // Inspector Settings
    // ────────────────────────────────────────

    [Header("Detection")]
    public float attackRange = 2.5f;
    [Range(0f, 180f)]
    public float attackAngle = 100f;
    public string enemyTag = "Enemy";

    [Header("Rotation to Target")]
    [Tooltip("หันหน้าหาเป้าก่อนโจมตีกี่วินาที")]
    public float snapRotateDuration = 0.12f;
    [Tooltip("ความเร็วหมุนหาเป้า")]
    public float rotateSpeed = 720f;

    [Header("Attack Points")]
    [Tooltip("จุด origin ของหมัด (ระดับอก)")]
    public Transform punchPoint;
    [Tooltip("จุด origin ของเตะ (ระดับเอว/ขา)")]
    public Transform kickPoint;

    [Header("Damage")]
    public int punchDamage = 1;
    public int kickDamage = 1;

    [Header("Attack Ranges")]
    [Tooltip("radius ตรวจโดน — Punch")]
    public float punchDetectRadius = 1.5f;
    [Tooltip("radius ตรวจโดน — Kick (กว้างกว่าเล็กน้อย)")]
    public float kickDetectRadius = 1.8f;

    [Header("Animator Parameters")]
    public string punchTrigger = "Attack";   // Bool หรือ Trigger ใน Animator
    public string kickTrigger  = "Kick";     // ต้องเพิ่มใน Animator Controller

    [Header("Debug")]
    public bool showGizmos = true;

    // ────────────────────────────────────────
    // Private
    // ────────────────────────────────────────

    private Animator _animator;
    private bool _isExecuting = false;
    private GameObject _lockedTarget;

    // Animator IDs
    private int _animPunch;
    private int _animKick;

    // ────────────────────────────────────────
    // Public API (เรียกจาก ThirdPersonController)
    // ────────────────────────────────────────

    void Awake()
    {
        _animator  = GetComponent<Animator>();
        _animPunch = Animator.StringToHash(punchTrigger);
        _animKick  = Animator.StringToHash(kickTrigger);

        // fallback attack points
        if (punchPoint == null) punchPoint = transform;
        if (kickPoint  == null) kickPoint  = transform;
    }

    /// <summary>
    /// เรียกจาก ThirdPersonController แทน PerformAttack เดิม
    /// </summary>
    public void TriggerAttack()
    {
        if (_isExecuting) return;
        StartCoroutine(AttackSequence());
    }

    /// <summary>
    /// เรียกจาก Animation Event (Punch)
    /// </summary>
    public void OnPunchHit()
    {
        DoDetection(punchPoint, punchDetectRadius, punchDamage,
                    heightMin: 0f, heightMax: 999f); // ต่อยโดนทุก height
    }

    /// <summary>
    /// เรียกจาก Animation Event (Kick)
    /// </summary>
    public void OnKickHit()
    {
        DoDetection(kickPoint, kickDetectRadius, kickDamage,
                    heightMin: 0f, heightMax: 1.4f); // เตะโดนเฉพาะตัวเล็ก
    }

    // ────────────────────────────────────────
    // Core Sequence
    // ────────────────────────────────────────

    IEnumerator AttackSequence()
    {
        _isExecuting = true;

        // 1. หาเป้าที่ใกล้และอยู่ในมุม
        _lockedTarget = FindBestTarget();

        // 2. หันหน้าหาเป้า
        if (_lockedTarget != null)
            yield return StartCoroutine(SnapRotateToTarget(_lockedTarget.transform));

        // 3. เช็คขนาด monster → เลือก type
        AttackType type = DetermineAttackType(_lockedTarget);

        // 4. Play animation
        ExecuteAnimation(type);

        _isExecuting = false;
    }

    // ────────────────────────────────────────
    // Step Functions
    // ────────────────────────────────────────

    enum AttackType { Punch, Kick }

    GameObject FindBestTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, attackRange);
        GameObject best = null;
        float closestAngle = attackAngle / 2f;

        foreach (var col in cols)
        {
            if (!col.CompareTag(enemyTag)) continue;

            MonsterHealth mh = col.GetComponent<MonsterHealth>();
            if (mh == null || !mh.IsAlive()) continue;

            Vector3 dir = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);

            if (angle < closestAngle)
            {
                closestAngle = angle;
                best = col.gameObject;
            }
        }

        return best;
    }

    IEnumerator SnapRotateToTarget(Transform target)
    {
        float elapsed = 0f;
        Vector3 dir = (target.position - transform.position);
        dir.y = 0;
        if (dir == Vector3.zero) yield break;

        Quaternion targetRot = Quaternion.LookRotation(dir);

        while (elapsed < snapRotateDuration)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot,
                rotateSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, targetRot) < 1f) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRot;
    }

    AttackType DetermineAttackType(GameObject target)
    {
        if (target == null) return AttackType.Punch; // ไม่มีเป้า ต่อยลม

        // เช็ค MonsterSizeProfile ก่อน
        MonsterSizeProfile profile = target.GetComponent<MonsterSizeProfile>();
        if (profile != null)
        {
            return profile.GetSize() == MonsterSizeProfile.MonsterSize.Small
                ? AttackType.Kick
                : AttackType.Punch;
        }

        // Fallback: เช็คจาก Bounds height
        Collider col = target.GetComponent<Collider>();
        if (col != null && col.bounds.size.y < 1.2f)
            return AttackType.Kick;

        return AttackType.Punch;
    }

    void ExecuteAnimation(AttackType type)
    {
        if (_animator == null) return;

        if (type == AttackType.Kick)
        {
            _animator.SetTrigger(_animKick);
            Debug.Log("[SmartAttack] → KICK (monster ตัวเล็ก)");
        }
        else
        {
            _animator.SetTrigger(_animPunch);
            Debug.Log("[SmartAttack] → PUNCH");
        }

        // Haptic
        HapticManager.Instance?.Vibrate(0.15f, type == AttackType.Kick ? 0.5f : 0.3f, 0.15f);
    }

    // ────────────────────────────────────────
    // Damage Detection (เรียกจาก Animation Event)
    // ────────────────────────────────────────

    void DoDetection(Transform origin, float radius, int damage,
                     float heightMin, float heightMax)
    {
        Collider[] hits = Physics.OverlapSphere(origin.position, radius);

        foreach (var col in hits)
        {
            if (!col.CompareTag(enemyTag)) continue;

            MonsterHealth mh = col.GetComponent<MonsterHealth>();
            if (mh == null || !mh.IsAlive()) continue;

            // เช็คมุมโจมตี
            Vector3 dir = (col.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) > attackAngle / 2f) continue;

            // เช็ค height ของ monster (สำหรับ kick)
            float monsterH = col.bounds.size.y;
            if (monsterH < heightMin || monsterH > heightMax) continue;

            mh.TakeDamage(damage, transform.position);
            Debug.Log($"[SmartAttack] Hit: {col.name} | DMG: {damage}");

            HapticManager.Instance?.Vibrate(0.1f, 0.4f, 0.1f);
        }
    }

    // ────────────────────────────────────────
    // Gizmos
    // ────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        // Attack range
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Angle boundaries
        Gizmos.color = Color.yellow;
        Vector3 fwd = Application.isPlaying ? transform.forward : transform.forward;
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0,  attackAngle/2f, 0) * fwd * attackRange);
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0, -attackAngle/2f, 0) * fwd * attackRange);

        // Punch point
        if (punchPoint) { Gizmos.color = Color.blue;  Gizmos.DrawWireSphere(punchPoint.position, punchDetectRadius); }
        // Kick point
        if (kickPoint)  { Gizmos.color = Color.green; Gizmos.DrawWireSphere(kickPoint.position,  kickDetectRadius);  }
    }
}