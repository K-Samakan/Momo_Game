using UnityEngine;
using UnityEngine.AI;
using MOMO.AI.Pathfinding;
using System.Collections;
using System.Collections.Generic;

namespace MOMO.AI.Monster
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class MonsterAI_Hybrid : MonoBehaviour
    {
        [Header("References")]
        public MonsterStats stats;
        public Transform player;
        public ThetaStarNavMesh pathfinding;
        public MonsterAnimator monsterAnimator;
        
        [Header("State")]
        public MonsterState currentState = MonsterState.Idle;
        
        [Header("Path Settings")]
        [Tooltip("ใช้ Theta* แทน NavMesh pathfinding")]
        public bool useThetaStar = true;
        
        [Tooltip("Update path ทุกกี่วินาที")]
        public float pathUpdateInterval = 0.5f;
        
        [Header("Runtime")]
        public float currentHealth;
        
        // Private Fields
        float lastAttackTime;
        float lastPathUpdateTime;
        int currentPatrolIndex = 0;
        float patrolWaitTimer;
        
        NavMeshAgent agent;
        Vector3 spawnPosition;
        List<Vector3> currentPath = new List<Vector3>();
        int currentWaypointIndex = 0;
        bool isDead = false;
        
        public enum MonsterState
        {
            Idle,
            Patrol,
            Chase,
            Attack,
            Return,
            Hit,
            Dead
        }
        
        #region Unity Lifecycle
        
        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            currentHealth = stats.maxHealth;
            spawnPosition = transform.position;
            
            // ตั้งค่า NavMeshAgent
            agent.speed = stats.walkSpeed;
            agent.stoppingDistance = stats.attackRange * 0.8f;
            agent.acceleration = 8f;
            agent.angularSpeed = 120f;
            agent.autoBraking = true;
            
            // ปิด auto pathing ถ้าใช้ Theta*
            if (useThetaStar)
            {
                agent.updateRotation = false;
            }
            
            // หา Player
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                    player = playerObj.transform;
            }
            
            // หา Pathfinding System
            if (pathfinding == null)
            {
                pathfinding = FindObjectOfType<ThetaStarNavMesh>();
            }
            
            // หา Animator
            if (monsterAnimator == null)
            {
                monsterAnimator = GetComponent<MonsterAnimator>();
            }
            
            Debug.Log($"<color=green>[{stats.monsterName}]</color> Initialized at {spawnPosition}");
            
            // เริ่มต้นด้วย Idle หรือ Patrol
            if (stats.patrolPoints != null && stats.patrolPoints.Length > 0)
            {
                ChangeState(MonsterState.Patrol);
            }
            else
            {
                ChangeState(MonsterState.Idle);
            }
        }
        
        void Update()
        {
            if (isDead) return;
            
            // อัพเดทตาม State ปัจจุบัน
            switch (currentState)
            {
                case MonsterState.Idle:
                    UpdateIdle();
                    break;
                case MonsterState.Patrol:
                    UpdatePatrol();
                    break;
                case MonsterState.Chase:
                    UpdateChase();
                    break;
                case MonsterState.Attack:
                    UpdateAttack();
                    break;
                case MonsterState.Return:
                    UpdateReturn();
                    break;
                case MonsterState.Hit:
                    UpdateHit();
                    break;
            }
        }
        
        #endregion
        
        #region State Updates
        
        void UpdateIdle()
        {
            // หมุนหาผู้เล่น
            if (CanSeePlayer())
            {
                ChangeState(MonsterState.Chase);
                return;
            }
            
            // ถ้ามี Patrol Points ให้ไปลาดตระเวน
            if (stats.patrolPoints != null && stats.patrolPoints.Length > 0)
            {
                ChangeState(MonsterState.Patrol);
            }
        }
        
        void UpdatePatrol()
        {
            // เช็คว่าเห็นผู้เล่นไหม
            if (CanSeePlayer())
            {
                ChangeState(MonsterState.Chase);
                return;
            }
            
            // ถ้าไม่มี Patrol Points
            if (stats.patrolPoints == null || stats.patrolPoints.Length == 0)
            {
                ChangeState(MonsterState.Idle);
                return;
            }
            
            // รอที่ Waypoint
            if (patrolWaitTimer > 0)
            {
                patrolWaitTimer -= Time.deltaTime;
                agent.isStopped = true;
                return;
            }
            
            agent.isStopped = false;
            agent.speed = stats.walkSpeed;
            
            // หา Path ไป Patrol Point
            Vector3 targetPoint = spawnPosition + stats.patrolPoints[currentPatrolIndex];
            
            if (useThetaStar && pathfinding != null)
            {
                // ใช้ Theta* หา Path
                if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
                {
                    currentPath = pathfinding.FindPath(transform.position, targetPoint);
                    currentWaypointIndex = 0;
                    lastPathUpdateTime = Time.time;
                }
                
                FollowThetaStarPath();
            }
            else
            {
                // ใช้ NavMesh ปกติ
                agent.SetDestination(targetPoint);
            }
            
            // เช็คว่าถึง Waypoint แล้วหรือยัง
            float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
            
            if (distanceToTarget < stats.waypointReachedDistance)
            {
                // ถึง Waypoint แล้ว
                currentPatrolIndex = (currentPatrolIndex + 1) % stats.patrolPoints.Length;
                patrolWaitTimer = stats.patrolWaitTime;
                
                Debug.Log($"<color=cyan>[{stats.monsterName}]</color> Reached patrol point {currentPatrolIndex}");
            }
        }
        
        void UpdateChase()
        {
            if (player == null)
            {
                ChangeState(MonsterState.Idle);
                return;
            }
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // ผู้เล่นหนีไปไกลเกิน Leash Range
            float distanceFromSpawn = Vector3.Distance(transform.position, spawnPosition);
            if (distanceFromSpawn > stats.leashRange)
            {
                Debug.Log($"<color=yellow>[{stats.monsterName}]</color> Out of leash range, returning...");
                ChangeState(MonsterState.Return);
                return;
            }
            
            // เข้าใกล้พอที่จะโจมตี
            if (distanceToPlayer <= stats.attackRange)
            {
                ChangeState(MonsterState.Attack);
                return;
            }
            
            // ไม่เห็นผู้เล่นแล้ว
            if (!CanSeePlayer())
            {
                ChangeState(MonsterState.Return);
                return;
            }
            
            // ไล่ตามผู้เล่น
            agent.isStopped = false;
            agent.speed = stats.runSpeed;
            
            if (useThetaStar && pathfinding != null)
            {
                // อัพเดท Path ทุก ๆ interval
                if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
                {
                    currentPath = pathfinding.FindPath(transform.position, player.position);
                    currentWaypointIndex = 0;
                    lastPathUpdateTime = Time.time;
                }
                
                FollowThetaStarPath();
            }
            else
            {
                // ใช้ NavMesh ปกติ
                agent.SetDestination(player.position);
            }
        }
        
        void UpdateAttack()
        {
            if (player == null)
            {
                ChangeState(MonsterState.Idle);
                return;
            }
            
            agent.isStopped = true;
            currentPath.Clear();
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // ผู้เล่นหนีไปไกล
            if (distanceToPlayer > stats.attackRange * 1.5f)
            {
                ChangeState(MonsterState.Chase);
                return;
            }
            
            // หันหน้าไปหาผู้เล่น
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    lookRotation,
                    Time.deltaTime * 5f
                );
            }
            
            // รอ Animation โจมตีจบก่อน
            if (monsterAnimator != null && monsterAnimator.IsAttacking())
            {
                return;
            }
            
            // โจมตี
            if (Time.time >= lastAttackTime + stats.attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }
        }
        
        void UpdateReturn()
        {
            agent.isStopped = false;
            agent.speed = stats.walkSpeed;
            
            float distanceToSpawn = Vector3.Distance(transform.position, spawnPosition);
            
            // กลับถึงจุดเริ่มต้นแล้ว
            if (distanceToSpawn < stats.waypointReachedDistance)
            {
                currentHealth = stats.maxHealth; // ฟื้นเลือด
                Debug.Log($"<color=green>[{stats.monsterName}]</color> Returned to spawn, HP restored");
                
                if (stats.patrolPoints != null && stats.patrolPoints.Length > 0)
                {
                    ChangeState(MonsterState.Patrol);
                }
                else
                {
                    ChangeState(MonsterState.Idle);
                }
                return;
            }
            
            // เห็นผู้เล่นระหว่างกลับ
            if (CanSeePlayer())
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer <= stats.detectionRange * 0.5f) // ครึ่งหนึ่งของ detection range
                {
                    ChangeState(MonsterState.Chase);
                    return;
                }
            }
            
            // เดินกลับจุดเริ่มต้น
            if (useThetaStar && pathfinding != null)
            {
                if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
                {
                    currentPath = pathfinding.FindPath(transform.position, spawnPosition);
                    currentWaypointIndex = 0;
                    lastPathUpdateTime = Time.time;
                }
                
                FollowThetaStarPath();
            }
            else
            {
                agent.SetDestination(spawnPosition);
            }
        }
        
        void UpdateHit()
        {
            // หยุดเคลื่อนที่ชั่วคราว
            agent.isStopped = true;
            currentPath.Clear();
            
            // รอ Animation Hit จบ
            if (monsterAnimator != null && !monsterAnimator.IsGettingHit())
            {
                // กลับไปไล่ต่อ
                if (player != null)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                    if (distanceToPlayer <= stats.attackRange)
                    {
                        ChangeState(MonsterState.Attack);
                    }
                    else if (distanceToPlayer <= stats.detectionRange)
                    {
                        ChangeState(MonsterState.Chase);
                    }
                    else
                    {
                        ChangeState(MonsterState.Idle);
                    }
                }
                else
                {
                    ChangeState(MonsterState.Idle);
                }
            }
        }
        
        #endregion
        
        #region Pathfinding Methods
        
        void FollowThetaStarPath()
        {
            if (currentPath == null || currentPath.Count == 0)
            {
                agent.isStopped = true;
                return;
            }
            
            agent.isStopped = false;
            
            // เช็คว่าถึง Waypoint ปัจจุบันแล้วหรือยัง
            if (currentWaypointIndex < currentPath.Count)
            {
                Vector3 targetWaypoint = currentPath[currentWaypointIndex];
                float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint);
                
                if (distanceToWaypoint < stats.waypointReachedDistance)
                {
                    currentWaypointIndex++;
                }
                else
                {
                    // เดินไปที่ Waypoint
                    agent.SetDestination(targetWaypoint);
                    
                    // หันหน้าไปทาง Waypoint
                    Vector3 direction = (targetWaypoint - transform.position).normalized;
                    direction.y = 0;
                    if (direction != Vector3.zero)
                    {
                        Quaternion lookRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            lookRotation,
                            Time.deltaTime * 5f
                        );
                    }
                }
            }
            else
            {
                // ถึงจุดหมายแล้ว
                currentPath.Clear();
                currentWaypointIndex = 0;
            }
        }
        
        #endregion
        
        #region Detection Methods
        
        bool CanSeePlayer()
        {
            if (player == null) return false;
            
            float distance = Vector3.Distance(transform.position, player.position);
            
            // เช็คระยะ
            if (distance > stats.detectionRange) return false;
            
            // เช็ค Line of Sight (Raycast)
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            
            RaycastHit hit;
            if (Physics.Raycast(
                transform.position + Vector3.up * 0.5f,
                directionToPlayer,
                out hit,
                stats.detectionRange
            ))
            {
                if (hit.transform == player)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        #endregion
        
        #region Combat Methods
        
        void PerformAttack()
        {
            Debug.Log($"<color=red>[{stats.monsterName}]</color> Performs Attack!");
            
            // เล่น Animation โจมตี
            if (monsterAnimator != null)
            {
                monsterAnimator.PlayAttack();
            }
            else
            {
                // ถ้าไม่มี Animator ทำ Damage ทันที
                DealDamageToPlayer();
            }
        }
        
        /// <summary>
        /// ทำ Damage ให้ผู้เล่น (เรียกจาก MonsterAnimator)
        /// </summary>
        public void DealDamageToPlayer()
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // ตรวจสอบว่ายังอยู่ในระยะโจมตี
            if (distanceToPlayer <= stats.attackRange * 1.2f)
            {
                MOMO.Player.PlayerHealth playerHealth = player.GetComponent<MOMO.Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage((int)stats.attackDamage);
                    Debug.Log($"<color=red>[{stats.monsterName}]</color> Hit Player for {stats.attackDamage} damage!");
                }
            }
            else
            {
                Debug.Log("<color=yellow>[Monster]</color> Player out of range, missed!");
            }
        }
        
        /// <summary>
        /// Monster โดน Damage
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (isDead) return;
            
            currentHealth -= damage;
            Debug.Log($"<color=orange>[{stats.monsterName}]</color> Took {damage} damage. HP: {currentHealth}/{stats.maxHealth}");
            
            // ยังไม่ตาย
            if (currentHealth > 0)
            {
                // เล่น Animation โดนโจมตี
                if (monsterAnimator != null)
                {
                    monsterAnimator.PlayHit();
                }
                
                ChangeState(MonsterState.Hit);
            }
            else
            {
                // ตาย
                Die();
            }
        }
        
        void Die()
        {
            if (isDead) return;
            
            isDead = true;
            Debug.Log($"<color=red>[{stats.monsterName}]</color> Died!");
            
            ChangeState(MonsterState.Dead);
            
            // เล่น Animation ตาย
            if (monsterAnimator != null)
            {
                monsterAnimator.PlayDeath();
            }
            
            // หยุดการทำงานของ AI
            agent.isStopped = true;
            this.enabled = false;
            
            // รอ Animation ตายจบแล้วค่อยดรอป Soul
            StartCoroutine(DeathSequence());
        }
        
        IEnumerator DeathSequence()
        {
            // รอ Animation ตายจบ
            if (monsterAnimator != null)
            {
                yield return new WaitForSeconds(0.5f); // รอให้ Animation เริ่ม
                
                while (!monsterAnimator.IsDeathAnimationFinished())
                {
                    yield return null;
                }
                
                yield return new WaitForSeconds(0.5f); // รออีกนิด
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }
            
            // ดรอป Soul
            DropSoul();
            
            // ลบ GameObject
            Destroy(gameObject);
        }
        
        void DropSoul()
        {
            if (stats.soulPrefab == null) return;
            
            // Random จำนวน Soul ที่ดรอป
            int soulCount = 0;
            for (int i = 0; i < stats.soulDropCount; i++)
            {
                if (Random.value <= stats.soulDropChance)
                {
                    soulCount++;
                }
            }
            
            if (soulCount <= 0)
            {
                Debug.Log($"<color=yellow>[{stats.monsterName}]</color> No soul dropped");
                return;
            }
            
            Debug.Log($"<color=green>[{stats.monsterName}]</color> Dropped {soulCount} soul(s)!");
            
            // วาง Soul รอบ ๆ ตัว Monster
            for (int i = 0; i < soulCount; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * 1.5f;
                randomOffset.y = 0.5f;
                
                Vector3 spawnPosition = transform.position + randomOffset;
                
                GameObject soul = Instantiate(stats.soulPrefab, spawnPosition, Quaternion.identity);
                
                // ใส่แรงเล็กน้อย
                Rigidbody rb = soul.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 force = new Vector3(
                        Random.Range(-1f, 1f),
                        Random.Range(2f, 4f),
                        Random.Range(-1f, 1f)
                    );
                    rb.AddForce(force, ForceMode.Impulse);
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        void ChangeState(MonsterState newState)
        {
            if (currentState == newState) return;
            
            Debug.Log($"<color=cyan>[{stats.monsterName}]</color> State: {currentState} → {newState}");
            
            MonsterState oldState = currentState;
            currentState = newState;
            
            // ล้าง Path
            currentPath.Clear();
            currentWaypointIndex = 0;
            
            // ตั้งค่าตาม State ใหม่
            switch (newState)
            {
                case MonsterState.Idle:
                case MonsterState.Attack:
                case MonsterState.Hit:
                case MonsterState.Dead:
                    agent.isStopped = true;
                    break;
                    
                case MonsterState.Patrol:
                case MonsterState.Return:
                    agent.isStopped = false;
                    agent.speed = stats.walkSpeed;
                    break;
                    
                case MonsterState.Chase:
                    agent.isStopped = false;
                    agent.speed = stats.runSpeed;
                    break;
            }
        }
        
        #endregion
        
        #region Debug
        
        void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            if (stats == null) return;
            
            // Detection Range (สีเหลือง)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectionRange);
            
            // Attack Range (สีแดง)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.attackRange);
            
            // Leash Range (สีส้ม)
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(spawnPosition, stats.leashRange);
            
            // Line to Player
            if (player != null && (currentState == MonsterState.Chase || currentState == MonsterState.Attack))
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up, player.position + Vector3.up);
            }
            
            // Current Path
            if (currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(
                        currentPath[i] + Vector3.up * 0.5f,
                        currentPath[i + 1] + Vector3.up * 0.5f
                    );
                }
                
                // Current Waypoint
                if (currentWaypointIndex < currentPath.Count)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawSphere(currentPath[currentWaypointIndex] + Vector3.up * 0.5f, 0.3f);
                }
            }
            
            // Patrol Points
            if (stats.patrolPoints != null && stats.patrolPoints.Length > 0)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < stats.patrolPoints.Length; i++)
                {
                    Vector3 worldPos = spawnPosition + stats.patrolPoints[i];
                    Gizmos.DrawSphere(worldPos + Vector3.up * 0.2f, 0.4f);
                    
                    // เส้นเชื่อม
                    if (i < stats.patrolPoints.Length - 1)
                    {
                        Vector3 nextWorldPos = spawnPosition + stats.patrolPoints[i + 1];
                        Gizmos.DrawLine(worldPos + Vector3.up * 0.2f, nextWorldPos + Vector3.up * 0.2f);
                    }
                    else
                    {
                        // เชื่อมจุดสุดท้ายกลับจุดแรก
                        Vector3 firstWorldPos = spawnPosition + stats.patrolPoints[0];
                        Gizmos.DrawLine(worldPos + Vector3.up * 0.2f, firstWorldPos + Vector3.up * 0.2f);
                    }
                }
            }
        }
        
        #endregion
    }
}