using UnityEngine;
using UnityEngine.AI;
using MOMO.AI.BehaviorTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// บอสด่าน 3 — หุ่นยนต์ Snowman
/// 
/// ท่าที่ 1: SnowballThrow — ปาหิมะ 3 ลูก (ATK=2, slow, ทำลายได้)
/// ท่าที่ 2: SpinAttack — หมุนตัว 5 รอบ (ATK=1/รอบ) → หยุดนิ่ง 10 วิ
/// จุดอ่อน: ธนูไฟ 3 ครั้ง → หยุดนิ่ง 20 วิ (เตรียมไว้)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BossMap3BT : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public BossMap3Health health;
    public BossMap3UI bossUI;

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
    public float staggerDuration = 5f;
    public float staggerCooldown = 12f;
    public float attackRange = 5f;
    public float detectionRange = 20f;
    public float moveSpeed = 4f;
    public float pathUpdateInterval = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("โอกาสเลือกท่า SnowballThrow (ที่เหลือ = SpinAttack)")]
    public float snowballChance = 0.6f;

    // ═══════════════════════════════════════════════════
    // ท่าที่ 1: SnowballThrow — ปาหิมะ
    // ═══════════════════════════════════════════════════
    [Header("SnowballThrow Pattern")]
    [Tooltip("Prefab ก้อนหิมะ (ต้องมี SnowballProjectile + Collider + Rigidbody)")]
    public GameObject snowballPrefab;

    [Tooltip("จุด Spawn หิมะ (มือบอส)")]
    public Transform snowballSpawnPoint;

    [Tooltip("ดีเลย์ระหว่างหิมะแต่ละลูก")]
    public float snowballInterval = 1.0f;

    [Tooltip("Damage ต่อหิมะ")]
    public int snowballDamage = 2;

    [Tooltip("ความเร็วหิมะ")]
    public float snowballSpeed = 12f;

    [Tooltip("ระยะเวลา slow เมื่อโดนหิมะ (วินาที)")]
    public float snowballSlowDuration = 3f;

    [Tooltip("ตัวคูณความเร็วตอนโดน slow (0.5 = ช้าลงครึ่งหนึ่ง)")]
    public float snowballSlowMultiplier = 0.5f;

    [Tooltip("แรง Knockback เมื่อโดนหิมะ")]
    public float snowballKnockbackForce = 6f;

    [Tooltip("ระยะเวลาหยุดนิ่งหลังปาหิมะครบ (นี่คือเวลารวมทั้งหมด — เดิมมี delay ซ่อนเพิ่มอีก 1 วิ ตอนนี้ลบออกแล้ว)")]
    public float snowballStuckDuration = 1.0f;

    // ═══════════════════════════════════════════════════
    // ท่าที่ 2: SpinAttack — หมุนตัวโจมตี
    // ═══════════════════════════════════════════════════
    [Header("SpinAttack Pattern")]
    [Tooltip("จำนวนรอบที่หมุน")]
    public int spinMaxRotations = 5;

    [Tooltip("ความเร็วหมุน (องศา/วินาที)")]
    public float spinSpeed = 720f;

    [Tooltip("ความเร็วเคลื่อนที่ขณะหมุน")]
    public float spinMoveSpeed = 5f;

    [Tooltip("Damage ต่อรอบ")]
    public int spinDamage = 1;

    [Tooltip("รัศมี hitbox ขณะหมุน")]
    public float spinHitRadius = 3f;

    [Tooltip("ระยะเวลาหยุดนิ่งหลังหมุนครบ (ปรับได้)")]
    public float spinStunDuration = 10f;

    [Tooltip("Knockback แรงดันขณะหมุน")]
    public float spinKnockbackForce = 8f;

    [Header("Spin VFX")]
    [Tooltip("Trail Renderer สำหรับ VFX หมุน (ใส่ 1-2 ตัว child ของ Boss)")]
    public TrailRenderer[] spinTrails;

    [Tooltip("สี VFX หมุน")]
    public Color spinVFXColor = new Color(0.4f, 0.9f, 1f, 0.8f);

    [Header("Spin Tornado VFX (Procedural — ไม่ต้องใช้ Asset ภายนอก)")]
    [Tooltip("สีพายุหมุน (ค่า default เป็นสีเขียวเหลือง คล้ายในภาพตัวอย่าง)")]
    public Color tornadoColor = new Color(0.6f, 1f, 0.15f, 0.8f);

    [Tooltip("รัศมีพายุหมุน (แนะนำให้เท่ากับ spinHitRadius)")]
    public float tornadoRadius = 3f;

    // ═══════════════════════════════════════════════════
    // จุดอ่อน: ธนูไฟ — เตรียมไว้
    // ═══════════════════════════════════════════════════
    [Header("Fire Arrow Weakness (เตรียมไว้)")]
    [Tooltip("จำนวนธนูไฟที่ต้องยิงเพื่อ stun")]
    public int fireArrowsRequired = 3;

    [Tooltip("ระยะเวลา stun เมื่อโดนธนูไฟครบ")]
    public float fireArrowStunDuration = 20f;

    // ═══════════════════════════════════════════════════
    // Hit Flash & Death
    // ═══════════════════════════════════════════════════
    [Header("Hit Flash")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.2f;

    [Header("Death Fade")]
    public float deathAnimDuration = 3f;
    public float fadeDuration = 1.5f;

    [Header("Debug (Runtime)")]
    [SerializeField] private string currentState = "None";
    [SerializeField] private bool logStateChanges = false;

    [Header("🩺 On-Screen Debug (ไม่พึ่ง Console)")]
    [Tooltip("แสดงกล่อง Debug บนหน้าจอ Game view ตอน Play — เห็นชัดแม้ Console จะถูก filter ซ่อนไว้")]
    public bool showOnScreenDebug = true;

    public string CurrentState => currentState;
    public float DistanceToPlayer => _player ? Vector3.Distance(transform.position, _player.position) : -1f;
    public float AgentSpeed => (_agent != null && _agent.enabled && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;

    // ✅ ตัวเช็คตรงๆ ว่า Agent พร้อมเดินจริงหรือไม่ — ไม่ผ่าน Console เลย
    public bool IsAgentReady => _agent != null && _agent.enabled && _agent.isOnNavMesh;
    public bool HasPath => _agent != null && _agent.enabled && _agent.isOnNavMesh && _agent.hasPath;
    public bool InitNavMeshCompleted { get; private set; } = false;

    // ✅ Diagnostic ที่พิสูจน์ได้จริงว่าฟังก์ชันถูกเรียกกี่ครั้ง — กัน enum-default หลอก
    private int _chasePlayerCallCount = 0;
    private int _setDestinationCallCount = 0;
    private string _chaseSkipReason = "ยังไม่เคยเรียก ChasePlayer()";
    private bool _playerPositionOnNavMesh = false;
    private bool _loggedPathIssueOnce = false;

    // ✅ ใหม่: ระบบยกเลิกการโจมตีเมื่อโดน Stagger กลางคัน
    private bool _attackCancelled = false;
    private SpinTornadoVFX _activeTornado;

    void SetState(string s) { if (currentState == s) return; currentState = s; if (logStateChanges) Debug.Log($"[BossMap3] → {s}"); }

    // ── Private Flags ──
    private bool _isIntro = true, _introStarted = false, _isDead = false;
    private bool _isStagger = false, _isSnowballing = false, _isSpinning = false;
    private bool _isFireStunned = false;
    private bool _isHit = false, _isPatternRunning = false, _deathTriggered = false;
    private float _hitRecoveryTimer;
    private int _comboCount = 0;
    private float _lastStaggerTime = -999f;

    // Fire arrow tracking
    private int _fireArrowHitCount = 0;

    public NavMeshAgent _agent;
    private Transform _player;
    private float _lastPathUpdateTime;

    // Animator Hashes — ใช้ Slam เดิมจาก Boss2 สำหรับปาหิมะ
    private static readonly int HashSlamRight = Animator.StringToHash("SlamRight");
    private static readonly int HashSlamLeft = Animator.StringToHash("SlamLeft");
    private static readonly int HashSlamJump = Animator.StringToHash("SlamJump");
    private static readonly int HashSpin = Animator.StringToHash("Spin");      // ✅ ใหม่: T-pose ตอนหมุน
    private static readonly int HashReturnIdle = Animator.StringToHash("ReturnToIdle"); // ✅ ใหม่: สั่งกลับ Idle ตรงๆ ไม่พึ่ง Exit Time
    private static readonly int HashStagger = Animator.StringToHash("Stagger");
    private static readonly int HashDead = Animator.StringToHash("Dead");
    private static readonly int HashSpeed = Animator.StringToHash("Speed");

    private BTNode _root;
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private bool _isFlashing = false;

    // ═══════════════════════════════════════════════════
    // Unity Lifecycle
    // ═══════════════════════════════════════════════════

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
        // ✅ ใช้ Singleton แทน FindGameObjectWithTag ที่อาจเจอ "ผี" Player ค้างจาก DontDestroyOnLoad
        _player = PlayerReference.GetPlayerTransform();

        if (_player == null)
            Debug.LogError("[BossMap3] ❌ ไม่พบ Player เลย! เช็คว่าติด PlayerReference.cs บน Player Prefab หรือยัง");
        else
            Debug.Log($"[BossMap3] ✅ เชื่อมต่อ Player สำเร็จที่ position {_player.position}");
        StartCoroutine(InitNavMesh());
        _root = BuildTree();
        if (!_introStarted) StartCoroutine(IntroSequence());

        // ปิด Trail Renderer ไว้ก่อน
        SetSpinTrails(false);
    }

    IEnumerator InitNavMesh()
    {
        yield return null; yield return null;

        // ✅ เพิ่มรัศมีค้นหาจาก 5f → 20f กัน spawn point อยู่ไกลจาก NavMesh
        if (!NavMesh.SamplePosition(transform.position, out var hit, 20f, NavMesh.AllAreas))
        {
            Debug.LogError($"[BossMap3] ❌ ไม่พบ NavMesh ในรัศมี 20m จากตำแหน่ง {transform.position}! " +
                            "เช็คว่า Bake NavMesh ครอบคลุมจุดวางบอสหรือยัง");
            InitNavMeshCompleted = true; // จบแล้ว (แม้ fail) — เพื่อให้ On-Screen Debug แสดงผลได้ถูก
            yield break;
        }

        _agent.enabled = true;

        // ✅ ใช้ Warp() แทนการตั้ง transform.position ตรงๆ
        // Warp จะ sync ตำแหน่ง Agent เข้ากับ NavMesh ถูกต้องกว่า
        bool warpSuccess = _agent.Warp(hit.position);

        if (!warpSuccess || !_agent.isOnNavMesh)
        {
            Debug.LogError($"[BossMap3] ❌ Agent.Warp() ล้มเหลว! isOnNavMesh={_agent.isOnNavMesh} " +
                            $"— ตรวจสอบ Agent Type ตรงกับ NavMesh Surface ที่ Bake ไว้หรือไม่");
            InitNavMeshCompleted = true;
            yield break;
        }

        _agent.speed = moveSpeed;
        _agent.stoppingDistance = attackRange * 0.8f;
        _agent.acceleration = 12f;
        _agent.angularSpeed = 360f;

        InitNavMeshCompleted = true;
        Debug.Log($"[BossMap3] ✅ NavMesh Ready! Position: {_agent.transform.position}, isOnNavMesh: {_agent.isOnNavMesh}");
        yield return null;
    }

    // ═══════════════════════════════════════════════════
    // ✅ On-Screen Debug — เห็นแน่นอนไม่ว่า Console จะถูก filter ไว้หรือไม่
    // ═══════════════════════════════════════════════════
    void OnGUI()
    {
        if (!showOnScreenDebug) return;

        float w = 380, h = 300;
        float x = Screen.width - w - 10;
        float y = 10;

        GUI.Box(new Rect(x, y, w, h), "");

        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, normal = { textColor = Color.yellow } };
        var okStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.green } };
        var badStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.red } };
        var neutralStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white } };

        float lx = x + 10, ly = y + 8, lh = 19;

        GUI.Label(new Rect(lx, ly, w, lh), "🩺 BossMap3 NavMesh Debug (v2 — ไม่โดน enum หลอก)", titleStyle); ly += lh + 4;
        GUI.Label(new Rect(lx, ly, w, lh), $"InitNavMesh Completed: {InitNavMeshCompleted}", InitNavMeshCompleted ? okStyle : badStyle); ly += lh;
        GUI.Label(new Rect(lx, ly, w, lh), $"IsAgentReady: {IsAgentReady}", IsAgentReady ? okStyle : badStyle); ly += lh;
        GUI.Label(new Rect(lx, ly, w, lh), "", neutralStyle); ly += 2;

        // ✅ ตัวพิสูจน์จริง: ChasePlayer() ถูกเรียกกี่ครั้ง
        GUI.Label(new Rect(lx, ly, w, lh), $"ChasePlayer() ถูกเรียก: {_chasePlayerCallCount} ครั้ง",
            _chasePlayerCallCount > 0 ? okStyle : badStyle); ly += lh;

        // ✅ ตัวพิสูจน์จริง: SetDestination() ถูกเรียกกี่ครั้ง
        GUI.Label(new Rect(lx, ly, w, lh), $"SetDestination() ถูกเรียก: {_setDestinationCallCount} ครั้ง",
            _setDestinationCallCount > 0 ? okStyle : badStyle); ly += lh;

        GUI.Label(new Rect(lx, ly, w, lh), $"เหตุผลล่าสุด: {_chaseSkipReason}",
            _chaseSkipReason == "OK" ? okStyle : badStyle); ly += lh;

        // ✅ ผลลัพธ์จริงของ SetDestination — ไม่ใช่ enum default
        GUI.Label(new Rect(lx, ly, w, lh), $"SetDestination() คืนค่า: {_lastDestinationResult}",
            _lastDestinationResult ? okStyle : badStyle); ly += lh;

        // ✅ ตำแหน่ง Player เองอยู่บน NavMesh ไหม (แยกจากตำแหน่งบอส)
        GUI.Label(new Rect(lx, ly, w, lh), $"ตำแหน่ง Player อยู่บน NavMesh: {_playerPositionOnNavMesh}",
            _playerPositionOnNavMesh ? okStyle : badStyle); ly += lh;

        GUI.Label(new Rect(lx, ly, w, lh), $"HasPath: {HasPath}", neutralStyle); ly += lh;
        GUI.Label(new Rect(lx, ly, w, lh), $"agent.pathPending: {(_agent != null && _agent.pathPending)}", neutralStyle); ly += lh;
        GUI.Label(new Rect(lx, ly, w, lh), $"Velocity: {(_agent != null ? _agent.velocity.magnitude : 0f):F2}", neutralStyle); ly += lh;
        GUI.Label(new Rect(lx, ly, w, lh), $"State: {currentState}", neutralStyle);
    }

    void Update() => _root.Tick();

    // ═══════════════════════════════════════════════════
    // Behavior Tree
    // ═══════════════════════════════════════════════════

    BTNode BuildTree() => new BTSelector(new List<BTNode>
    {
        new BTSequence(new List<BTNode> { new BTLeaf("dead", IsDead), new BTLeaf("doDead", DoDead) }),
        new BTSequence(new List<BTNode> { new BTLeaf("intro", IsIntro), new BTLeaf("doIntro", DoIntroIdle) }),
        new BTSequence(new List<BTNode> { new BTLeaf("hit", IsHit), new BTLeaf("doHit", DoHitStun) }),
        new BTSequence(new List<BTNode> { new BTLeaf("stagger", IsStagger), new BTLeaf("doStagger", DoStagger) }),
        new BTSequence(new List<BTNode> { new BTLeaf("fireStun", IsFireStunned), new BTLeaf("doFireStun", DoFireStun) }),
        new BTSequence(new List<BTNode> { new BTLeaf("spin", IsSpinning), new BTLeaf("doSpin", DoSpinState) }),
        new BTSequence(new List<BTNode> { new BTLeaf("snow", IsSnowballing), new BTLeaf("doSnow", DoSnowballState) }),
        new BTSequence(new List<BTNode> {
            new BTLeaf("see", CanSeePlayer),
            new BTSelector(new List<BTNode> {
                new BTSequence(new List<BTNode> { new BTLeaf("range", InAttackRange), new BTLeaf("face", FaceTarget), new BTLeaf("choose", ChoosePattern) }),
                new BTLeaf("chase", DoChase)
            })
        }),
        new BTLeaf("idle", DoIdle)
    });

    // ── Conditions ──
    BTStatus IsDead() => _isDead ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsIntro() => _isIntro ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsHit() => _isHit ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsStagger() => _isStagger ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsFireStunned() => _isFireStunned ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsSpinning() => _isSpinning ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsSnowballing() => _isSnowballing ? BTStatus.Success : BTStatus.Failure;
    BTStatus CanSeePlayer() => _player != null && Vector3.Distance(transform.position, _player.position) <= detectionRange ? BTStatus.Success : BTStatus.Failure;
    BTStatus InAttackRange() => _player != null && Vector3.Distance(transform.position, _player.position) <= attackRange ? BTStatus.Success : BTStatus.Failure;

    // ── Actions ──
    BTStatus DoDead() { if (_deathTriggered) return BTStatus.Running; _deathTriggered = true; SetState("Dead"); StopAllCoroutines(); _agent.ResetPath(); animator.SetFloat(HashSpeed, 0f); bossUI.HideBossUI(); StartCoroutine(DeathSequence()); return BTStatus.Running; }
    BTStatus DoIntroIdle() { SetState("Intro"); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }
    BTStatus DoHitStun() { SetState("Hit"); _hitRecoveryTimer -= Time.deltaTime; if (_hitRecoveryTimer <= 0f) _isHit = false; return BTStatus.Running; }
    BTStatus DoStagger() { SetState("Stagger"); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }
    BTStatus DoFireStun() { SetState("FireStun"); animator.SetFloat(HashSpeed, 0f); if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; } return BTStatus.Running; }
    BTStatus DoSpinState() { SetState("Spin"); return BTStatus.Running; }
    BTStatus DoSnowballState() { SetState("Snowball"); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }

    BTStatus FaceTarget()
    {
        if (_player == null) return BTStatus.Failure;
        Vector3 dir = (_player.position - transform.position).normalized; dir.y = 0;
        if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 15f);
        return BTStatus.Success;
    }

    BTStatus ChoosePattern()
    {
        SetState("Attack");
        if (_isPatternRunning) return BTStatus.Running;
        if (Random.value < snowballChance) StartCoroutine(SnowballSequence());
        else StartCoroutine(SpinAttackSequence());
        return BTStatus.Success;
    }

    BTStatus DoChase()
    {
        SetState("Chase");

        bool canMove = _agent.enabled && _agent.isOnNavMesh;

        if (canMove)
        {
            _agent.isStopped = false;
            _agent.speed = moveSpeed;
            ChasePlayer();
        }

        // ✅ เล่น animation เดินก็ต่อเมื่อ Agent ขยับได้จริงเท่านั้น
        // กันปัญหาโมเดลเล่นท่าเดินอยู่กับที่ (moonwalk) ตอน Agent หลุด NavMesh
        animator.SetFloat(HashSpeed, canMove ? 1f : 0f);

        if (!canMove)
            Debug.LogWarning("[BossMap3] ⚠️ Agent ไม่สามารถเดินได้ (enabled/isOnNavMesh=false) — ไม่เล่น walk animation");

        return BTStatus.Running;
    }

    BTStatus DoIdle() { SetState("Idle"); if (_agent.enabled && _agent.isOnNavMesh) _agent.ResetPath(); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }

    // ═══════════════════════════════════════════════════
    // ท่าที่ 1: SnowballThrow — ปาหิมะ 3 ลูก
    // ═══════════════════════════════════════════════════

    IEnumerator SnowballSequence()
    {
        _attackCancelled = false; // ✅ reset ทุกครั้งที่เริ่มท่าใหม่
        _isPatternRunning = true;
        _isSnowballing = true;
        animator.SetFloat(HashSpeed, 0f);
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }

        // ✅ ใช้ SlamRight chain เดียวกับ Boss2
        // Animator จะ chain: SlamRight → SlamLeft → SlamJump → Idle
        FaceTarget();
        animator.SetTrigger(HashSlamRight);

        // หิมะลูกที่ 1 (ระหว่าง SlamRight)
        yield return new WaitForSeconds(0.5f);
        if (_attackCancelled) { EndSnowballSequence(); yield break; }
        SpawnSnowball();
        Debug.Log("[BossMap3] ❄️ ปาหิมะลูกที่ 1/3");

        // หิมะลูกที่ 2 (ระหว่าง SlamLeft)
        yield return new WaitForSeconds(snowballInterval + 0.5f);
        if (_attackCancelled) { EndSnowballSequence(); yield break; }
        FaceTarget();
        SpawnSnowball();
        Debug.Log("[BossMap3] ❄️ ปาหิมะลูกที่ 2/3");

        // หิมะลูกที่ 3 (ระหว่าง SlamJump)
        yield return new WaitForSeconds(snowballInterval + 0.5f);
        if (_attackCancelled) { EndSnowballSequence(); yield break; }
        FaceTarget();
        SpawnSnowball();
        Debug.Log("[BossMap3] ❄️ ปาหิมะลูกที่ 3/3");

        // ✅ แก้ไข: รวมเวลาหน่วงเป็นค่าเดียว (เดิมมี WaitForSeconds(1.0f) แอบซ่อนอยู่
        // บวกกับ snowballStuckDuration แยกกัน รวมเป็น 2.5 วิ ทำให้รู้สึกค้างนาน)
        // ตอนนี้ปรับได้จุดเดียวผ่าน "Snowball Stuck Duration" ใน Inspector
        Debug.Log($"[BossMap3] ❄️ ปาหิมะครบ → หยุดนิ่ง {snowballStuckDuration} วิ");
        yield return new WaitForSeconds(snowballStuckDuration);
        if (_attackCancelled) { EndSnowballSequence(); yield break; }

        EndSnowballSequence();
    }

    /// <summary>Cleanup ปลายทาง — เรียกทั้งตอนจบปกติและตอนถูกยกเลิกกลางคัน</summary>
    void EndSnowballSequence()
    {
        _isSnowballing = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        _isPatternRunning = false;

        // ✅ สั่ง Animator กลับ Idle ตรงๆ ทันที — ไม่ต้องรอ Exit Time ของ Clip
        animator.SetTrigger(HashReturnIdle);
    }

    void SpawnSnowball()
    {
        if (_player == null || snowballPrefab == null) return;

        Vector3 spawnPos = snowballSpawnPoint != null
            ? snowballSpawnPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 2f;

        Vector3 dir = (_player.position + Vector3.up - spawnPos).normalized;

        var snowball = Instantiate(snowballPrefab, spawnPos, Quaternion.LookRotation(dir));

        // ✅ Auto-Attach: ถ้า Prefab ลืมใส่ SnowballProjectile ไว้ → เพิ่มให้อัตโนมัติ
        // กันปัญหา "หิมะลอยนิ่ง" แบบถาวร ไม่ต้องพึ่งการตั้งค่า Prefab ให้ถูกต้องเป๊ะ
        var proj = snowball.GetComponent<SnowballProjectile>();
        if (proj == null)
        {
            proj = snowball.AddComponent<SnowballProjectile>();
            Debug.LogWarning("[BossMap3] ⚠️ Snowball Prefab ไม่มี SnowballProjectile.cs ติดมา " +
                              "→ เพิ่มให้อัตโนมัติแล้ว (แนะนำ: ไปเพิ่ม Component นี้ที่ตัว Prefab ให้ถาวรด้วย)");
        }

        proj.Init(snowballDamage, dir, snowballSpeed, snowballSlowDuration, snowballSlowMultiplier,
            snowballKnockbackForce);

        Destroy(snowball, 8f);
    }

    // ═══════════════════════════════════════════════════
    // ท่าที่ 2: SpinAttack — หมุนตัว 5 รอบ + VFX
    // ═══════════════════════════════════════════════════

    IEnumerator SpinAttackSequence()
    {
        _attackCancelled = false; // ✅ reset ทุกครั้งที่เริ่มท่าใหม่
        _isPatternRunning = true;
        _isSpinning = true;

        // ✅ เล่น T-pose ผ่าน Animator State แทนการปิด Animator ทั้งตัว
        // ต้องมี State "Spin_TPose" ใน Animator Controller (Motion = T-pose clip, Loop Time = true)
        // + Transition: Any State → Spin_TPose (Condition: Spin trigger, Has Exit Time ❌)
        animator.SetTrigger(HashSpin);

        // เปิด VFX
        SetSpinTrails(true);

        // ✅ เปิด Tornado VFX (Procedural — หมุนวนรอบตัว เหมือนภาพตัวอย่าง)
        _activeTornado = SpinTornadoVFX.Spawn(transform, tornadoRadius, tornadoColor);

        // เปิด NavMesh เคลื่อนที่ตามผู้เล่น
        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.speed = spinMoveSpeed;
            _agent.updateRotation = false; // ปิดหมุนตาม NavMesh ให้โค้ดหมุนเอง
        }

        float totalDegreesRotated = 0f;
        int rotationsCompleted = 0;
        HashSet<int> hitPlayersThisRotation = new HashSet<int>();

        Debug.Log("[BossMap3] 🌀 เริ่มหมุน! (T-pose)");

        while (rotationsCompleted < spinMaxRotations)
        {
            // ✅ เช็ค Cancellation ทุก frame — ถ้าโดนตีจนสตั้นกลางคัน ให้หยุดทันที
            if (_attackCancelled)
            {
                EndSpinSequence(interrupted: true);
                yield break;
            }

            // หมุนตัว
            float rotateThisFrame = spinSpeed * Time.deltaTime;
            transform.Rotate(0f, rotateThisFrame, 0f, Space.World);
            totalDegreesRotated += rotateThisFrame;

            // เคลื่อนที่ตามผู้เล่น
            if (_agent.enabled && _agent.isOnNavMesh && _player != null)
                _agent.SetDestination(_player.position);

            // เช็คว่าหมุนครบ 1 รอบ (360°)
            if (totalDegreesRotated >= (rotationsCompleted + 1) * 360f)
            {
                rotationsCompleted++;
                hitPlayersThisRotation.Clear();
                Debug.Log($"[BossMap3] 🌀 หมุนครบ {rotationsCompleted}/{spinMaxRotations} รอบ");
            }

            // เช็ค Damage ทุก frame (กัน hit ซ้ำด้วย HashSet)
            if (_player != null)
            {
                float dist = Vector3.Distance(transform.position, _player.position);
                if (dist <= spinHitRadius)
                {
                    int currentRotation = rotationsCompleted;
                    if (!hitPlayersThisRotation.Contains(currentRotation))
                    {
                        hitPlayersThisRotation.Add(currentRotation);
                        SpinHitPlayer();
                    }
                }
            }

            yield return null;
        }

        // ── หมุนครบตามปกติ ──
        Debug.Log($"[BossMap3] 🌀 หมุนครบ {spinMaxRotations} รอบ → หยุดนิ่ง {spinStunDuration} วิ!");
        EndSpinSequence(interrupted: false);

        _isStagger = true;
        animator.SetTrigger(HashStagger);

        yield return new WaitForSeconds(spinStunDuration);
        if (_attackCancelled) yield break; // เผื่อโดนตีสตั้นซ้อนระหว่างสตั้นเดิม (edge case)

        _isStagger = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        _isPatternRunning = false;
    }

    /// <summary>
    /// Cleanup ปลายทางของ Spin — เรียกทั้งตอนจบปกติและตอนถูกยกเลิกกลางคัน (interrupted)
    /// เมื่อ interrupted=true จะไม่เล่น Stagger เอง เพราะ StaggerSequence จากภายนอกจะเข้ามาคุมต่อ
    /// </summary>
    void EndSpinSequence(bool interrupted)
    {
        SetSpinTrails(false);

        // ✅ ปิด Tornado VFX เสมอ ไม่ว่าจะจบแบบไหน
        if (_activeTornado != null)
        {
            _activeTornado.StopAndDestroy();
            _activeTornado = null;
        }

        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.updateRotation = true;
        }

        _isSpinning = false;

        if (interrupted)
        {
            // ไม่ต้อง set _isStagger/_isPatternRunning เอง เพราะ CancelCurrentAttackPattern()
            // ที่เรียกจาก OnHitByPlayer() จัดการไปแล้ว และ StaggerSequence() ภายนอกจะทำงานต่อ
        }
    }

    void SpinHitPlayer()
    {
        if (_player == null) return;

        var ph = _player.GetComponent<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            Vector3 knockDir = (_player.position - transform.position).normalized;
            knockDir.y = 0.3f;

            // ✅ ใช้ Overload เดิมที่มีอยู่แล้วใน PlayerHealth.cs — ไม่ต้องแก้ PlayerHealth.cs เลย
            ph.TakeDamage(spinDamage, knockDir, spinKnockbackForce);

            Debug.Log($"[BossMap3] 🌀 Spin โดนผู้เล่น! DMG={spinDamage}, Knockback={spinKnockbackForce}");
        }

        HapticManager.Instance?.Vibrate(0.2f, 0.5f, 0.1f);
    }

    // ═══════════════════════════════════════════════════
    // ธนูไฟ Weakness — เตรียมไว้ให้ระบบธนูไฟเรียก
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// เรียกจากระบบธนูไฟเมื่อยิงโดนบอส
    /// เมื่อครบ fireArrowsRequired ครั้ง → บอส stun
    /// </summary>
    public void OnFireArrowHit()
    {
        if (_isDead || _isFireStunned) return;

        _fireArrowHitCount++;
        Debug.Log($"[BossMap3] 🔥 ธนูไฟโดน! ({_fireArrowHitCount}/{fireArrowsRequired})");

        if (_fireArrowHitCount >= fireArrowsRequired)
        {
            _fireArrowHitCount = 0;
            StartCoroutine(FireArrowStunSequence());
        }
    }

    IEnumerator FireArrowStunSequence()
    {
        _isFireStunned = true;
        _isSpinning = false;
        _isSnowballing = false;
        _isPatternRunning = false;

        // เปิด Animator กลับ (กรณีกำลังหมุนอยู่)
        animator.enabled = true;
        SetSpinTrails(false);

        animator.SetFloat(HashSpeed, 0f);
        animator.SetTrigger(HashStagger);

        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.updateRotation = true;
        }

        Debug.Log($"[BossMap3] 🔥❄️ น้ำแข็งละลาย! บอส stun {fireArrowStunDuration} วิ!");
        yield return new WaitForSeconds(fireArrowStunDuration);

        _isFireStunned = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;

        // ✅ สั่ง Animator กลับ Idle ตรงๆ ทันที — ไม่ต้องรอ Exit Time ของ Clip
        animator.SetTrigger(HashReturnIdle);

        Debug.Log("[BossMap3] 🔥 ฟื้นจาก Fire Stun!");
    }

    // ═══════════════════════════════════════════════════
    // Other Sequences
    // ═══════════════════════════════════════════════════

    IEnumerator IntroSequence() { if (_introStarted) yield break; _introStarted = true; _isIntro = true; yield return new WaitForSeconds(introDelay); _isIntro = false; }
    IEnumerator StaggerSequence()
    {
        _isStagger = true;
        animator.SetFloat(HashSpeed, 0f);
        animator.SetTrigger(HashStagger);
        yield return new WaitForSeconds(staggerDuration);
        _isStagger = false;

        // ✅ สั่ง Animator กลับ Idle ตรงๆ ทันที — ไม่ต้องรอ Exit Time ของ Clip
        animator.SetTrigger(HashReturnIdle);
    }

    IEnumerator DeathSequence()
    {
        if (bossUI != null) bossUI.HideBossUI();
        SetSpinTrails(false);
        animator.enabled = true;
        animator.Play("Dead", 0, 0f);
        animator.speed = 1f;
        yield return new WaitForSeconds(deathAnimDuration);
        if (deathVFX != null) { var vfx = Instantiate(deathVFX, transform.position, Quaternion.identity); Destroy(vfx, deathVFXDuration + 0.5f); }
        foreach (var r in _renderers) if (r != null) r.enabled = false;
        yield return new WaitForSeconds(fadeDuration);
        SceneManager.LoadScene("Map3_Outro");
    }

    // ═══════════════════════════════════════════════════
    // Public Events
    // ═══════════════════════════════════════════════════

    public void OnPlayerEnterBossRoom() { StartCoroutine(IntroSequence()); }

    public void OnHitByPlayer()
    {
        if (_isDead) return;
        _isHit = true; _hitRecoveryTimer = 0.3f;
        if (hitVFX != null) { var vfx = Instantiate(hitVFX, transform.position + Vector3.up, Quaternion.identity); Destroy(vfx, 2f); }
        StartCoroutine(HitFlash());
        _comboCount++;

        if (_comboCount >= 5 && !_isStagger) // ✅ กันไม่ให้ Stagger ซ้อน Stagger เดิม
        {
            _comboCount = 0;
            if (Time.time >= _lastStaggerTime + staggerCooldown)
            {
                _lastStaggerTime = Time.time;

                // ✅ ยกเลิกท่าที่กำลังโจมตีอยู่ทันที ก่อนเข้า Stagger
                // แก้ปัญหา: ตีครบ combo กลางท่าปาหิมะ/หมุนตัว แล้ว snowball ลูกที่เหลือยังพุ่งออกมา
                CancelCurrentAttackPattern();

                StartCoroutine(StaggerSequence());
            }
        }
    }

    /// <summary>
    /// ✅ ยกเลิกท่าโจมตีที่กำลังทำงานอยู่ทันที (SnowballSequence / SpinAttackSequence)
    /// เรียกก่อนเริ่ม Stagger เสมอ กันปัญหา snowball/spin ยังทำงานต่อทั้งที่ boss เข้า stagger แล้ว
    /// </summary>
    void CancelCurrentAttackPattern()
    {
        _attackCancelled = true; // Coroutine ที่กำลังรันจะเช็คค่านี้แล้วออกเองที่ checkpoint ถัดไป

        // เคลียร์ VFX ที่อาจค้างจาก Spin
        SetSpinTrails(false);
        if (_activeTornado != null)
        {
            _activeTornado.StopAndDestroy();
            _activeTornado = null;
        }

        // เคลียร์ Trigger ที่อาจค้างจากท่าที่กำลังเล่นอยู่
        animator.ResetTrigger(HashSlamRight);
        animator.ResetTrigger(HashSlamLeft);
        animator.ResetTrigger(HashSlamJump);
        animator.ResetTrigger(HashSpin);

        // คืนค่า Agent เผื่อค้างจาก Spin (ปิด updateRotation ไว้)
        if (_agent.enabled && _agent.isOnNavMesh)
            _agent.updateRotation = true;

        // Reset flags ทั้งหมดทันที — ไม่ต้องรอ Coroutine เดินมาถึง checkpoint
        _isSnowballing = false;
        _isSpinning = false;
        _isPatternRunning = false;

        Debug.Log("[BossMap3] 🛑 ยกเลิกท่าโจมตีที่กำลังทำงาน → เข้า Stagger ทันที");
    }

    public void OnDead()
    {
        if (_isDead) return; _isDead = true; _deathTriggered = true;
        StopAllCoroutines();
        SetSpinTrails(false);
        animator.enabled = true;
        animator.Play("Dead", 0, 0f); animator.speed = 1f; animator.SetFloat(HashSpeed, 0f);
        animator.ResetTrigger(HashSlamRight); animator.ResetTrigger(HashSlamLeft); animator.ResetTrigger(HashSlamJump);
        animator.ResetTrigger(HashSpin); animator.ResetTrigger(HashStagger);
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); _agent.updateRotation = true; }
        StartCoroutine(DeathSequence());
    }

    public void OnHitboxOpenLeft(int damage) => attackHitboxL?.EnableHitbox(damage);
    public void OnHitboxCloseLeft() => attackHitboxL?.DisableHitbox();
    public void OnHitboxOpenRight(int damage) => attackHitboxR?.EnableHitbox(damage);
    public void OnHitboxCloseRight() => attackHitboxR?.DisableHitbox();

    // ═══════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════
    // (Knockback logic ย้ายไปที่ KnockbackUtility.cs แล้ว — ใช้ร่วมกันได้ทุก Boss)

    void SetSpinTrails(bool on)
    {
        if (spinTrails == null) return;
        foreach (var trail in spinTrails)
        {
            if (trail == null) continue;
            trail.emitting = on;
            if (!on) trail.Clear();
        }
    }

    void ChasePlayer()
    {
        _chasePlayerCallCount++; // ✅ พิสูจน์ว่าฟังก์ชันนี้ถูกเรียกจริงหรือไม่

        if (_player == null) { _chaseSkipReason = "_player == null"; return; }
        if (!_agent.enabled) { _chaseSkipReason = "_agent.enabled == false"; return; }
        if (!_agent.isOnNavMesh) { _chaseSkipReason = "_agent.isOnNavMesh == false"; return; }

        if (Time.time >= _lastPathUpdateTime + pathUpdateInterval)
        {
            _setDestinationCallCount++; // ✅ พิสูจน์ว่าเรียก SetDestination จริง

            // ✅ เช็คก่อนว่าตำแหน่งผู้เล่นเองอยู่บน NavMesh ไหม (แยกจากตำแหน่งบอส)
            bool playerOnMesh = NavMesh.SamplePosition(_player.position, out var playerHit, 2f, NavMesh.AllAreas);
            _playerPositionOnNavMesh = playerOnMesh;

            bool destSet = _agent.SetDestination(_player.position);
            _lastPathUpdateTime = Time.time;
            _lastDestinationResult = destSet;   // ✅ ค่านี้คือความจริง ไม่ใช่ enum ที่โดน default หลอก
            _lastPathStatus = _agent.pathStatus;
            _chaseSkipReason = destSet ? "OK" : "SetDestination() returned FALSE";

            if (!_loggedPathIssueOnce && (!destSet || !playerOnMesh))
            {
                _loggedPathIssueOnce = true;
                Debug.LogError(
                    $"[BossMap3] ❌ ปัญหาที่แท้จริง!\n" +
                    $"SetDestination() returned: {destSet}\n" +
                    $"Player position on NavMesh (2m radius): {playerOnMesh}\n" +
                    $"BossPos={transform.position}, PlayerPos={_player.position}\n" +
                    (playerOnMesh ? "" : "→ ตำแหน่ง Player ไม่อยู่บน NavMesh! เช็คว่า Player transform pivot สูงจากพื้นเกินไปหรือไม่\n") +
                    "→ เช็คด้วย NavMeshIslandChecker: Window > AI > NavMesh Island Checker");
            }
        }
    }

    // ── ตัวแปร Diagnostic แบบพิสูจน์ได้จริง (ไม่โดน enum-default หลอก) ──
    private bool _lastDestinationResult = false;
    private UnityEngine.AI.NavMeshPathStatus _lastPathStatus = UnityEngine.AI.NavMeshPathStatus.PathInvalid;

    IEnumerator HitFlash()
    {
        if (_isFlashing) yield break; _isFlashing = true;
        foreach (var r in _renderers) if (r.material != null) r.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        for (int i = 0; i < _renderers.Length; i++) if (_renderers[i].material != null) _renderers[i].material.color = _originalColors[i];
        _isFlashing = false;
    }
}