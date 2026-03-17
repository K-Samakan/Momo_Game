using UnityEngine;

public class RadialLayoutManual : MonoBehaviour
{
    public int segments = 16; // จำนวนไอคอนรอบวง
    public float radius = 150f;
    

    public int GetIndexFromAngle(float angle)
    {
        float segmentAngle = 360f / segments;
        return Mathf.RoundToInt(angle / segmentAngle) % segments;
    }

    public Vector2 GetPositionAtIndex(int index)
    {
        float angleDeg = (360f / segments) * index;
        float angleRad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad)) * radius;
    }
    

}