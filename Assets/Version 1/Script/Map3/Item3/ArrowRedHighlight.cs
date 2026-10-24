// ============================================================
// FILE: ArrowRedHighlight.cs
// PATH: Assets/Version 1/Script/Map3/Item3/ArrowRedHighlight.cs
// ใส่ที่ Prefab ลูกธนู (ตัวเดียวกับที่มี FireArrowProjectile)
// ลูกธนูที่ยิงตอนชาร์จเต็ม (ธนูไฟ) จะเรืองแดง + หางแดง + แสงแดงระหว่างบิน
// ลูกธนูธรรมดา = หน้าตาเดิม
// FireBowController เรียก Apply(isFireArrow) ทันทีหลัง Instantiate
// ============================================================
using UnityEngine;

public class ArrowRedHighlight : MonoBehaviour
{
    public Renderer[] arrowRenderers;              // เว้นว่าง = หาลูกทั้งหมดอัตโนมัติ
    [ColorUsage(false, true)]
    public Color highlightColor = new Color(1f, 0.08f, 0.03f);
    public float intensity = 4f;

    [Header("Optional")]
    public TrailRenderer trail;                     // หางลูกธนู
    public Light glowLight;                         // Point Light เล็ก ๆ
    public GameObject fireOnlyVFX;                  // เช่น particle ไฟที่ปลายลูกธนู

    static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");
    static readonly int EmissiveID = Shader.PropertyToID("_EmissiveColor");
    readonly System.Collections.Generic.List<Material> _instances = new System.Collections.Generic.List<Material>();

    public void Apply(bool charged)
    {
        if (fireOnlyVFX != null) fireOnlyVFX.SetActive(charged);
        if (glowLight != null) { glowLight.enabled = charged; glowLight.color = highlightColor; }

        if (trail != null)
        {
            trail.emitting = charged;
            if (charged)
            {
                trail.startColor = highlightColor;
                trail.endColor = new Color(highlightColor.r, highlightColor.g, highlightColor.b, 0f);
            }
        }

        if (!charged) return;

        if (arrowRenderers == null || arrowRenderers.Length == 0)
            arrowRenderers = GetComponentsInChildren<Renderer>(true);

        foreach (var r in arrowRenderers)
        {
            if (r == null || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
            foreach (var m in r.materials) // instance ต่อลูก — ถูกทำลายพร้อมลูกธนู
            {
                _instances.Add(m);
                int prop = m.HasProperty(EmissionID) ? EmissionID :
                           m.HasProperty(EmissiveID) ? EmissiveID : -1;
                if (prop == -1) continue;
                m.EnableKeyword("_EMISSION");
                m.SetColor(prop, m.GetColor(prop) + highlightColor * intensity);
            }
        }
    }

    void OnDestroy()
    {
        foreach (var m in _instances) if (m != null) Destroy(m);
    }
}
