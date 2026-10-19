// ============================================================
// FILE: FishingRodLashEffect.cs
// PATH: Assets/Version 1/Script/Efx_Item/FishingRodLashEffect.cs
// เอฟเฟกต์ "เส้นสีเหลือง" พุ่งออกจากปลายเบ็ดไปข้างหน้า ยาวตามระยะโจมตี
// - ยืดออก (extend) → ค้าง (hold) → หดกลับ + จางหาย (retract/fade)
// - Raycast ตัดเส้นเมื่อชนกำแพง/ศัตรู → แสดงจุด impact
// - ใช้ LineRenderer pool (ไม่ Instantiate ทุกครั้ง) รองรับคอมโบที่ซ้อนกัน
// ============================================================
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FishingRodLashEffect : MonoBehaviour
{
    [Header("=== Line Look ===")]
    [ColorUsage(true, true)] // HDR → เปิด Bloom ใน URP Volume จะเรืองแสง
    public Color lineColor = new Color(1f, 0.85f, 0.1f, 1f) * 2.5f;
    public float lineWidth = 0.06f;
    [Tooltip("ท่าที่ 3 (ท่าจบ) เส้นหนาขึ้นกี่เท่า")]
    public float finisherWidthMultiplier = 2.2f;
    [Tooltip("ปล่อยว่างได้ ระบบจะสร้าง material Unlit ให้เอง")]
    public Material lineMaterial;
    [Tooltip("จำนวนจุดของเส้น (มากขึ้น = โค้งสวยขึ้นตอนยืด)")]
    [Range(2, 32)] public int segments = 12;

    [Header("=== Timing ===")]
    public float extendTime = 0.08f;
    public float holdTime = 0.06f;
    public float retractTime = 0.14f;

    [Header("=== Whip Curve (ความพลิ้วของสาย) ===")]
    [Tooltip("ความโค้งของสายตอนพุ่ง (0 = ตรงเป๊ะ)")]
    public float whipAmplitude = 0.25f;

    [Header("=== Collision ===")]
    [Tooltip("Layer ที่เส้นจะชนแล้วหยุด (กำแพง + ศัตรู) — อย่าใส่ Layer Player")]
    public LayerMask hitMask = ~0;

    [Header("=== Impact (optional) ===")]
    [Tooltip("Prefab ประกายตอนเส้นชน (Particle) — ไม่ใส่ก็ได้")]
    public GameObject impactPrefab;
    public float impactLifetime = 0.6f;

    private readonly List<LineRenderer> _pool = new List<LineRenderer>();

    // ─────────────────────────────────────────────
    // Public API — FishingRodController เรียก
    // ─────────────────────────────────────────────
    /// <param name="origin">ปลายเบ็ด</param>
    /// <param name="direction">ทิศตัวละครหันไป</param>
    /// <param name="range">ระยะโจมตี (= extendedAttackRange)</param>
    /// <param name="comboStep">1,2,3 (3 = ท่าจบ)</param>
    /// <returns>ระยะจริงที่เส้นไปถึง (ถูกตัดถ้าชน)</returns>
    public float Play(Vector3 origin, Vector3 direction, float range, int comboStep)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
        direction.Normalize();

        // ✅ ตัดเส้นเมื่อชนกำแพง/ศัตรู
        float reach = range;
        bool hit = Physics.Raycast(origin, direction, out RaycastHit info, range, hitMask,
                                   QueryTriggerInteraction.Ignore)
                   && !info.transform.IsChildOf(transform.root);
        if (hit) reach = info.distance;

        LineRenderer lr = GetLine();
        bool finisher = comboStep >= 3;
        StartCoroutine(LashRoutine(lr, origin, direction, reach, finisher,
                                   hit ? info.point : (Vector3?)null,
                                   comboStep == 2 ? -1f : 1f)); // ท่า 2 สะบัดโค้งอีกด้าน
        return reach;
    }

    // ─────────────────────────────────────────────
    IEnumerator LashRoutine(LineRenderer lr, Vector3 origin, Vector3 dir, float reach,
                            bool finisher, Vector3? impactPoint, float curveSide)
    {
        lr.gameObject.SetActive(true);
        float width = lineWidth * (finisher ? finisherWidthMultiplier : 1f);
        float hold = holdTime * (finisher ? 2f : 1f);
        Vector3 side = Vector3.Cross(Vector3.up, dir) * curveSide;

        // 1) Extend — ease-out ให้ดูพุ่งเร็วแล้วตึง
        for (float t = 0; t < extendTime; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / extendTime, 3f);
            DrawLine(lr, origin, dir, side, reach * k, 1f - k, width, 1f);
            yield return null;
        }
        DrawLine(lr, origin, dir, side, reach, 0f, width, 1f);

        if (impactPoint.HasValue && impactPrefab != null)
        {
            var fx = Instantiate(impactPrefab, impactPoint.Value, Quaternion.LookRotation(-dir));
            Destroy(fx, impactLifetime);
        }

        // 2) Hold
        yield return new WaitForSeconds(hold);

        // 3) Retract + Fade — หดจากปลายกลับเข้าหาเบ็ด
        for (float t = 0; t < retractTime; t += Time.deltaTime)
        {
            float k = t / retractTime;
            DrawLine(lr, origin, dir, side, reach * (1f - k), k * 0.5f, width * (1f - k * 0.7f), 1f - k);
            yield return null;
        }

        lr.gameObject.SetActive(false);
    }

    void DrawLine(LineRenderer lr, Vector3 origin, Vector3 dir, Vector3 side,
                  float length, float curve, float width, float alpha)
    {
        lr.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float u = i / (float)(segments - 1);
            // sin โค้งเฉพาะช่วงกลางสาย → ดูเป็นสายเบ็ดสะบัด
            Vector3 offset = side * Mathf.Sin(u * Mathf.PI) * whipAmplitude * curve;
            lr.SetPosition(i, origin + dir * (length * u) + offset);
        }

        lr.startWidth = width;
        lr.endWidth = width * 0.35f; // ปลายเรียวแหลม

        Color c = lineColor; c.a = alpha;
        Color end = c; end.a = alpha * 0.6f;
        lr.startColor = c;
        lr.endColor = end;
    }

    // ─────────────────────────────────────────────
    // Pool
    // ─────────────────────────────────────────────
    LineRenderer GetLine()
    {
        foreach (var l in _pool)
            if (!l.gameObject.activeSelf) return l;

        var go = new GameObject("RodLashLine_" + _pool.Count);
        // ไม่ parent กับตัวละคร → เส้นไม่สะบัดตามตอนหมุนตัว (world space)
        go.hideFlags = HideFlags.DontSave;
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.numCapVertices = 4;
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.material = lineMaterial != null ? lineMaterial : CreateDefaultMaterial();
        go.SetActive(false);
        _pool.Add(lr);
        return lr;
    }

    Material _runtimeMat;
    Material CreateDefaultMaterial()
    {
        if (_runtimeMat != null) return _runtimeMat;
        Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Sprites/Default");
        _runtimeMat = new Material(s);
        if (_runtimeMat.HasProperty("_Surface")) _runtimeMat.SetFloat("_Surface", 1f); // Transparent
        if (_runtimeMat.HasProperty("_Blend")) _runtimeMat.SetFloat("_Blend", 2f);     // Additive
        _runtimeMat.SetColor("_BaseColor", Color.white);
        return _runtimeMat;
    }

    void OnDestroy()
    {
        foreach (var l in _pool)
            if (l != null) Destroy(l.gameObject);
        if (_runtimeMat != null) Destroy(_runtimeMat);
    }
}