using UnityEngine;

/// <summary>
/// ทำให้ป้าย World Space หันหน้าเข้าหากล้องตลอดเวลา
/// แปะที่ Canvas ของป้าย
/// </summary>
public class FaceCamera : MonoBehaviour
{
    [Tooltip("ติ๊กไว้ = หมุนเฉพาะแกน Y ป้ายจะตั้งตรงเสมอ ไม่เอียงตามมุมกล้อง")]
    public bool lockVertical = true;

    private Camera cam;

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 forward = transform.position - cam.transform.position;
        if (lockVertical) forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(forward);
    }
}