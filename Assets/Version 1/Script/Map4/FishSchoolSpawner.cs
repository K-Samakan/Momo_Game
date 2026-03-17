using UnityEngine;

public class FishSchoolSpawner : MonoBehaviour
{
    [Header("=== Spawn Settings ===")]
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private int fishCount = 50;
    [SerializeField] private Vector3 spawnArea = new Vector3(20, 10, 20);
    
    [Header("=== Variation ===")]
    [SerializeField] private Vector2 speedRange = new Vector2(2f, 4f);
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);
    
    void Start()
    {
        SpawnFishSchool();
    }
    
    private void SpawnFishSchool()
    {
        for (int i = 0; i < fishCount; i++)
        {
            // สุ่มตำแหน่ง
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
                Random.Range(-spawnArea.y / 2, spawnArea.y / 2),
                Random.Range(-spawnArea.z / 2, spawnArea.z / 2)
            );
            
            // สุ่มมุม
            Quaternion spawnRot = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            
            // Spawn
            GameObject fish = Instantiate(fishPrefab, spawnPos, spawnRot, transform);
            fish.name = $"BoidFish_{i}";
            
            // สุ่ม Scale
            float scale = Random.Range(scaleRange.x, scaleRange.y);
            fish.transform.localScale = Vector3.one * scale;
            
            // สุ่ม Speed
            BoidFish boidFish = fish.GetComponent<BoidFish>();
            if (boidFish != null)
            {
                // ใช้ Reflection เพื่อตั้งค่า private field
                var speedField = typeof(BoidFish).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (speedField != null)
                {
                    speedField.SetValue(boidFish, Random.Range(speedRange.x, speedRange.y));
                }
            }
            
            // ตั้ง Layer
            fish.layer = LayerMask.NameToLayer("Fish");
        }
        
        Debug.Log($"✅ Spawned {fishCount} boid fish");
    }
}