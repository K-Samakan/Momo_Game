using UnityEngine;
using UnityEngine.InputSystem;

public class Valve : MonoBehaviour
{
    [Header("Valve Settings")]
    public WaterTile connectedTile; // Tile ที่วาล์วนี้ควบคุม
    public int maxRotations = 5;
    public int currentRotationCount = 0;
    public bool isBroken = false;

    private WaterValvePuzzleManager puzzleManager;

    void Start()
    {
        puzzleManager = FindObjectOfType<WaterValvePuzzleManager>();

        Debug.Log($"[{gameObject.name}] Initialized | Max Rotations: {maxRotations} | Connected Tile: {(connectedTile != null ? connectedTile.gameObject.name : "None")}");
    }

    public void RotateClockwise()
    {
        if (isBroken)
        {
            Debug.Log($"[{gameObject.name}] ❌ Cannot rotate - Valve is BROKEN");
            return;
        }

        if (connectedTile == null)
        {
            Debug.LogError($"[{gameObject.name}] ❌ No connected tile assigned!");
            return;
        }

        currentRotationCount++;

        // Haptic feedback based on rotation count
        if (Gamepad.current != null)
        {
            if (currentRotationCount < 4)
            {
                // Light vibration (1-3 rotations)
                Gamepad.current.SetMotorSpeeds(0.2f, 0.2f);
                Invoke(nameof(StopHaptics), 0.2f);
            }
            else if (currentRotationCount == 4)
            {
                // Warning vibration (4th rotation - near limit!)
                Gamepad.current.SetMotorSpeeds(0.5f, 0.5f);
                Invoke(nameof(StopHaptics), 0.5f);
            }
        }

        // Check if exceeded max rotations
        if (currentRotationCount >= maxRotations)
        {
            BreakValve();
            return;
        }

        // Rotate the connected tile
        connectedTile.RotateClockwise();

        Debug.Log($"[{gameObject.name}] 🔄 Rotated tile CLOCKWISE (+90°) | Count: <color=yellow>{currentRotationCount}/{maxRotations}</color>");
    }

    public void RotateCounterClockwise()
    {
        if (isBroken)
        {
            Debug.Log($"[{gameObject.name}] ❌ Cannot rotate - Valve is BROKEN");
            return;
        }

        if (connectedTile == null)
        {
            Debug.LogError($"[{gameObject.name}] ❌ No connected tile assigned!");
            return;
        }

        currentRotationCount++;

        // Haptic feedback
        if (Gamepad.current != null)
        {
            if (currentRotationCount < 4)
            {
                Gamepad.current.SetMotorSpeeds(0.2f, 0.2f);
                Invoke(nameof(StopHaptics), 0.2f);
            }
            else if (currentRotationCount == 4)
            {
                Gamepad.current.SetMotorSpeeds(0.5f, 0.5f);
                Invoke(nameof(StopHaptics), 0.5f);
            }
        }

        // Check if exceeded max rotations
        if (currentRotationCount >= maxRotations)
        {
            BreakValve();
            return;
        }

        // Rotate the connected tile
        connectedTile.RotateCounterClockwise();

        Debug.Log($"[{gameObject.name}] 🔄 Rotated tile COUNTER-CLOCKWISE (-90°) | Count: <color=yellow>{currentRotationCount}/{maxRotations}</color>");
    }

    void BreakValve()
    {
        isBroken = true;
        currentRotationCount = 0;

        // Visual feedback (change color to red)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red;
        }

        // Strong haptic feedback (2 seconds)
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(1f, 1f);
            Invoke(nameof(StopHaptics), 2f);
        }

        // Lose heart
        if (puzzleManager != null)
        {
            puzzleManager.LoseHeart();

            puzzleManager.OnValveBroken(this);

        }

        Debug.Log($"╔═══════════════════════════════════════════╗");
        Debug.Log($"║     💥 {gameObject.name} IS BROKEN! 💥");
        Debug.Log($"║     Exceeded max rotations!               ║");
        Debug.Log($"╚═══════════════════════════════════════════╝");
    }

    public void RepairValve()
    {
        isBroken = false;
        currentRotationCount = 0;

        // Visual feedback (restore original color)
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }

        Debug.Log($"[{gameObject.name}] ✅ <color=green>REPAIRED</color> | Rotation count reset");
    }

    public void ResetValve()
    {
        currentRotationCount = 0;
        isBroken = false;

        // Restore color
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.white;
        }

        // Reset connected tile
        if (connectedTile != null)
        {
            connectedTile.ResetTile();
        }

        Debug.Log($"[{gameObject.name}] 🔄 <color=cyan>RESET</color> valve and tile");
    }

    void StopHaptics()
    {
        if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }
}