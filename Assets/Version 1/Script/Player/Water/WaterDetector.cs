using UnityEngine;

public class WaterDetector : MonoBehaviour
{
    [SerializeField] private Transform waterSurfaceTransform;
    [SerializeField] private float waterSurfaceY = 5f;
    [SerializeField] private float detectionInterval = 0.1f;
    
    private SwimmingControllerHybrid swimmer;
    private bool wasInWater = false;
    private float lastCheckTime;
    
    void Start()
    {
        swimmer = GetComponent<SwimmingControllerHybrid>();
        
        if (waterSurfaceTransform != null)
        {
            waterSurfaceY = waterSurfaceTransform.position.y;
        }
        
        Debug.Log($"WaterDetector initialized. Water surface at Y={waterSurfaceY}");
    }
    
    void Update()
    {
        if (Time.time - lastCheckTime < detectionInterval) return;
        lastCheckTime = Time.time;
        
        // ตรวจสอบว่าอยู่ใต้ผิวน้ำหรือไม่
        bool isInWater = transform.position.y < waterSurfaceY;
        
        // เปลี่ยนสถานะ
        if (isInWater && !wasInWater)
        {
            Debug.Log("🌊 Entered water (Manual Detection)");
            if (swimmer != null) swimmer.EnterWater();
            wasInWater = true;
        }
        else if (!isInWater && wasInWater)
        {
            Debug.Log("🏖️ Exited water (Manual Detection)");
            if (swimmer != null) swimmer.ExitWater();
            wasInWater = false;
        }
    }
}