using UnityEngine;
using System.Collections;
using StarterAssets;

/// <summary>
/// SnowballSlowEffect — ลดความเร็วผู้เล่นชั่วคราวเมื่อโดนหิมะ
/// 
/// Auto-attach บนผู้เล่นตอนโดนหิมะ หรือ AddComponent ไว้ล่วงหน้าก็ได้
/// จัดการ stack → slow ครั้งใหม่จะ reset timer ไม่ซ้อนกัน
/// </summary>
public class SnowballSlowEffect : MonoBehaviour
{
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

        // ถ้ากำลัง slow อยู่ → หยุด coroutine เดิม แล้วเริ่มใหม่
        if (_slowCoroutine != null)
            StopCoroutine(_slowCoroutine);

        _slowCoroutine = StartCoroutine(SlowRoutine(multiplier, duration));
    }

    IEnumerator SlowRoutine(float multiplier, float duration)
    {
        // ลดความเร็ว
        if (!_isSlowed)
        {
            _originalMoveSpeed = _tpc.MoveSpeed;
            _originalSprintSpeed = _tpc.SprintSpeed;
        }

        _isSlowed = true;
        _tpc.MoveSpeed = _originalMoveSpeed * multiplier;
        _tpc.SprintSpeed = _originalSprintSpeed * multiplier;

        Debug.Log($"[SlowEffect] ❄️ ช้าลง! Speed: {_tpc.MoveSpeed:F1} (×{multiplier}) เป็นเวลา {duration} วิ");

        // Haptic feedback ต่อเนื่องเบาๆ ระหว่าง slow
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // สั่นเบาๆ ทุก 1 วินาทีเตือนว่ายัง slow อยู่
            if (Mathf.FloorToInt(elapsed) != Mathf.FloorToInt(elapsed - Time.deltaTime))
                HapticManager.Instance?.Vibrate(0.05f, 0.15f, 0.05f);

            yield return null;
        }

        // คืนความเร็วปกติ
        _tpc.MoveSpeed = _originalMoveSpeed;
        _tpc.SprintSpeed = _originalSprintSpeed;
        _isSlowed = false;

        Debug.Log($"[SlowEffect] ✅ ความเร็วกลับปกติ! Speed: {_tpc.MoveSpeed:F1}");
    }

    void OnDestroy()
    {
        // กัน speed ค้างถ้า component ถูกลบ
        if (_isSlowed && _tpc != null)
        {
            _tpc.MoveSpeed = _originalMoveSpeed;
            _tpc.SprintSpeed = _originalSprintSpeed;
        }
    }
}