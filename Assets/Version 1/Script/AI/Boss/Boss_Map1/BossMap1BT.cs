using UnityEngine;
using UnityEngine.AI;
using MOMO.AI.BehaviorTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

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

    // ═══════════════════════════════════════════════════
    // ✅ ใหม่: ดาเมจด้วยโค้ด (ไม่พึ่ง Animation Event / Hitbox)
    // ═══════════════════════════════════════════════════
    [Header("Code Damage")]
    [Tooltip("เปิด = ทำดาเมจด้วยโค้ด และปิดระบบ Hitbox เดิม (กันโดนดาเมจซ้ำ)")]
    public bool useCodeDamage = true;

    [Header("Slam (ทุบ)")]
    public int slamDamage1 = 1;
    public int slamDamage2 = 1;
    public int slamDamage3 = 2;
    [Tooltip("ระยะที่ผู้เล่นจะโดนทุบ (วัดจากตัวบอส)")]
    public float slamHitRadius = 4f;
    [Tooltip("มุมด้านหน้าบอสที่โดน (องศา) — 360 = รอบตัว")]
    [Range(30f, 360f)] public float slamHitAngle = 140f;
    [Tooltip("เวลาหลังเริ่มท่าทุบ 1-2 จนมือกระแทกพื้น (ปรับให้ตรงแอนิเมชัน)")]
    public float slamImpactDelay = 0.4f;
    [Tooltip("เวลาหลังเริ่มท่าทุบครั้งที่ 3 จนกระแทกพื้น")]
    public float slamJumpImpactDelay = 0.5f;
    public float slamKnockbackForce = 8f;

    [Header("Spin (หมุน)")]
    public int spinDamage = 1;
    public float spinHitRadius = 3f;
    public float spinKnockbackForce = 6f;

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

    // ✅ ผูกเองจาก Inspector ได้ถ้าต้องการ (ไม่บังคับ — ปกติ GetComponent จะหาเจอเองแล้ว)
    [Header("Tiger Ally (ไม่บังคับผูก ถ้า TigerAllyEffect อยู่บน GameObject เดียวกัน)")]
    public TigerAllyEffect tigerAllyEffectRef;

    void SetState(string s)
    {
        if (currentState == s) return;
        currentState = s;
        if (logStateChanges) Debug.Log($"[Boss State] → {s}");
    }

    private bool _isIntro = true;
    private bool _introStarted = false;
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

        // ✅ ลำดับการหา TigerAllyEffect:
        // 1) ผูกจาก Inspector (ถ้ามี) — ชัวร์สุด
        // 2) GetComponentInChildren เพราะ TigerAllyEffect อยู่บน GameObject "Boss" ซึ่งเป็นลูกของ "BossRoot" (ตัวที่มี BossMap1BT)
        // 3) Fallback: หาแบบรวม inactive object ทั้ง Scene
        if (tigerAllyEffectRef != null)
        {
            _tigerAllyEffect = tigerAllyEffectRef;
            Debug.Log("<color=#00FF7F>[BossMap1BT] ✅ ใช้ TigerAllyEffect ที่ผูกจาก Inspector</color>");
        }
        else if (GetComponentInChildren<TigerAllyEffect>() != null)
        {
            _tigerAllyEffect = GetComponentInChildren<TigerAllyEffect>();
            Debug.Log("<color=#00FF7F>[BossMap1BT] ✅ เจอ TigerAllyEffect บน Child GameObject (GetComponentInChildren)</color>");
        }
        else
        {
            _tigerAllyEffect = FindFirstObjectByType<TigerAllyEffect>(FindObjectsInactive.Include);

            if (_tigerAllyEffect != null)
                Debug.Log("<color=#00FF7F>[BossMap1BT] ✅ Auto-find เจอ TigerAllyEffect (รวม inactive object)</color>");
            else
                Debug.LogWarning("[BossMap1BT] ⚠️ หา TigerAllyEffect ในฉากไม่เจอเลย!");
        }

        _monsterHealth = GetComponent<MonsterHealth>();

        StartCoroutine(InitNavMesh());

        _root = BuildTree();

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

        yield return null;
    }

    void Update()
    {
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
        _agent.ResetPath();
        animator.SetFloat(HashSpeed, 0f);
        bossUI.HideBossUI();
        StartCoroutine(DeathSequence());
        return BTStatus.Running;
    }

    BTStatus DoIntroIdle()
    {
        SetState("Intro");
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoHitStun()
    {
        SetState("Hit");
        _hitRecoveryTimer -= Time.deltaTime;
        if (_hitRecoveryTimer <= 0f)
            _isHit = false;
        return BTStatus.Running;
    }

    BTStatus DoStagger()
    {
        SetState("Stagger");
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoDizzy()
    {
        SetState("Dizzy");
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoSlam()
    {
        SetState("Slam");
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    BTStatus DoSpin()
    {
        SetState("Spin");
        _agent.speed = spinMoveSpeed;
        animator.SetFloat(HashSpeed, 1f);
        ChasePlayer();

        _spinSwingTimer += Time.deltaTime;
        if (_spinSwingTimer >= _spinSwingInterval)
        {
            _spinSwingTimer = 0f;
            _spinCount++;

            // ✅ ใหม่: ดาเมจ + เสียง 1 ครั้งต่อรอบ
            DoSpinHit();

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
        SetState("Chase");
        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.speed = moveSpeed;
            ChasePlayer();
        }
        animator.SetFloat(HashSpeed, 1f);
        return BTStatus.Running;
    }

    BTStatus DoIdle()
    {
        SetState("Idle");
        _agent.ResetPath();
        animator.SetFloat(HashSpeed, 0f);
        return BTStatus.Running;
    }

    IEnumerator IntroSequence()
    {
        if (_introStarted) yield break;
        _introStarted = true;

        _isIntro = true;
        yield return new WaitForSeconds(introDelay);
        _isIntro = false;
    }

    // ✅ แก้ไข: แทรกจุดกระแทก (ดาเมจ + เสียง) ระหว่างท่า — เวลารวมของท่าเท่าเดิม
    IEnumerator SlamSequence()
    {
        _isPatternRunning = true;
        _isSlamming = true;
        animator.SetFloat(HashSpeed, 0f);

        // ── ทุบครั้งที่ 1 (SlamLeft) ──
        animator.SetTrigger(HashSlamLeft);
        yield return new WaitForSeconds(slamImpactDelay);
        DoSlamHit(slamDamage1);
        yield return new WaitForSeconds(Mathf.Max(0f, 0.8f - slamImpactDelay));

        // ── ทุบครั้งที่ 2 (SlamRight) ──
        FaceTarget();
        animator.SetTrigger(HashSlamRight);
        yield return new WaitForSeconds(slamImpactDelay);
        DoSlamHit(slamDamage2);
        yield return new WaitForSeconds(Mathf.Max(0f, 0.8f - slamImpactDelay));

        // ── ทุบครั้งที่ 3 (SlamJump) — แรงสุด แล้วมือติดพื้น ──
        FaceTarget();
        animator.SetTrigger(HashSlamJump);
        yield return new WaitForSeconds(slamJumpImpactDelay);
        DoSlamHit(slamDamage3);

        animator.speed = 0f;              // มือค้างพื้น
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

    IEnumerator DeathSequence()
    {
        Debug.Log("🎬 เริ่ม DeathSequence");

        if (bossUI != null)
            bossUI.HideBossUI();

        animator.Play("Dead", 0, 0f);
        Debug.Log("💀 เล่น Dead animation");

        yield return new WaitForSeconds(deathAnimDuration);

        if (deathVFX != null)
        {
            GameObject vfx = Instantiate(deathVFX, transform.position, Quaternion.identity, null);
            Destroy(vfx, deathVFXDuration + 0.5f);
        }

        foreach (var r in _renderers)
            if (r != null) r.enabled = false;

        Debug.Log("👋 บอสหายไป รอ VFX จบ");

        MusicManager.Instance?.PlayOutroMusic();

        yield return new WaitForSeconds(fadeDuration);

        Debug.Log("🎬 โหลด Map1_Outro");
        SceneManager.LoadScene("Map1_Outro");
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

        // 🧪 Debug: เห็นคอมโบทุกครั้งที่ตี
        Debug.Log($"<color=#87CEEB>[BossCombo] t={Time.time:F1}s | ผู้เล่นตีบอส Combo: {_comboCount}/5</color>");

        if (_comboCount >= 5)
        {
            _comboCount = 0;

            // ✅ ใช้ 'health' (BossMap1Health) แทน _monsterHealth เพราะบอสตัวนี้ไม่มี MonsterHealth
            if (_tigerAllyEffect != null && health != null)
            {
                Debug.Log($"<color=#FFD700>[BossCombo] t={Time.time:F1}s | 🐯 ครบ 5 ดาบ! เรียก TigerAlly ทันที</color>");
                _tigerAllyEffect.OnPlayerAttackBoss(health);
            }
            else
            {
                Debug.LogWarning($"[BossCombo] ⚠️ เรียกเสือไม่ได้: _tigerAllyEffect={(_tigerAllyEffect == null ? "NULL" : "OK")}, health={(health == null ? "NULL" : "OK")}");
            }

            // ✅ Stagger: แยกเช็ค cooldown ของตัวเอง ไม่เกี่ยวกับเสือแล้ว
            if (Time.time >= _lastStaggerTime + staggerCooldown)
            {
                _lastStaggerTime = Time.time;
                StartCoroutine(StaggerSequence());

                Debug.Log($"<color=#FFA500>[BossCombo] t={Time.time:F1}s | ⚡ Stagger เข้าเงื่อนไข! บอสจะหยุดนิ่ง</color>");
            }
            else
            {
                float cooldownRemain = (_lastStaggerTime + staggerCooldown) - Time.time;
                Debug.Log($"<color=#808080>[BossCombo] t={Time.time:F1}s | Stagger ยังติด Cooldown อีก {cooldownRemain:F1}s (ไม่กระทบเสือ)</color>");
            }
        }
    }

    public void OnDead()
    {
        if (_isDead) return;
        _isDead = true;
        _deathTriggered = true;

        Debug.Log("☠️ OnDead() — ยกเลิกทุก state ตายทันที");

        StopAllCoroutines();

        animator.Play("Dead", 0, 0f);
        Debug.Log("💀 เล่น Dead animation");

        animator.speed = 1f;
        animator.SetFloat(HashSpeed, 0f);
        animator.SetBool("IsSpinning", false);
        animator.ResetTrigger(HashSlamLeft);
        animator.ResetTrigger(HashSlamRight);
        animator.ResetTrigger(HashSlamJump);
        animator.ResetTrigger(HashStagger);

        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        StartCoroutine(DeathSequence());
    }

    // ✅ แก้ไข: ข้ามถ้าใช้ Code Damage (กันดาเมจซ้ำ ถ้าคลิปมี Animation Event ติดอยู่)
    public void OnHitboxOpenLeft(int damage)
    {
        if (useCodeDamage) return;
        attackHitboxL?.EnableHitbox(damage);
    }

    public void OnHitboxCloseLeft()
    {
        attackHitboxL?.DisableHitbox();
    }

    // ✅ แก้ไข: ข้ามถ้าใช้ Code Damage
    public void OnHitboxOpenRight(int damage)
    {
        if (useCodeDamage) return;
        attackHitboxR?.EnableHitbox(damage);
    }

    public void OnHitboxCloseRight()
    {
        attackHitboxR?.DisableHitbox();
    }

    // ═══════════════════════════════════════════════════
    // ✅ ใหม่: Code Damage + SFX
    // ═══════════════════════════════════════════════════

    void DoSlamHit(int damage)
    {
        // เสียงเล่นทุกครั้งที่กระแทก ไม่ว่าจะโดนผู้เล่นหรือไม่
        if (health != null && health.bossAudio != null) health.bossAudio.PlaySlam();

        if (!useCodeDamage || _player == null) return;

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        bool inRange = dist <= slamHitRadius;
        bool inFront = slamHitAngle >= 360f || dist < 0.01f ||
                       Vector3.Angle(transform.forward, toPlayer) <= slamHitAngle * 0.5f;

        Debug.Log($"[BossMap1] 👊 ทุบ! dist={dist:F1}/{slamHitRadius}, inFront={inFront}");

        if (!inRange || !inFront) return;

        DamagePlayer(damage, toPlayer, slamKnockbackForce);
        HapticManager.Instance?.Vibrate(0.4f, 0.8f, 0.2f);
    }

    void DoSpinHit()
    {
        if (health != null && health.bossAudio != null) health.bossAudio.PlaySpin();

        if (!useCodeDamage || _player == null) return;

        Vector3 toPlayer = _player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;

        if (dist > spinHitRadius) return;

        Debug.Log($"[BossMap1] 🌀 หมุนโดนผู้เล่น! รอบที่ {_spinCount}");
        DamagePlayer(spinDamage, toPlayer, spinKnockbackForce);
        HapticManager.Instance?.Vibrate(0.2f, 0.5f, 0.1f);
    }

    void DamagePlayer(int damage, Vector3 toPlayer, float knockbackForce)
    {
        var ph = _player.GetComponent<MOMO.Player.PlayerHealth>();
        if (ph == null)
        {
            Debug.LogWarning("[BossMap1] ⚠️ ไม่พบ PlayerHealth บนผู้เล่น!");
            return;
        }

        Vector3 knockDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
        knockDir.y = 0.2f;

        // ใช้ Overload ที่มีอยู่แล้วใน PlayerHealth (ดาเมจ + knockback)
        ph.TakeDamage(damage, knockDir, knockbackForce);
        Debug.Log($"[BossMap1] 💥 ผู้เล่นโดน {damage} DMG");
    }

    void ChasePlayer()
    {
        if (_player == null) return;
        if (!_agent.enabled || !_agent.isOnNavMesh) return;

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