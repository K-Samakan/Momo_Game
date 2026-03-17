using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance; // Singleton เพื่อให้เข้าถึงได้จากทุกที่
    public bool CorrectPuzzle;



    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // ทำลาย Instance ซ้ำ
            return;
        }
        Instance = this;
        //DontDestroyOnLoad(gameObject); // เก็บ Inventory ไว้ตลอดทั้งเกม
    }

    private void Update()
    {
        //เพิ่มตัวหรือเงื่่อนไขที่ทำให้CorrectPuzzle=true โดยจะเป็นการแก้puzzleได้ CorrectPuzzle = !CorrectPuzzle;

        
    }
}
