using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class FishAI : MonoBehaviour
{
    #region === SETTINGS ===

    [Header("=== Behavior ===")]
    [SerializeField] private FishBehavior behavior = FishBehavior.Patrol;
    [SerializeField] private bool autoFleeFromPlayer = true;
    [SerializeField] private bool permanentFlee = false;
    [SerializeField] private float swimSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("=== Patrol Settings ===")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float waypointReachDistance = 1f;
    [SerializeField] private float waypointWaitTime = 2f;

    [Header("=== Chase Settings ===")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float escapeDistance = 8f;

    [Header("=== Water Boundaries ===")]
    [SerializeField] private Transform waterVolume;
    [SerializeField] private float minDepth = 0.5f;
    [SerializeField] private float maxDepth = 0.5f;

    [Header("=== Animation ===")]
    [SerializeField] private Animator animator;
    [SerializeField] private string swimSpeedParam = "SwimSpeed";

    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    #endregion

    #region === COMPONENTS ===

    private NavMeshAgent agent;
    private Transform player;
    private Vector3 currentWaypoint;
    private float waypointTimer = 0f;
    private float waterSurfaceY;
    private float waterBottomY;

    #endregion

    #region === BEHAVIOR ENUM ===

    public enum FishBehavior
    {
        Idle,
        Patrol,
        Chase,
        Flee
    }

    #endregion

    #region === UNITY LIFECYCLE ===

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.speed = swimSpeed;
        agent.angularSpeed = rotationSpeed * 100f;
        agent.acceleration = 16f;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.autoBraking = false;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"✅ {gameObject.name} found Player: {player.name} at {player.position}");
        }
        else
        {
            Debug.LogError($"❌ {gameObject.name} cannot find Player! Make sure Player GameObject has tag 'Player'");
        }

        CalculateWaterBoundaries();

        if (behavior == FishBehavior.Patrol)
        {
            SetRandomWaypoint();
        }

        if (showDebugLogs) Debug.Log($"✅ FishAI initialized: {gameObject.name}");
    }

    void Update()
    {
        switch (behavior)
        {
            case FishBehavior.Idle:
                UpdateIdle();
                break;
            case FishBehavior.Patrol:
                UpdatePatrol();
                break;
            case FishBehavior.Chase:
                UpdateChase();
                break;
            case FishBehavior.Flee:
                UpdateFlee();
                break;
        }

        UpdateRotation();
        UpdateAnimation();
        EnforceWaterBoundaries();

        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"🐟 {gameObject.name} | Pos: {transform.position} | Behavior: {behavior} | Velocity: {agent.velocity.magnitude:F2}");
        }
    }

    #endregion

    #region === BEHAVIOR LOGIC ===

    private void UpdateIdle()
    {
        agent.isStopped = true;
    }

    private void UpdatePatrol()
    {
        agent.speed = swimSpeed;
        agent.isStopped = false;

        if (autoFleeFromPlayer && player != null)
        {
            float playerDistance = Vector3.Distance(transform.position, player.position);

            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.Log($"🐟 {gameObject.name} | Distance to Player: {playerDistance:F2}m | Detection: {detectionRadius}m");
            }

            if (playerDistance < detectionRadius)
            {
                Debug.LogWarning($"😱 {gameObject.name} DETECTED PLAYER! Changing to FLEE! Distance: {playerDistance:F2}m");
                behavior = FishBehavior.Flee;
                return;
            }
        }
        else
        {
            if (showDebugLogs && Time.frameCount % 120 == 0)
            {
                if (!autoFleeFromPlayer)
                {
                    Debug.Log($"ℹ️ {gameObject.name} Auto Flee is DISABLED");
                }
                if (player == null)
                {
                    Debug.LogError($"❌ {gameObject.name} Player is NULL!");
                }
            }
        }

        float distance = Vector3.Distance(transform.position, currentWaypoint);

        if (distance < waypointReachDistance)
        {
            waypointTimer += Time.deltaTime;

            if (waypointTimer >= waypointWaitTime)
            {
                SetRandomWaypoint();
                waypointTimer = 0f;
            }
        }
        else
        {
            if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                if (showDebugLogs && Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning($"⚠️ {gameObject.name} has no path! Recalculating...");
                }
                SetRandomWaypoint();
            }
            else
            {
                agent.SetDestination(currentWaypoint);
            }
        }
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            behavior = FishBehavior.Patrol;
            return;
        }

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < escapeDistance)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            behavior = FishBehavior.Patrol;
            SetRandomWaypoint();
        }
    }

    private void UpdateFlee()
    {
        if (player == null)
        {
            agent.isStopped = true;
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance < escapeDistance)
        {
            agent.speed = chaseSpeed;
            agent.isStopped = false;

            // คำนวณทิศทางหนี (เฉพาะ X, Z)
            Vector3 playerPos = player.position;
            Vector3 fishPos = transform.position;

            // ← แก้: ใช้แค่ X, Z (ไม่ใช้ Y)
            playerPos.y = 0f;
            fishPos.y = 0f;

            Vector3 fleeDirection = (fishPos - playerPos).normalized;

            // ← แก้: คำนวณ Target ใหม่ โดยใช้ Y ของปลาเอง
            Vector3 fleeTarget = transform.position + new Vector3(fleeDirection.x, 0f, fleeDirection.z) * 20f;

            // ← แก้: ใช้ Y ปัจจุบันของปลา (ไม่ให้ลอยขึ้นไป)
            fleeTarget.y = transform.position.y;

            // Clamp ให้อยู่ใน Water Boundaries
            fleeTarget = ClampToWaterBoundaries(fleeTarget);

            // หาจุดที่ไปถึงได้บน NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleeTarget, out hit, 10f, NavMesh.AllAreas))
            {
                fleeTarget = hit.position;

                if (showDebugLogs && Time.frameCount % 30 == 0)
                {
                    Debug.Log($"✅ Found NavMesh at: {hit.position}");
                }
            }
            else
            {
                // ถ้าหาไม่เจอ ใช้ตำแหน่งปัจจุบัน + offset เล็กน้อย
                fleeTarget = transform.position + new Vector3(fleeDirection.x, 0f, fleeDirection.z) * 5f;
                fleeTarget.y = transform.position.y;
                fleeTarget = ClampToWaterBoundaries(fleeTarget);

                if (showDebugLogs && Time.frameCount % 30 == 0)
                {
                    Debug.LogWarning($"⚠️ No NavMesh found, using offset position");
                }
            }

            // ← เพิ่ม Debug: ตรวจสอบว่า Target อยู่บน NavMesh หรือไม่
            if (showDebugLogs && Time.frameCount % 30 == 0)
            {
                NavMeshHit testHit;
                bool isOnNavMesh = NavMesh.SamplePosition(fleeTarget, out testHit, 0.5f, NavMesh.AllAreas);
                Debug.Log($"😱 Fleeing! Distance: {distance:F2}m | Target: {fleeTarget} | OnNavMesh: {isOnNavMesh} | HasPath: {agent.hasPath} | Velocity: {agent.velocity.magnitude:F2}");
            }

            agent.SetDestination(fleeTarget);
        }
        else
        {
            if (permanentFlee)
            {
                agent.isStopped = true;

                if (showDebugLogs && Time.frameCount % 120 == 0)
                {
                    Debug.Log($"✅ {gameObject.name} stopped fleeing (distance: {distance:F2}m)");
                }
            }
            else
            {
                behavior = FishBehavior.Patrol;
                SetRandomWaypoint();

                if (showDebugLogs)
                {
                    Debug.Log($"✅ {gameObject.name} returning to patrol (distance: {distance:F2}m)");
                }
            }
        }
    }
    #endregion

    #region === MOVEMENT ===

    private void SetRandomWaypoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        Vector3 randomPoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        randomPoint.y = Random.Range(waterBottomY + maxDepth, waterSurfaceY - minDepth);

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, 5f, NavMesh.AllAreas))
        {
            currentWaypoint = hit.position;
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"⚠️ No NavMesh near {randomPoint}, using current position");
            }
            currentWaypoint = transform.position + Random.insideUnitSphere * 2f;
            currentWaypoint.y = transform.position.y;
        }

        agent.SetDestination(currentWaypoint);

        if (showDebugLogs)
        {
            Debug.Log($"🐟 New waypoint: {currentWaypoint} | Distance: {Vector3.Distance(transform.position, currentWaypoint):F2}");
        }
    }

    private void UpdateRotation()
    {
        // ← แก้: เช็ค Behavior ก่อน
        if (behavior == FishBehavior.Flee && player != null)
        {
            // สำหรับ Flee: หมุนหน้าตรงข้าม Player
            Vector3 fleeDirection = (transform.position - player.position).normalized;

            if (fleeDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed * 2f);
            }
        }
        else
        {
            // สำหรับ Patrol/Chase: หมุนตาม Velocity
            if (agent.velocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(agent.velocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    #endregion

    #region === WATER BOUNDARIES ===

    private void CalculateWaterBoundaries()
    {
        if (waterVolume != null)
        {
            BoxCollider waterCollider = waterVolume.GetComponent<BoxCollider>();
            if (waterCollider != null)
            {
                float waterHeight = waterCollider.size.y * waterVolume.lossyScale.y;
                waterSurfaceY = waterVolume.position.y + (waterHeight / 2f);
                waterBottomY = waterVolume.position.y - (waterHeight / 2f);

                if (showDebugLogs)
                {
                    Debug.Log($"💧 Water boundaries: Surface={waterSurfaceY:F2}, Bottom={waterBottomY:F2} | Swim Range: {waterBottomY + maxDepth:F2} to {waterSurfaceY - minDepth:F2}");
                }
            }
        }
        else
        {
            Debug.LogError($"❌ Water Volume not assigned for {gameObject.name}!");
        }
    }

    private Vector3 ClampToWaterBoundaries(Vector3 position)
    {
        position.y = Mathf.Clamp(position.y, waterBottomY + maxDepth, waterSurfaceY - minDepth);
        return position;
    }

    private void EnforceWaterBoundaries()
    {
        Vector3 position = transform.position;
        float clampedY = Mathf.Clamp(position.y, waterBottomY + maxDepth, waterSurfaceY - minDepth);

        if (Mathf.Abs(position.y - clampedY) > 0.1f)
        {
            position.y = clampedY;
            transform.position = position;

            if (showDebugLogs && Time.frameCount % 60 == 0)
            {
                Debug.LogWarning($"⚠️ {gameObject.name} Y clamped to {clampedY:F2}");
            }
        }
    }

    #endregion

    #region === ANIMATION ===

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float speed = agent.velocity.magnitude;
        float normalizedSpeed = Mathf.Clamp01(speed / chaseSpeed);

        animator.SetFloat(swimSpeedParam, normalizedSpeed);
    }

    #endregion

    #region === PUBLIC API ===

    public void SetBehavior(FishBehavior newBehavior)
    {
        behavior = newBehavior;

        if (behavior == FishBehavior.Patrol)
        {
            SetRandomWaypoint();
        }
    }

    public FishBehavior GetBehavior() => behavior;

    #endregion

    #region === DEBUG ===

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        if (Application.isPlaying && behavior == FishBehavior.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
        }

        if (Application.isPlaying)
        {
            NavMeshAgent agent = GetComponent<NavMeshAgent>();
            if (agent != null && agent.velocity.magnitude > 0.1f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(transform.position, agent.velocity.normalized * 2f);
            }
        }

        if (waterVolume != null)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
            BoxCollider waterCollider = waterVolume.GetComponent<BoxCollider>();
            if (waterCollider != null)
            {
                Gizmos.matrix = waterVolume.localToWorldMatrix;
                Gizmos.DrawWireCube(waterCollider.center, waterCollider.size);
            }
        }

        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Vector3 surfaceCenter = transform.position;
            surfaceCenter.y = waterSurfaceY - minDepth;
            Gizmos.DrawWireCube(surfaceCenter, new Vector3(patrolRadius * 2, 0.1f, patrolRadius * 2));

            Gizmos.color = Color.gray;
            Vector3 bottomCenter = transform.position;
            bottomCenter.y = waterBottomY + maxDepth;
            Gizmos.DrawWireCube(bottomCenter, new Vector3(patrolRadius * 2, 0.1f, patrolRadius * 2));
        }
    }

    #endregion
}