using UnityEngine;
using System.Collections.Generic;

public class Laser : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask mirrorMask;
    public LineRenderer line;
    public float maxDistance = 128;
    public int maxBounces = 128;
    public bool checkForIntruders = false;
    public LayerMask intruderMask;

    [Header("Door Control")]
    public GameObject doorToHide; // ประตูที่จะหายไปเมื่อพบ intruder

    [Header("State")]
    readonly List<Vector3> _linePoints = new();

    void Update()
    {
        _linePoints.Clear();
        _linePoints.Add(transform.position);
        ShootLaser(transform.position, transform.forward, maxBounces);
        line.positionCount = _linePoints.Count;
        line.SetPositions(_linePoints.ToArray());
    }

    void ShootLaser(Vector3 position, Vector3 direction, int bounceLimit)
    {
        if (bounceLimit == 0)
        {
            return;
        }
        --bounceLimit;
        direction = direction.normalized;
        if (!Physics.Raycast(position, direction, out RaycastHit hit, maxDistance))
        {
            _linePoints.Add(position + direction * maxDistance);
            return;
        }
        _linePoints.Add(hit.point);
        var target = hit.collider.gameObject;
        if (checkForIntruders && IsObjectIntruder(target))
        {
            IntruderDetected();
            return;
        }
        if (IsObjectMirror(target))
        {
            var reflectedDirection = Vector3.Reflect(direction, hit.normal);
            ShootLaser(hit.point, reflectedDirection, bounceLimit);
        }
    }

    bool IsObjectMirror(GameObject target)
    {
        return (mirrorMask.value & (1 << target.layer)) != 0;
    }

    bool IsObjectIntruder(GameObject target)
    {
        return (intruderMask.value & (1 << target.layer)) != 0;
    }

    void IntruderDetected()
    {
        Debug.Log("Intruder Found! Opening door...");

        // ทำให้ประตูหายไป
        if (doorToHide != null)
        {
            doorToHide.SetActive(false);
            Debug.Log($"Door {doorToHide.name} has been opened (hidden)");
        }
        else
        {
            Debug.LogWarning("Door GameObject is not assigned to Laser script!");
        }

        if (MirrorInteractionManager.Instance != null)
        {
            MirrorInteractionManager.Instance.OnPuzzleComplete();
        }


        // เล่น Haptic Feedback สำหรับความสำเร็จ
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPattern(HapticPatternType.PuzzleComplete);
            Debug.Log("Playing success haptic feedback for puzzle completion!");
        }
    }
}