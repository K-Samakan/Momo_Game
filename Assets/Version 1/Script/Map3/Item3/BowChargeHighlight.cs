// ============================================================
// FILE: BowChargeHighlight.cs
// PATH: Assets/Version 1/Script/Map3/Item3/BowChargeHighlight.cs
// ไฮไลต์สีแดง (Emission) ที่ "ตัวธนู" + "ลูกธนูที่พาดสาย" ระหว่างชาร์จ R2
//   - ระหว่างชาร์จ : ค่อย ๆ แดงขึ้นตาม % ชาร์จ (ปิดได้)
//   - ชาร์จเต็ม    : แดงจ้า + เต้นเป็นจังหวะ (pulse) = สัญญาณภาพสำหรับผู้เล่นที่ไม่ได้ยินเสียง
//   - ปล่อย R2     : คืนสีเดิมทันที
// ใส่สคริปต์นี้ไว้ที่ GameObject ตัวธนู (bowMesh) แล้วลากเข้า FireBowController.chargeHighlight
// รองรับ URP/Built-in (_EmissionColor) และ HDRP (_EmissiveColor)
// ============================================================
using System.Collections.Generic;
using UnityEngine;

public class BowChargeHighlight : MonoBehaviour
{
    [Header("Renderers ที่จะเรืองแสง")]
    [Tooltip("Mesh ของตัวธนู (เว้นว่าง = หา Renderer ลูกทั้งหมดของ GameObject นี้อัตโนมัติ)")]
    public Renderer[] bowRenderers;

    [Tooltip("ลูกธนูที่พาดอยู่บนสาย (โมเดลลูกธนูเปล่า ๆ ไม่มีสคริปต์) — โชว์ตอนง้าง ซ่อนตอนปล่อย")]
    public GameObject nockedArrow;

    [Header("สี")]
    [ColorUsage(false, true)]
    public Color highlightColor = new Color(1f, 0.08f, 0.03f);

    [Tooltip("ให้ค่อย ๆ แดงขึ้นระหว่างชาร์จด้วยไหม (false = แดงทีเดียวตอนเต็ม) — FireBowController ไม่ส่ง progress แล้ว จึงแดงเฉพาะตอนเต็มเสมอ")]
    public bool buildUpWhileCharging = false;
    [Tooltip("ความสว่างสูงสุดช่วงกำลังชาร์จ (ยังไม่เต็ม)")]
    public float chargingMaxIntensity = 1.2f;
    [Tooltip("ความสว่างตอนชาร์จเต็ม (ต้องเปิด Bloom ใน Volume ถึงจะเห็นแสงฟุ้ง)")]
    public float fullChargeIntensity = 4f;

    [Header("Pulse ตอนชาร์จเต็ม")]
    public float pulseSpeed = 8f;
    [Range(0f, 1f)] public float pulseAmount = 0.35f;
    [Tooltip("แฟลชจ้าแวบแรกตอนเพิ่งเต็ม (วินาที)")]
    public float fullFlashDuration = 0.2f;
    public float fullFlashMultiplier = 2.5f;

    [Header("Optional")]
    [Tooltip("Point Light สีแดงเล็ก ๆ ที่ธนู (ไม่ใส่ก็ได้)")]
    public Light glowLight;
    public float glowLightMaxIntensity = 2f;

    // ── internal ──
    static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");   // URP / Built-in
    static readonly int EmissiveID = Shader.PropertyToID("_EmissiveColor");   // HDRP

    struct MatSlot { public Material mat; public int prop; public Color original; }
    readonly List<MatSlot> _slots = new List<MatSlot>();
    readonly List<Material> _ownedInstances = new List<Material>();

    float _progress;
    bool _full;
    float _fullTime;
    bool _active;

    void Awake()
    {
        if (bowRenderers == null || bowRenderers.Length == 0)
            bowRenderers = GetComponentsInChildren<Renderer>(true);

        CollectMaterials(bowRenderers);
        if (nockedArrow != null)
        {
            CollectMaterials(nockedArrow.GetComponentsInChildren<Renderer>(true));
            nockedArrow.SetActive(false);
        }

        if (glowLight != null)
        {
            glowLight.color = highlightColor;
            glowLight.intensity = 0f;
            glowLight.enabled = false;
        }
    }

    void CollectMaterials(Renderer[] renderers)
    {
        foreach (var r in renderers)
        {
            if (r == null || r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;

            // .materials = สร้าง instance ของ renderer นี้ (ไม่ไปแก้ material ต้นฉบับใน Project)
            foreach (var m in r.materials)
            {
                if (m == null) continue;
                _ownedInstances.Add(m);

                int prop = m.HasProperty(EmissionID) ? EmissionID :
                           m.HasProperty(EmissiveID) ? EmissiveID : -1;
                if (prop == -1)
                {
                    Debug.LogWarning($"[BowChargeHighlight] ⚠️ Material '{m.name}' ไม่มี Emission — เปลี่ยนเป็น URP/Lit หรือ Shader ที่มี _EmissionColor");
                    continue;
                }

                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                _slots.Add(new MatSlot { mat = m, prop = prop, original = m.GetColor(prop) });
            }
        }
    }

    void OnDestroy()
    {
        foreach (var m in _ownedInstances) if (m != null) Destroy(m);
    }

    // ───────────── API ที่ FireBowController เรียก ─────────────
    public void BeginCharge()
    {
        _active = true;
        _full = false;
        _progress = 0f;
        if (nockedArrow != null) nockedArrow.SetActive(true);
        if (glowLight != null) glowLight.enabled = true;
        Apply(0f);
    }

    public void SetProgress(float t) => _progress = Mathf.Clamp01(t);

    public void SetFullyCharged()
    {
        _full = true;
        _fullTime = Time.time;
    }

    public void ResetHighlight()
    {
        _active = false;
        _full = false;
        _progress = 0f;
        if (nockedArrow != null) nockedArrow.SetActive(false);
        if (glowLight != null) { glowLight.intensity = 0f; glowLight.enabled = false; }
        foreach (var s in _slots) if (s.mat != null) s.mat.SetColor(s.prop, s.original);
    }

    // ─────────────────────────────────────────────
    void Update()
    {
        if (!_active) return;

        float intensity;
        if (_full)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            float flash = (Time.time - _fullTime) < fullFlashDuration ? fullFlashMultiplier : 1f;
            intensity = fullChargeIntensity * pulse * flash;
        }
        else
        {
            // ease-in: ช่วงแรกแดงอ่อน ใกล้เต็มค่อยแรง → อ่านง่ายว่า "ใกล้แล้ว"
            intensity = buildUpWhileCharging ? chargingMaxIntensity * _progress * _progress : 0f;
        }

        Apply(intensity);
    }

    void Apply(float intensity)
    {
        Color add = highlightColor * intensity;
        foreach (var s in _slots)
            if (s.mat != null) s.mat.SetColor(s.prop, s.original + add);

        if (glowLight != null)
            glowLight.intensity = Mathf.Min(intensity, fullChargeIntensity) / fullChargeIntensity * glowLightMaxIntensity;
    }
}