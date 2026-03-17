using UnityEngine;

public class PlayerAttackDetection : MonoBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage dealt per attack")]
    public int attackDamage = 1;

    [Header("Detection Settings")]
    [Tooltip("Attack range")]
    public float attackRange = 2f;
    
    [Tooltip("Attack angle (degrees)")]
    public float attackAngle = 90f;
    
    [Tooltip("tag for enemies")]
    //public LayerMask enemyLayer;
    public string enemyTag = "Enemy";

    [Header("Attack Point")]
    [Tooltip("Position where attack originates (optional)")]
    public Transform attackPoint;

    [Header("Visual Debug")]
    public bool showDebugGizmos = true;

    private StarterAssets.ThirdPersonController playerController;

    private void Start()
    {
        playerController = GetComponent<StarterAssets.ThirdPersonController>();

        // ถ้าไม่มี attack point ให้ใช้ตำแหน่งตัวละคร
        if (attackPoint == null)
        {
            attackPoint = transform;
        }
    }

    // เรียกจาก Animation Event
    public void OnAttackHit()
    {
        PerformAttackDetection();
    }

    private void PerformAttackDetection()
    {
        Vector3 attackOrigin = attackPoint.position;
        Vector3 attackDirection = transform.forward;

        // ค้นหา Colliders ในรัศมี
        Collider[] hitColliders = Physics.OverlapSphere(attackOrigin, attackRange);
        int enemiesFound = 0;

        //Debug.Log($"Found {hitColliders.Length} potential targets");

        foreach (Collider col in hitColliders)
        {
            // ตรวจสอบ Tag แทน Layer
            if (!col.CompareTag(enemyTag))
                continue;

            enemiesFound++;

            // คำนวณทิศทาง
            Vector3 directionToTarget = (col.transform.position - attackOrigin).normalized;
            
            // คำนวณมุม
            float angleToTarget = Vector3.Angle(attackDirection, directionToTarget);

            // ตรวจสอบว่าอยู่ในมุมโจมตีหรือไม่
            if (angleToTarget < attackAngle / 2f)
            {
                // พยายามหา MonsterHealth component
                MonsterHealth monsterHealth = col.GetComponent<MonsterHealth>();
                
                if (monsterHealth != null && monsterHealth.IsAlive())
                {
                    // โจมตีโดน!
                    monsterHealth.TakeDamage(attackDamage, transform.position);
                    Debug.Log($"Hit {col.gameObject.name}!");

                    // Haptic feedback (optional)
                    if (HapticManager.Instance != null)
                    {
                        HapticManager.Instance.Vibrate(0.1f, 0.3f, 0.1f);
                    }
                }
            }
        }

        Debug.Log($"Found {enemiesFound} enemies with tag '{enemyTag}'");
        
    }

    // Gizmos for visualization
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 origin = attackPoint != null ? attackPoint.position : transform.position;
        Vector3 forward = transform.forward;

        // วงกลมรัศมีโจมตี
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin, attackRange);

        // แสดงมุมโจมตี
        Gizmos.color = Color.yellow;
        Vector3 rightBoundary = Quaternion.Euler(0, attackAngle / 2f, 0) * forward * attackRange;
        Vector3 leftBoundary = Quaternion.Euler(0, -attackAngle / 2f, 0) * forward * attackRange;

        Gizmos.DrawLine(origin, origin + rightBoundary);
        Gizmos.DrawLine(origin, origin + leftBoundary);
        Gizmos.DrawLine(origin, origin + forward * attackRange);
    }
}