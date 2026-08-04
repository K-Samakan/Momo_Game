using UnityEngine;
using UnityEngine.AI;
using MOMO.AI.BehaviorTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// บอสด่าน 4 — หุ่นยนต์หมึกยักษ์ (Giant Octopus Robot)
/// โครงสร้างอิงจาก BossMap3BT (โครงล่าสุด) แต่ตัดส่วนที่ไม่เกี่ยวข้องออก
/// เพื่อลดจุดที่อาจเกิด error เวลาต่อ Animator / Inspector
///
/// ท่าที่ 1: TentacleLunge — พุ่งหนวดใส่ผู้เล่น 1 ครั้ง ดาเมจ 3
/// ท่าที่ 2: WaveJump — กระโดดเป็นจังหวะ 5 ครั้ง แต่ละครั้งสร้างคลื่นน้ำรอบตัว ดาเมจ 3/ครั้ง
///
/// จุดอ่อน (เบ็ดยืด) อยู่ใน BossMap4Health.cs — เรียก health.OnHookHit() จากระบบไอเทม
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BossMap4BT : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public BossMap4Health health;
    public BossMap4UI bossUI;

    [Header("Hit VFX")]
    public GameObject hitVFX;

    [Header("Death VFX")]
    public GameObject deathVFX;
    public float deathVFXDuration = 2f;

    [Tooltip("Scene ที่จะโหลดหลังบอสตาย — ปรับให้ตรงกับชื่อจริงในโปรเจกต์")]
    public string outroSceneName = "Map4_Outro";

    [Header("Settings")]
    public float introDelay = 3f;
    public float staggerDuration = 5f;
    public float staggerCooldown = 12f;
    public float attackRange = 6f;
    public float detectionRange = 20f;
    public float moveSpeed = 4f;
    public float pathUpdateInterval = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("โอกาสเลือกท่า TentacleLunge (ที่เหลือ = WaveJump)")]
    public float tentacleLungeChance = 0.5f;

    [Tooltip("เวลาพักระหว่างท่าโจมตี — กันบอสปล่อยสกิลรัวติดกันไม่มีจังหวะพัก")]
    public float attackCooldown = 2f;

    // ═══════════════════════════════════════════════════
    // ท่าที่ 1: หนวดพุ่ง (Tentacle Lunge)
    // ═══════════════════════════════════════════════════
    [Header("ท่าที่ 1: หนวดพุ่ง (Tentacle Lunge)")]
    [Tooltip("ระยะที่หนวดโดนผู้เล่น")]
    public float lungeRange = 4f;
    public int lungeDamage = 3;
    public float lungeKnockbackForce = 10f;
    public float lungeRecovery = 1f;

    [Header("VFX หนวด (เส้นยืดออกแล้วติดตามผู้เล่น)")]
    [Tooltip("จุดปล่อยหนวดบนตัวบอส — ถ้าไม่ตั้งจะใช้ transform หลักของบอส + ยกสูงตาม Tentacle Origin Fallback Height แทน")]
    public Transform tentacleOrigin;
    [Tooltip("ใช้เฉพาะตอนยังไม่ได้ตั้ง Tentacle Origin — ยกจุดปล่อยขึ้นจากพื้นเท่านี้ (กันหนวดออกจากพื้นเพราะ pivot ของบอสอยู่ระดับเท้า)")]
    public float tentacleOriginFallbackHeight = 3f;
    [Tooltip("เวลาที่หนวดยืดจากบอสไปถึงผู้เล่นครั้งแรก — ยิ่งนานยิ่งให้เวลาผู้เล่นวิ่งหนีทัน")]
    public float tentacleGrowDuration = 1.2f;
    [Tooltip("เวลารวมทั้งหมดที่หนวดคงอยู่ ก่อนหายไป (คล้าย lifetime ของก้อนหิมะ Map3) ต้อง ≥ Grow Duration")]
    public float tentacleTrackDuration = 2f;
    public float tentacleWidth = 0.3f;
    [Tooltip("สีหนวด — ดีฟอลต์ม่วงเข้มโทนหมึก")]
    public Color tentacleColor = new Color(0.35f, 0.15f, 0.5f, 0.9f);

    // ═══════════════════════════════════════════════════
    // ท่าที่ 2: คลื่นน้ำ (Water Wave Jump)
    // ═══════════════════════════════════════════════════
    [Header("ท่าที่ 2: คลื่นน้ำ (Water Wave Jump)")]
    [Tooltip("จำนวนครั้งที่กระโดด/สร้างคลื่น")]
    public int waveJumpCount = 5;
    [Tooltip("ช่วงเวลาระหว่างการกระโดดแต่ละครั้ง (วินาที)")]
    public float waveJumpInterval = 1f;
    [Tooltip("รัศมีคลื่นน้ำที่สร้างความเสียหาย")]
    public float waveRadius = 6f;
    public int waveDamage = 3;
    public float waveKnockbackForce = 8f;
    [Tooltip("เวลาหยุดนิ่งหลังกระโดดครบตามจำนวน")]
    public float waveStuckDuration = 3f;
    [Tooltip("ความสูงที่บอสจะลอยขึ้นตอนกระโดด (แก้ปัญหา animation clip ไม่มี root motion แนวตั้ง)")]
    public float jumpHopHeight = 1.5f;

    [Header("คลื่นน้ำ — VFX ที่พื้น")]
    [Tooltip("ระยะเวลาขยายวงคลื่น (แยกจากจังหวะกระโดด ปรับให้เห็นสีชัดขึ้นได้)")]
    public float waveRingExpandDuration = 0.5f;
    [Tooltip("สีเริ่มต้นของวงคลื่น (ตอนเพิ่งเกิด)")]
    public Color waveColorStart = new Color(0.4f, 0.85f, 1f, 0.45f);   // ฟ้าอ่อนใส
    [Tooltip("สีตอนวงขยายเต็มที่ ก่อนกระแทก")]
    public Color waveColorEnd = new Color(0.1f, 0.35f, 0.9f, 0.6f);    // น้ำเงินเข้ม
    [Tooltip("สีตอนกระแทก (แฟลชสั้นๆ)")]
    public Color waveColorImpact = new Color(0.85f, 0.97f, 1f, 0.9f); // ฟองขาวอมฟ้า (แทนขาวล้วน)

    [Header("Hit Flash")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.2f;

    [Header("Death Fade")]
    public float deathAnimDuration = 3f;
    public float fadeDuration = 1.5f;

    [Header("🔍 Debug (Runtime)")]
    [SerializeField] private string currentState = "None";
    [SerializeField] private bool logStateChanges = false;

    public string CurrentState => currentState;
    public float DistanceToPlayer => _player ? Vector3.Distance(transform.position, _player.position) : -1f;
    public float AgentSpeed => (_agent != null && _agent.enabled && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;

    void SetState(string s)
    {
        if (currentState == s) return;
        currentState = s;
        if (logStateChanges) Debug.Log($"[BossMap4] → {s}");
    }

    // ── Private Flags ──
    private bool _isIntro = true;
    private bool _introStarted = false;
    private bool _isDead = false;
    private bool _isStagger = false;
    private bool _isLunging = false;
    private bool _isWaveJumping = false;
    private bool _isPatternRunning = false;
    private bool _deathTriggered = false;

    // ✅ ยกเลิกท่าโจมตีปัจจุบันเมื่อโดนผู้เล่นตีกลางท่า (กันค้าง)
    private bool _attackCancelled = false;

    private float _lastStaggerTime = -999f;
    private float _lastAttackEndTime = -999f;

    public NavMeshAgent _agent;
    private Transform _player;
    private float _lastPathUpdateTime;

    // ── Animator Hashes ──
    private static readonly int HashLunge = Animator.StringToHash("TentacleLunge");
    private static readonly int HashWaveJump = Animator.StringToHash("WaveJump");
    private static readonly int HashReturnIdle = Animator.StringToHash("ReturnToIdle");
    private static readonly int HashStagger = Animator.StringToHash("Stagger");
    private static readonly int HashDead = Animator.StringToHash("Dead");
    private static readonly int HashSpeed = Animator.StringToHash("Speed");

    private BTNode _root;
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private bool _isFlashing = false;
    private TentacleWhipVFX _activeTentacleWhip;

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
        // ✅ ใช้ PlayerReference ก่อน (กันเจอ Player ผีค้างจาก DontDestroyOnLoad)
        // ถ้าไม่มี PlayerReference ในโปรเจกต์ ให้ fallback ไปหา tag "Player" แทน
        var prTransform = PlayerReference.GetPlayerTransform();
        if (prTransform != null)
        {
            _player = prTransform;
        }
        else
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }

        if (_player == null)
            Debug.LogError("[BossMap4] ❌ ไม่พบ Player! เช็คว่าติด PlayerReference.cs หรือ tag \"Player\" บน Player Prefab");

        StartCoroutine(InitNavMesh());
        _root = BuildTree();

        if (!_introStarted)
            StartCoroutine(IntroSequence());
    }

    IEnumerator InitNavMesh()
    {
        yield return null;
        yield return null;

        if (NavMesh.SamplePosition(transform.position, out var hit, 20f, NavMesh.AllAreas))
            transform.position = hit.position;
        else
            Debug.LogWarning("[BossMap4] ⚠️ ไม่พบ NavMesh ในรัศมี 20m จากตำแหน่งเริ่มต้น!");

        _agent.enabled = true;
        _agent.speed = moveSpeed;
        _agent.stoppingDistance = attackRange * 0.8f;
        _agent.acceleration = 12f;
        _agent.angularSpeed = 360f;

        yield return null;
    }

    void Update() => _root.Tick();

    // ═══════════════════════════════════════════════════
    // Behavior Tree
    // ═══════════════════════════════════════════════════

    BTNode BuildTree() => new BTSelector(new List<BTNode>
    {
        new BTSequence(new List<BTNode> { new BTLeaf("dead", IsDead), new BTLeaf("doDead", DoDead) }),
        new BTSequence(new List<BTNode> { new BTLeaf("intro", IsIntro), new BTLeaf("doIntro", DoIntroIdle) }),
        new BTSequence(new List<BTNode> { new BTLeaf("stagger", IsStagger), new BTLeaf("doStagger", DoStagger) }),
        new BTSequence(new List<BTNode> { new BTLeaf("lunge", IsLunging), new BTLeaf("doLunge", DoLungeState) }),
        new BTSequence(new List<BTNode> { new BTLeaf("wave", IsWaveJumping), new BTLeaf("doWave", DoWaveState) }),
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
    BTStatus IsStagger() => _isStagger ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsLunging() => _isLunging ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsWaveJumping() => _isWaveJumping ? BTStatus.Success : BTStatus.Failure;

    BTStatus CanSeePlayer() =>
        _player != null && Vector3.Distance(transform.position, _player.position) <= detectionRange
            ? BTStatus.Success : BTStatus.Failure;

    BTStatus InAttackRange() =>
        _player != null && Vector3.Distance(transform.position, _player.position) <= attackRange
            ? BTStatus.Success : BTStatus.Failure;

    // ── Actions ──
    BTStatus FaceTarget()
    {
        if (_player == null) return BTStatus.Failure;
        Vector3 dir = _player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);
        return BTStatus.Success;
    }

    BTStatus ChoosePattern()
    {
        if (_isPatternRunning) return BTStatus.Running;

        // ✅ กันปล่อยสกิลรัว — ต้องรอ attackCooldown หลังจบท่าก่อนหน้าก่อน ถึงจะเลือกท่าใหม่ได้
        //    ระหว่างรอ ฟังก์ชันนี้จะ Failure → BT จะ fallback ไปเดินเข้าหาผู้เล่นต่อ (DoChase) แทน
        if (Time.time < _lastAttackEndTime + attackCooldown)
            return BTStatus.Failure;

        float roll = Random.value;
        if (roll <= tentacleLungeChance)
            StartCoroutine(TentacleLungeSequence());
        else
            StartCoroutine(WaveJumpSequence());

        return BTStatus.Running;
    }

    BTStatus DoChase()
    {
        SetState("Chase");
        ChasePlayer();
        animator.SetFloat(HashSpeed, AgentSpeed > 0.1f ? 1f : 0f);
        return BTStatus.Running;
    }

    BTStatus DoIdle()
    {
        SetState("Idle");
        if (_agent.enabled && _agent.isOnNavMesh) _agent.ResetPath();
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoIntroIdle()
    {
        SetState("Intro");
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoStagger()
    {
        SetState("Stagger");
        return BTStatus.Running;
    }

    BTStatus DoDead() => BTStatus.Running;

    BTStatus DoLungeState() { SetState("TentacleLunge"); return BTStatus.Running; }
    BTStatus DoWaveState() { SetState("WaveJump"); return BTStatus.Running; }

    // ═══════════════════════════════════════════════════
    // ท่าที่ 1: TentacleLunge — พุ่งหนวดใส่ผู้เล่น (ATK = 3)
    // ═══════════════════════════════════════════════════

    IEnumerator TentacleLungeSequence()
    {
        _attackCancelled = false;
        _isPatternRunning = true;
        _isLunging = true;
        animator.SetFloat(HashSpeed, 0f);
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }

        FaceTarget();
        animator.SetTrigger(HashLunge);
        Debug.Log("[BossMap4] 🐙 เริ่มพุ่งหนวด (เส้นติดตามผู้เล่น)!");

        // ✅ ระยะเดียวกันนี้ใช้ทั้งจำกัดว่าหนวดยืดได้ไกลแค่ไหน (ภาพ) และเช็คว่าดาเมจเข้าไหม (กฎ)
        //    ผู้เล่นที่วิ่งหนีไกลกว่านี้ทันจะเห็นหนวดยืดสุดแล้วหยุดกลางอากาศ ไม่โดนดาเมจจริง
        float effectiveRange = Mathf.Max(lungeRange, attackRange);

        // ✅ สร้างหนวดแบบเส้นที่ยืดออกจากบอสแล้ววิ่งตามผู้เล่นแบบ real-time
        //    คอนเซ็ปต์เดียวกับ SpawnSnowball() ของ Map3 แต่แทนที่ก้อนหิมะด้วยเส้นหนวดที่ track เป้าหมาย
        //    ถ้ายังไม่ได้ตั้ง Tentacle Origin จะยกจุดปล่อยขึ้นจากพื้นอัตโนมัติ กันหนวดออกมาจากพื้น
        Transform origin = tentacleOrigin != null ? tentacleOrigin : transform;
        Vector3 originOffset = tentacleOrigin != null ? Vector3.zero : Vector3.up * tentacleOriginFallbackHeight;
        _activeTentacleWhip = TentacleWhipVFX.Spawn(
            origin, originOffset, _player, tentacleGrowDuration, tentacleTrackDuration, effectiveRange, tentacleColor, tentacleWidth);

        // รอให้หนวดยืดออกไปถึงผู้เล่นก่อน (เหมือนก้อนหิมะที่ต้องบินถึงเป้าหมายก่อนถึงจะกระทบ)
        yield return new WaitForSeconds(tentacleGrowDuration);
        if (_attackCancelled) { EndLungeSequence(); yield break; }

        DoLungeHit();

        // ปล่อยให้หนวดยังติดตามผู้เล่นต่อจนครบเวลาที่เหลือ ก่อนหายไปเอง (เหมือน lifetime ก้อนหิมะ)
        float remaining = Mathf.Max(0f, tentacleTrackDuration - tentacleGrowDuration);
        if (remaining > 0f)
        {
            yield return new WaitForSeconds(remaining);
            if (_attackCancelled) { EndLungeSequence(); yield break; }
        }

        yield return new WaitForSeconds(lungeRecovery);
        if (_attackCancelled) { EndLungeSequence(); yield break; }

        EndLungeSequence();
    }

    void DoLungeHit()
    {
        if (_player == null) return;

        // ✅ หนวดเป็นเส้นที่ยืดไป "หาตำแหน่งผู้เล่นโดยตรง" เสมอ ไม่ใช่อาวุธระยะประชิดที่มีระยะตายตัวแคบๆ
        //    ใช้ค่าที่มากกว่าระหว่าง Lunge Range กับ Attack Range เป็นเกณฑ์ — กันพลาดเวลาสองค่านี้ตั้งไม่ตรงกัน
        //    (เดิมถ้า Lunge Range < Attack Range บอสจะเลือกโจมตีได้ แต่เช็คดาเมจไม่ผ่านเพราะระยะแคบกว่า)
        float effectiveRange = Mathf.Max(lungeRange, attackRange);
        float dist = Vector3.Distance(transform.position, _player.position);
        Debug.Log($"[BossMap4] 🐙 หนวดพุ่ง! effectiveRange={effectiveRange:F1}, playerDist={dist:F1}");
        if (dist > effectiveRange) return;

        var ph = _player.GetComponent<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            Vector3 knockDir = (_player.position - transform.position).normalized;
            knockDir.y = 0.2f;
            ph.TakeDamage(lungeDamage, knockDir, lungeKnockbackForce);
            Debug.Log($"[BossMap4] 🐙 หนวดโดนผู้เล่น! DMG={lungeDamage}");
        }

        if (hitVFX != null)
            Destroy(Instantiate(hitVFX, _player.position, Quaternion.identity), 2f);

        HapticManager.Instance?.Vibrate(0.3f, 0.7f, 0.15f);
    }

    void EndLungeSequence()
    {
        _isLunging = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        _lastAttackEndTime = Time.time; // ✅ เริ่มนับ cooldown ก่อนปล่อยท่าถัดไปได้

        // ✅ กันหนวดค้างอยู่ถ้าโดนตัดจบกลางคัน (เช่นโดนผู้เล่นตีสตั้นระหว่างหนวดยังยืดอยู่)
        if (_activeTentacleWhip != null)
        {
            _activeTentacleWhip.StopAndDestroy();
            _activeTentacleWhip = null;
        }

        _isPatternRunning = false;
        animator.SetTrigger(HashReturnIdle);
    }

    // ═══════════════════════════════════════════════════
    // ท่าที่ 2: WaveJump — กระโดด 5 ครั้ง สร้างคลื่นน้ำรอบตัว (ATK = 3/ครั้ง)
    // ═══════════════════════════════════════════════════

    IEnumerator WaveJumpSequence()
    {
        _attackCancelled = false;
        _isPatternRunning = true;
        _isWaveJumping = true;
        animator.SetFloat(HashSpeed, 0f);
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }

        for (int i = 0; i < waveJumpCount; i++)
        {
            if (_attackCancelled) { EndWaveJumpSequence(); yield break; }

            FaceTarget();
            animator.SetTrigger(HashWaveJump);
            Debug.Log($"[BossMap4] 🌊 กระโดดคลื่นน้ำ ครั้งที่ {i + 1}/{waveJumpCount}");

            // ✅ รอให้กระโดดจบเต็มรอบ (ขึ้น + ลง) ก่อน — ตอนนี้ baseOffset จะกลับเป็น 0 พอดี
            //    แปลว่าบอส "ถึงพื้นจริง" ตรงนี้ ไม่ใช่ยังลอยอยู่กลางอากาศ
            yield return StartCoroutine(JumpHop(waveJumpInterval * 0.8f, jumpHopHeight));
            if (_attackCancelled) { EndWaveJumpSequence(); yield break; }

            // ✅ ทำดาเมจ + สร้างคลื่นตอนบอสถึงพื้นแล้วเท่านั้น
            DoWaveHit();
            SpawnWaveRing();

            HapticManager.Instance?.Vibrate(0.3f, 0.6f, 0.15f);

            // พักสั้นๆ ก่อนกระโดดครั้งถัดไป
            yield return new WaitForSeconds(waveJumpInterval * 0.2f);
        }

        Debug.Log($"[BossMap4] 🌊 คลื่นน้ำครบ {waveJumpCount} ครั้ง → หยุดนิ่ง {waveStuckDuration} วิ");
        if (_attackCancelled) { EndWaveJumpSequence(); yield break; }
        yield return new WaitForSeconds(waveStuckDuration);

        EndWaveJumpSequence();
    }

    /// <summary>
    /// ยกบอสลอยขึ้น-ลงเป็นเส้นโค้ง ผ่าน NavMeshAgent.baseOffset
    /// ใช้แทน Root Motion เพราะ animation clip ที่ผูกไว้ (Standing 2H Magic Area Attack)
    /// ไม่มี motion แนวตั้งบันทึกอยู่ในคลิปเลย — baseOffset ปลอดภัยกว่าขยับ transform.position.y ตรงๆ
    /// เพราะไม่รบกวนการเดินบน NavMesh
    /// </summary>
    IEnumerator JumpHop(float duration, float height)
    {
        if (_agent == null || height <= 0f) yield break;

        float half = Mathf.Max(0.05f, duration * 0.5f);
        float t;

        // ขาขึ้น
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            _agent.baseOffset = Mathf.Sin(p * Mathf.PI * 0.5f) * height;
            yield return null;
        }

        // ขาลง
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / half);
            _agent.baseOffset = Mathf.Cos(p * Mathf.PI * 0.5f) * height;
            yield return null;
        }

        _agent.baseOffset = 0f;
    }

    /// <summary>
    /// สร้างวงคลื่นน้ำที่พื้น — ใช้ AoeGroundIndicator ตัวเดิมจาก BossMap2
    /// แต่ override สีเป็นธีมน้ำ (ฟ้า→น้ำเงิน→ฟองขาว) แทนสีดีฟอลต์ (เหลือง→แดง→ขาว)
    /// เรียกหลังจากบอส "ถึงพื้นแล้ว" เท่านั้น (ดู WaveJumpSequence)
    /// </summary>
    void SpawnWaveRing()
    {
        var indicator = AoeGroundIndicator.Spawn(transform.position, waveRadius, waveRingExpandDuration);
        if (indicator == null) return;

        indicator.warningColor = waveColorStart;
        indicator.dangerColor = waveColorEnd;
        indicator.impactColor = waveColorImpact;
    }

    void DoWaveHit()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        Debug.Log($"[BossMap4] 🌊 คลื่นน้ำ! radius={waveRadius}, playerDist={dist:F1}");
        if (dist > waveRadius) return;

        var ph = _player.GetComponent<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            Vector3 knockDir = (_player.position - transform.position).normalized;
            knockDir.y = 0.15f;
            ph.TakeDamage(waveDamage, knockDir, waveKnockbackForce);
            Debug.Log($"[BossMap4] 🌊 คลื่นน้ำโดนผู้เล่น! DMG={waveDamage}");
        }
    }

    void EndWaveJumpSequence()
    {
        _isWaveJumping = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        if (_agent != null) _agent.baseOffset = 0f; // ✅ กันค้างลอยกลางอากาศถ้าโดนตัดจบกลางกระโดด
        _lastAttackEndTime = Time.time; // ✅ เริ่มนับ cooldown ก่อนปล่อยท่าถัดไปได้
        _isPatternRunning = false;
        animator.SetTrigger(HashReturnIdle);
    }

    // ═══════════════════════════════════════════════════
    // Other Sequences
    // ═══════════════════════════════════════════════════

    IEnumerator IntroSequence()
    {
        if (_introStarted) yield break;
        _introStarted = true;
        _isIntro = true;
        yield return new WaitForSeconds(introDelay);
        _isIntro = false;
    }

    IEnumerator StaggerSequence()
    {
        _isStagger = true;
        animator.SetFloat(HashSpeed, 0f);
        animator.SetTrigger(HashStagger);
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }

        yield return new WaitForSeconds(staggerDuration);

        _isStagger = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        animator.SetTrigger(HashReturnIdle);
    }

    IEnumerator DeathSequence()
    {
        if (bossUI != null) bossUI.HideBossUI();
        animator.enabled = true;
        animator.Play("Dead", 0, 0f);
        animator.speed = 1f;

        yield return new WaitForSeconds(deathAnimDuration);

        if (deathVFX != null)
        {
            var vfx = Instantiate(deathVFX, transform.position, Quaternion.identity);
            Destroy(vfx, deathVFXDuration + 0.5f);
        }

        foreach (var r in _renderers) if (r != null) r.enabled = false;

        yield return new WaitForSeconds(fadeDuration);

        if (!string.IsNullOrEmpty(outroSceneName))
            SceneManager.LoadScene(outroSceneName);
    }

    IEnumerator HitFlash()
    {
        if (_isFlashing) yield break;
        _isFlashing = true;
        foreach (var r in _renderers) if (r != null && r.material != null) r.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        for (int i = 0; i < _renderers.Length; i++)
            if (_renderers[i] != null && _renderers[i].material != null) _renderers[i].material.color = _originalColors[i];
        _isFlashing = false;
    }

    void ChasePlayer()
    {
        if (_player == null || !_agent.enabled || !_agent.isOnNavMesh) return;
        if (Time.time >= _lastPathUpdateTime + pathUpdateInterval)
        {
            _agent.SetDestination(_player.position);
            _lastPathUpdateTime = Time.time;
        }
    }

    // ═══════════════════════════════════════════════════
    // Public Events
    // ═══════════════════════════════════════════════════

    public void OnPlayerEnterBossRoom() => StartCoroutine(IntroSequence());

    /// <summary>เรียกจาก BossMap4Health.TakeDamage() ทุกครั้งที่บอสโดนตี</summary>
    public void OnHitByPlayer()
    {
        if (_isDead) return;

        StartCoroutine(HitFlash());

        // ✅ ยกเลิกท่าโจมตีที่กำลังทำอยู่ (ถ้ามี) กันโดนตีกลางท่าแล้วค้าง
        if (_isPatternRunning) _attackCancelled = true;

        if (!_isStagger && Time.time >= _lastStaggerTime + staggerCooldown)
        {
            _lastStaggerTime = Time.time;
            StartCoroutine(StaggerSequence());
        }
    }

    // ═══════════════════════════════════════════════════
    // 🧪 Debug / Tester Hooks — เรียกจาก BossMap4Tester.cs เท่านั้น
    // ═══════════════════════════════════════════════════

    public void DebugForceLunge()
    {
        if (_isDead || _isPatternRunning) return;
        StartCoroutine(TentacleLungeSequence());
    }

    public void DebugForceWaveJump()
    {
        if (_isDead || _isPatternRunning) return;
        StartCoroutine(WaveJumpSequence());
    }

    public void DebugForceStagger()
    {
        if (_isDead || _isStagger) return;
        StartCoroutine(StaggerSequence());
    }

    /// <summary>เรียกจาก BossMap4Health.TakeDamage() เมื่อ HP = 0</summary>
    public void OnDead()
    {
        if (_isDead) return;
        _isDead = true;
        _deathTriggered = true;

        StopAllCoroutines();
        animator.ResetTrigger(HashLunge);
        animator.ResetTrigger(HashWaveJump);
        animator.ResetTrigger(HashStagger);
        animator.SetFloat(HashSpeed, 0f);

        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }
        _agent.baseOffset = 0f; // ✅ กันตายค้างลอยกลางอากาศถ้าโดน Force Death ระหว่างกระโดด

        // ✅ กันหนวดค้างอยู่ในซีนถ้าตายกลางคันขณะกำลังพุ่งหนวด
        if (_activeTentacleWhip != null)
        {
            _activeTentacleWhip.StopAndDestroy();
            _activeTentacleWhip = null;
        }

        StartCoroutine(DeathSequence());
    }
}