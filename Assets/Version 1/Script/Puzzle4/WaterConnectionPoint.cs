using UnityEngine;
using System.Collections;

public class WaterConnectionPoint : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float detectionRadius = 3f;
    [SerializeField] private float checkInterval = 0.5f;
    [SerializeField] private float waterTimeout = 0.3f;
    [SerializeField] private float endPipeTimeout = 1.5f; // ⭐ เพิ่มบรรทัดนี้!

    [Header("Status")]
    public bool hasWater = false;
    public bool isConnected = false;
    public WaterConnectionPoint connectedPoint = null;

    [Header("References")]
    public WaterTile parentTile;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private Coroutine checkCoroutine;
    private float lastConnectionTime = 0f;

    #region Initialization

    void Start()
    {
        parentTile = GetComponentInParent<WaterTile>();

        if (parentTile == null)
        {
            Debug.LogError($"[{gameObject.name}] ❌ NO PARENT TILE!");
            return;
        }

        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.isTrigger = true;
        col.radius = detectionRadius;

        lastConnectionTime = Time.time;

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] ✅ INIT | Parent: {parentTile.gameObject.name}");
        }

        StartAutoCheck();
    }

    void OnDestroy()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
        }
    }

    #endregion

    #region Auto Check

    void StartAutoCheck()
    {
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
        }
        checkCoroutine = StartCoroutine(AutoCheckRoutine());
    }

    IEnumerator AutoCheckRoutine()
    {
        while (true)
        {
            DetectConnection();
            CheckWaterPropagation();
            CheckWaterTimeout();
            
            yield return new WaitForSeconds(checkInterval);
        }
    }

    #endregion

    #region Water Propagation

    void CheckWaterPropagation()
    {
        if (hasWater && isConnected && connectedPoint != null && !connectedPoint.hasWater)
        {
            PropagateWater();
        }
    }

    void PropagateWater()
    {
        if (connectedPoint == null)
        {
            if (showDebugLogs)
                Debug.LogWarning($"[{gameObject.name}] ❌ Cannot propagate - no connection");
            return;
        }

        if (connectedPoint.hasWater)
        {
            return;
        }

        if (connectedPoint.parentTile == null)
        {
            if (showDebugLogs)
                Debug.LogError($"[{gameObject.name}] ❌ {connectedPoint.gameObject.name} has NO PARENT!");
            return;
        }

        Vector3 waterDirection = (connectedPoint.transform.position - transform.position).normalized;

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 🌊 → [{connectedPoint.gameObject.name}]");
        }

        bool canAccept = true;

        if (canAccept)
        {
            connectedPoint.ReceiveWater();
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning($"[{gameObject.name}] ❌ Blocked by direction");
        }
    }

    public void ReceiveWater()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 💧 Receiving water");
        }

        SetWaterStatus(true);

        if (parentTile != null)
        {
            parentTile.SetWaterStatus(true);
        }
    }

    #endregion

    #region Water Timeout

    void CheckWaterTimeout()
    {
        // ข้ามถ้าเป็น Source
        if (parentTile != null)
        {
            PipeStateController state = parentTile.GetComponent<PipeStateController>();
            if (state != null && state.IsSource)
            {
                return;
            }
        }

        // ⭐ เช็คว่ามี ConnectionPoint อื่นที่เชื่อมกับน้ำหรือไม่ (บอกว่าเป็นท่อสุดทาง)
        bool hasWaterConnection = false;
        
        if (parentTile != null)
        {
            foreach (WaterConnectionPoint otherPoint in parentTile.connectionPoints)
            {
                if (otherPoint != null && 
                    otherPoint != this && 
                    otherPoint.isConnected && 
                    otherPoint.connectedPoint != null && 
                    otherPoint.connectedPoint.hasWater)
                {
                    hasWaterConnection = true;
                    break;
                }
            }
        }

        // ถ้าเชื่อมกับจุดที่มีน้ำ → รีเซ็ต Timer
        if (isConnected && connectedPoint != null && connectedPoint.hasWater)
        {
            lastConnectionTime = Time.time;
            return;
        }

        // ⭐ กำหนด Timeout
        // - ถ้ามี ConnectionPoint อื่นเชื่อมกับน้ำ = ท่อสุดทาง → ใช้ endPipeTimeout (1.5s)
        // - ถ้าไม่มี = ท่อที่หมุนออก → ใช้ waterTimeout (0.3s)
        float currentTimeout = hasWaterConnection ? endPipeTimeout : waterTimeout;

        // ถ้ามีน้ำ แต่ไม่เชื่อม + เกิน Timeout → ลบน้ำ
        if (hasWater && Time.time - lastConnectionTime > currentTimeout)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[{gameObject.name}] ⏱️ Timeout ({currentTimeout}s)! Removing water");
            }

            SetWaterStatus(false);

            if (parentTile != null)
            {
                parentTile.SetWaterStatus(false);
            }
        }
    }

    /// <summary>
    /// ⭐ ตรวจสอบและลบน้ำทันที (เรียกจาก WaterTile เมื่อหมุน)
    /// </summary>
    public void CheckAndRemoveWater()
    {
        // ข้ามถ้าเป็น Source
        if (parentTile != null)
        {
            PipeStateController state = parentTile.GetComponent<PipeStateController>();
            if (state != null && state.IsSource)
            {
                return;
            }
        }

        // ถ้าไม่มีน้ำอยู่แล้ว → ข้าม
        if (!hasWater)
        {
            return;
        }

        // ⭐ ถ้ามีน้ำ แต่ไม่เชื่อมต่อ
        if (!isConnected)
        {
            // ⭐ เช็คว่ามี ConnectionPoint อื่นใน Tile เดียวกันที่เชื่อมกับท่อที่มีน้ำหรือไม่
            bool hasWaterConnection = false;
            
            if (parentTile != null)
            {
                foreach (WaterConnectionPoint otherPoint in parentTile.connectionPoints)
                {
                    if (otherPoint != null && 
                        otherPoint != this && 
                        otherPoint.isConnected && 
                        otherPoint.connectedPoint != null && 
                        otherPoint.connectedPoint.hasWater)
                    {
                        hasWaterConnection = true;
                        break;
                    }
                }
            }

            // ⭐ ถ้าไม่มี ConnectionPoint ไหนเชื่อมกับน้ำ → ลบน้ำ
            if (!hasWaterConnection)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[{gameObject.name}] 🔄 Removing water (no water connection)");
                }

                SetWaterStatus(false);

                if (parentTile != null)
                {
                    parentTile.SetWaterStatus(false);
                }
            }

            return;
        }

        // ⭐ ถ้าเชื่อมต่อ แต่ ConnectedPoint ไม่มีน้ำ
        if (isConnected && connectedPoint != null && !connectedPoint.hasWater)
        {
            // เช็คว่า ConnectedPoint เป็น Source หรือไม่
            bool connectedIsSource = false;
            if (connectedPoint.parentTile != null)
            {
                PipeStateController state = connectedPoint.parentTile.GetComponent<PipeStateController>();
                if (state != null && state.IsSource)
                {
                    connectedIsSource = true;
                }
            }

            if (!connectedIsSource)
            {
                // ⭐ เช็คว่ามี ConnectionPoint อื่นที่มีน้ำหรือไม่
                bool hasOtherWaterSource = false;
                
                if (parentTile != null)
                {
                    foreach (WaterConnectionPoint otherPoint in parentTile.connectionPoints)
                    {
                        if (otherPoint != null && 
                            otherPoint != this && 
                            otherPoint.isConnected && 
                            otherPoint.connectedPoint != null && 
                            otherPoint.connectedPoint.hasWater)
                        {
                            hasOtherWaterSource = true;
                            break;
                        }
                    }
                }

                if (!hasOtherWaterSource)
                {
                    if (showDebugLogs)
                    {
                        Debug.LogWarning($"[{gameObject.name}] 🔄 Removing water (no other water source)");
                    }

                    SetWaterStatus(false);

                    if (parentTile != null)
                    {
                        parentTile.SetWaterStatus(false);
                    }
                }
            }
        }
    }

    #endregion

    #region Connection Detection

    public void DetectConnection()
    {
        WaterConnectionPoint oldConnection = connectedPoint;
        isConnected = false;
        connectedPoint = null;

        WaterConnectionPoint[] allPoints = FindObjectsOfType<WaterConnectionPoint>();
        WaterConnectionPoint bestMatch = null;
        float closestDistance = float.MaxValue;

        foreach (WaterConnectionPoint other in allPoints)
        {
            if (other == this) continue;
            if (other.parentTile == this.parentTile) continue;
            if (other.parentTile == null) continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);

            if (distance <= detectionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                bestMatch = other;
            }
        }

        if (bestMatch != null)
        {
            isConnected = true;
            connectedPoint = bestMatch;
            bestMatch.SetConnection(this);

            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] ✅ Connected to [{connectedPoint.gameObject.name}] ({closestDistance:F2}m)");
            }
        }
    }

    public void SetConnection(WaterConnectionPoint otherPoint)
    {
        if (otherPoint != null && otherPoint != this && otherPoint.parentTile != this.parentTile)
        {
            connectedPoint = otherPoint;
            isConnected = true;
            lastConnectionTime = Time.time;
        }
    }

    #endregion

    #region Water Status

    public void SetWaterStatus(bool waterStatus)
    {
        if (hasWater == waterStatus)
        {
            return;
        }

        hasWater = waterStatus;

        if (hasWater)
        {
            lastConnectionTime = Time.time;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 💧 {(hasWater ? "HAS" : "NONE")}");
        }

        if (hasWater && isConnected && connectedPoint != null && !connectedPoint.hasWater)
        {
            PropagateWater();
        }
    }

    public void ResetWater()
    {
        hasWater = false;
        isConnected = false;
        connectedPoint = null;
        lastConnectionTime = Time.time;
    }

    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        Gizmos.color = isConnected ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = hasWater ? Color.cyan : Color.gray;
        Gizmos.DrawRay(transform.position, transform.forward * 0.3f);

        if (isConnected && connectedPoint != null)
        {
            Gizmos.color = hasWater ? Color.blue : Color.yellow;
            Gizmos.DrawLine(transform.position, connectedPoint.transform.position);
        }

#if UNITY_EDITOR
        string label = $"{gameObject.name}\n";
        label += hasWater ? "💧" : "⚪";
        if (isConnected) label += $"\n🔗 {connectedPoint?.gameObject.name}";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.3f, label);
#endif
    }

    #endregion
}