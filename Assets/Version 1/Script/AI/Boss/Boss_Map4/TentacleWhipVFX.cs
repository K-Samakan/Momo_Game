using UnityEngine;

/// <summary>
/// TentacleWhipVFX — เส้นหนวดที่ยืดออกจากจุดปล่อย (origin) แล้ว "ติดตาม" ตำแหน่งผู้เล่นแบบ real-time
/// จนครบเวลาที่กำหนด แล้วหายไปเอง (Destroy ตัวเอง) — คอนเซ็ปต์เดียวกับ SpawnSnowball() ของ BossMap3
/// แต่แทนที่จะเป็นก้อน object ที่บินไปกระทบ กลายเป็นเส้นที่ค้างอยู่และ track เป้าหมายตลอดเวลาแทน
///
/// วิธีใช้:
///   var whip = TentacleWhipVFX.Spawn(originTransform, targetTransform, growDuration, totalDuration, color, width);
///   ... (ถ้าต้องการยกเลิกกลางคัน เช่นบอสโดนสตั้น) ...
///   whip.StopAndDestroy();
/// </summary>
public class TentacleWhipVFX : MonoBehaviour
{
    private Transform _origin;
    private Vector3 _originOffset;
    private Transform _target;
    private LineRenderer _line;

    private float _growDuration;   // เวลาที่หนวดยืดจาก origin ไปถึงเป้าหมายครั้งแรก
    private float _totalDuration;  // เวลารวมทั้งหมดที่หนวดมีชีวิตอยู่ ก่อนหายไป (เหมือน lifetime ก้อนหิมะ)
    private float _maxReach;       // ระยะไกลสุดที่หนวดยืดได้ — ถ้าผู้เล่นหนีไกลกว่านี้ หนวดจะยืดสุดแล้วหยุด ไม่ตามไปเรื่อยๆ
    private float _elapsed = 0f;

    private Vector3 _targetOffset = Vector3.up * 1f; // เล็งไปที่ระดับอก/หัวผู้เล่น ไม่ใช่พื้นเท้า

    public static TentacleWhipVFX Spawn(Transform origin, Vector3 originOffset, Transform target,
        float growDuration, float totalDuration, float maxReach, Color color, float width = 0.3f)
    {
        var go = new GameObject("TentacleWhipVFX");
        var vfx = go.AddComponent<TentacleWhipVFX>();
        vfx.Setup(origin, originOffset, target, growDuration, totalDuration, maxReach, color, width);
        return vfx;
    }

    void Setup(Transform origin, Vector3 originOffset, Transform target, float growDuration, float totalDuration, float maxReach, Color color, float width)
    {
        _origin = origin;
        _originOffset = originOffset;
        _target = target;
        _maxReach = Mathf.Max(0.5f, maxReach);
        _growDuration = Mathf.Max(0.05f, growDuration);
        _totalDuration = Mathf.Max(_growDuration, totalDuration);

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.useWorldSpace = true;
        _line.numCapVertices = 4;
        _line.numCornerVertices = 2;
        _line.startWidth = width;
        _line.endWidth = width * 0.6f; // ปลายหนวดเรียวกว่าโคน ดูเป็นหนวดจริง

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        _line.material = mat;
        _line.startColor = color;
        _line.endColor = color;
        _line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _line.receiveShadows = false;

        // วางตำแหน่งเริ่มต้น (ยังไม่ยืด) ไว้ที่ origin ทั้งสองจุดก่อน กัน Line กระตุกตอนเฟรมแรก
        if (_origin != null)
        {
            Vector3 startPos = _origin.position + _originOffset;
            _line.SetPosition(0, startPos);
            _line.SetPosition(1, startPos);
        }
    }

    void Update()
    {
        if (_origin == null)
        {
            Destroy(gameObject);
            return;
        }

        _elapsed += Time.deltaTime;

        Vector3 originPos = _origin.position + _originOffset;
        Vector3 targetPos = _target != null ? _target.position + _targetOffset : originPos;

        // ✅ ผู้เล่นหนีได้จริง — ถ้าระยะจาก origin ไป targetPos เกิน maxReach
        //    ให้หนวดยืดสุดแค่ maxReach ตามทิศทางนั้น แทนที่จะยืดตามผู้เล่นไปเรื่อยๆ ไม่มีที่สิ้นสุด
        Vector3 dirToTarget = targetPos - originPos;
        float distToTarget = dirToTarget.magnitude;
        Vector3 reachLimitedTarget = distToTarget > _maxReach
            ? originPos + dirToTarget.normalized * _maxReach
            : targetPos;

        float growT = Mathf.Clamp01(_elapsed / _growDuration);
        float easedT = 1f - (1f - growT) * (1f - growT); // ease-out เดียวกับ AoeGroundIndicator

        Vector3 tipPos = Vector3.Lerp(originPos, reachLimitedTarget, easedT);

        _line.SetPosition(0, originPos);
        _line.SetPosition(1, tipPos);

        if (_elapsed >= _totalDuration)
            Destroy(gameObject);
    }

    /// <summary>เรียกจากภายนอกเมื่อต้องการตัดจบหนวดก่อนเวลา (เช่นบอสโดนตีสตั้นกลางคัน)</summary>
    public void StopAndDestroy()
    {
        if (this != null && gameObject != null)
            Destroy(gameObject);
    }
}