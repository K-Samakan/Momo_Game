using UnityEngine;

public class IceSymbol : MonoBehaviour
{
    [Header("Symbol Settings")]
    public bool hasBlock = false;
    public GameObject activationEffect; // เอฟเฟกต์เมื่อมีบล็อกมาครอบ
    public Material activatedMaterial; // Material เมื่อถูกเปิดใช้งาน
    public Material deactivatedMaterial; // Material ปกติ
    
    private GamePuzzleManager gameManager;
    private Renderer symbolRenderer;
    
    void Start()
    {
        gameManager = FindObjectOfType<GamePuzzleManager>();
        symbolRenderer = GetComponent<Renderer>();
        
        // ตั้งค่า material เริ่มต้น
        if (symbolRenderer != null && deactivatedMaterial != null)
        {
            symbolRenderer.material = deactivatedMaterial;
        }
        
        // ซ่อน activation effect ตอนเริ่ม
        if (activationEffect != null)
            activationEffect.SetActive(false);
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<IceBlock>() != null && !hasBlock)
        {
            hasBlock = true;
            OnBlockPlaced();
            
            if (gameManager != null)
            {
                gameManager.CheckVictoryCondition();
            }
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<IceBlock>() != null && hasBlock)
        {
            hasBlock = false;
            OnBlockRemoved();
        }
    }
    
    void OnBlockPlaced()
    {
        // เปลี่ยน visual เมื่อมีบล็อกมาครอบ
        if (symbolRenderer != null && activatedMaterial != null)
        {
            symbolRenderer.material = activatedMaterial;
        }
        
        // แสดง activation effect
        if (activationEffect != null)
        {
            activationEffect.SetActive(true);
        }
        
        // Haptic feedback เมื่อวางบล็อกลงไป
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPattern(HapticPatternType.Success);
        }
        
        Debug.Log($"Ice Symbol {gameObject.name} activated!");
    }
    
    void OnBlockRemoved()
    {
        // เปลี่ยนกลับเป็น visual ปกติ
        if (symbolRenderer != null && deactivatedMaterial != null)
        {
            symbolRenderer.material = deactivatedMaterial;
        }
        
        // ซ่อน activation effect
        if (activationEffect != null)
        {
            activationEffect.SetActive(false);
        }
        
        Debug.Log($"Ice Symbol {gameObject.name} deactivated!");
    }
    
    // Method สำหรับเช็คสถานะจากภายนอก
    public bool IsActivated()
    {
        return hasBlock;
    }
    
    // Method สำหรับ force activate/deactivate
    public void SetActivation(bool activate)
    {
        if (activate && !hasBlock)
        {
            hasBlock = true;
            OnBlockPlaced();
        }
        else if (!activate && hasBlock)
        {
            hasBlock = false;
            OnBlockRemoved();
        }
    }
}