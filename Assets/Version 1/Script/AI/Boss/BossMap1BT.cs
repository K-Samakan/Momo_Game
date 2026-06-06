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
    public float staggerCooldown = 12f;
    public float attackRange = 5f;
    [Range(0f, 1f)]
    public float slamChance = 0.7f;
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

    // ✅ FIX 1: ตัวแปรแสดง State ปัจจุบันใน Inspector
    [Header("🔍 Debug (Runtime)")]
    [SerializeField] private string currentState = "None";
    [SerializeField] private bool logStateChanges = false;

    // property ให้ Custom Editor อ่านค่าแบบ real-time
    public string CurrentState => currentState;
    public float DistanceToPlayer => _player ? Vector3.Distance(transform.position, _player.position) : -1f;
    public float AgentSpeed => (_agent != null && _agent.enabled && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;

    void SetState(string s)
    {
        if (currentState == s) return;
        currentState = s;
        if (logStateChanges) Debug.Log($"[Boss State] → {s}");
    }

    private bool _isIntro = true;
    private bool _introStarted = false;   // ✅ FIX: กัน IntroSequence เรียกซ้ำ
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
    private float _lastStaggerTime = -999f;

    private int _spinCount = 0;
    private float _spinSwingTimer = 0f;
    private float _spinSwingInterval = 0.5f;

    public NavMeshAgent _agent;
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

        _root = BuildTree();

        // ✅ FIX: เริ่ม intro อัตโนมัติ กันค้างที่ Intro ถ้า BossRoomTrigger ไม่ยิง event
        // ถ้าต้องการให้ intro เริ่มตอนผู้เล่นเข้าห้องเท่านั้น ให้คอมเมนต์บรรทัดล่างนี้ออก
        // แล้วไปตรวจ BossRoomTrigger ให้เรียก OnPlayerEnterBossRoom() แทน
        if (!_introStarted)
            StartCoroutine(IntroSequence());
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
        //_agent.isStopped = true;

        yield return null;

        //if (_agent.isOnNavMesh)

    }

    void Update()
    {
        //if (_isDead || _agent == null || !_agent.enabled || !_agent.isOnNavMesh || _root == null) return;
        _root.Tick();

    }

    BTNode BuildTree() => new BTSelector(new List<BTNode>
    {
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("dead",IsDead),
            new BTLeaf("doDead",DoDead)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("intro",IsIntro),
            new BTLeaf("doIntroIdle",DoIntroIdle)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("hit",IsHit),
            new BTLeaf("doHitStun",DoHitStun)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("stagger",IsStagger),
            new BTLeaf("doStagger",DoStagger)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("dizzy",IsDizzy),
            new BTLeaf("doDizzy",DoDizzy)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("slam",IsSlamming),
            new BTLeaf("doSlam",DoSlam)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("spinning",IsSpinning),
            new BTLeaf("doSpin",DoSpin)
        }),
        new BTSequence(new List<BTNode>
        {
            new BTLeaf("canSeePlayer",CanSeePlayer),
            new BTSelector(new List<BTNode>
            {
                new BTSequence(new List<BTNode>
                {
                    new BTLeaf("inAttackRange",InAttackRange),
                    new BTLeaf("faceTarget",FaceTarget),
                    new BTLeaf("choosePattern",ChoosePattern)
                }),
                new BTLeaf("doChase",DoChase)
            })
        }),
        new BTLeaf("doIdle",DoIdle)
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
        SetState("Dead");

        Debug.Log("💀 Boss กำลังตาย - เริ่ม DeathSequence");

        StopAllCoroutines();
        //_agent.isStopped = true;
        _agent.ResetPath();
        animator.SetFloat(HashSpeed, 0f);
        bossUI.HideBossUI();
        StartCoroutine(DeathSequence());
        return BTStatus.Running;
    }

    BTStatus DoIntroIdle()
    {
        SetState("Intro");
        //_agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoHitStun()
    {
        SetState("Hit");
        //_agent.isStopped = true;
        _hitRecoveryTimer -= Time.deltaTime;
        if (_hitRecoveryTimer <= 0f)
            _isHit = false;
        return BTStatus.Running;
    }

    BTStatus DoStagger()
    {
        SetState("Stagger");
        //_agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoDizzy()
    {
        SetState("Dizzy");
        //_agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoSlam()
    {
        SetState("Slam");
        //_agent.isStopped = true;
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoSpin()
    {
        SetState("Spin");
        //_agent.isStopped = false;
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
        SetState("Attack");
        if (_isPatternRunning) return BTStatus.Running;

        if (Random.value < slamChance)
            StartCoroutine(SlamSequence());
        else
            StartCoroutine(SpinSequence());

        return BTStatus.Success;
    }

    BTStatus DoChase()
    {
        SetState("Chase");                         // ✅ FIX 1
        if (_agent.enabled && _agent.isOnNavMesh)  // ✅ FIX 2: guard ก่อนสั่ง agent
        {
            _agent.isStopped = false;              // ✅ FIX 2: ปลดล็อกให้เดินได้
            _agent.speed = moveSpeed;
            ChasePlayer();
        }
        animator.SetFloat(HashSpeed, 1f);
        return BTStatus.Running;
    }

    BTStatus DoIdle()
    {
        SetState("Idle");
        //_agent.isStopped = true;
        _agent.ResetPath();
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    IEnumerator IntroSequence()
    {
        if (_introStarted) yield break;   // ✅ FIX: กันเรียกซ้ำ
        _introStarted = true;

        _isIntro = true;
        yield return new WaitForSeconds(introDelay);
        _isIntro = false;
    }

    IEnumerator SlamSequence()
    {
        _isPatternRunning = true;
        _isSlamming = true;
        //_agent.isStopped = true;
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
            if (Time.time >= _lastStaggerTime + staggerCooldown)
            {
                _lastStaggerTime = Time.time;
                StartCoroutine(StaggerSequence());

                if (_tigerAllyEffect != null && _monsterHealth != null)
                    _tigerAllyEffect.OnPlayerAttackBoss(_monsterHealth);
            }
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
        if (_player == null) return;
        if (!_agent.enabled || !_agent.isOnNavMesh) return;   // ✅ guard กัน error ช่วงเริ่มเกม

        if (Time.time >= _lastPathUpdateTime + pathUpdateInterval)
        {
            _agent.SetDestination(_player.position);            // ✅ FIX 2: เปิดกลับมา = หัวใจการไล่ล่า
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