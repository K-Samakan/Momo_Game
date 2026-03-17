using UnityEngine;
using UnityEngine.InputSystem;
public class GamePuzzleManager2 : MonoBehaviour
{
    public static GamePuzzleManager2 Instance;
    
   [Header("Level Settings")]
    public SokobanTarget[] targets;
    public int totalBlocks;
    
    private int completedTargets = 0;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void CheckLevelComplete()
    {
        completedTargets = 0;
        
        foreach (SokobanTarget target in targets)
        {
            if (target.IsOccupied())
            {
                completedTargets++;
            }
        }
        
        if (completedTargets == targets.Length)
        {
            LevelComplete();
        }
    }
    
    void LevelComplete()
    {
        Debug.Log("Level Complete!");
        // เพิ่มฟังก์ชันการผ่านด่าน เช่น UI, เสียง, หรือไปด่านถัดไป
    }
}