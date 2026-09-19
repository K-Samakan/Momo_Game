using UnityEngine;

/// <summary>
/// VFX วงแหวนโล่รอบตัวผู้เล่น (ground ring + sparkle particles ลอยขึ้น)
/// สร้างทุกอย่างด้วยโค้ดตอน runtime — ไม่ต้องพึ่ง Shader Graph/VFX Graph/Asset ภายนอก
/// ใช้แพทเทิร์นเดียวกับ AoeGroundIndicator.cs และ SpinTornadoVFX.cs ที่มีอยู่แล้วในโปรเจกต์
/// (ใช้ shader "Sprites/Default" — ปลอดภัยข้าม Render Pipeline ทุกแบบ)
///
/// วิธีใช้: ติดสคริปต์นี้บน Child GameObject ใต้ Player (ปิด GameObject ไว้ก่อนใน Scene)
/// แล้วลาก GameObject ตัวนั้นไปใส่ช่อง `Shield VFX` ใน OasisShieldEffect
/// OasisShieldEffect จะ SetActive(true/false) ให้เอง → OnEnable/OnDisable ที่นี่จะเล่น/หยุด particle ให้อัตโนมัติ
/// </summary>
public class ShieldRingVFX : MonoBehaviour
{
    [Header("Ring Settings")]
    [Tooltip("ลาก Assets/Version 1/Image/VisualSoundEffects/Icons/circular-ring.png มาใส่")]
    public Texture2D ringTexture;
    public Color ringColor = new Color(1f, 0.7f, 0.15f, 0.9f); // เหลืองส้ม (Amber/Gold)
    public float ringDiameter = 2.2f;
    public float rotateSpeed = 40f;
    public float pulseSpeed = 2f;
    public float pulseAmount = 0.08f;

    [Header("3D Sphere Look")]
    [Tooltip("เปิด = สร้าง 3 วงตั้งฉากกันแบบ Sphere Collider Gizmo (ดู 3 มิติ) / ปิด = วงแหวนราบพื้นวงเดียวแบบเดิม")]
    public bool sphericalMode = true;

    [Header("Particle Settings")]
    public float particleRateOverTime = 25f;
    public float particleLifetime = 1.2f;
    public float particleRiseSpeed = 1.2f;
    public float particleSize = 0.08f;

    // แต่ละวงเก็บ transform + แกนที่มันจะหมุนรอบ (world space)
    private struct RingInstance
    {
        public Transform t;
        public Vector3 spinAxis;
        public Vector3 baseScale;
    }

    private System.Collections.Generic.List<RingInstance> _rings
        = new System.Collections.Generic.List<RingInstance>();
    private ParticleSystem _ps;

    void Awake()
    {
        BuildRing();
        BuildParticles();
    }

    // ════════════════════════════════════════════════════════════
    // สร้างวงแหวน — โหมดปกติ = วงเดียวราบพื้น / sphericalMode = 3 วงตั้งฉากกัน (ทรงกลม)
    // ════════════════════════════════════════════════════════════
    void BuildRing()
    {
        // ✅ ถ้าไม่ได้ลาก Ring Texture ใส่ → สร้างเท็กซ์เจอร์วงแหวนเองด้วยโค้ด ไม่ต้องหาไฟล์ PNG เลย
        Texture2D texToUse = ringTexture != null ? ringTexture : GenerateRingTexture();

        if (sphericalMode)
        {
            // วง 1 — แนวนอน (พื้น) หมุนวนรอบแกน World.up
            CreateOneRing("ShieldRing_Ground", Quaternion.Euler(90f, 0f, 0f), Vector3.up, texToUse);
            // วง 2 — แนวตั้ง หันหน้า Z หมุนวนรอบแกน World.right
            CreateOneRing("ShieldRing_Vertical_A", Quaternion.identity, Vector3.right, texToUse);
            // วง 3 — แนวตั้ง หันหน้า X (ตั้งฉากกับวง 2) หมุนวนรอบแกน World.forward
            CreateOneRing("ShieldRing_Vertical_B", Quaternion.Euler(0f, 90f, 0f), Vector3.forward, texToUse);
        }
        else
        {
            // โหมดเดิม — วงแหวนราบพื้นวงเดียว
            CreateOneRing("ShieldRing_Visual", Quaternion.Euler(90f, 0f, 0f), Vector3.up, texToUse);
        }
    }

    void CreateOneRing(string name, Quaternion localRot, Vector3 spinAxis, Texture2D tex)
    {
        GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ringObj.name = name;
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localPosition = Vector3.zero;
        ringObj.transform.localRotation = localRot;
        ringObj.transform.localScale = new Vector3(ringDiameter, ringDiameter, 1f);

        // ลบ collider กัน physics/interact ชนโดนวงแหวน
        var col = ringObj.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var renderer = ringObj.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Sprites/Default")); // Unlit + Transparent, ปลอดภัยทุก Render Pipeline
        mat.mainTexture = tex;
        mat.color = ringColor;
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        _rings.Add(new RingInstance
        {
            t = ringObj.transform,
            spinAxis = spinAxis,
            baseScale = ringObj.transform.localScale
        });
    }

    // ════════════════════════════════════════════════════════════
    // สร้างเท็กซ์เจอร์วงแหวนเองด้วยโค้ด (fallback เมื่อไม่มีไฟล์ PNG)
    // วาดวงแหวนสีขาวขอบนุ่ม บนพื้นหลังโปร่งใส — material.color (ringColor) จะทาสีทับทีหลัง
    // ════════════════════════════════════════════════════════════
    [Header("Fallback Texture (ใช้เมื่อไม่ได้ลาก Ring Texture)")]
    [Tooltip("ความละเอียดของเท็กซ์เจอร์วงแหวนที่สร้างเอง (พิกเซล)")]
    public int generatedTextureSize = 256;
    [Range(0f, 0.95f)]
    [Tooltip("รัศมีขอบในของวงแหวน (0=จุดกึ่งกลาง, 1=ขอบภาพ)")]
    public float ringInnerRadius = 0.65f;
    [Range(0.05f, 1f)]
    [Tooltip("รัศมีขอบนอกของวงแหวน")]
    public float ringOuterRadius = 0.92f;

    Texture2D GenerateRingTexture()
    {
        int size = generatedTextureSize;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;
        float edgeSoftness = 0.035f; // ความนุ่มของขอบ กันรอยหยัก

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;

                float innerFade = Mathf.InverseLerp(ringInnerRadius, ringInnerRadius + edgeSoftness, dist);
                float outerFade = 1f - Mathf.InverseLerp(ringOuterRadius - edgeSoftness, ringOuterRadius, dist);
                float alpha = Mathf.Clamp01(Mathf.Min(innerFade, outerFade));

                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // ════════════════════════════════════════════════════════════
    // สร้างประกายไฟลอยขึ้นจากขอบวงแหวน (แพทเทิร์นเดียวกับ SpinTornadoVFX.cs)
    // ════════════════════════════════════════════════════════════
    void BuildParticles()
    {
        GameObject psObj = new GameObject("ShieldSparkles");
        psObj.transform.SetParent(transform, false);
        psObj.transform.localPosition = Vector3.zero;

        _ps = psObj.AddComponent<ParticleSystem>();

        var main = _ps.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0f; // ใช้ velocityOverLifetime คุมการลอยขึ้นแทน
        main.startSize = particleSize;
        main.startColor = ringColor;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = false;

        var emission = _ps.emission;
        emission.rateOverTime = particleRateOverTime;

        var shape = _ps.shape;
        var velocityOverLifetime = _ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;

        // ✅ ต้องตั้งค่า X และ Z ด้วยแม้ไม่ต้องการ velocity แนวนอน
        // เพราะ Unity บังคับให้ X/Y/Z อยู่ในโหมดเดียวกัน (ที่นี่คือ TwoConstants จาก MinMaxCurve(min,max))
        // ถ้าปล่อย X/Z ไว้เป็น default (โหมด Constant) จะชนกับ Y (โหมด TwoConstants) แล้ว error ทันที
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        if (sphericalMode)
        {
            // ✅ กระจายรอบทรงกลมทั้งลูก (บน-ล่าง-ซ้าย-ขวา-หน้า-หลัง) แทนแค่วงกลมพื้น
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = ringDiameter * 0.5f;
            // ลอยขึ้นเบาๆ เท่านั้น ส่วนแนวกระจายรอบตัวมาจาก Shape เอง
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(particleRiseSpeed * 0.3f, particleRiseSpeed * 0.5f);
        }
        else
        {
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = ringDiameter * 0.5f;
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(particleRiseSpeed * 0.7f, particleRiseSpeed);
        }

        var colorOverLifetime = _ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(ringColor, 0f), new GradientColorKey(ringColor, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.9f, 0.2f),
                new GradientAlphaKey(0.6f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        var renderer = _ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white; // สีคุมผ่าน colorOverLifetime แทน
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ════════════════════════════════════════════════════════════
    // เล่น/หยุด particle ตาม SetActive ที่ OasisShieldEffect เรียก
    // ════════════════════════════════════════════════════════════
    void OnEnable()
    {
        if (_ps != null) _ps.Play();
    }

    void OnDisable()
    {
        if (_ps != null) _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void Update()
    {
        if (_rings.Count == 0) return;

        // Pulse ขนาดเบาๆ ให้ดูมีชีวิตชีวา (เหมือนโล่ "หายใจ") — คำนวณครั้งเดียวใช้กับทุกวง
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;

        foreach (var ring in _rings)
        {
            if (ring.t == null) continue;

            // หมุนแต่ละวงรอบแกนของตัวเอง (World space) — คนละแกนกันทำให้ดูเป็นไจโรสโคป 3 มิติ
            ring.t.Rotate(ring.spinAxis, rotateSpeed * Time.deltaTime, Space.World);
            ring.t.localScale = ring.baseScale * pulse;
        }
    }
}