using UnityEngine;

/// <summary>
/// ติดกับ GameObject ที่มีเสียง
/// เรียก Emit() เฉพาะตอนเสียงเกิดขึ้นจริง → event-based ไม่ใช่ poll
/// 
/// ใช้กับ: Monster, Platform, Side Quest, Boss ฯลฯ
/// </summary>
public class SoundEmitter : MonoBehaviour
{
    [Header("VSE Settings")]
    [Tooltip("tag ที่ตรงกับ VisualType ใน VSE Inspector\nเช่น Enemy, Warning, SideQuest")]
    public string soundTag = "Enemy";

    [Range(0f, 1f)]
    [Tooltip("ความแรงพื้นฐานของไอคอน")]
    public float baseIntensity = 0.85f;

    [Tooltip("รัศมีสูงสุดที่ได้ยินเสียง (ใช้กับ EmitByDistance)")]
    public float maxHearingRadius = 15f;

    [Header("Cooldown (ป้องกัน Spam)")]
    [Tooltip("หน่วงกี่วินาทีก่อนจะ Emit ซ้ำได้")]
    public float emitCooldown = 0.3f;

    private float _lastEmitTime = -999f;

    // ════════════════════════════════════════════════════════════
    #region Public API
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// Emit ด้วย intensity คงที่ (baseIntensity)
    /// ใช้กับ: MonsterBT.DoAttack(), Side Quest event trigger
    /// </summary>
    public void Emit()
    {
        if (!CanEmit()) return;

        _lastEmitTime = Time.time;
        VisualSoundEffectManager.Instance?.ShowIcon(
            transform.position, baseIntensity, soundTag);

        InGameCCManager.Instance?.ShowCC(transform.position, soundTag);
    }

    /// <summary>
    /// Emit พร้อมคำนวณ intensity จากระยะห่างกับ player
    /// ยิ่งใกล้ → intensity ยิ่งสูง
    /// ใช้กับ: DoChase footstep, เสียงบรรยากาศที่ต่อเนื่อง
    /// </summary>
    public void EmitByDistance(Transform playerTransform)
    {
        if (!CanEmit() || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        float intensity = 1f - Mathf.Clamp01(dist / maxHearingRadius);

        // ยิ่งไกล intensity ยิ่งน้อย → icon เล็กลงและจางลง
        if (intensity <= 0.05f) return;

        _lastEmitTime = Time.time;
        VisualSoundEffectManager.Instance?.ShowIcon(
            transform.position, intensity, soundTag);

        InGameCCManager.Instance?.ShowCC(transform.position, soundTag);
    }

    /// <summary>
    /// Emit ด้วย intensity กำหนดเอง (override baseIntensity)
    /// ใช้กับ: เหตุการณ์พิเศษที่ต้องการความแรงเฉพาะ
    /// </summary>
    public void EmitWithIntensity(float customIntensity)
    {
        if (!CanEmit()) return;

        _lastEmitTime = Time.time;
        VisualSoundEffectManager.Instance?.ShowIcon(
            transform.position, customIntensity, soundTag);
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Helpers
    // ════════════════════════════════════════════════════════════

    bool CanEmit()
    {
        if (VisualSoundEffectManager.Instance == null)
        {
            Debug.LogWarning($"[SoundEmitter] VSE Instance ไม่พบ — {gameObject.name}");
            return false;
        }
        return Time.time >= _lastEmitTime + emitCooldown;
    }

    #endregion
}