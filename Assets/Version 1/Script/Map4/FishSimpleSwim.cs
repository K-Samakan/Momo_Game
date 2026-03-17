using UnityEngine;

public class FishSimpleMove : MonoBehaviour
{
    [Header("=== Swimming Settings ===")]
    [SerializeField] private float swimSpeed = 2f;
    [SerializeField] private float rotationSpeed = 3f; // ← ลดจาก 5 เป็น 3
    [SerializeField] private float waypointRadius = 10f;
    [SerializeField] private float waypointReachDistance = 2f; // ← เพิ่มจาก 1.5
    [SerializeField] private float waypointWaitTime = 3f; // ← เพิ่มจาก 2
    
    [Header("=== Movement Smoothing ===")]
    [SerializeField] private float movementSmoothing = 5f; // ← เพิ่มใหม่
    [SerializeField] private bool useInertia = true; // ← เพิ่มใหม่
    
    [Header("=== Water Boundaries ===")]
    [SerializeField] private Transform waterVolume;
    [SerializeField] private float minDepthFromSurface = 0.5f;
    [SerializeField] private float minDistanceFromBottom = 0.5f;
    
    [Header("=== Collision ===")]
    [SerializeField] private LayerMask obstacleLayer; // ← เพิ่มใหม่
    [SerializeField] private float obstacleDetectionDistance = 2f; // ← เพิ่มใหม่
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = false;
    
    private Vector3 currentWaypoint;
    private Vector3 currentVelocity; // ← เพิ่มใหม่
    private float waypointTimer = 0f;
    private float waterSurfaceY;
    private float waterBottomY;
    
    void Start()
    {
        CalculateWaterBoundaries();
        SetRandomWaypoint();
        
        if (showDebugLogs) Debug.Log($"✅ {gameObject.name} initialized - Simple Move (No NavMesh)");
    }
    
    void Update()
    {
        CheckForObstacles(); // ← เพิ่มใหม่
        MoveTowardsWaypoint();
        UpdateRotation();
    }
    
    private void MoveTowardsWaypoint()
    {
        float distance = Vector3.Distance(transform.position, currentWaypoint);
        
        if (distance < waypointReachDistance)
        {
            // ถึง Waypoint แล้ว
            waypointTimer += Time.deltaTime;
            
            if (waypointTimer >= waypointWaitTime)
            {
                SetRandomWaypoint();
                waypointTimer = 0f;
            }
            
            // ← เพิ่ม: ค่อยๆ หยุด
            if (useInertia)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, Time.deltaTime * 2f);
                transform.position += currentVelocity * Time.deltaTime;
            }
        }
        else
        {
            // ยังไม่ถึง Waypoint
            Vector3 direction = (currentWaypoint - transform.position).normalized;
            Vector3 targetVelocity = direction * swimSpeed;
            
            // ← แก้: ใช้ Smooth Movement แทน Instant Movement
            if (useInertia)
            {
                currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, Time.deltaTime * movementSmoothing);
                transform.position += currentVelocity * Time.deltaTime;
            }
            else
            {
                transform.position += direction * swimSpeed * Time.deltaTime;
            }
            
            // Clamp ให้อยู่ในน้ำ
            Vector3 pos = transform.position;
            pos.y = Mathf.Clamp(pos.y, waterBottomY + minDistanceFromBottom, waterSurfaceY - minDepthFromSurface);
            transform.position = pos;
        }
    }
    
    private void SetRandomWaypoint()
    {
        // สุ่มตำแหน่งใหม่
        Vector2 randomCircle = Random.insideUnitCircle * waypointRadius;
        currentWaypoint = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
        currentWaypoint.y = transform.position.y;
        
        // ← เพิ่ม: ตรวจสอบว่า Waypoint ชนกำแพงหรือไม่
        Vector3 directionToWaypoint = (currentWaypoint - transform.position).normalized;
        float distanceToWaypoint = Vector3.Distance(transform.position, currentWaypoint);
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, directionToWaypoint, out hit, distanceToWaypoint, obstacleLayer))
        {
            // ชนกำแพง → เลือกจุดใหม่
            if (showDebugLogs) Debug.Log($"⚠️ {gameObject.name} waypoint blocked! Choosing new one...");
            SetRandomWaypoint(); // Recursive call
            return;
        }
        
        if (showDebugLogs) Debug.Log($"🎯 {gameObject.name} new waypoint: {currentWaypoint}");
    }
    
    private void UpdateRotation()
    {
        // ← แก้: ใช้ currentVelocity แทน direction to waypoint
        Vector3 rotationDirection = useInertia ? currentVelocity : (currentWaypoint - transform.position);
        
        if (rotationDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(rotationDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
    
    // ← เพิ่มใหม่: ตรวจสอบกำแพงข้างหน้า
    private void CheckForObstacles()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, obstacleDetectionDistance, obstacleLayer))
        {
            // เจอกำแพงข้างหน้า → เลือก Waypoint ใหม่ทันที
            if (showDebugLogs) Debug.LogWarning($"⚠️ {gameObject.name} detected obstacle ahead! Changing direction...");
            SetRandomWaypoint();
        }
    }
    
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
                
                if (showDebugLogs) Debug.Log($"💧 Water boundaries: Surface={waterSurfaceY:F2}, Bottom={waterBottomY:F2}");
            }
        }
        else
        {
            Debug.LogWarning($"⚠️ {gameObject.name} - Water Volume not assigned!");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;
        
        // Waypoint Radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, waypointRadius);
        
        // Current Waypoint
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(currentWaypoint, 0.4f);
            Gizmos.DrawLine(transform.position, currentWaypoint);
            
            // Velocity Direction
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, currentVelocity.normalized * 2f);
            
            // Obstacle Detection Ray
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, transform.forward * obstacleDetectionDistance);
        }
    }
}