using UnityEngine;
using UnityEngine.AI;
using MOMO.AI.BehaviorTree;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class BossMap1BT : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public BossMap1Health health;
    public BossMap1UI bossUI;

    [Header("Settings")]
    public float introDelay = 3f;
    public float slamStuckDuration = 1.5f;
    public float spinDizzyDuration = 3f;
    public float staggerDuration = 5f;
    public float attackRange = 3f;
    public float detectionRange = 15f;
    public float moveSpeed = 4f;
    public float spinMoveSpeed = 5f;
    public float pathUpdateInterval = 0.3f;

    [Header("Hit Flash")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.2f;

    // ── State Flags ─────────────────────────────────────────
    private bool _isIntro = true;
    private bool _isDead = false;
    private bool _isStagger = false;
    private bool _isDizzy = false;
    private bool _isSlamming = false;
    private bool _isSpinning = false;
    private bool _isHit = false;
    private float _hitRecoveryTimer;

    // ── Combo (โดนโจมตี 5 ครั้ง → Stagger) ─────────────────
    private int _comboCount = 0;

    // ── Spin Counter ─────────────────────────────────────────
    private int _spinCount = 0;
    private float _spinSwingTimer = 0f;
    private float _spinSwingInterval = 0.5f;

    // ── NavMesh / Player ─────────────────────────────────────
    private NavMeshAgent _agent;
    private Transform _player;
    private float _lastPathUpdateTime;

    // ── Animator Hashes ─────────────────────────────────────
    private static readonly int HashSlamLeft = Animator.StringToHash("SlamLeft");
    private static readonly int HashSlamRight = Animator.StringToHash("SlamRight");
    private static readonly int HashSlamJump = Animator.StringToHash("SlamJump");
    private static readonly int HashStagger = Animator.StringToHash("Stagger");
    private static readonly int HashDead = Animator.StringToHash("Dead");

    // ── BT ──────────────────────────────────────────────────
    private BTNode _root;

    // ── Hit Flash ───────────────────────────────────────────
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private bool _isFlashing = false;

    // ── Tiger ───────────────────────────────────────────────
    private TigerAllyEffect _tigerAllyEffect;
    private MonsterHealth _monsterHealth;

    // ════════════════════════════════════════════════════════
    // Unity Lifecycle
    // ════════════════════════════════════════════════════════

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = false;

        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i].material != null)
                _originalColors[i] = _renderers[i].material.color;
    }

    void Start()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;

        _tigerAllyEffect = FindFirstObjectByType<TigerAllyEffect>();
        _monsterHealth = GetComponent<MonsterHealth>();

        StartCoroutine(InitNavMesh());
    }

    IEnumerator InitNavMesh()
    {
        yield return null;
        yield return null;

        if (NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            transform.position = hit.position;
        else
        {
            Debug.LogWarning("[BossMap1BT] ไม่พบ NavMesh!");
            yield break;
        }

        _agent.enabled = true;
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = attackRange * 0.8f;
        _agent.acceleration = 12f;
        _agent.angularSpeed = 360f;
        _agent.isStopped = true;

        yield return null;

        if (_agent.isOnNavMesh)
            _root = BuildTree();
    }

    void Update()
    {
        if (_isDead || _root == null) return;
        _root.Tick();
    }

    // ════════════════════════════════════════════════════════
    // Build Tree
    // ════════════════════════════════════════════════════════

    BTNode BuildTree() => new BTSelector(new List<BTNode>
    {
        // 1. ตาย
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsDead),
            new BTLeaf(DoDead)
        }),
        // 2. Intro หน่วง 3 วิ
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsIntro),
            new BTLeaf(DoIntroIdle)
        }),
        // 3. โดนโจมตี (Hit Stun)
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsHit),
            new BTLeaf(DoHitStun)
        }),
        // 4. Stagger (โดน combo 5 ครั้ง)
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsStagger),
            new BTLeaf(DoStagger)
        }),
        // 5. Dizzy (หลัง Spin ครบ 5 รอบ)
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsDizzy),
            new BTLeaf(DoDizzy)
        }),
        // 6. กำลัง Slam
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsSlamming),
            new BTLeaf(DoSlam)
        }),
        // 7. กำลัง Spin
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsSpinning),
            new BTLeaf(DoSpin)
        }),
        // 8. เห็นผู้เล่น → เลือก Attack หรือ Chase
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(CanSeePlayer),
            new BTSelector(new List<BTNode>
            {
                // โจมตีถ้าอยู่ในระยะ
                new BTSequence(new List<BTNode>
                {
                    new BTLeaf(InAttackRange),
                    new BTLeaf(FaceTarget),
                    new BTLeaf(ChoosePattern)
                }),
                // ไล่ถ้ายังไม่ถึง
                new BTLeaf(DoChase)
            })
        }),
        // 9. Idle
        new BTLeaf(DoIdle)
    });

    // ════════════════════════════════════════════════════════
    // Conditions
    // ════════════════════════════════════════════════════════

    BTStatus IsDead() => _isDead ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsIntro() => _isIntro ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsHit() => _isHit ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsStagger() => _isStagger ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsDizzy() => _isDizzy ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsSlamming() => _isSlamming ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsSpinning() => _isSpinning ? BTStatus.Success : BTStatus.Failure;

    BTStatus CanSeePlayer()
    {
        if (_player == null) return BTStatus.Failure;
        float dist = Vector3.Distance(transform.position, _player.position);
        return dist <= detectionRange ? BTStatus.Success : BTStatus.Failure;
    }

    BTStatus InAttackRange()
    {
        if (_player == null) return BTStatus.Failure;
        float dist = Vector3.Distance(transform.position, _player.position);
        return dist <= attackRange ? BTStatus.Success : BTStatus.Failure;
    }

    // ════════════════════════════════════════════════════════
    // Actions
    // ════════════════════════════════════════════════════════

    BTStatus DoDead()
    {
        _agent.isStopped = true;
        _agent.ResetPath();
        animator.SetTrigger(HashDead);
        bossUI.HideBossUI();
        Destroy(gameObject, 3f);
        return BTStatus.Running;
    }

    BTStatus DoIntroIdle()
    {
        // ยืนนิ่งระหว่าง intro — Coroutine จัดการ flag
        _agent.isStopped = true;
        return BTStatus.Running;
    }

    BTStatus DoHitStun()
    {
        _agent.isStopped = true;
        _hitRecoveryTimer -= Time.deltaTime;
        if (_hitRecoveryTimer <= 0f)
            _isHit = false;
        return BTStatus.Running;
    }

    BTStatus DoStagger()
    {
        // Coroutine จัดการ — BT แค่รอ flag หาย
        _agent.isStopped = true;
        return BTStatus.Running;
    }

    BTStatus DoDizzy()
    {
        // Coroutine จัดการ — BT แค่รอ flag หาย
        _agent.isStopped = true;
        return BTStatus.Running;
    }

    BTStatus DoSlam()
    {
        // Coroutine จัดการ — BT แค่รอ flag หาย
        _agent.isStopped = true;
        return BTStatus.Running;
    }

    BTStatus DoSpin()
    {
        // เคลื่อนที่หาผู้เล่นขณะ Spin
        _agent.isStopped = false;
        _agent.speed = spinMoveSpeed;
        ChasePlayer();

        // นับ swing ทุก interval
        _spinSwingTimer += Time.deltaTime;
        if (_spinSwingTimer >= _spinSwingInterval)
        {
            _spinSwingTimer = 0f;
            _spinCount++;

            if (_spinCount >= 5)
            {
                animator.SetBool("IsSpinning", false);
                StartCoroutine(DizzySequence());
            }
        }
        return BTStatus.Running;
    }

    BTStatus FaceTarget()
    {
        if (_player == null) return BTStatus.Failure;
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 15f);
        return BTStatus.Success;
    }

    BTStatus ChoosePattern()
    {
        // สุ่ม Pattern — 70% Slam, 30% Spin
        if (Random.value < 0.7f)
            StartCoroutine(SlamSequence());
        else
            StartCoroutine(SpinSequence());

        return BTStatus.Success;
    }

    BTStatus DoChase()
    {
        _agent.isStopped = false;
        _agent.speed = moveSpeed;
        animator.SetBool("IsChasing", true);
        ChasePlayer();
        return BTStatus.Running;
    }

    BTStatus DoIdle()
    {
        _agent.isStopped = true;
        _agent.ResetPath();
        animator.SetBool("IsChasing", false);
        return BTStatus.Running;
    }

    // ════════════════════════════════════════════════════════
    // Coroutines
    // ════════════════════════════════════════════════════════

    IEnumerator IntroSequence()
    {
        _isIntro = true;
        animator.SetBool("IsChasing", false);
        yield return new WaitForSeconds(introDelay);
        _isIntro = false;
    }

    IEnumerator SlamSequence()
    {
        _isSlamming = true;
        _agent.isStopped = true;

        // ครั้งที่ 1
        animator.SetTrigger(HashSlamLeft);
        yield return new WaitForSeconds(0.8f);

        // ครั้งที่ 2
        FaceTarget();
        animator.SetTrigger(HashSlamRight);
        yield return new WaitForSeconds(0.8f);

        // ครั้งที่ 3 — กระโดดลงพื้น + ค้าง
        FaceTarget();
        animator.SetTrigger(HashSlamJump);
        yield return new WaitForSeconds(slamStuckDuration);

        _isSlamming = false;
    }

    IEnumerator SpinSequence()
    {
        _isSpinning = true;
        _spinCount = 0;
        _spinSwingTimer = 0f;

        animator.SetBool("IsSpinning", true);

        // DoSpin() จะนับและจัดการ loop เอง
        // รอจน _isSpinning = false (set จาก DizzySequence)
        yield return new WaitUntil(() => !_isSpinning);
    }

    IEnumerator DizzySequence()
    {
        _isSpinning = false;
        _isDizzy = true;

        yield return new WaitForSeconds(spinDizzyDuration);
        _isDizzy = false;
    }

    IEnumerator StaggerSequence()
    {
        _isStagger = true;
        animator.SetTrigger(HashStagger);
        yield return new WaitForSeconds(staggerDuration);
        _isStagger = false;
    }

    // ════════════════════════════════════════════════════════
    // Public API
    // ════════════════════════════════════════════════════════

    // เรียกจาก BossRoomTrigger
    public void OnPlayerEnterBossRoom()
    {
        StartCoroutine(IntroSequence());
    }

    // เรียกจาก BossMap1Health
    public void OnHitByPlayer()
    {
        if (_isDead) return;

        _isHit = true;
        _hitRecoveryTimer = 0.3f;
        StartCoroutine(HitFlash());

        _comboCount++;
        if (_comboCount >= 5)
        {
            _comboCount = 0;
            StartCoroutine(StaggerSequence());

            if (_tigerAllyEffect != null && _monsterHealth != null)
                _tigerAllyEffect.OnPlayerAttackBoss(_monsterHealth);
        }
    }

    public void OnDead()
    {
        _isDead = true;
        StopAllCoroutines();
    }

    // ════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════

    void ChasePlayer()
    {
        if (_player == null || !_agent.isOnNavMesh) return;
        if (Time.time >= _lastPathUpdateTime + pathUpdateInterval)
        {
            _agent.SetDestination(_player.position);
            _lastPathUpdateTime = Time.time;
        }
    }

    IEnumerator HitFlash()
    {
        if (_isFlashing) yield break;
        _isFlashing = true;

        foreach (var r in _renderers)
            if (r.material != null)
                r.material.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i].material != null)
                _renderers[i].material.color = _originalColors[i];

        _isFlashing = false;
    }
}