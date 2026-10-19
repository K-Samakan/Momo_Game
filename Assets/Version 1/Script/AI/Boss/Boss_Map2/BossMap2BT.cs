using UnityEngine;
using UnityEngine.AI;
using MOMO.AI.BehaviorTree;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// บอสด่าน 2 — หุ่นยนต์พ่อมด (v4)
/// 
/// ท่าที่ 1: FistSummon — SlamRight → SlamLeft → SlamJump chain → Stagger
/// ท่าที่ 2: AoE Attack — คลื่นพลังรอบตัว + VFX วงกลมที่พื้น + Knockback
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class BossMap2BT : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public BossMap2Health health;
    public BossMap2UI bossUI;

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
    [Tooltip("โอกาสเลือกท่า FistSummon (ที่เหลือ = AoE Attack)")]
    public float fistSummonChance = 0.6f;

    // ═══════════════════════════════════════════════════
    // FistSummon — ใช้ Slam animation chain
    // ═══════════════════════════════════════════════════
    [Header("FistSummon Pattern (Slam Anim Chain)")]
    public GameObject fistProjectilePrefab;
    public Transform fistSpawnPoint;
    public float fistInterval = 0.8f;
    public int fistDamage = 1;
    public int fistDamage3 = 2;
    public float fistSpeed = 15f;
    public float fistStuckDuration = 1.5f;
    public float fistKnockbackForce = 8f;

    // ═══════════════════════════════════════════════════
    // AoE Attack — คลื่นพลังรอบตัว
    // ═══════════════════════════════════════════════════
    [Header("AoE Attack (คลื่นพลังรอบตัว)")]
    [Tooltip("Damage ของ AoE")]
    public int aoeDamage = 2;

    [Tooltip("รัศมี AoE (ผู้เล่นจะเห็นวงกลมที่พื้นขนาดนี้)")]
    public float aoeRadius = 5f;

    [Tooltip("แรง Knockback เมื่อโดน AoE")]
    public float aoeKnockbackForce = 12f;

    [Tooltip("แรง Knockback ขึ้นบน")]
    public float aoeKnockbackUpward = 3f;

    [Tooltip("ระยะเวลา wind-up ก่อน AoE ระเบิด (ผู้เล่นเห็น VFX เตือนช่วงนี้)")]
    public float aoeWindUpDelay = 1.5f;

    [Tooltip("ระยะเวลาพักหลัง AoE ก่อนกลับไปโจมตี")]
    public float aoeRecoveryDuration = 1.0f;

    [Tooltip("Knockback duration (วินาที)")]
    public float aoeKnockbackDuration = 0.3f;

    // ═══════════════════════════════════════════════════
    // Hit Flash & Death Fade
    // ═══════════════════════════════════════════════════
    [Header("Hit Flash")]
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.2f;

    [Header("Death Fade")]
    public float deathAnimDuration = 3f;
    public float fadeDuration = 1.5f;

    // ═══════════════════════════════════════════════════
    // Debug
    // ═══════════════════════════════════════════════════
    [Header("Debug (Runtime)")]
    [SerializeField] private string currentState = "None";
    [SerializeField] private bool logStateChanges = false;

    public string CurrentState => currentState;
    public float DistanceToPlayer => _player ? Vector3.Distance(transform.position, _player.position) : -1f;
    public float AgentSpeed => (_agent != null && _agent.enabled && _agent.isOnNavMesh) ? _agent.velocity.magnitude : 0f;

    void SetState(string s) { if (currentState == s) return; currentState = s; if (logStateChanges) Debug.Log($"[BossMap2] → {s}"); }

    // ── Private Flags ──
    private bool _isIntro = true, _introStarted = false, _isDead = false;
    private bool _isStagger = false, _isFistSummoning = false, _isAoeing = false;
    private bool _isHit = false, _isPatternRunning = false, _deathTriggered = false;
    private float _hitRecoveryTimer;
    private int _comboCount = 0;
    private float _lastStaggerTime = -999f;

    public NavMeshAgent _agent;
    private Transform _player;
    private float _lastPathUpdateTime;

    // ── Animator Hashes (ใช้ชื่อเดิมของ Boss1) ──
    private static readonly int HashSlamRight = Animator.StringToHash("SlamRight");
    private static readonly int HashSlamLeft = Animator.StringToHash("SlamLeft");
    private static readonly int HashSlamJump = Animator.StringToHash("SlamJump");
    private static readonly int HashSummon = Animator.StringToHash("Summon");
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
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go != null) _player = go.transform;
        StartCoroutine(InitNavMesh());
        _root = BuildTree();
        if (!_introStarted) StartCoroutine(IntroSequence());
    }

    IEnumerator InitNavMesh()
    {
        yield return null; yield return null;
        if (NavMesh.SamplePosition(transform.position, out var hit, 5f, NavMesh.AllAreas))
            transform.position = hit.position;
        else { Debug.LogWarning("[BossMap2] ไม่พบ NavMesh!"); yield break; }
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
        new BTSequence(new List<BTNode> { new BTLeaf("hit", IsHit), new BTLeaf("doHit", DoHitStun) }),
        new BTSequence(new List<BTNode> { new BTLeaf("stagger", IsStagger), new BTLeaf("doStagger", DoStagger) }),
        new BTSequence(new List<BTNode> { new BTLeaf("aoe", IsAoeing), new BTLeaf("doAoe", DoAoeState) }),
        new BTSequence(new List<BTNode> { new BTLeaf("fist", IsFistSummoning), new BTLeaf("doFist", DoFistSummon) }),
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
    BTStatus IsAoeing() => _isAoeing ? BTStatus.Success : BTStatus.Failure;
    BTStatus IsFistSummoning() => _isFistSummoning ? BTStatus.Success : BTStatus.Failure;
    BTStatus CanSeePlayer() => _player != null && Vector3.Distance(transform.position, _player.position) <= detectionRange ? BTStatus.Success : BTStatus.Failure;
    BTStatus InAttackRange() => _player != null && Vector3.Distance(transform.position, _player.position) <= attackRange ? BTStatus.Success : BTStatus.Failure;

    // ── Actions ──
    BTStatus DoDead() { if (_deathTriggered) return BTStatus.Running; _deathTriggered = true; SetState("Dead"); StopAllCoroutines(); _agent.ResetPath(); animator.SetFloat(HashSpeed, 0f); bossUI.HideBossUI(); StartCoroutine(DeathSequence()); return BTStatus.Running; }
    BTStatus DoIntroIdle() { SetState("Intro"); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }
    BTStatus DoHitStun() { SetState("Hit"); _hitRecoveryTimer -= Time.deltaTime; if (_hitRecoveryTimer <= 0f) _isHit = false; return BTStatus.Running; }
    BTStatus DoStagger() { SetState("Stagger"); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }
    BTStatus DoAoeState() { SetState("AoE"); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }
    BTStatus DoFistSummon() { SetState("FistSummon"); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }

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
        if (Random.value >= fistSummonChance) StartCoroutine(AoeAttackSequence());
        else StartCoroutine(FistSummonSequence());
        return BTStatus.Success;
    }

    BTStatus DoChase()
    {
        SetState("Chase");
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = false; _agent.speed = moveSpeed; ChasePlayer(); }
        animator.SetFloat(HashSpeed, 1f);
        return BTStatus.Running;
    }

    BTStatus DoIdle() { SetState("Idle"); _agent.ResetPath(); animator.SetFloat(HashSpeed, 0f); return BTStatus.Running; }

    // ═══════════════════════════════════════════════════
    // ท่าที่ 1: FistSummon — SlamRight chain → Stagger
    // ═══════════════════════════════════════════════════

    IEnumerator FistSummonSequence()
    {
        _isPatternRunning = true;
        _isFistSummoning = true;
        animator.SetFloat(HashSpeed, 0f);

        // Trigger SlamRight → Animator chain → SlamLeft → SlamJump → Idle
        FaceTarget();
        animator.SetTrigger(HashSlamRight);

        // หมัดที่ 1
        yield return new WaitForSeconds(0.5f);
        SpawnFistProjectile(fistDamage);

        // หมัดที่ 2
        yield return new WaitForSeconds(fistInterval + 0.5f);
        FaceTarget();
        SpawnFistProjectile(fistDamage);

        // หมัดที่ 3 (แรงกว่า)
        yield return new WaitForSeconds(fistInterval + 0.5f);
        FaceTarget();
        SpawnFistProjectile(fistDamage3);

        yield return new WaitForSeconds(1.0f);

        // สตั้นหลังหมัดครบ
        Debug.Log("[BossMap2] 💫 หมัดครบ 3 ลูก → สตั้น!");
        _isFistSummoning = false;
        _isStagger = true;
        animator.SetTrigger(HashStagger);
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }

        yield return new WaitForSeconds(staggerDuration);
        _isStagger = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        _isPatternRunning = false;
    }

    // ═══════════════════════════════════════════════════
    // ท่าที่ 2: AoE Attack — คลื่นพลังรอบตัว
    // ═══════════════════════════════════════════════════

    IEnumerator AoeAttackSequence()
    {
        _isPatternRunning = true;
        _isAoeing = true;
        animator.SetTrigger(HashSummon);
        animator.SetFloat(HashSpeed, 0f);

        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // ✅ Spawn VFX วงกลมที่พื้น — ขยายจาก 0 → aoeRadius ใน aoeWindUpDelay
        AoeGroundIndicator.Spawn(transform.position, aoeRadius, aoeWindUpDelay);

        Debug.Log($"[BossMap2] ⚡ เริ่ม AoE wind-up! ({aoeWindUpDelay}s)");

        // รอ wind-up → ผู้เล่นเห็นวงกลมเตือนและมีเวลาหนี
        yield return new WaitForSeconds(aoeWindUpDelay);

        // ✅ ระเบิด! ทำ damage + knockback
        DoAoEAttack();

        // Haptic สะเทือน
        HapticManager.Instance?.Vibrate(0.5f, 1.0f, 0.25f);

        // รอ recovery
        yield return new WaitForSeconds(aoeRecoveryDuration);

        _isAoeing = false;
        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;
        _isPatternRunning = false;
    }

    /// <summary>
    /// ✅ AoE Damage + Knockback — ใช้ CharacterController เหมือน BossAttackHitbox
    /// </summary>
    void DoAoEAttack()
    {
        if (_player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        Debug.Log($"[BossMap2] 💥 AoE ระเบิด! radius={aoeRadius}, playerDist={dist:F1}");

        if (dist > aoeRadius) return;

        // --- Damage ---
        var playerHealth = _player.GetComponent<MOMO.Player.PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(aoeDamage);
            Debug.Log($"[BossMap2] 💥 AoE โดนผู้เล่น! DMG={aoeDamage}");
        }

        // --- Knockback ผ่าน CharacterController ---
        var cc = _player.GetComponent<CharacterController>();
        if (cc != null)
        {
            StartCoroutine(DoAoeKnockback(cc, _player));
        }
    }

    /// <summary>
    /// Knockback ผ่าน CharacterController.Move() — เหมือน BossAttackHitbox
    /// </summary>
    IEnumerator DoAoeKnockback(CharacterController cc, Transform player)
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        Vector3 force = dir * aoeKnockbackForce + Vector3.up * aoeKnockbackUpward;

        float elapsed = 0f;
        while (elapsed < aoeKnockbackDuration)
        {
            if (cc == null || !cc.enabled) yield break;
            float t = 1f - (elapsed / aoeKnockbackDuration);
            cc.Move(force * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════
    // Other Sequences
    // ═══════════════════════════════════════════════════

    IEnumerator IntroSequence() { if (_introStarted) yield break; _introStarted = true; _isIntro = true; yield return new WaitForSeconds(introDelay); _isIntro = false; }
    IEnumerator StaggerSequence() { _isStagger = true; animator.SetFloat(HashSpeed, 0f); animator.SetTrigger(HashStagger); yield return new WaitForSeconds(staggerDuration); _isStagger = false; }

    IEnumerator DeathSequence()
    {
        if (bossUI != null) bossUI.HideBossUI();
        animator.Play("Dead", 0, 0f);
        animator.speed = 1f;
        yield return new WaitForSeconds(deathAnimDuration);
        if (deathVFX != null) { var vfx = Instantiate(deathVFX, transform.position, Quaternion.identity); Destroy(vfx, deathVFXDuration + 0.5f); }
        foreach (var r in _renderers) if (r != null) r.enabled = false;
        yield return new WaitForSeconds(fadeDuration);
        SceneManager.LoadScene("Map2_Outro");
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
        if (_comboCount >= 5) { _comboCount = 0; if (Time.time >= _lastStaggerTime + staggerCooldown) { _lastStaggerTime = Time.time; StartCoroutine(StaggerSequence()); } }
    }

    public void OnDead()
    {
        if (_isDead) return; _isDead = true; _deathTriggered = true;
        StopAllCoroutines();
        animator.Play("Dead", 0, 0f); animator.speed = 1f; animator.SetFloat(HashSpeed, 0f);
        animator.ResetTrigger(HashSlamRight); animator.ResetTrigger(HashSlamLeft); animator.ResetTrigger(HashSlamJump);
        animator.ResetTrigger(HashSummon); animator.ResetTrigger(HashStagger);
        if (_agent.enabled && _agent.isOnNavMesh) { _agent.isStopped = true; _agent.ResetPath(); }
        StartCoroutine(DeathSequence());
    }

    public void OnHitboxOpenLeft(int damage) => attackHitboxL?.EnableHitbox(damage);
    public void OnHitboxCloseLeft() => attackHitboxL?.DisableHitbox();
    public void OnHitboxOpenRight(int damage) => attackHitboxR?.EnableHitbox(damage);
    public void OnHitboxCloseRight() => attackHitboxR?.DisableHitbox();

    // ═══════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════

    void SpawnFistProjectile(int damage)
    {
        if (_player == null || fistProjectilePrefab == null) return;
        Vector3 spawnPos = fistSpawnPoint != null ? fistSpawnPoint.position : transform.position + transform.forward * 1.5f + Vector3.up * 1.5f;
        Vector3 dir = (_player.position + Vector3.up - spawnPos).normalized;
        var fist = Instantiate(fistProjectilePrefab, spawnPos, Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f));
        var rb = fist.GetComponent<Rigidbody>(); if (rb != null) rb.linearVelocity = dir * fistSpeed;
        var proj = fist.GetComponent<FistProjectile>(); if (proj != null) proj.Init(damage, dir, fistKnockbackForce);   // ← บรรทัด 432

        ProjectileShootEmitter.Attach(fist);   // ✅ เพิ่มตรงนี้

        Destroy(fist, 5f);   // ← บรรทัด 433 เดิม
    }

    void ChasePlayer()
    {
        if (_player == null || !_agent.enabled || !_agent.isOnNavMesh) return;
        if (Time.time >= _lastPathUpdateTime + pathUpdateInterval) { _agent.SetDestination(_player.position); _lastPathUpdateTime = Time.time; }
    }

    IEnumerator HitFlash()
    {
        if (_isFlashing) yield break; _isFlashing = true;
        foreach (var r in _renderers) if (r.material != null) r.material.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        for (int i = 0; i < _renderers.Length; i++) if (_renderers[i].material != null) _renderers[i].material.color = _originalColors[i];
        _isFlashing = false;
    }
}