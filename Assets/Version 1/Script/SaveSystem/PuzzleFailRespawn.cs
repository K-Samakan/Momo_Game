// ============================================================
// FILE: PuzzleFailRespawn.cs
// PATH: Assets/Version 1/Script/SaveSystem/PuzzleFailRespawn.cs
// ============================================================
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ใช้ร่วมกันทุก Puzzle (Map 2 / 3 / 4)
/// หัวใจหมด → แสดง Fail UI + สั่น → รอ → โหลดฉากใหม่ (Puzzle รีเซ็ต) → วาปไปจุด Save ล่าสุด (หน้าห้อง Puzzle)
/// </summary>
public static class PuzzleFailRespawn
{
    private static bool _isRespawning = false;

    /// <param name="host">MonoBehaviour ที่ยังไม่ถูก Destroy (ใช้รัน Coroutine)</param>
    /// <param name="failUI">UI "ปริศนาล้มเหลว" (ไม่บังคับ) — สำคัญสำหรับผู้เล่นที่ไม่ได้ยินเสียง</param>
    /// <param name="onBeforeReload">โค้ดที่ต้องทำก่อนโหลดฉาก เช่น สลับ ActionMap กลับเป็น Player</param>
    /// <param name="delay">เวลารอก่อนวาป (วินาที, ไม่ขึ้นกับ timeScale)</param>
    public static void Trigger(MonoBehaviour host, GameObject failUI = null,
                               Action onBeforeReload = null, float delay = 2.5f)
    {
        if (_isRespawning)
        {
            Debug.Log("[PuzzleFailRespawn] ⏳ กำลัง respawn อยู่แล้ว — ข้าม");
            return;
        }
        if (host == null || !host.isActiveAndEnabled)
        {
            Debug.LogWarning("[PuzzleFailRespawn] ⚠️ host ใช้ไม่ได้ → respawn ทันที");
            DoReload(onBeforeReload);
            return;
        }

        _isRespawning = true;
        host.StartCoroutine(Run(failUI, onBeforeReload, delay));
    }

    private static IEnumerator Run(GameObject failUI, Action onBeforeReload, float delay)
    {
        Debug.Log($"[PuzzleFailRespawn] 💔 Puzzle Fail → กลับจุด Save ใน {delay} วินาที");

        if (failUI != null) failUI.SetActive(true);

        yield return new WaitForSecondsRealtime(delay);

        DoReload(onBeforeReload);
    }

    private static void DoReload(Action onBeforeReload)
    {
        // เคลียร์ DualSense ไม่ให้สั่น / Trigger แข็งค้างข้ามฉาก
        if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
        if (HapticManager.Instance != null) HapticManager.Instance.ResetAdaptiveTriggers();

        try { onBeforeReload?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }

        Time.timeScale = 1f;
        _isRespawning = false;

        if (SaveManager.Instance != null)
            SaveManager.Instance.ReloadSceneAndRespawn();
        else
        {
            Debug.LogWarning("[PuzzleFailRespawn] ⚠️ ไม่พบ SaveManager → โหลดฉากเดิมแทน");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}