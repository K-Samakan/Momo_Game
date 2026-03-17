using UnityEngine;

public class WaterTile : MonoBehaviour
{
    [Header("Tile Settings")]
    public Transform tileFloor;
    public float rotationAngle = 90f;

    [Header("Model Settings")]
    [Tooltip("Offset ของ Model Rotation (แก้ไขถ้า Model ไม่ตรงกับ Code)")]
    public float modelRotationOffset = 0f;

    [Header("Pipe Type")]
    public PipeType pipeType = PipeType.Straight;

    [Header("Water State")]
    [SerializeField] private PipeStateController stateController;

    [Header("Connection Points")]
    public WaterConnectionPoint[] connectionPoints;

    [Header("Tile State")]
    public bool isRotatable = false;

    // Property สำหรับเข้าถึงสถานะน้ำ
    public bool hasWater
    {
        get { return stateController != null ? stateController.HasWater : false; }
        set { if (stateController != null) stateController.SetWaterState(value); }
    }

    private Quaternion initialRotation;
    public float currentAngle = 0f;

    public enum PipeType
    {
        Straight,   // ท่อตรง (I)
        Curved,     // ท่อโค้ง (L)
        TJunction   // ท่อ T
    }

    #region Initialization

    void Start()
    {
        if (tileFloor == null)
        {
            tileFloor = transform;
        }

        // หา PipeStateController
        if (stateController == null)
        {
            stateController = GetComponent<PipeStateController>();
            if (stateController == null)
            {
                Debug.LogWarning($"[{gameObject.name}] No PipeStateController found! Adding one...");
                stateController = gameObject.AddComponent<PipeStateController>();
            }
        }

        initialRotation = tileFloor.rotation;
        currentAngle = tileFloor.rotation.eulerAngles.y;
        currentAngle = currentAngle % 360f;
        if (currentAngle < 0) currentAngle += 360f;

        // หา Connection Points ทั้งหมด
        connectionPoints = GetComponentsInChildren<WaterConnectionPoint>();
        Debug.Log($"[{gameObject.name}] Found {connectionPoints.Length} connection points");

        Debug.Log($"[{gameObject.name}] Initialized | Type: {pipeType} | Angle: {currentAngle}°");
    }

    #endregion

    #region Water Management

    /// <summary>
    /// ตั้งค่าสถานะน้ำและอัปเดต Connection Points
    /// </summary>
    public void SetWaterStatus(bool waterStatus)
    {
        hasWater = waterStatus;

        // อัปเดตทุก Connection Points ใน Tile นี้
        foreach (WaterConnectionPoint point in connectionPoints)
        {
            if (point != null)
            {
                point.SetWaterStatus(waterStatus);
            }
        }

        Debug.Log($"[{gameObject.name}] Tile water status: {(hasWater ? "💧 HAS WATER" : "⚪ NO WATER")}");
    }

    /// <summary>
    /// อัปเดตการเชื่อมต่อของทุก Connection Points
    /// </summary>
    public void UpdateConnectionPoints()
    {
        foreach (WaterConnectionPoint point in connectionPoints)
        {
            if (point != null)
            {
                point.DetectConnection();
            }
        }
    }

    /// <summary>
    /// รีเซ็ตทุก Connection Points
    /// </summary>
    public void ResetConnectionPoints()
    {
        foreach (WaterConnectionPoint point in connectionPoints)
        {
            if (point != null)
            {
                point.ResetWater();
            }
        }
    }

    #endregion

    #region Rotation

    /// <summary>
    /// รีเซ็ต Tile กลับสู่สถานะเริ่มต้น
    /// </summary>
    public void ResetTile()
    {
        tileFloor.rotation = initialRotation;
        currentAngle = initialRotation.eulerAngles.y;
        currentAngle = currentAngle % 360f;
        if (currentAngle < 0) currentAngle += 360f;

        SetWaterStatus(false);
        ResetConnectionPoints();

        Debug.Log($"[{gameObject.name}] 🔄 Reset → {currentAngle}°");
    }

    /// <summary>
    /// หมุนท่อตามเข็มนาฬิกา (+90°)
    /// </summary>
    public void RotateClockwise()
    {
        if (!isRotatable) return;

        Rotate(rotationAngle);
        UpdateConnectionPoints();
        Debug.Log($"[{gameObject.name}] 🔄 +90° → {currentAngle}°");
    }

    /// <summary>
    /// หมุนท่อทวนเข็มนาฬิกา (-90°)
    /// </summary>
    public void RotateCounterClockwise()
    {
        if (!isRotatable) return;

        Rotate(-rotationAngle);
        UpdateConnectionPoints();
        Debug.Log($"[{gameObject.name}] 🔄 -90° → {currentAngle}°");
    }

    /// <summary>
    /// หมุนท่อ
    /// </summary>
    void Rotate(float angle)
    {
        currentAngle += angle;
        currentAngle = currentAngle % 360f;
        if (currentAngle < 0) currentAngle += 360f;

        tileFloor.Rotate(Vector3.up, angle, Space.World);
    }

    #endregion

    #region Direction Logic

    /// <summary>
    /// ดึงทิศทางที่ท่อเชื่อมต่อ (World Space)
    /// </summary>
    public Vector3[] GetOutputDirections()
    {
        // คำนวณมุมจริง (Model Rotation + Offset)
        float logicAngle = (currentAngle + modelRotationOffset) % 360f;
        if (logicAngle < 0) logicAngle += 360f;

        Vector3[] outputs;

        switch (pipeType)
        {
            case PipeType.Straight:
                float straightAngle = logicAngle % 180f;
                if (Mathf.Approximately(straightAngle, 0f))
                {
                    // North-South (┃)
                    outputs = new Vector3[] { Vector3.forward, Vector3.back };
                }
                else
                {
                    // East-West (━)
                    outputs = new Vector3[] { Vector3.right, Vector3.left };
                }
                break;

            case PipeType.Curved:
                if (logicAngle >= 315f || logicAngle < 45f)
                {
                    // ┗ (North + East)
                    outputs = new Vector3[] { Vector3.forward, Vector3.right };
                }
                else if (logicAngle >= 45f && logicAngle < 135f)
                {
                    // ┏ (East + South)
                    outputs = new Vector3[] { Vector3.right, Vector3.back };
                }
                else if (logicAngle >= 135f && logicAngle < 225f)
                {
                    // ┓ (South + West)
                    outputs = new Vector3[] { Vector3.back, Vector3.left };
                }
                else
                {
                    // ┛ (West + North)
                    outputs = new Vector3[] { Vector3.left, Vector3.forward };
                }
                break;

            case PipeType.TJunction:
                if (logicAngle < 45f || logicAngle >= 315f)
                {
                    // ┳ (North + East + West)
                    outputs = new Vector3[] { Vector3.forward, Vector3.right, Vector3.left };
                }
                else if (logicAngle >= 45f && logicAngle < 135f)
                {
                    // ┣ (East + North + South)
                    outputs = new Vector3[] { Vector3.right, Vector3.forward, Vector3.back };
                }
                else if (logicAngle >= 135f && logicAngle < 225f)
                {
                    // ┻ (South + East + West)
                    outputs = new Vector3[] { Vector3.back, Vector3.right, Vector3.left };
                }
                else
                {
                    // ┫ (West + North + South)
                    outputs = new Vector3[] { Vector3.left, Vector3.forward, Vector3.back };
                }
                break;

            default:
                outputs = new Vector3[0];
                break;
        }

        return outputs;
    }

    /// <summary>
    /// ตรวจสอบว่าท่อสามารถรับน้ำจากทิศทางนี้ได้หรือไม่
    /// </summary>
    public bool CanAcceptWaterFrom(Vector3 incomingDirection)
    {
        incomingDirection = incomingDirection.normalized;
        return HasConnectionInWorldDirection(incomingDirection);
    }

    /// <summary>
    /// ตรวจสอบว่ามีการเชื่อมต่อในทิศทางนี้หรือไม่
    /// </summary>
    bool HasConnectionInWorldDirection(Vector3 worldDirection)
    {
        float logicAngle = (currentAngle + modelRotationOffset) % 360f;
        if (logicAngle < 0) logicAngle += 360f;

        float threshold = 0.3f;

        switch (pipeType)
        {
            case PipeType.Straight:
                if (Mathf.Approximately(logicAngle % 180f, 0f))
                {
                    // North-South (┃) - รับน้ำจาก North หรือ South
                    return Vector3.Dot(worldDirection, Vector3.forward) > threshold ||
                           Vector3.Dot(worldDirection, Vector3.back) > threshold;
                }
                else
                {
                    // East-West (━) - รับน้ำจาก East หรือ West
                    return Vector3.Dot(worldDirection, Vector3.right) > threshold ||
                           Vector3.Dot(worldDirection, Vector3.left) > threshold;
                }

            case PipeType.Curved:
                // Curved pipe สามารถรับน้ำจาก 2 ทิศทางได้
                if (logicAngle < 45f || logicAngle >= 315f)
                {
                    // ┗ (North + East)
                    return Vector3.Dot(worldDirection, Vector3.forward) > threshold ||
                           Vector3.Dot(worldDirection, Vector3.right) > threshold;
                }
                else if (logicAngle >= 45f && logicAngle < 135f)
                {
                    // ┏ (East + South)
                    return Vector3.Dot(worldDirection, Vector3.right) > threshold ||
                           Vector3.Dot(worldDirection, Vector3.back) > threshold;
                }
                else if (logicAngle >= 135f && logicAngle < 225f)
                {
                    // ┓ (South + West)
                    return Vector3.Dot(worldDirection, Vector3.back) > threshold ||
                           Vector3.Dot(worldDirection, Vector3.left) > threshold;
                }
                else
                {
                    // ┛ (West + North)
                    return Vector3.Dot(worldDirection, Vector3.left) > threshold ||
                           Vector3.Dot(worldDirection, Vector3.forward) > threshold;
                }

            case PipeType.TJunction:
                // T-Junction สามารถรับน้ำจาก 3 ทิศทางได้ (ทุกทิศทางยกเว้นหนึ่งทิศทาง)
                if (logicAngle < 45f || logicAngle >= 315f)
                {
                    // ┳ (North + East + West) - ไม่รับจาก South
                    return Vector3.Dot(worldDirection, Vector3.back) < threshold;
                }
                else if (logicAngle >= 45f && logicAngle < 135f)
                {
                    // ┣ (East + North + South) - ไม่รับจาก West
                    return Vector3.Dot(worldDirection, Vector3.left) < threshold;
                }
                else if (logicAngle >= 135f && logicAngle < 225f)
                {
                    // ┻ (South + East + West) - ไม่รับจาก North
                    return Vector3.Dot(worldDirection, Vector3.forward) < threshold;
                }
                else
                {
                    // ┫ (West + North + South) - ไม่รับจาก East
                    return Vector3.Dot(worldDirection, Vector3.right) < threshold;
                }
        }

        return false;
    }

    #endregion

    #region Gizmos

    void OnDrawGizmos()
    {
        if (tileFloor == null) tileFloor = transform;

        // กล่องแสดง Tile
        Gizmos.color = isRotatable ? Color.magenta : Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);

        // แสดงทิศทางการเชื่อมต่อ
        if (Application.isPlaying)
        {
            Vector3[] outputs = GetOutputDirections();

            for (int i = 0; i < outputs.Length; i++)
            {
                Gizmos.color = hasWater ? Color.blue : Color.yellow;

                Vector3 start = transform.position + Vector3.up * 0.1f;
                Vector3 end = start + outputs[i] * 0.8f;

                Gizmos.DrawLine(start, end);
                Gizmos.DrawSphere(end, 0.1f);
            }
        }

#if UNITY_EDITOR
        // แสดง Label
        string label = $"{gameObject.name}\n{pipeType}\n{currentAngle}°";
        if (hasWater) label += "\n💧";

        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, label);
#endif
    }

    #endregion
}