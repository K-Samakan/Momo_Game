using UnityEngine;
using UnityEngine.InputSystem;

// วางสคริปต์นี้ที่ "แท่นกด" หน้าห้อง puzzle (ต้องมี Collider แบบ IsTrigger)
public class Puzzle2WarpTrigger : MonoBehaviour
{
    [Header("References")]
    public MirrorInteractionManager puzzleManager;   // ลาก manager มาใส่

    private bool playerInRange = false;
    private bool isLocked = false;   // true เมื่อแก้ puzzle สำเร็จแล้ว -> กดไม่ได้อีก

    void Update()
    {
        if (isLocked || !playerInRange) return;
        if (Gamepad.current == null) return;

        // ⭕ Circle (ปุ่มขวาของจอย PS5)
        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            Debug.Log("[⭕ Circle] Warping to Puzzle2 room...");
            if (puzzleManager != null)
                puzzleManager.EnterPuzzle();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isLocked) return;
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("🚪 [Puzzle2] กด ⭕ Circle เพื่อเข้าห้องปริศนา");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

    // เรียกจาก manager ตอนแก้ puzzle สำเร็จ -> ล็อกแท่นถาวร
    public void LockPedestal()
    {
        isLocked = true;
        playerInRange = false;
        Debug.Log("🔒 [Puzzle2] แท่นกดถูกล็อกแล้ว (แก้ puzzle สำเร็จ)");
    }
}