using UnityEngine;

public class PipeStateController : MonoBehaviour
{
    [Header("Pipe State")]
    [SerializeField] private bool hasWater = false;
    [SerializeField] private bool isSource = false;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hasWaterParameter = "HasWater"; // ⭐ Parameter name
    
    [Header("Visual")]
    [SerializeField] private GameObject waterOverlay;
    [SerializeField] private Renderer waterRenderer;
    [SerializeField] private Color flowingColor = new Color(0, 0.5f, 1f, 0.7f);
    [SerializeField] private Color idleColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    #region Initialization

    private void Awake()
    {
        // ✅ หา Animator
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            
            if (animator == null)
            {
                Debug.LogWarning($"[{gameObject.name}] ⚠️ No Animator found!");
            }
        }
        
        // ✅ หา Water Overlay
        if (waterOverlay == null)
        {
            Transform overlay = transform.Find("WaterOverlay");
            if (overlay != null)
            {
                waterOverlay = overlay.gameObject;
                waterRenderer = waterOverlay.GetComponent<Renderer>();
            }
        }
        
        // ✅ Source ต้องมีน้ำตลอด
        if (isSource)
        {
            SetWaterState(true);
        }
    }

    private void Start()
    {
        UpdateVisual();
        
        // ⭐ ตั้งค่า Animator Parameter เริ่มต้น
        if (animator != null)
        {
            animator.SetBool(hasWaterParameter, hasWater);
        }
    }

    #endregion

    #region Water State

    /// <summary>
    /// ✅ เปลี่ยนสถานะน้ำของท่อ
    /// </summary>
    public void SetWaterState(bool newWaterState)
    {
        // ✅ Source ต้องมีน้ำตลอด
        if (isSource && !newWaterState)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] ℹ️ Cannot remove water from SOURCE");
            }
            return;
        }
        
        // ✅ ถ้าสถานะไม่เปลี่ยน → ข้าม
        if (this.hasWater == newWaterState)
        {
            return;
        }
        
        bool wasFlowing = this.hasWater;
        this.hasWater = newWaterState;
        
        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 💧 Water State: {(hasWater ? "✅ HAS WATER" : "❌ NO WATER")}");
        }
        
        UpdateVisual();
        UpdateAnimator(); // ⭐ อัปเดต Animator
    }

    /// <summary>
    /// ✅ อัปเดต Visual (Overlay, Material)
    /// </summary>
    private void UpdateVisual()
    {
        if (waterOverlay != null)
        {
            waterOverlay.SetActive(hasWater);
        }
        
        if (waterRenderer != null)
        {
            Color color = hasWater ? flowingColor : idleColor;
            waterRenderer.material.color = color;
        }
    }

    /// <summary>
    /// ⭐ อัปเดต Animator Parameter
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[{gameObject.name}] ⚠️ No Animator - skipping animation");
            }
            return;
        }

        // ⭐ ตั้งค่า Bool Parameter
        animator.SetBool(hasWaterParameter, hasWater);

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] 🎬 Animator Parameter '{hasWaterParameter}' = {hasWater}");
        }
    }

    #endregion

    #region Public Accessors

    public bool HasWater => hasWater;
    public bool IsSource => isSource;

    /// <summary>
    /// ✅ ตั้งค่าให้เป็น Source (ต้นน้ำ)
    /// </summary>
    public void SetAsSource(bool source)
    {
        isSource = source;
        if (isSource)
        {
            SetWaterState(true);
            
            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] 🌊 Set as WATER SOURCE");
            }
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        Gizmos.color = hasWater ? Color.cyan : Color.gray;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
        
        if (isSource)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.3f);
        }

#if UNITY_EDITOR
        string label = gameObject.name;
        if (isSource) label += "\n🌊 SOURCE";
        if (hasWater) label += "\n💧 Water";
        
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, label);
#endif
    }

    #endregion
}