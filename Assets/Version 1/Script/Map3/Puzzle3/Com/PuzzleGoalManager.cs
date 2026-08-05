// ============================================================
// FILE: PuzzleGoalManager.cs
// PATH: Assets/Version 1/Script/Puzzle3/Com/PuzzleGoalManager.cs
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ✅ จัดการเป้าหมายของ Puzzle (เช็คว่าบล็อกอยู่บน Goal หรือไม่)
/// </summary>
public class PuzzleGoalManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private GameObject[] goalObjects;
    private IceBlock[] allIceBlocks;
    private GameObject puzzleDoor3;

    [Header("Goal Settings")]
    [SerializeField] private int requiredGoals = 3;
    [SerializeField] private float checkRadius = 1f;

    private bool puzzleCompleted = false;

    public void Initialize(PlayerInput input)
    {
        playerInput = input;

        // หา Goal Objects
        goalObjects = GameObject.FindGameObjectsWithTag("Goal");
        Debug.Log($"✅ Found {goalObjects.Length} Goal objects");

        // หา PuzzleDoor3 (ถ้ามี)
        puzzleDoor3 = GameObject.Find("PuzzleDoor3");
        if (puzzleDoor3 != null)
        {
            Debug.Log("✅ Found PuzzleDoor3 GameObject");
        }
        else
        {
            Debug.LogWarning("⚠️ PuzzleDoor3 GameObject not found!");
        }

        RefreshIceBlocksList();
    }

    void RefreshIceBlocksList()
    {
        allIceBlocks = FindObjectsByType<IceBlock>(FindObjectsSortMode.None);
        Debug.Log($"📊 Found {allIceBlocks.Length} IceBlock objects");
    }

    /// <summary>
    /// ✅ ตรวจสอบว่าบล็อกอยู่บนเป้าหมายครบหรือไม่
    /// </summary>
    public void CheckGoals()
    {
        if (puzzleCompleted) return;

        RefreshIceBlocksList();

        int completedGoals = 0;

        // เช็คแต่ละ Goal
        foreach (GameObject goal in goalObjects)
        {
            if (goal == null) continue;

            bool hasBlockOnGoal = false;

            // เช็คว่ามี Block อยู่บน Goal นี้หรือไม่
            foreach (IceBlock block in allIceBlocks)
            {
                if (block == null) continue;

                float distance = Vector3.Distance(block.transform.position, goal.transform.position);
                if (distance <= checkRadius)
                {
                    hasBlockOnGoal = true;
                    Debug.Log($"🎯 Block {block.name} อยู่บน Goal {goal.name} | Distance: {distance:F1}");
                    break;
                }
            }

            if (hasBlockOnGoal)
            {
                completedGoals++;
            }
        }

        Debug.Log($"📊 Goals completed: {completedGoals}/{requiredGoals}");

        // ✅ ถ้าครบทุก Goal → แก้ปริศนาสำเร็จ
        if (completedGoals >= requiredGoals)
        {
            CompletePuzzle();
        }
    }

    void CompletePuzzle()
    {
        puzzleCompleted = true;
        Debug.Log("🎉 Puzzle completed! All goals achieved!");

        // ✅ บันทึกสถานะลง Inventory — ใช้เช็คว่าไอเทมธนูไฟจะปรากฏได้หรือยัง
        if (Inventory.Instance != null)
        {
            Inventory.Instance.CorrectPuzzle3 = true;
            Debug.Log("[PuzzleGoalManager] ✅ CorrectPuzzle3 = true");
        }
        else
        {
            Debug.LogWarning("⚠️ Inventory.Instance not found! ไม่สามารถบันทึก CorrectPuzzle3 ได้");
        }

        // ✅ เรียก IcePuzzleManager
        if (IcePuzzleManager.Instance != null)
        {
            IcePuzzleManager.Instance.OnPuzzleComplete();
        }
        else
        {
            // Fallback: ถ้าไม่มี IcePuzzleManager
            Debug.LogWarning("⚠️ IcePuzzleManager not found! Using fallback...");

            if (playerInput != null)
            {
                playerInput.SwitchCurrentActionMap("Player");
            }

            if (puzzleDoor3 != null)
            {
                puzzleDoor3.SetActive(false);
            }
        }

        ShowCompletionEffect();
    }

    void ShowCompletionEffect()
    {
        Debug.Log("✨ Showing puzzle completion effects");
        // เพิ่ม Particle Effects หรือ Animation ได้ที่นี่
    }

    /// <summary>
    /// ✅ รีเซ็ต Puzzle
    /// </summary>
    public void ResetPuzzle()
    {
        puzzleCompleted = false;

        if (puzzleDoor3 != null)
        {
            puzzleDoor3.SetActive(true);
        }

        Debug.Log("🔄 Puzzle reset");
    }

    #region Gizmos

    private void OnDrawGizmos()
    {
        if (goalObjects == null || goalObjects.Length == 0) return;

#if UNITY_EDITOR
        foreach (GameObject goal in goalObjects)
        {
            if (goal == null) continue;

            // วงกลมแสดงระยะตรวจจับ
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(goal.transform.position, checkRadius);

            // Label
            UnityEditor.Handles.Label(
                goal.transform.position + Vector3.up,
                $"🎯 {goal.name}"
            );
        }
#endif
    }

    #endregion
}