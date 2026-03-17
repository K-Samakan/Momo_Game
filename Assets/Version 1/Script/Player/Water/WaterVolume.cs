using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class WaterVolume : MonoBehaviour
{
    [Header("=== Water Properties ===")]
    [SerializeField] private Color waterColor = new Color(0.2f, 0.4f, 0.6f, 0.3f);
    [SerializeField] private float waterSurfaceLevel = 0f;
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool useCollisionInsteadOfTrigger = false; // ← ใหม่
    
    private BoxCollider waterCollider;
    
    void Awake()
    {
        waterCollider = GetComponent<BoxCollider>();
        waterCollider.isTrigger = !useCollisionInsteadOfTrigger; // ← แก้
        
        if (gameObject.layer == 0)
        {
            int waterLayer = LayerMask.NameToLayer("Water");
            if (waterLayer != -1)
            {
                gameObject.layer = waterLayer;
            }
        }
        
        float scaledHeight = waterCollider.size.y * transform.lossyScale.y;
        waterSurfaceLevel = transform.position.y + (scaledHeight / 2f);
        
        if (showDebugLogs) Debug.Log($"✅ WaterVolume created at Y={waterSurfaceLevel:F2}");
        
        // เพิ่ม Rigidbody ถ้าใช้ Collision
        if (useCollisionInsteadOfTrigger)
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    // OnTriggerEnter (เดิม)
    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"🔍 <color=cyan>[TRIGGER]</color> Entered by: {other.gameObject.name} | Tag: {other.tag}");
        HandleWaterEntry(other.gameObject);
    }
    
    void OnTriggerExit(Collider other)
    {
        Debug.Log($"🔍 <color=yellow>[TRIGGER]</color> Exited by: {other.gameObject.name}");
        HandleWaterExit(other.gameObject);
    }
    
    // OnCollisionEnter (ใหม่ - สำรอง)
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"🔍 <color=cyan>[COLLISION]</color> Entered by: {collision.gameObject.name} | Tag: {collision.gameObject.tag}");
        HandleWaterEntry(collision.gameObject);
    }
    
    void OnCollisionExit(Collision collision)
    {
        Debug.Log($"🔍 <color=yellow>[COLLISION]</color> Exited by: {collision.gameObject.name}");
        HandleWaterExit(collision.gameObject);
    }
    
    // Logic ร่วมกัน
    private void HandleWaterEntry(GameObject obj)
    {
        GameObject targetObject = obj;
        
        if (!targetObject.CompareTag("Player"))
        {
            Transform parent = targetObject.transform.parent;
            while (parent != null)
            {
                if (parent.CompareTag("Player"))
                {
                    targetObject = parent.gameObject;
                    Debug.Log($"✅ Found Player in parent: {targetObject.name}");
                    break;
                }
                parent = parent.parent;
            }
        }
        
        if (!targetObject.CompareTag("Player"))
        {
            Debug.LogWarning($"⚠️ Not a Player! Tag is: {obj.tag}, Name: {obj.name}");
            return;
        }
        
        if (showDebugLogs) Debug.Log($"👤 <color=cyan>{targetObject.name} entered water</color>");
        
        SwimmingControllerHybrid swimmer = targetObject.GetComponent<SwimmingControllerHybrid>();
        if (swimmer != null)
        {
            swimmer.EnterWater();
        }
        else
        {
            Debug.LogError($"❌ {targetObject.name} doesn't have SwimmingControllerHybrid component!");
        }
    }
    
    private void HandleWaterExit(GameObject obj)
    {
        GameObject targetObject = obj;
        
        if (!targetObject.CompareTag("Player"))
        {
            Transform parent = targetObject.transform.parent;
            while (parent != null)
            {
                if (parent.CompareTag("Player"))
                {
                    targetObject = parent.gameObject;
                    break;
                }
                parent = parent.parent;
            }
        }
        
        if (!targetObject.CompareTag("Player"))
        {
            return;
        }
        
        if (showDebugLogs) Debug.Log($"👤 <color=yellow>{targetObject.name} exited water</color>");
        
        SwimmingControllerHybrid swimmer = targetObject.GetComponent<SwimmingControllerHybrid>();
        if (swimmer != null)
        {
            swimmer.ExitWater();
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos) return;
        
        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null) return;
        
        Gizmos.color = waterColor;
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawCube(col.center, col.size);
        
        Gizmos.color = Color.cyan;
        Vector3 surfaceCenter = col.center + new Vector3(0, col.size.y / 2f, 0);
        Gizmos.DrawWireCube(surfaceCenter, new Vector3(col.size.x, 0.05f, col.size.z));
        
        Gizmos.matrix = Matrix4x4.identity;
    }
}
