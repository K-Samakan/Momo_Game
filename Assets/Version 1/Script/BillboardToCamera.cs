using UnityEngine;

/// <summary>
/// หมุน object ให้หันเข้าหากล้องเสมอ (สำหรับ World Space UI)
/// ใช้สูตรเดียวกับ InteractionPromptUI ของระบบเดิม
/// </summary>
public class BillboardToCamera : MonoBehaviour
{
    private Camera _mainCam;

    void Start()
    {
        _mainCam = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCam == null)
        {
            _mainCam = Camera.main;
            if (_mainCam == null) return;
        }

        var rotation = _mainCam.transform.rotation;
        transform.LookAt(transform.position + rotation * Vector3.forward,
                         rotation * Vector3.up);
    }
}