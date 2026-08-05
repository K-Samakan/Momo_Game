using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class TestPuzzleManager : MonoBehaviour
{
    [Header("Test Settings")]
    public PlayerInput playerInput;

    
    
    private bool isPuzzleMode = true;
    
    void Start()
    {
        // ให้ TestPuzzleManager จัดการ timeScale แทน
        StartCoroutine(EnsureUnpauseCoroutine());
        
        // หา PlayerInput ถ้าไม่ได้กำหนด
        if(playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }
        
        if(playerInput != null)
        {
            Debug.Log($"✅ TestPuzzleManager: PlayerInput found - {playerInput.name}");
            
            // ตั้งค่าเป็น Puzzle3 ทันที
            playerInput.SwitchCurrentActionMap("Puzzle3");
            isPuzzleMode = true;
            
            Debug.Log($"🎮 Current Action Map: {playerInput.currentActionMap?.name}");
        }
        else
        {
            Debug.LogError("❌ TestPuzzleManager: PlayerInput not found!");
        }
    }
    
    IEnumerator EnsureUnpauseCoroutine()
    {
        // บังคับ unpause หลายครั้งเพื่อให้แน่ใจ
        for (int i = 0; i < 30; i++) // ทำ 30 frame แรก
        {
            Time.timeScale = 1f;
            if (i % 10 == 0) // แสดง log ทุก 10 frame
            {
                Debug.Log($"🔧 Frame {i}: Ensuring TimeScale = 1f");
            }
            yield return null;
        }
        
        Debug.Log("✅ TestPuzzleManager: Unpause sequence complete");
    }
    
    void Update()
    {
        // ป้องกันการ pause ระหว่างเล่น puzzle
        if (Time.timeScale != 1f && isPuzzleMode)
        {
            Debug.LogWarning($"⚠️ Detected TimeScale change to {Time.timeScale} - forcing back to 1f");
            Time.timeScale = 1f;
        }
        
        // แสดงสถานะปัจจุบันทุก 2 วินาที
        if(Time.frameCount % 120 == 0 && playerInput != null)
        {
            Debug.Log($"📊 Status: ActionMap={playerInput.currentActionMap?.name} | PuzzleMode={isPuzzleMode} | TimeScale={Time.timeScale}");
        }
    }
    
    [ContextMenu("Force Set Puzzle3")]
    void ForceSetPuzzle3()
    {
        if(playerInput != null)
        {
            Time.timeScale = 1f;
            playerInput.SwitchCurrentActionMap("Puzzle3");
            isPuzzleMode = true;
            Debug.Log("🔧 Force set to Puzzle3 Action Map");
        }
    }
    
    [ContextMenu("Check Input System Status")]
    void CheckInputStatus()
    {
        if(playerInput != null)
        {
            Debug.Log($"🔍 Input System Debug:");
            Debug.Log($"   - Current Action Map: {playerInput.currentActionMap?.name}");
            Debug.Log($"   - Time Scale: {Time.timeScale}");
            Debug.Log($"   - Puzzle Mode: {isPuzzleMode}");
        }
    }
}