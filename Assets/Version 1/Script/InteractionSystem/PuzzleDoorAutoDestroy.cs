using UnityEngine;

public class PuzzleDoorAutoDestroy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ตรวจสอบว่า Inventory มีอยู่และแก้ Puzzle สำเร็จ
        if (Inventory.Instance != null && Inventory.Instance.CorrectPuzzle)
        {
            Debug.Log("Puzzle solved. Destroying door.");
            Destroy(gameObject); // ทำลายประตู
        }
        else
        {
            Debug.Log("Puzzle not solved. Door remains.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
