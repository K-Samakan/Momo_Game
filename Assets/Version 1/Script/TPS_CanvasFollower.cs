using UnityEngine;

public class TPS_CanvasFollower : MonoBehaviour
{
    [Header("References")]
    public Transform playerCameraRoot;  // ลาก PlayerCameraRoot จาก Hierarchy มาใส่ที่นี่
    public Vector3 positionOffset = new Vector3(0, 0, 2f);  // Offset จากตำแหน่ง Player

    [Header("Settings")]
    public bool faceCamera = true;      // ให้ UI หันหน้าไปทางกล้องเสมอ
    public float smoothSpeed = 5f;      // ความลื่นไหลของการเคลื่อนที่

    void LateUpdate()
    {
        if (playerCameraRoot == null) return;

        // คำนวณตำแหน่งใหม่
        Vector3 targetPosition = playerCameraRoot.position + 
                                playerCameraRoot.forward * positionOffset.z + 
                                playerCameraRoot.up * positionOffset.y;

        // เคลื่อนที่แบบลื่นไหล (Smooth)
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // หมุน UI ให้หันไปทางกล้อง (ถ้าเปิดใช้งาน)
        if (faceCamera)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}
