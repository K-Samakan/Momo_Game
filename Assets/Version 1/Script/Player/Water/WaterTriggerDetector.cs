using UnityEngine;

public class WaterTriggerDetector : MonoBehaviour
{
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showGizmos = true;  // ← ใหม่
    
    private SwimmingControllerHybrid swimmer;
    private int waterLayerMask;
    
    void Start()
    {
        swimmer = GetComponentInParent<SwimmingControllerHybrid>();
        
        if (swimmer == null)
        {
            Debug.LogError("❌ WaterTriggerDetector: Cannot find SwimmingControllerHybrid in parent!");
        }
        else
        {
            if (showDebugLogs) Debug.Log("✅ WaterTriggerDetector initialized");
        }
        
        waterLayerMask = LayerMask.NameToLayer("Water");
        if (waterLayerMask == -1)
        {
            Debug.LogError("❌ Layer 'Water' not found!");
        }
        else
        {
            Debug.Log($"✅ Water layer index: {waterLayerMask}");  // ← ใหม่
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🔍 <color=cyan>[Child Trigger]</color> Entered: {other.gameObject.name} | Layer: {LayerMask.LayerToName(other.gameObject.layer)} | LayerIndex: {other.gameObject.layer}");
        }
        
        if (other.gameObject.layer != waterLayerMask)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"⚠️ Not water layer! Expected: {waterLayerMask}, Got: {other.gameObject.layer}");
            }
            return;
        }
        
        if (showDebugLogs) Debug.Log("💧 <color=cyan>Water detected!</color>");
        
        if (swimmer != null && !swimmer.IsSwimming())
        {
            swimmer.EnterWater();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🔍 <color=yellow>[Child Trigger]</color> Exited: {other.gameObject.name}");
        }
        
        if (other.gameObject.layer != waterLayerMask)
        {
            return;
        }
        
        if (showDebugLogs) Debug.Log("🏖️ <color=yellow>Left water!</color>");
        
        if (swimmer != null && swimmer.IsSwimming())
        {
            swimmer.ExitWater();
        }
    }
    
    // ← เพิ่มส่วนนี้ทั้งหมด
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        // วาด Sphere Collider
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
            
            // วาดเส้นไปยัง Parent
            if (transform.parent != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, transform.parent.position);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;
        
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position + sphere.center, sphere.radius * transform.lossyScale.x);
        }
    }
}