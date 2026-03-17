using UnityEngine;

public class PuzzleTargetTrigger : MonoBehaviour
{
    [Header("Status")]
    public bool hasWaterConnection = false;
    public WaterConnectionPoint connectedPoint = null;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    void OnTriggerEnter(Collider other)
    {
        // ⭐ เช็คว่าเป็น ConnectionPoint หรือไม่
        WaterConnectionPoint connectionPoint = other.GetComponent<WaterConnectionPoint>();
        
        if (connectionPoint != null)
        {
            connectedPoint = connectionPoint;
            UpdateWaterStatus();

            if (showDebugLogs)
            {
                Debug.Log($"╔══════════════════════════════════════════╗");
                Debug.Log($"║ [Target] CONNECTION DETECTED");
                Debug.Log($"╠══════════════════════════════════════════╣");
                Debug.Log($"║  Point: {connectionPoint.gameObject.name}");
                Debug.Log($"║  Has Water: {connectionPoint.hasWater}");
                Debug.Log($"╚══════════════════════════════════════════╝");
            }
        }
    }

    void OnTriggerStay(Collider other)
    {
        // ⭐ อัปเดตสถานะตลอดเวลาที่ชนอยู่
        WaterConnectionPoint connectionPoint = other.GetComponent<WaterConnectionPoint>();
        
        if (connectionPoint != null && connectionPoint == connectedPoint)
        {
            UpdateWaterStatus();
        }
    }

    void OnTriggerExit(Collider other)
    {
        // ⭐ เมื่อ ConnectionPoint ออกจาก Trigger
        WaterConnectionPoint connectionPoint = other.GetComponent<WaterConnectionPoint>();
        
        if (connectionPoint != null && connectionPoint == connectedPoint)
        {
            connectedPoint = null;
            hasWaterConnection = false;

            if (showDebugLogs)
            {
                Debug.Log($"[Target] ❌ Connection lost: {connectionPoint.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// ⭐ อัปเดตสถานะน้ำ
    /// </summary>
    void UpdateWaterStatus()
    {
        if (connectedPoint != null)
        {
            bool previousStatus = hasWaterConnection;
            hasWaterConnection = connectedPoint.hasWater;

            if (previousStatus != hasWaterConnection && showDebugLogs)
            {
                Debug.Log($"[Target] 💧 Water status: {(hasWaterConnection ? "✅ HAS WATER" : "❌ NO WATER")}");
            }
        }
    }

    /// <summary>
    /// ⭐ เช็คว่ามีน้ำหรือไม่
    /// </summary>
    public bool HasWater()
    {
        if (connectedPoint == null)
            return false;

        return connectedPoint.hasWater;
    }

    void OnDrawGizmos()
    {
        // ⭐ วาดกล่อง Target
        Gizmos.color = hasWaterConnection ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);

#if UNITY_EDITOR
        string label = "🎯 TARGET\n";
        label += hasWaterConnection ? "💧 Water" : "⚪ Dry";
        if (connectedPoint != null)
        {
            label += $"\n🔗 {connectedPoint.gameObject.name}";
        }

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
#endif
    }
}