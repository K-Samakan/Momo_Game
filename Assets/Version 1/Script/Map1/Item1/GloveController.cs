using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.AI.Monster;

/// <summary>
/// ระบบถุงมือลิง — ยืดหมัดโจมตี + ทำลาย BreakableWall
/// ไม่มีปลอกแขน — แสดงแค่ FistHead + LineRenderer
/// สลับมือซ้าย/ขวาตาม Combo
/// </summary>
public class GloveController : MonoBehaviour
{
    [Header("Right Hand")]
    public Transform shoulderOrigin_R;
    public Transform fistHead_R;
    public LineRenderer chainLine_R;

    [Header("Left Hand")]
    public Transform shoulderOrigin_L;
    public Transform fistHead_L;
    public LineRenderer chainLine_L;

    [Header("Extend Settings")]
    public float maxExtendDistance = 6f;
    public float extendSpeed = 18f;
    public float retractSpeed = 25f;
    public float attackCooldown = 0.8f;

    [Header("Damage")]
    public int gloveDamage = 2;

    // ── Private ──
    private bool _isExtending = false;
    private float _lastAttackTime = -999f;
    private bool _useRightHand = true; // สลับซ้าย/ขวา
    private InputAction _attackAction;
    private PlayerInput _playerInput;
    private Animator _animator;
    private int _hashExtendPunch;

    // ─────────────────────────────────────────────
    void Awake()
    {
        _animator = GetComponentInParent<Animator>();
        _hashExtendPunch = Animator.StringToHash("ExtendPunch");

        // ซ่อนทุกอย่างตอนเริ่ม
        HideAll();
    }

    void OnEnable()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput == null)
            _playerInput = FindFirstObjectByType<PlayerInput>();

        if (_playerInput != null)
        {
            var map = _playerInput.actions.FindActionMap("Player");
            _attackAction = map?.FindAction("Attack");
            if (_attackAction != null)
                _attackAction.performed += OnAttackInput;
            else
                Debug.LogError("[GloveController] ❌ ไม่พบ Action 'Attack'!");
        }
        else
        {
            Debug.LogError("[GloveController] ❌ ไม่พบ PlayerInput!");
        }
    }

    void OnDisable()
    {
        if (_attackAction != null)
            _attackAction.performed -= OnAttackInput;
        HideAll();
    }

    // ─────────────────────────────────────────────
    // เรียกจาก GlovePickup ตอน Equip
    // ─────────────────────────────────────────────
    public void OnEquip()
    {
        Debug.Log("[GloveController] 🥊 Equipped! FistHead พร้อมแล้ว");
        // FistHead ยังซ่อนอยู่ จะโผล่เฉพาะตอนโจมตี
    }

    // ─────────────────────────────────────────────
    // Input
    // ─────────────────────────────────────────────
    void OnAttackInput(InputAction.CallbackContext context)
    {
        if (_isExtending) return;
        if (Time.time < _lastAttackTime + attackCooldown) return;

        // สลับมือ
        bool useRight = _useRightHand;
        _useRightHand = !_useRightHand;

        Transform fist = useRight ? fistHead_R : fistHead_L;
        Transform origin = useRight ? shoulderOrigin_R : shoulderOrigin_L;
        LineRenderer line = useRight ? chainLine_R : chainLine_L;

        if (fist == null || origin == null)
        {
            Debug.LogWarning("[GloveController] ⚠️ FistHead หรือ ShoulderOrigin ยังไม่ได้ลาก Reference!");
            return;
        }

        StartCoroutine(ExtendPunch(fist, origin, line));
    }

    // ─────────────────────────────────────────────
    // LateUpdate — อัพเดท LineRenderer
    // ─────────────────────────────────────────────
    void LateUpdate()
    {
        UpdateLine(chainLine_R, shoulderOrigin_R, fistHead_R);
        UpdateLine(chainLine_L, shoulderOrigin_L, fistHead_L);
    }

    void UpdateLine(LineRenderer line, Transform origin, Transform fist)
    {
        if (line == null || !line.enabled || origin == null || fist == null) return;
        line.SetPosition(0, origin.position);
        line.SetPosition(1, fist.position);
    }

    // ─────────────────────────────────────────────
    // Extend Punch
    // ─────────────────────────────────────────────
    IEnumerator ExtendPunch(Transform fist, Transform origin, LineRenderer line)
    {
        _isExtending = true;
        _lastAttackTime = Time.time;

        // เริ่มต้น
        fist.position = origin.position;
        fist.gameObject.SetActive(true);
        if (line != null) line.enabled = true;

        // Animation
        if (_animator != null)
            _animator.SetTrigger(_hashExtendPunch);

        // Haptic
        HapticManager.Instance?.Vibrate(0.2f, 0.5f, 0.1f);

        Debug.Log($"[GloveController] 👊 ยืดหมัดออก | มือ: {fist.name}");

        Vector3 targetPos = origin.position + transform.forward * maxExtendDistance;
        bool hitSomething = false;

        // ── Phase 1: พุ่งออก ──
        while (Vector3.Distance(fist.position, targetPos) > 0.15f)
        {
            fist.position = Vector3.MoveTowards(
                fist.position, targetPos, extendSpeed * Time.deltaTime);

            Collider[] hits = Physics.OverlapSphere(fist.position, 0.35f);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Enemy"))
                {
                    DealDamage(hit);
                    hitSomething = true;
                    break;
                }
                if (hit.CompareTag("BreakableWall"))
                {
                    hit.GetComponent<BreakableWall>()?.Break();
                    hitSomething = true;
                    break;
                }
            }
            if (hitSomething) break;
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        // ── Phase 2: ดึงกลับ ──
        while (Vector3.Distance(fist.position, origin.position) > 0.1f)
        {
            fist.position = Vector3.MoveTowards(
                fist.position, origin.position, retractSpeed * Time.deltaTime);
            yield return null;
        }

        // ── จบ ──
        fist.gameObject.SetActive(false);
        if (line != null) line.enabled = false;
        _isExtending = false;

        Debug.Log("[GloveController] 🥊 หมัดกลับแล้ว");
    }

    // ─────────────────────────────────────────────
    // Deal Damage
    // ─────────────────────────────────────────────
    void DealDamage(Collider enemyCol)
    {
        MonsterBT bt = enemyCol.GetComponent<MonsterBT>();
        if (bt != null)
        {
            bt.TakeDamage(gloveDamage);
            Debug.Log($"[GloveController] 💥 Hit BT: {enemyCol.name} | DMG: {gloveDamage}");
            HapticManager.Instance?.Vibrate(0.15f, 0.5f, 0.12f);
            return;
        }

        MonsterHealth mh = enemyCol.GetComponent<MonsterHealth>();
        if (mh != null && mh.IsAlive())
        {
            mh.TakeDamage(gloveDamage, enemyCol.transform.position);
            Debug.Log($"[GloveController] 💥 Hit MH: {enemyCol.name} | DMG: {gloveDamage}");
            HapticManager.Instance?.Vibrate(0.15f, 0.5f, 0.12f);
        }
    }

    // ─────────────────────────────────────────────
    // Helper
    // ─────────────────────────────────────────────
    void HideAll()
    {
        if (fistHead_R != null) fistHead_R.gameObject.SetActive(false);
        if (fistHead_L != null) fistHead_L.gameObject.SetActive(false);
        if (chainLine_R != null) chainLine_R.enabled = false;
        if (chainLine_L != null) chainLine_L.enabled = false;
    }

    // ─────────────────────────────────────────────
    // Gizmos
    // ─────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (shoulderOrigin_R != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(shoulderOrigin_R.position,
                shoulderOrigin_R.position + transform.forward * maxExtendDistance);
            Gizmos.DrawWireSphere(
                shoulderOrigin_R.position + transform.forward * maxExtendDistance, 0.35f);
        }
        if (shoulderOrigin_L != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(shoulderOrigin_L.position,
                shoulderOrigin_L.position + transform.forward * maxExtendDistance);
        }
    }
}