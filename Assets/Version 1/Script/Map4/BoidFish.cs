using UnityEngine;

public class BoidFish : MonoBehaviour
{
    [Header("=== Boid Settings ===")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("=== Boid Weights ===")]
    [SerializeField] private float cohesionWeight = 1f; // ดึงดูดกัน
    [SerializeField] private float separationWeight = 1.5f; // แยกกัน
    [SerializeField] private float alignmentWeight = 1f; // ว่ายทิศทางเดียวกัน
    
    [Header("=== Detection ===")]
    [SerializeField] private float neighborDistance = 5f;
    [SerializeField] private float separationDistance = 2f;
    
    [Header("=== Boundaries ===")]
    [SerializeField] private Vector3 boundsCenter;
    [SerializeField] private Vector3 boundsSize = new Vector3(20, 10, 20);
    [SerializeField] private float boundaryForce = 10f;
    
    // Internal
    private Vector3 velocity;
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        // สุ่ม Velocity เริ่มต้น
        velocity = new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-0.5f, 0.5f),
            Random.Range(-1f, 1f)
        ).normalized * moveSpeed;
    }
    
    void Update()
    {
        // หาปลาตัวอื่นในรัศมี
        Collider[] neighbors = Physics.OverlapSphere(transform.position, neighborDistance, LayerMask.GetMask("Fish"));
        
        Vector3 cohesion = CalculateCohesion(neighbors);
        Vector3 separation = CalculateSeparation(neighbors);
        Vector3 alignment = CalculateAlignment(neighbors);
        Vector3 boundary = CalculateBoundary();
        
        // รวมแรงทั้งหมด
        Vector3 acceleration = 
            cohesion * cohesionWeight +
            separation * separationWeight +
            alignment * alignmentWeight +
            boundary * boundaryForce;
        
        velocity += acceleration * Time.deltaTime;
        velocity = Vector3.ClampMagnitude(velocity, moveSpeed);
        
        // เคลื่อนที่
        transform.position += velocity * Time.deltaTime;
        
        // หมุนตามทิศทาง
        if (velocity.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(velocity);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
        
        // Animation
        if (animator != null)
        {
            animator.SetFloat("SwimSpeed", velocity.magnitude / moveSpeed);
        }
    }
    
    // หลักการ Boids 1: Cohesion (ดึงดูดกัน - เคลื่อนที่ไปหาศูนย์กลางฝูง)
    private Vector3 CalculateCohesion(Collider[] neighbors)
    {
        if (neighbors.Length == 0) return Vector3.zero;
        
        Vector3 center = Vector3.zero;
        int count = 0;
        
        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                center += neighbor.transform.position;
                count++;
            }
        }
        
        if (count == 0) return Vector3.zero;
        
        center /= count;
        return (center - transform.position).normalized;
    }
    
    // หลักการ Boids 2: Separation (แยกกัน - หลีกเลี่ยงการชนกัน)
    private Vector3 CalculateSeparation(Collider[] neighbors)
    {
        Vector3 separationForce = Vector3.zero;
        int count = 0;
        
        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                float distance = Vector3.Distance(transform.position, neighbor.transform.position);
                
                if (distance < separationDistance)
                {
                    Vector3 diff = transform.position - neighbor.transform.position;
                    separationForce += diff.normalized / distance; // ยิ่งใกล้ ยิ่งแรง
                    count++;
                }
            }
        }
        
        if (count == 0) return Vector3.zero;
        
        return separationForce.normalized;
    }
    
    // หลักการ Boids 3: Alignment (ว่ายทิศทางเดียวกัน)
    private Vector3 CalculateAlignment(Collider[] neighbors)
    {
        if (neighbors.Length == 0) return Vector3.zero;
        
        Vector3 averageVelocity = Vector3.zero;
        int count = 0;
        
        foreach (Collider neighbor in neighbors)
        {
            if (neighbor.gameObject != gameObject)
            {
                BoidFish otherFish = neighbor.GetComponent<BoidFish>();
                if (otherFish != null)
                {
                    averageVelocity += otherFish.velocity;
                    count++;
                }
            }
        }
        
        if (count == 0) return Vector3.zero;
        
        averageVelocity /= count;
        return averageVelocity.normalized;
    }
    
    // ป้องกันออกนอกขอบเขต
    private Vector3 CalculateBoundary()
    {
        Vector3 force = Vector3.zero;
        Vector3 min = boundsCenter - boundsSize / 2;
        Vector3 max = boundsCenter + boundsSize / 2;
        
        if (transform.position.x < min.x) force.x = 1;
        if (transform.position.x > max.x) force.x = -1;
        if (transform.position.y < min.y) force.y = 1;
        if (transform.position.y > max.y) force.y = -1;
        if (transform.position.z < min.z) force.z = 1;
        if (transform.position.z > max.z) force.z = -1;
        
        return force;
    }
    
    void OnDrawGizmos()
    {
        // แสดงขอบเขต
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(boundsCenter, boundsSize);
        
        // แสดงรัศมีตรวจจับ
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, neighborDistance);
        
        // แสดงรัศมีแยกตัว
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, separationDistance);
    }
}