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

    [Header("Attack Hitbox")]
    public BossAttackHitbox attackHitboxL;
    public BossAttackHitbox attackHitboxR;

    [Header("Hit VFX")]
    public GameObject hitVFX;

    [Header("Death VFX")]
    public GameObject deathVFX;
    public float deathVFXDuration = 2f;

    [Header("Settings")]
    public float introDelay = 3f;
    public float slamStuckDuration = 1.5f;
    public float spinDizzyDuration = 3f;
    public float staggerDuration = 5f;
    public float attackRange = 5f;
    public float detectionRange = 20f;
    public float moveSpeed = 4f;
    public float spinMoveSpeed = 5f;
    public float pathUpdateInterval = 0.3f;

    [Header("Hit Flash")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.2f;

    [Header("Death Fade")]
    public float deathAnimDuration = 3f;
    public float fadeDuration = 1.5f;

    private bool _isIntro = true;
    private bool _isDead = false;
    private bool _isStagger = false;
    private bool _isDizzy = false;
    private bool _isSlamming = false;
    private bool _isSpinning = false;
    private bool _isHit = false;
    private bool _isPatternRunning = false;
    private bool _deathTriggered = false;
    private float _hitRecoveryTimer;

    private int _comboCount = 0;

    private int _spinCount = 0;
    private float _spinSwingTimer = 0f;
    private float _spinSwingInterval = 0.5f;

    private NavMeshAgent _agent;
    private Transform _player;
    private float _lastPathUpdateTime;

    private static readonly int HashSlamLeft = Animator.StringToHash("SlamLeft");
    private static readonly int HashSlamRight = Animator.StringToHash("SlamRight");
    private static readonly int HashSlamJump = Animator.StringToHash("SlamJump");
    private static readonly int HashStagger = Animator.StringToHash("Stagger");
    private static readonly int HashDead = Animator.StringToHash("Dead");
    private static readonly int HashSpeed = Animator.StringToHash("Speed");

    private BTNode _root;

    private Renderer[] _renderers;
    private Color[] _originalColors;
    private bool _isFlashing = false;

    private TigerAllyEffect _tigerAllyEffect;
    private MonsterHealth _monsterHealth;

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

        if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
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
        if (_isDead || _agent == null || !_agent.enabled || !_agent.isOnNavMesh || _root == null) return;
        _root.Tick();
    }

    BTNode BuildTree() => new BTSelector(new List<BTNode>
    {
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsDead),
            new BTLeaf(DoDead)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsIntro),
            new BTLeaf(DoIntroIdle)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsHit),
            new BTLeaf(DoHitStun)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsStagger),
            new BTLeaf(DoStagger)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsDizzy),
            new BTLeaf(DoDizzy)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsSlamming),
            new BTLeaf(DoSlam)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(IsSpinning),
            new BTLeaf(DoSpin)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf(CanSeePlayer),
            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTLeaf(InAttackRange),
                    new BTLeaf(FaceTarget),
                    new BTLeaf(ChoosePattern)
                }),
                new BTLeaf(DoChase)
            })
        }),
        new BTLeaf(DoIdle)
    });

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

    BTStatus DoDead()
    {
        if (_deathTriggered) return BTStatus.Running;
        _deathTriggered = true;

        Debug.Log("💀 Boss กำลังตาย - เริ่ม DeathSequence");

        StopAllCoroutines();
        _agent.isStopped = true;
        _agent.ResetPath();
        animator.SetFloat(HashSpeed, 0f);
        bossUI.HideBossUI();
        StartCoroutine(DeathSequence());
        return BTStatus.Running;
    }

    BTStatus DoIntroIdle()
    {
        _agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
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
        _agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoDizzy()
    {
        _agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoSlam()
    {
        _agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoSpin()
    {
        _agent.isStopped = false;
        _agent.speed = spinMoveSpeed;
        animator.SetFloat(HashSpeed, 1f);
        ChasePlayer();

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
        if (_isPatternRunning) return BTStatus.Running;

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
        animator.SetFloat(HashSpeed, 1f);
        ChasePlayer();
        return BTStatus.Running;
    }

    BTStatus DoIdle()
    {
        _agent.isStopped = true;
        _agent.ResetPath();
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    IEnumerator IntroSequence()
    {
        _isIntro = true;
        yield return new WaitForSeconds(introDelay);
        _isIntro = false;
    }

    IEnumerator SlamSequence()
    {
        _isPatternRunning = true;
        _isSlamming = true;
        _agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);

        animator.SetTrigger(HashSlamLeft);
        yield return new WaitForSeconds(0.8f);

        FaceTarget();
        animator.SetTrigger(HashSlamRight);
        yield return new WaitForSeconds(0.8f);

        FaceTarget();
        animator.SetTrigger(HashSlamJump);

        yield return new WaitForSeconds(0.5f);
        animator.speed = 0f;
        yield return new WaitForSeconds(1.0f);
        animator.speed = 1f;

        yield return new WaitForSeconds(slamStuckDuration);

        _isSlamming = false;
        _isPatternRunning = false;
    }

    IEnumerator SpinSequence()
    {
        _isPatternRunning = true;
        _isSpinning = true;
        _spinCount = 0;
        _spinSwingTimer = 0f;

        animator.SetBool("IsSpinning", true);
        yield return new WaitUntil(() => !_isSpinning);

        _isPatternRunning = false;
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
        animator.SetFloat(HashSpeed, 0f);
        animator.SetTrigger(HashStagger);
        yield return new WaitForSeconds(staggerDuration);
        _isStagger = false;
    }

    // ✅ แก้ไข DeathSequence
    IEnumerator DeathSequence()
    {
        Debug.Log("🎬 เริ่ม DeathSequence");

        // ซ่อน UI
        if (bossUI != null)
        {
            bossUI.HideBossUI();
        }

        // ✅ Spawn VFX แยกจาก Boss
        if (deathVFX != null)
        {
            Debug.Log($"✨ Spawn VFX: {deathVFX.name} ที่ตำแหน่ง {transform.position}");

            GameObject vfx = Instantiate(deathVFX, transform.position, Quaternion.identity, null);

            ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>();
            float maxDuration = 2f;

            foreach (var ps in particleSystems)
            {
                if (ps != null)
                {
                    var main = ps.main;
                    float duration = main.duration + main.startLifetime.constantMax;
                    if (duration > maxDuration)
                        maxDuration = duration;
                }
            }

            Debug.Log($"⏱️ VFX ใช้เวลา: {maxDuration} วินาที");
            Destroy(vfx, maxDuration + 0.5f);
        }
        else
        {
            Debug.LogWarning("⚠️ Death VFX เป็น null");
        }

        // ✅ ปิด Boss ทันที
        Debug.Log("👋 ปิด Boss GameObject");
        gameObject.SetActive(false);

        yield break;  // สิ้นสุด Coroutine
    }

    public void OnPlayerEnterBossRoom()
    {
        StartCoroutine(IntroSequence());
    }

    public void OnHitByPlayer()
    {
        if (_isDead) return;

        _isHit = true;
        _hitRecoveryTimer = 0.3f;

        if (hitVFX != null)
        {
            GameObject vfx = Instantiate(hitVFX, transform.position + Vector3.up, Quaternion.identity);
            Destroy(vfx, 2f);
        }

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

    // ✅ แก้ OnDead() ให้เรียก DeathSequence ทันที
    public void OnDead()
    {
        if (_isDead) return;  // ป้องกันเรียกซ้ำ

        Debug.Log("☠️ OnDead() ถูกเรียก - เริ่ม DeathSequence ทันที");
        _isDead = true;

        // ✅ เรียก DeathSequence ทันที (ไม่รอ BT)
        StartCoroutine(DeathSequence());
    }

    public void OnHitboxOpenLeft(int damage)
    {
        attackHitboxL?.EnableHitbox(damage);
    }

    public void OnHitboxCloseLeft()
    {
        attackHitboxL?.DisableHitbox();
    }

    public void OnHitboxOpenRight(int damage)
    {
        attackHitboxR?.EnableHitbox(damage);
    }

    public void OnHitboxCloseRight()
    {
        attackHitboxR?.DisableHitbox();
    }

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