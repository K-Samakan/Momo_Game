using UnityEngine;
using System.Collections;

/// <summary>
/// AoE Ground Indicator — วงกลมเตือนที่พื้นก่อน AoE ระเบิด
/// 
/// วิธีใช้:
///   var indicator = AoeGroundIndicator.Spawn(transform.position, aoeRadius, windUpTime);
///   // รอ windUpTime → indicator จะ flash แล้วหายเอง
///   
/// หรือใช้ Prefab:
///   1) สร้าง Empty → Add AoeGroundIndicator
///   2) ลากเป็น Prefab → ใส่ช่อง aoeVFXPrefab ใน BossMap2BT
/// </summary>
public class AoeGroundIndicator : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("รัศมีสุดท้ายของวงกลม (จะถูก override จากโค้ดถ้าใช้ Spawn)")]
    public float targetRadius = 5f;

    [Tooltip("ระยะเวลาขยาย (จะถูก override จากโค้ดถ้าใช้ Spawn)")]
    public float expandDuration = 1.0f;

    [Header("Colors")]
    public Color warningColor = new Color(1f, 0.9f, 0f, 0.35f);    // เหลืองใส
    public Color dangerColor = new Color(1f, 0.15f, 0.1f, 0.5f);   // แดง
    public Color impactColor = new Color(1f, 1f, 1f, 0.8f);        // ขาวแฟลช

    [Header("Impact")]
    public float impactFlashDuration = 0.15f;
    public float fadeOutDuration = 0.4f;

    [Header("Ring (ขอบวงกลม)")]
    public bool showRing = true;
    public float ringWidth = 0.15f;

    // ── Private ──
    private GameObject _discObj;
    private GameObject _ringObj;
    private MeshRenderer _discRenderer;
    private MeshRenderer _ringRenderer;
    private Material _discMat;
    private Material _ringMat;
    private float _elapsed = 0f;
    private bool _expanding = false;
    private bool _impacted = false;

    // ═══════════════════════════════════════════════════
    // Static Factory — เรียกจากโค้ดได้เลย
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// สร้าง AoE indicator ที่ตำแหน่ง worldPos
    /// วงกลมจะขยายจาก 0 → radius ใน duration วินาที
    /// แล้ว flash + fade out + ทำลายตัวเอง
    /// </summary>
    public static AoeGroundIndicator Spawn(Vector3 worldPos, float radius, float duration)
    {
        var go = new GameObject("AoeGroundIndicator");
        go.transform.position = worldPos + Vector3.up * 0.05f; // ลอยเหนือพื้นนิดเดียว
        var indicator = go.AddComponent<AoeGroundIndicator>();
        indicator.targetRadius = radius;
        indicator.expandDuration = duration;
        return indicator;
    }

    // ═══════════════════════════════════════════════════
    // Unity Lifecycle
    // ═══════════════════════════════════════════════════

    void Start()
    {
        CreateDisc();
        if (showRing) CreateRing();
        StartCoroutine(ExpandSequence());
    }

    void Update()
    {
        if (!_expanding) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / expandDuration);

        // ขยายขนาด
        float currentRadius = Mathf.Lerp(0f, targetRadius, EaseOutQuad(t));
        float diameter = currentRadius * 2f;
        _discObj.transform.localScale = new Vector3(diameter, 0.01f, diameter);
        if (_ringObj != null)
        {
            float ringDiameter = diameter + ringWidth;
            _ringObj.transform.localScale = new Vector3(ringDiameter, 0.01f, ringDiameter);
        }

        // เปลี่ยนสี เหลือง → แดง
        Color currentColor = Color.Lerp(warningColor, dangerColor, t);
        _discMat.color = currentColor;
        if (_ringMat != null)
        {
            Color ringColor = currentColor;
            ringColor.a = Mathf.Lerp(0.5f, 0.8f, t);
            _ringMat.color = ringColor;
        }
    }

    // ═══════════════════════════════════════════════════
    // Sequences
    // ═══════════════════════════════════════════════════

    IEnumerator ExpandSequence()
    {
        _expanding = true;

        // รอจนขยายเต็ม
        yield return new WaitForSeconds(expandDuration);
        _expanding = false;

        // Flash impact
        yield return StartCoroutine(ImpactFlash());

        // Fade out
        yield return StartCoroutine(FadeOut());

        // ทำลายตัวเอง
        Destroy(gameObject);
    }

    IEnumerator ImpactFlash()
    {
        _impacted = true;

        // ขยายเพิ่มนิดหน่อย (punch effect)
        float punchScale = targetRadius * 2f * 1.15f;
        _discObj.transform.localScale = new Vector3(punchScale, 0.01f, punchScale);

        _discMat.color = impactColor;
        if (_ringMat != null) _ringMat.color = impactColor;

        yield return new WaitForSeconds(impactFlashDuration);

        // กลับขนาดปกติ
        float normalScale = targetRadius * 2f;
        _discObj.transform.localScale = new Vector3(normalScale, 0.01f, normalScale);
    }

    IEnumerator FadeOut()
    {
        Color startColor = dangerColor;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, t);
            _discMat.color = c;
            if (_ringMat != null) _ringMat.color = c;

            // หดเล็กลงเล็กน้อย
            float shrink = Mathf.Lerp(1f, 0.9f, t);
            float scale = targetRadius * 2f * shrink;
            _discObj.transform.localScale = new Vector3(scale, 0.01f, scale);

            yield return null;
        }
    }

    // ═══════════════════════════════════════════════════
    // Create Meshes
    // ═══════════════════════════════════════════════════

    void CreateDisc()
    {
        _discObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _discObj.name = "AoE_Disc";
        _discObj.transform.SetParent(transform, false);
        _discObj.transform.localPosition = Vector3.zero;
        _discObj.transform.localScale = new Vector3(0f, 0.01f, 0f); // เริ่มจาก 0

        // ลบ collider กัน physics ชน
        var col = _discObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // สร้าง material แบบ Transparent
        _discRenderer = _discObj.GetComponent<MeshRenderer>();
        _discMat = new Material(Shader.Find("Sprites/Default")); // Unlit + Transparent
        _discMat.color = warningColor;
        _discRenderer.material = _discMat;
        _discRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _discRenderer.receiveShadows = false;
    }

    void CreateRing()
    {
        _ringObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _ringObj.name = "AoE_Ring";
        _ringObj.transform.SetParent(transform, false);
        _ringObj.transform.localPosition = Vector3.zero;
        _ringObj.transform.localScale = new Vector3(0f, 0.015f, 0f);

        var col = _ringObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        _ringRenderer = _ringObj.GetComponent<MeshRenderer>();
        _ringMat = new Material(Shader.Find("Sprites/Default"));
        Color ringColor = warningColor;
        ringColor.a = 0.6f;
        _ringMat.color = ringColor;
        _ringRenderer.material = _ringMat;
        _ringRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _ringRenderer.receiveShadows = false;
    }

    // ═══════════════════════════════════════════════════
    // Easing
    // ═══════════════════════════════════════════════════

    float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}