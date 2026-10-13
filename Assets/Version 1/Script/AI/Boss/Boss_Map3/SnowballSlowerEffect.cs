using UnityEngine;
using System.Collections;
using StarterAssets;

/// <summary>
/// SnowballSlowEffect — ลดความเร็วผู้เล่นชั่วคราวเมื่อโดนหิมะ
///
/// Auto-attach บนผู้เล่นตอนโดนหิมะ หรือ AddComponent ไว้ล่วงหน้าก็ได้
/// จัดการ stack → slow ครั้งใหม่จะ reset timer ไม่ซ้อนกัน
///
/// ✅ [Map3 Side Quest] เพิ่ม SlowImmune — WhaleGuardianEffect เปิดเมื่อได้รางวัลวาฬโบราณ
/// </summary>
public class SnowballSlowEffect : MonoBehaviour
{
    /// <summary>true = ไม่โดน slow จากหิมะเลย (ตั้งโดย WhaleGuardianEffect)</summary>
    public static bool SlowImmune = false;

    private ThirdPersonController _tpc;
    private float _originalMoveSpeed;
    private float _originalSprintSpeed;
    private bool _isSlowed = false;
    private Coroutine _slowCoroutine;

    void Awake()
    {
        _tpc = GetComponent<ThirdPersonController>();
        if (_tpc != null)
        {
            _originalMoveSpeed = _tpc.MoveSpeed;
            _originalSprintSpeed = _tpc.SprintSpeed;
        }
    }

    /// <summary>
    /// ลดความเร็ว — ถ้ากำลัง slow อยู่จะ reset timer ใหม่ (ไม่ซ้อน)
    /// </summary>
    public void ApplySlow(float multiplier, float duration)
    {
        // 🐋 วาฬโบราณปกป้อง — ไม่โดนสโลว์
        if (SlowImmune)
        {
            Debug.Log("[SlowEffect] 🐋 Whale Guardian กันสถานะช้าไว้แล้ว");
            return;
        }

        if (_tpc == null)
        {
            _tpc = GetComponent<ThirdPersonController>();
            if (_tpc == null)
            {
                Debug.LogWarning("[SlowEffect] ไม่พบ ThirdPersonController!");
                return;
            }
            _originalMoveSpeed = _tpc.MoveSpeed;
            _originalSprintSpeed = _tpc.SprintSpeed;
        }

        if (_slowCoroutine != null)
            StopCoroutine(_slowCoroutine);

        _slowCoroutine = StartCoroutine(SlowRoutine(multiplier, duration));
    }

    /// <summary>
    /// ✅ [Map3] ล้าง slow ที่ค้างอยู่ทันที คืนความเร็วปกติ
    /// </summary>
    public void ClearSlow()
    {
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            _slowCoroutine = null;
        }

        if (_isSlowed && _tpc != null)
        {
            _tpc.MoveSpeed = _originalMoveSpeed;
            _tpc.SprintSpeed = _originalSprintSpeed;
        }
        _isSlowed = false;
    }

    IEnumerator SlowRoutine(float multiplier, float duration)
    {
        if (!_isSlowed)
        {
            _originalMoveSpeed = _tpc.MoveSpeed;
            _originalSprintSpeed = _tpc.SprintSpeed;
        }

        _isSlowed = true;
        _tpc.MoveSpeed = _originalMoveSpeed * multiplier;
        _tpc.SprintSpeed = _originalSprintSpeed * multiplier;

        Debug.Log($"[SlowEffect] ❄️ ช้าลง! Speed: {_tpc.MoveSpeed:F1} (×{multiplier}) เป็นเวลา {duration} วิ");

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (Mathf.FloorToInt(elapsed) != Mathf.FloorToInt(elapsed - Time.deltaTime))
                HapticManager.Instance?.Vibrate(0.05f, 0.15f, 0.05f);

            yield return null;
        }

        _tpc.MoveSpeed = _originalMoveSpeed;
        _tpc.SprintSpeed = _originalSprintSpeed;
        _isSlowed = false;
        _slowCoroutine = null;

        Debug.Log($"[SlowEffect] ✅ ความเร็วกลับปกติ! Speed: {_tpc.MoveSpeed:F1}");
    }

    void OnDestroy()
    {
        if (_isSlowed && _tpc != null)
        {
            _tpc.MoveSpeed = _originalMoveSpeed;
            _tpc.SprintSpeed = _originalSprintSpeed;
        }
    }
}