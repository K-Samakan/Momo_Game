using UnityEngine;

public class WristBandAttacher : MonoBehaviour
{
    public Transform targetBone;
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;

    // เก็บ rotation เริ่มต้นของ mesh เทียบกับ bone
    private Quaternion _initialLocalRotation;

    void Start()
    {
        if (targetBone == null) return;

        // จำ local rotation ตอนแรกไว้
        // (ค่าที่ตั้งใน Editor ก่อน Play)
        _initialLocalRotation = Quaternion.Euler(rotationOffset);
    }

    void LateUpdate()
    {
        if (targetBone == null) return;

        // Position ตาม bone
        transform.position = targetBone.TransformPoint(positionOffset);

        // Rotation = bone rotation × offset ที่กำหนด
        transform.rotation = targetBone.rotation * _initialLocalRotation;
    }
}