using System.Collections;
using UnityEngine;
using MOMO.Combat;
using MOMO.AI.Monster;

/// <summary>
/// Smart Attack System
/// 
/// Punch Combo (3 ครั้ง ต่อเนื่อง):
///   ครั้งที่ 1 → PunchRight  (ขวาแบบที่ 1)
///   ครั้งที่ 2 → PunchLeft   (ซ้าย)
///   ครั้งที่ 3 → PunchRight2 (ขวาแบบที่ 2 / ท่าจบ)
///   → วนกลับครั้งที่ 1 ถ้ากดต่อเนื่อง
///
/// Kick Combo (3 ครั้ง ต่อเนื่อง) เมื่อ Monster เล็ก:
///   ครั้งที่ 1 → KickRight
///   ครั้งที่ 2 → KickLeft
///   ครั้งที่ 3 → KickRight2
///   → วนกลับครั้งที่ 1 ถ้ากดต่อเนื่อง
///
/// ถ้าหยุดกดนานเกิน comboResetTime → reset กลับครั้งที่ 1
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
    public float snapRotateDuration = 0.12f;
    public float rotateSpeed = 720f;

    [Header("Attack Points")]
    public Transform punchPoint;
    public Transform kickPoint;

    [Header("Damage")]
    public int punchDamage = 1;
    public int punchDamage2 = 2;  // ท่าจบ Punch แรงกว่า
    public int kickDamage = 1;
    public int kickDamage2 = 2;  // ท่าจบ Kick แรงกว่า

    [Header("Detection Radius")]
    public float punchDetectRadius = 1.5f;
    public float kickDetectRadius = 1.8f;

    [Header("Animator Parameters")]
    public string punchRightTrigger = "PunchRight";   // ขวาแบบที่ 1
    public string punchLeftTrigger = "PunchLeft";    // ซ้าย
    public string punchRight2Trigger = "PunchRight2";  // ขวาแบบที่ 2 (ท่าจบ)
    public string kickRightTrigger = "KickRight";    // เตะขวาแบบที่ 1
    public string kickLeftTrigger = "KickLeft";     // เตะซ้าย
    public string kickRight2Trigger = "KickRight2";   // เตะขวาแบบที่ 2 (ท่าจบ)

    [Header("Combo Settings")]
    [Tooltip("ถ้าหยุดกดนานเกินนี้ (วินาที) → reset combo กลับครั้งที่ 1")]
    public float comboResetTime = 1.2f;

    [Header("Debug")]
    public bool showGizmos = true;

    // ────────────────────────────────────────
    // Private
    // ────────────────────────────────────────

    private Animator _animator;
    private bool _isExecuting = false;
    private GameObject _lockedTarget;

    // Combo step: 0 = ครั้งที่1, 1 = ครั้งที่2, 2 = ครั้งที่3
    private int _punchStep = 0;
    private int _kickStep = 0;
    private float _lastAttackTime = -99f;

    // Animator Hashes
    private int _hashPunchRight;
    private int _hashPunchLeft;
    private int _hashPunchRight2;
    private int _hashKickRight;
    private int _hashKickLeft;
    private int _hashKickRight2;

    // ────────────────────────────────────────
    // Enum
    // ────────────────────────────────────────

    enum AttackType
    {
        PunchRight,   // ขวาแบบที่ 1
        PunchLeft,    // ซ้าย
        PunchRight2,  // ขวาแบบที่ 2 (ท่าจบ)
        KickRight,
        KickLeft,
        KickRight2
    }

    // ────────────────────────────────────────
    // Unity
    // ────────────────────────────────────────

    void Awake()
    {
        _animator = GetComponent<Animator>();

        _hashPunchRight = Animator.StringToHash(punchRightTrigger);
        _hashPunchLeft = Animator.StringToHash(punchLeftTrigger);
        _hashPunchRight2 = Animator.StringToHash(punchRight2Trigger);
        _hashKickRight = Animator.StringToHash(kickRightTrigger);
        _hashKickLeft = Animator.StringToHash(kickLeftTrigger);
        _hashKickRight2 = Animator.StringToHash(kickRight2Trigger);

        if (punchPoint == null) punchPoint = transform;
        if (kickPoint == null) kickPoint = transform;
    }

    // ────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────

    public void TriggerAttack()
    {
        if (_isExecuting) return;
        StartCoroutine(AttackSequence());
    }

    // ────────────────────────────────────────
    // Animation Events
    // ────────────────────────────────────────

    public void OnPunchRightHit() => DoDetection(punchPoint, punchDetectRadius, punchDamage);
    public void OnPunchLeftHit() => DoDetection(punchPoint, punchDetectRadius, punchDamage);
    public void OnPunchRight2Hit() => DoDetection(punchPoint, punchDetectRadius, punchDamage2);
    public void OnKickRightHit() => DoDetection(kickPoint, kickDetectRadius, kickDamage);
    public void OnKickLeftHit() => DoDetection(kickPoint, kickDetectRadius, kickDamage);
    public void OnKickRight2Hit() => DoDetection(kickPoint, kickDetectRadius, kickDamage2);

    // ────────────────────────────────────────
    // Core Sequence
    // ────────────────────────────────────────

    IEnumerator AttackSequence()
    {
        _isExecuting = true;

        // ── เช็ค Combo Reset ──
        float timeSinceLast = Time.time - _lastAttackTime;
        if (timeSinceLast > comboResetTime)
        {
            _punchStep = 0;
            _kickStep = 0;
            Debug.Log("[SmartAttack] Combo Reset → ครั้งที่ 1");
        }
        _lastAttackTime = Time.time;

        // ── หาเป้า ──
        _lockedTarget = FindBestTarget();

        // ── หันหน้าหาเป้า ──
        if (_lockedTarget != null)
            yield return StartCoroutine(SnapRotateToTarget(_lockedTarget.transform));

        // ── ตัดสินใจและโจมตี ──
        AttackType type = DetermineAttackType(_lockedTarget);
        ExecuteAnimation(type);

        _isExecuting = false;
    }

    // ────────────────────────────────────────
    // Step Functions
    // ────────────────────────────────────────

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
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir == Vector3.zero) yield break;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        float elapsed = 0f;

        while (elapsed < snapRotateDuration)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, targetRot, rotateSpeed * Time.deltaTime);

            if (Quaternion.Angle(transform.rotation, targetRot) < 1f) break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = targetRot;
    }

    AttackType DetermineAttackType(GameObject target)
    {
        bool isSmall = false;

        if (target != null)
        {
            MonsterSizeProfile profile = target.GetComponent<MonsterSizeProfile>();
            if (profile != null)
                isSmall = profile.GetSize() == MonsterSizeProfile.MonsterSize.Small;
            else
            {
                Collider col = target.GetComponent<Collider>();
                if (col != null) isSmall = col.bounds.size.y < 1.2f;
            }
        }

        if (isSmall)
        {
            // Kick combo 3 ขั้น
            switch (_kickStep)
            {
                case 0: return AttackType.KickRight;
                case 1: return AttackType.KickLeft;
                default: return AttackType.KickRight2;
            }
        }
        else
        {
            // Punch combo 3 ขั้น
            switch (_punchStep)
            {
                case 0: return AttackType.PunchRight;
                case 1: return AttackType.PunchLeft;
                default: return AttackType.PunchRight2;
            }
        }
    }

    void ExecuteAnimation(AttackType type)
    {
        if (_animator == null) return;

        switch (type)
        {
            case AttackType.PunchRight:
                _animator.SetTrigger(_hashPunchRight);
                _punchStep = 1;
                Debug.Log("[SmartAttack] ครั้งที่ 1 → PUNCH RIGHT");
                HapticManager.Instance?.Vibrate(0.15f, 0.3f, 0.12f);
                break;

            case AttackType.PunchLeft:
                _animator.SetTrigger(_hashPunchLeft);
                _punchStep = 2;
                Debug.Log("[SmartAttack] ครั้งที่ 2 → PUNCH LEFT");
                HapticManager.Instance?.Vibrate(0.15f, 0.3f, 0.12f);
                break;

            case AttackType.PunchRight2:
                _animator.SetTrigger(_hashPunchRight2);
                _punchStep = 0; // วนกลับครั้งที่ 1
                Debug.Log("[SmartAttack] ครั้งที่ 3 → PUNCH RIGHT2 (ท่าจบ) → reset");
                HapticManager.Instance?.Vibrate(0.25f, 0.6f, 0.2f); // แรงกว่า
                break;

            case AttackType.KickRight:
                _animator.SetTrigger(_hashKickRight);
                _kickStep = 1;
                Debug.Log("[SmartAttack] ครั้งที่ 1 → KICK RIGHT");
                HapticManager.Instance?.Vibrate(0.15f, 0.4f, 0.15f);
                break;

            case AttackType.KickLeft:
                _animator.SetTrigger(_hashKickLeft);
                _kickStep = 2;
                Debug.Log("[SmartAttack] ครั้งที่ 2 → KICK LEFT");
                HapticManager.Instance?.Vibrate(0.15f, 0.4f, 0.15f);
                break;

            case AttackType.KickRight2:
                _animator.SetTrigger(_hashKickRight2);
                _kickStep = 0; // วนกลับครั้งที่ 1
                Debug.Log("[SmartAttack] ครั้งที่ 3 → KICK RIGHT2 (ท่าจบ) → reset");
                HapticManager.Instance?.Vibrate(0.25f, 0.6f, 0.2f); // แรงกว่า
                break;
        }
    }

    // ────────────────────────────────────────
    // Damage Detection
    // ────────────────────────────────────────

    void DoDetection(Transform origin, float radius, int damage)
    {
        Debug.Log($"[DoDetection] Called! origin={origin.name}");

        // ── ตรวจกรง ──
        Collider[] allHits = Physics.OverlapSphere(transform.position, attackRange);
        Debug.Log($"[DoDetection] OverlapSphere พบ {allHits.Length} Collider, attackRange={attackRange}");

        foreach (var col in allHits)
        {
            Debug.Log($"[DoDetection] พบ: {col.name} | Tag: {col.tag}"); // ✅ ดูทุก Collider

            if (!col.CompareTag("Cage")) continue;

            Vector3 dir = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);
            Debug.Log($"[DoDetection] Cage พบ! angle={angle} | attackAngle/2={attackAngle / 2f}");

            if (angle > attackAngle / 2f) continue;

            CageController cage = col.GetComponent<CageController>();
            if (cage != null)
            {
                cage.TakeDamage(damage);
                Debug.Log($"[SmartAttack] Hit Cage: {col.name}");
                HapticManager.Instance?.Vibrate(0.2f, 0.3f, 0.1f);
            }
            else
            {
                Debug.Log($"[DoDetection] ไม่พบ CageController บน {col.name}!");
            }
        }

        // ── ตรวจศัตรู (ของเดิม ไม่แตะ) ──
        Collider[] hits = Physics.OverlapSphere(origin.position, radius);
        foreach (var col in hits)
        {
            if (!col.CompareTag(enemyTag)) continue;

            Vector3 dir = (col.transform.position - transform.position).normalized;
            if (Vector3.Angle(transform.forward, dir) > attackAngle / 2f) continue;

            MonsterBT bt = col.GetComponent<MonsterBT>();
            if (bt != null)
            {
                bt.TakeDamage(damage);
                Debug.Log($"[SmartAttack] Hit: {col.name} | DMG: {damage}");
                HapticManager.Instance?.Vibrate(0.1f, 0.4f, 0.1f);
                continue;
            }

            MonsterHealth mh = col.GetComponent<MonsterHealth>();
            if (mh != null && mh.IsAlive())
            {
                mh.TakeDamage(damage, transform.position);
                Debug.Log($"[SmartAttack] Hit (MH): {col.name} | DMG: {damage}");
                HapticManager.Instance?.Vibrate(0.1f, 0.4f, 0.1f);
            }
        }
    }

    // ────────────────────────────────────────
    // Gizmos
    // ────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Vector3 fwd = transform.forward;
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0, attackAngle / 2f, 0) * fwd * attackRange);
        Gizmos.DrawLine(transform.position,
            transform.position + Quaternion.Euler(0, -attackAngle / 2f, 0) * fwd * attackRange);

        if (punchPoint)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(punchPoint.position, punchDetectRadius);
        }

        if (kickPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(kickPoint.position, kickDetectRadius);
        }
    }
}