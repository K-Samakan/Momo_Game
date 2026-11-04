// PATH: Assets/Version 1/Script/AI/Monster/MonsterBT.cs

using UnityEngine;
using UnityEngine.AI;
using MOMO.AI.BehaviorTree;
using System.Collections;
using System.Collections.Generic;

namespace MOMO.AI.Monster
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterBT : MonoBehaviour
    {
        // ─── Inspector ──────────────────────────────────────────
        [Header("References")]
        public MonsterStats stats;
        public MonsterAnimator monsterAnimator;

        [Header("Detection")]
        [Tooltip("Layer ที่ขวาง Line of Sight เช่น Default, Ground, Wall")]
        public LayerMask obstacleMask;

        [Header("Chase Settings")]
        [Tooltip("อัปเดต path ทุกกี่วินาที")]
        public float pathUpdateInterval = 0.3f;

        [Header("Hit Flash")]
        [Tooltip("สีที่ flash เมื่อโดนโจมตี")]
        public Color hitFlashColor = Color.red;
        [Tooltip("ระยะเวลา flash (วินาที)")]
        public float hitFlashDuration = 0.2f;

        [Header("Debug (ลบออกก่อน build จริง)")]
        [Tooltip("กด T = ลด HP 1 | กด Y = ตายทันที")]
        public bool enableDebugKeys = true;

        [Header("Death Settings")]
        [Tooltip("เวลาก่อนหายหลังตาย (วินาที) — ควรยาวกว่าเสียง Hit เพื่อไม่ให้เสียงถูกตัด")]
        public float deathFadeDelay = 1f;

        [Header("Hit Effect")]
        [Tooltip("Particle prefab เมื่อโดนโจมตี")]
        public GameObject hitEffectPrefab;

        [Header("Visual Sound Effect")]
        [Tooltip("SoundEmitter สำหรับเสียง Monster — Add SoundEmitter Component แล้วลากมาใส่")]
        public SoundEmitter sfxEmitter;

        [Header("Audio")]
        [Tooltip("MonsterAudio บน prefab นี้ (ถ้าเว้นว่างจะหาอัตโนมัติใน Awake)")]
        [SerializeField] MonsterAudio monsterAudio;

        [Tooltip("true = เล่นเสียงโจมตีตอนเริ่มง้าง | false = เล่นตอนจังหวะฟาด (attackDamagePoint)")]
        [SerializeField] bool playAttackSfxOnWindup = false;

        private float _lastFootstepEmit = -999f;

        [SerializeField]
        [Tooltip("ส่งเสียงฝีเท้าทุกกี่วินาที ขณะ Chase")]
        private float footstepEmitInterval = 0.5f;

        // ─── Properties ─────────────────────────────────────────
        public float CurrentHealth { get; private set; }

        // ─── Private ────────────────────────────────────────────
        NavMeshAgent _agent;
        Transform _player;
        Vector3 _spawnPos;
        BTNode _root; //BT

        bool _isDead;
        bool _isHit;
        float _hitRecoveryTimer;
        float _lastAttackTime;
        float _lastPathUpdateTime;
        float _lastSeenPlayerTime = -999f;
        float _chaseMemoryDuration = 3f;
        int _patrolIndex;
        float _patrolWaitTimer;
        bool _isOnNavMesh = false;

        // Hit flash
        Renderer[] _renderers;
        Color[] _originalColors;
        bool _isFlashing = false;

        // ════════════════════════════════════════════════════════
        // Unity Lifecycle
        // ════════════════════════════════════════════════════════

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.enabled = false;

            var rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // ✅ SFX — หา MonsterAudio อัตโนมัติถ้ายังไม่ได้ลากใส่
            if (monsterAudio == null) monsterAudio = GetComponent<MonsterAudio>();
            if (monsterAudio == null) monsterAudio = GetComponentInChildren<MonsterAudio>();

            _renderers = GetComponentsInChildren<Renderer>();
            _originalColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material != null)
                    _originalColors[i] = _renderers[i].material.color;
            }
        }

        void Start()
        {
            CurrentHealth = stats.maxHealth;

            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
            else Debug.LogWarning($"[{stats.monsterName}] Player not found! ตรวจสอบ Tag 'Player'");

            // ✅ SFX — เตือนถ้าลืมใส่ MonsterAudio
            if (monsterAudio == null)
                Debug.LogWarning($"[{stats.monsterName}] ไม่พบ MonsterAudio — จะไม่มีเสียงโจมตี/โดนตี");

            StartCoroutine(InitOnNavMesh());
        }

        void Update()
        {
            if (_isDead || _root == null || !_isOnNavMesh) return;
            _root.Tick();

            if (enableDebugKeys)
            {
                if (Input.GetKeyDown(KeyCode.T))
                    TakeDamage(1f);

                if (Input.GetKeyDown(KeyCode.Y))
                    TakeDamage(CurrentHealth);
            }
        }

        // ════════════════════════════════════════════════════════
        // ✅ FIXED — retry + รัศมีค้นหากว้างขึ้น (เดิม: ลองครั้งเดียว 3m แล้วเลิกถาวร)
        // ════════════════════════════════════════════════════════
        IEnumerator InitOnNavMesh()
        {
            const int maxAttempts = 10;
            const float retryDelay = 0.5f;
            const float searchRadius = 5f;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                yield return (attempt == 1) ? null : new WaitForSeconds(retryDelay);
                if (attempt == 1) yield return null;

                if (NavMesh.SamplePosition(transform.position, out var hit, searchRadius, NavMesh.AllAreas))
                {
                    transform.position = hit.position;
                    _spawnPos = hit.position;

                    _agent.enabled = true;
                    _agent.speed = stats.walkSpeed;
                    _agent.stoppingDistance = stats.attackRange * 0.8f;
                    _agent.acceleration = 12f;
                    _agent.angularSpeed = 360f;
                    _agent.autoBraking = true;
                    _agent.isStopped = true;

                    yield return null;

                    if (_agent.isOnNavMesh)
                    {
                        _spawnPos = transform.position;
                        _isOnNavMesh = true;
                        _root = BuildTree();
                        Debug.Log($"[{stats.monsterName}] ✅ NavMesh Ready (attempt {attempt})");
                        yield break;
                    }

                    _agent.enabled = false;
                }

                if (attempt == maxAttempts)
                {
                    Debug.LogError($"[{stats.monsterName}] ❌ ไม่พบ NavMesh หลังลอง {maxAttempts} ครั้ง ที่ตำแหน่ง {transform.position} " +
                                    "— เช็คว่า Bake NavMeshSurface ครอบคลุมจุดนี้หรือยัง (Window → AI → NavMesh Island Checker)");
                }
                else if (attempt == 1)
                {
                    Debug.LogWarning($"[{stats.monsterName}] ไม่พบ NavMesh ใกล้เคียง — กำลังลองใหม่... (ตำแหน่ง {transform.position})");
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // Build Tree //BT
        // ════════════════════════════════════════════════════════

        BTNode BuildTree() => new BTSelector(new List<BTNode>
        {
            new BTSequence(new List<BTNode>
            {
                new BTLeaf("dead",IsDead),
                new BTLeaf("doDead",DoBehaviorDead)
            }),
            new BTSequence(new List<BTNode>
            {
                new BTLeaf("hit", IsHit),
                new BTLeaf("doHitStun", DoHitStun)
            }),
            new BTSequence(new List<BTNode>
            {
                new BTLeaf("canAttack", CanAttack),
                new BTLeaf("faceTarget", FaceTarget),
                new BTLeaf("doAttack", DoAttack)
            }),
            new BTSequence(new List<BTNode>
            {
                new BTLeaf("canSeePlayer", CanSeePlayer),
                new BTLeaf("doChase", DoChase)
            }),
            new BTSequence(new List<BTNode>
            {
                new BTLeaf("hasPatrol", HasPatrol),
                new BTLeaf("doPatrol", DoPatrol)
            }),
            new BTLeaf("doIdle", DoIdle)
        });

        // ════════════════════════════════════════════════════════
        // Conditions
        // ════════════════════════════════════════════════════════

        BTStatus IsDead()
            => CurrentHealth <= 0 ? BTStatus.Success : BTStatus.Failure;

        BTStatus IsHit()
            => _isHit ? BTStatus.Success : BTStatus.Failure;

        BTStatus CanAttack()
        {
            if (_player == null) return BTStatus.Failure;
            bool inRange = Vector3.Distance(transform.position, _player.position) <= stats.attackRange;
            bool cooledDown = Time.time >= _lastAttackTime + stats.attackCooldown;
            return (inRange && cooledDown) ? BTStatus.Success : BTStatus.Failure;
        }

        BTStatus CanSeePlayer()
        {
            if (_player == null) return BTStatus.Failure;

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > stats.detectionRange)
            {
                if (Time.time - _lastSeenPlayerTime < _chaseMemoryDuration)
                    return BTStatus.Success;
                return BTStatus.Failure;
            }

            if (dist <= stats.attackRange * 2f)
            {
                _lastSeenPlayerTime = Time.time;
                return BTStatus.Success;
            }

            if (obstacleMask.value == 0)
            {
                _lastSeenPlayerTime = Time.time;
                return BTStatus.Success;
            }

            Vector3 origin = transform.position + Vector3.up * 0.8f;
            Vector3 target = _player.position + Vector3.up * 0.8f;
            Vector3 dir = (target - origin).normalized;
            float range = Vector3.Distance(origin, target);

            if (Physics.Raycast(origin, dir, out var hit, range, obstacleMask))
            {
                if (hit.transform.CompareTag("Player"))
                {
                    _lastSeenPlayerTime = Time.time;
                    return BTStatus.Success;
                }
                if (Time.time - _lastSeenPlayerTime < _chaseMemoryDuration)
                    return BTStatus.Success;
                return BTStatus.Failure;
            }

            _lastSeenPlayerTime = Time.time;
            return BTStatus.Success;
        }

        BTStatus HasPatrol()
            => (stats.patrolPoints != null && stats.patrolPoints.Length > 0)
                ? BTStatus.Success : BTStatus.Failure;

        // ════════════════════════════════════════════════════════
        // Actions
        // ════════════════════════════════════════════════════════

        BTStatus DoBehaviorDead()
        {
            if (_isDead) return BTStatus.Running;

            _isDead = true;
            _agent.isStopped = true;
            _agent.ResetPath();
            monsterAnimator?.PlayDeath();
            DropSouls();
            Destroy(gameObject, deathFadeDelay);
            return BTStatus.Running;
        }

        BTStatus DoHitStun()
        {
            _agent.isStopped = true;
            _hitRecoveryTimer -= Time.deltaTime;

            if (_hitRecoveryTimer <= 0f)
            {
                _isHit = false;
                _agent.isStopped = false;
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

        BTStatus DoAttack()
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            _lastAttackTime = Time.time;
            monsterAnimator?.PlayAttack();

            sfxEmitter?.EmitByDistance(_player);

            // ✅ SFX — โหมดเล่นเสียงตอนเริ่มง้าง (เช่นเสียงคำราม)
            if (playAttackSfxOnWindup && monsterAudio != null)
                monsterAudio.PlayAttack();

            StartCoroutine(DealDamageDelayed());

            return BTStatus.Success;
        }

        IEnumerator DealDamageDelayed()
        {
            float waitTime = monsterAnimator != null
                ? monsterAnimator.attackDamagePoint
                : 0.5f;

            yield return new WaitForSeconds(waitTime);

            // ตายหรือโดนตีขัดจังหวะระหว่างรอ → ไม่ต้องเล่นเสียง/ทำดาเมจ
            if (_isDead || _player == null) yield break;

            // ✅ SFX — โหมดเล่นเสียงตรงจังหวะฟาด (ค่าเริ่มต้น) เล่นแม้ผู้เล่นหลบได้
            if (!playAttackSfxOnWindup && monsterAudio != null)
                monsterAudio.PlayAttack();

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= stats.attackRange)
            {
                var ph = _player.GetComponent<MOMO.Player.PlayerHealth>();
                ph?.TakeDamage((int)stats.attackDamage);
                Debug.Log($"[{stats.monsterName}] Deal {stats.attackDamage} damage to Player!");
            }
        }

        BTStatus DoChase()
        {
            _agent.isStopped = false;
            _agent.speed = stats.runSpeed;

            if (Time.time >= _lastPathUpdateTime + pathUpdateInterval)
            {
                _agent.SetDestination(_player.position);
                _lastPathUpdateTime = Time.time;
            }

            if (_player != null && Time.time >= _lastFootstepEmit + footstepEmitInterval)
            {
                _lastFootstepEmit = Time.time;
                sfxEmitter?.EmitByDistance(_player);
            }

            return BTStatus.Running;
        }

        BTStatus DoPatrol()
        {
            _agent.speed = stats.walkSpeed;

            if (_patrolWaitTimer > 0f)
            {
                _patrolWaitTimer -= Time.deltaTime;
                _agent.isStopped = true;
                return BTStatus.Running;
            }

            _agent.isStopped = false;
            Vector3 target = _spawnPos + stats.patrolPoints[_patrolIndex];

            if (Time.time >= _lastPathUpdateTime + pathUpdateInterval)
            {
                _agent.SetDestination(target);
                _lastPathUpdateTime = Time.time;
            }

            if (Vector3.Distance(transform.position, target) < stats.waypointReachedDistance)
            {
                _patrolIndex = (_patrolIndex + 1) % stats.patrolPoints.Length;
                _patrolWaitTimer = stats.patrolWaitTime;
            }

            return BTStatus.Running;
        }

        BTStatus DoIdle()
        {
            _agent.isStopped = true;
            _agent.ResetPath();
            return BTStatus.Running;
        }

        // ════════════════════════════════════════════════════════
        // Public API
        // ════════════════════════════════════════════════════════

        public void TakeDamage(float damage, float stunDuration = 0.4f)
        {
            if (_isDead) return;

            if (hitEffectPrefab != null)
            {
                var effect = Instantiate(hitEffectPrefab,
                    transform.position + Vector3.up * 0.5f,
                    Quaternion.identity);
                Destroy(effect, 2f);
            }

            CurrentHealth -= damage;

            // ✅ SFX — เล่นก่อนเช็คตาย เพื่อให้ hit สุดท้ายมีเสียงด้วย
            if (monsterAudio != null)
                monsterAudio.PlayHit();

            if (!_isFlashing)
                StartCoroutine(HitFlash());

            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                monsterAnimator?.PlayDeath();

                if (HapticManager.Instance != null)
                    HapticManager.Instance.Vibrate(0.8f, 0.8f, 0.3f);

                return;
            }

            _isHit = true;
            _hitRecoveryTimer = stunDuration;
            monsterAnimator?.PlayHit();

            if (HapticManager.Instance != null)
                HapticManager.Instance.Vibrate(0.4f, 0.4f, 0.12f);
        }

        // ════════════════════════════════════════════════════════
        // Hit Flash
        // ════════════════════════════════════════════════════════

        IEnumerator HitFlash()
        {
            _isFlashing = true;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _renderers[i].material != null)
                    _renderers[i].material.color = hitFlashColor;
            }

            yield return new WaitForSeconds(hitFlashDuration);

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null && _renderers[i].material != null)
                    _renderers[i].material.color = _originalColors[i];
            }

            _isFlashing = false;
        }

        // ════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════

        void DropSouls()
        {
            if (stats.soulPrefab == null) return;

            int dropped = 0;
            for (int i = 0; i < stats.soulDropCount; i++)
            {
                if (Random.value <= stats.soulDropChance)
                {
                    Vector3 offset = Random.insideUnitSphere * 0.8f;
                    offset.y = Mathf.Abs(offset.y) + 0.3f;

                    GameObject soul = Instantiate(
                        stats.soulPrefab,
                        transform.position + offset,
                        Quaternion.identity);

                    var rb = soul.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        Vector3 force = new Vector3(
                            Random.Range(-1f, 1f),
                            Random.Range(2f, 4f),
                            Random.Range(-1f, 1f));
                        rb.AddForce(force, ForceMode.Impulse);
                    }

                    dropped++;
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // Gizmos
        // ════════════════════════════════════════════════════════

        void OnDrawGizmosSelected()
        {
            if (stats == null) return;

            Vector3 origin = Application.isPlaying ? _spawnPos : transform.position;

            Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, stats.detectionRange);

            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, stats.attackRange);

            if (stats.patrolPoints == null || stats.patrolPoints.Length == 0) return;
            Gizmos.color = Color.blue;
            for (int i = 0; i < stats.patrolPoints.Length; i++)
            {
                Vector3 p = origin + stats.patrolPoints[i];
                Vector3 next = origin + stats.patrolPoints[(i + 1) % stats.patrolPoints.Length];
                Gizmos.DrawSphere(p + Vector3.up * 0.2f, 0.3f);
                Gizmos.DrawLine(p + Vector3.up * 0.2f, next + Vector3.up * 0.2f);
            }
        }
    }
}