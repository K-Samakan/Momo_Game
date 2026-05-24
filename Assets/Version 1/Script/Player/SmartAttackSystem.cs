// ============================================================
// FILE: SmartAttackSystem.cs
// PATH: Assets/Version 1/Script/Player/SmartAttackSystem.cs
// ============================================================
using System.Collections;
using UnityEngine;
using MOMO.Combat;
using MOMO.AI.Monster;

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
    public int punchDamage2 = 2;
    public int kickDamage = 1;
    public int kickDamage2 = 2;

    [Header("Detection Radius")]
    public float punchDetectRadius = 1.5f;
    public float kickDetectRadius = 1.8f;

    [Header("Glove Settings")]
    [Tooltip("Hitbox radius เพิ่มขึ้นเมื่อสวมถุงมือ")]
    public float glovePunchDetectRadius = 3f;

    [Header("Animator Parameters")]
    public string punchRightTrigger = "PunchRight";
    public string punchLeftTrigger = "PunchLeft";
    public string punchRight2Trigger = "PunchRight2";
    public string kickRightTrigger = "KickRight";
    public string kickLeftTrigger = "KickLeft";
    public string kickRight2Trigger = "KickRight2";

    [Header("Combo Settings")]
    public float comboResetTime = 1.2f;

    [Header("Debug")]
    public bool showGizmos = true;

    // ────────────────────────────────────────
    // Private
    // ────────────────────────────────────────

    private Animator _animator;
    private bool _isExecuting = false;
    private GameObject _lockedTarget;

    private int _punchStep = 0;
    private int _kickStep = 0;
    private float _lastAttackTime = -99f;

    private int _hashPunchRight;
    private int _hashPunchLeft;
    private int _hashPunchRight2;
    private int _hashKickRight;
    private int _hashKickLeft;
    private int _hashKickRight2;

    // ✅ Reference to GloveController
    private GloveController _gloveController;

    // ────────────────────────────────────────
    // Enum
    // ────────────────────────────────────────

    enum AttackType
    {
        PunchRight,
        PunchLeft,
        PunchRight2,
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

    void Start()
    {
        _gloveController = FindFirstObjectByType<GloveController>();
        if (_gloveController != null)
            Debug.Log("[SmartAttack] ✅ พบ GloveController");
        else
            Debug.Log("[SmartAttack] ℹ️ ไม่พบ GloveController (ปกติถ้ายังไม่ได้สวมถุงมือ)");
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

    public void OnPunchRightHit()
    {
        // ✅ ใช้ radius ใหญ่ขึ้นถ้าสวมถุงมือ
        float radius = (_gloveController != null && _gloveController.IsEquipped)
            ? glovePunchDetectRadius
            : punchDetectRadius;

        DoDetection(punchPoint, radius, punchDamage);
        _gloveController?.SpawnShockwaveRight();
    }

    public void OnPunchLeftHit()
    {
        float radius = (_gloveController != null && _gloveController.IsEquipped)
            ? glovePunchDetectRadius
            : punchDetectRadius;

        DoDetection(punchPoint, radius, punchDamage);
        _gloveController?.SpawnShockwaveLeft();
    }

    public void OnPunchRight2Hit()
    {
        float radius = (_gloveController != null && _gloveController.IsEquipped)
            ? glovePunchDetectRadius
            : punchDetectRadius;

        DoDetection(punchPoint, radius, punchDamage2);
        _gloveController?.SpawnShockwaveRight();
    }

    public void OnKickRightHit() => DoDetection(kickPoint, kickDetectRadius, kickDamage);
    public void OnKickLeftHit() => DoDetection(kickPoint, kickDetectRadius, kickDamage);
    public void OnKickRight2Hit() => DoDetection(kickPoint, kickDetectRadius, kickDamage2);

    // ────────────────────────────────────────
    // Core Sequence
    // ────────────────────────────────────────

    IEnumerator AttackSequence()
    {
        _isExecuting = true;

        float timeSinceLast = Time.time - _lastAttackTime;
        if (timeSinceLast > comboResetTime)
        {
            _punchStep = 0;
            _kickStep = 0;
            Debug.Log("[SmartAttack] Combo Reset → ครั้งที่ 1");
        }
        _lastAttackTime = Time.time;

        _lockedTarget = FindBestTarget();

        if (_lockedTarget != null)
            yield return StartCoroutine(SnapRotateToTarget(_lockedTarget.transform));

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
        // ✅ ถ้าสวมถุงมือ → ใช้แต่ Punch เท่านั้น ไม่มี Kick
        if (_gloveController != null && _gloveController.IsEquipped)
        {
            switch (_punchStep)
            {
                case 0: return AttackType.PunchRight;
                case 1: return AttackType.PunchLeft;
                default: return AttackType.PunchRight2;
            }
        }

        // ── ปกติ (ไม่มีถุงมือ) ──
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
            switch (_kickStep)
            {
                case 0: return AttackType.KickRight;
                case 1: return AttackType.KickLeft;
                default: return AttackType.KickRight2;
            }
        }
        else
        {
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
                _punchStep = 0;
                Debug.Log("[SmartAttack] ครั้งที่ 3 → PUNCH RIGHT2 (ท่าจบ) → reset");
                HapticManager.Instance?.Vibrate(0.25f, 0.6f, 0.2f);
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
                _kickStep = 0;
                Debug.Log("[SmartAttack] ครั้งที่ 3 → KICK RIGHT2 (ท่าจบ) → reset");
                HapticManager.Instance?.Vibrate(0.25f, 0.6f, 0.2f);
                break;
        }
    }

    // ────────────────────────────────────────
    // Damage Detection
    // ────────────────────────────────────────

    void DoDetection(Transform origin, float radius, int damage)
    {
        Debug.Log($"[DoDetection] Called! origin={origin.name}");

        Collider[] allHits = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var col in allHits)
        {
            if (!col.CompareTag("Cage")) continue;

            Vector3 dir = (col.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dir);

            if (angle > attackAngle / 2f) continue;

            CageController cage = col.GetComponent<CageController>();
            if (cage != null)
            {
                cage.TakeDamage(damage);
                Debug.Log($"[SmartAttack] Hit Cage: {col.name}");
                HapticManager.Instance?.Vibrate(0.2f, 0.3f, 0.1f);
            }
        }

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
            // ✅ แสดง radius ที่ใหญ่ขึ้นถ้าสวมถุงมือ
            bool equipped = _gloveController != null && _gloveController.IsEquipped;
            Gizmos.DrawWireSphere(punchPoint.position,
                equipped ? glovePunchDetectRadius : punchDetectRadius);
        }

        if (kickPoint)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(kickPoint.position, kickDetectRadius);
        }
    }

    public void SetAnimator(Animator newAnimator)
    {
        _animator = newAnimator;
        Debug.Log("[SmartAttackSystem] ✅ Animator อัพเดทแล้ว!");
    }
}