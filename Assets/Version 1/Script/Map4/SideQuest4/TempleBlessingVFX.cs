// ============================================================
// FILE: TempleBlessingVFX.cs
// PATH: Assets/Version 1/Script/Map4/SideQuest4/TempleBlessingVFX.cs
// ============================================================
using UnityEngine;
using System.Collections;

/// <summary>
/// เอฟเฟกต์ "พรวิหารโบราณ" ตอนสู้บอส Map4 — สร้างทุกอย่างด้วยโค้ด ไม่ต้องทำ Particle Prefab
/// ติดสคริปต์นี้บน PlayerArmature (หรือ Empty ลูกของ Player) แล้วลากใส่ TempleBlessingEffect
///
/// ลำดับเอฟเฟกต์:
///   1) PlayActivate  : เสาแสงทองพุ่งจากพื้น + วงคลื่นกระแทกบนพื้น + วงอักขระวิหารหมุนใต้เท้า
///                      + ละอองแสงลอยขึ้นรอบตัว + แสงทองที่หมัด + ไฟ Point Light เต้นเป็นจังหวะ
///   2) ระหว่างบัฟ    : วงอักขระหมุน, ละอองลอย, หมัดเรือง — วงจะ "หรี่ลง" เมื่อเวลาใกล้หมด
///                      (ช่วงท้าย 5 วิ จะกะพริบเร็ว เป็นสัญญาณภาพแทนเสียงเตือน)
///   3) PlayHeal      : ละอองสีเขียวทองระเบิดขึ้นจากตัว
///   4) PlayEnd       : วงอักขระจางหาย + ละอองแตกกระจาย
/// </summary>
public class TempleBlessingVFX : MonoBehaviour
{
    [Header("Follow")]
    [Tooltip("ตัวที่เอฟเฟกต์จะตาม (ปล่อยว่าง = ตัวเอง)")]
    public Transform followTarget;
    public float groundOffset = 0.05f;

    [Header("Materials (ไม่ใส่ก็ได้ — จะสร้างให้อัตโนมัติ)")]
    [Tooltip("แนะนำ: URP/Particles/Unlit, Surface = Transparent, Blend = Additive")]
    public Material particleMaterial;
    public Material lineMaterial;

    [Header("Colors")]
    public Color mainColor = new Color(1f, 0.82f, 0.3f, 1f);   // ทอง
    public Color runeColor = new Color(0.4f, 0.9f, 1f, 1f);    // ฟ้าน้ำทะเล (ธีมใต้ทะเล)
    public Color healColor = new Color(0.5f, 1f, 0.55f, 1f);   // เขียวทอง

    [Header("Rune Circle (วงอักขระใต้เท้า)")]
    public float runeRadius = 1.3f;
    public float runeRotateSpeed = 45f;
    public float runeLineWidth = 0.05f;

    [Header("Fist Glow (ไม่ใส่ก็ได้)")]
    [Tooltip("ลาก Bone มือซ้าย/ขวา (เช่น LeftHand, RightHand) มาใส่ — หมัดจะเรืองแสงตอนบัฟ")]
    public Transform[] fistPoints;

    [Header("Light")]
    public bool usePointLight = true;
    public float lightIntensity = 2f;
    public float lightRange = 4f;

    [Header("Warning (ช่วงใกล้หมดบัฟ)")]
    [Tooltip("สัดส่วนเวลาที่เหลือที่เริ่มกะพริบ (30 วิ × 0.17 ≈ 5 วิสุดท้าย)")]
    public float warningRatio = 0.17f;

    // ── runtime ──
    Material _pMat, _lMat;
    Transform _root, _runeRoot;
    LineRenderer[] _runeLines;
    LineRenderer _shockRing;
    ParticleSystem _aura, _beam, _healBurst, _endBurst;
    ParticleSystem[] _fistGlows;
    Light _light;
    bool _active;
    float _runeAlpha;
    float _remainRatio = 1f;
    Coroutine _fadeRoutine;

    // ════════════════════════════════════════════════════════════
    void Awake()
    {
        if (followTarget == null) followTarget = transform;
        BuildMaterials();
        BuildAll();
        StopImmediate();
    }

    void LateUpdate()
    {
        if (_root == null) return;

        _root.position = followTarget.position + Vector3.up * groundOffset;

        if (_runeAlpha <= 0f) return;

        _runeRoot.Rotate(0f, runeRotateSpeed * Time.deltaTime, 0f, Space.World);

        // หายใจ (pulse) ปกติ / กะพริบเร็วตอนใกล้หมด
        bool warning = _active && _remainRatio <= warningRatio;
        float pulse = warning
            ? (Mathf.Sin(Time.time * 18f) > 0f ? 1f : 0.25f)
            : 0.75f + 0.25f * Mathf.Sin(Time.time * 3f);

        float fade = _active ? Mathf.Lerp(0.45f, 1f, _remainRatio) : 1f;
        SetRuneAlpha(_runeAlpha * pulse * fade);

        if (_light)
            _light.intensity = lightIntensity * _runeAlpha * pulse * fade;
    }

    // ════════════════════════════════════════════════════════════
    #region Public API (เรียกจาก TempleBlessingEffect)
    // ════════════════════════════════════════════════════════════

    public void PlayActivate()
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _active = true;
        _remainRatio = 1f;

        _beam.Play();
        StartCoroutine(ShockwaveRoutine());
        _aura.Play();
        if (_fistGlows != null) foreach (var g in _fistGlows) if (g) g.Play();

        _runeRoot.gameObject.SetActive(true);
        if (_light) _light.enabled = true;
        _fadeRoutine = StartCoroutine(FadeRune(0f, 1f, 0.5f, false));
    }

    public void SetRemainingRatio(float ratio) => _remainRatio = Mathf.Clamp01(ratio);

    public void PlayHeal()
    {
        _healBurst.Emit(30);
    }

    public void PlayEnd()
    {
        _active = false;
        _aura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        if (_fistGlows != null) foreach (var g in _fistGlows) if (g) g.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        _endBurst.Emit(45);

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRune(_runeAlpha, 0f, 0.8f, true));
    }

    public void StopImmediate()
    {
        _active = false;
        _runeAlpha = 0f;
        foreach (var ps in new[] { _aura, _beam, _healBurst, _endBurst })
            if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_fistGlows != null) foreach (var g in _fistGlows) if (g) g.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_runeRoot) _runeRoot.gameObject.SetActive(false);
        if (_shockRing) _shockRing.enabled = false;
        if (_light) _light.enabled = false;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Coroutines
    // ════════════════════════════════════════════════════════════

    IEnumerator FadeRune(float from, float to, float duration, bool hideAtEnd)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _runeAlpha = Mathf.Lerp(from, to, t / duration);
            if (hideAtEnd && _light) _light.intensity = lightIntensity * _runeAlpha;
            yield return null;
        }
        _runeAlpha = to;

        if (hideAtEnd)
        {
            _runeRoot.gameObject.SetActive(false);
            if (_light) _light.enabled = false;
        }
        _fadeRoutine = null;
    }

    IEnumerator ShockwaveRoutine()
    {
        _shockRing.enabled = true;
        float t = 0f, dur = 0.45f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float p = t / dur;
            SetCircle(_shockRing, Mathf.Lerp(0.2f, runeRadius * 2.6f, p), 48);
            _shockRing.widthMultiplier = Mathf.Lerp(0.25f, 0.02f, p);
            Color c = mainColor; c.a = 1f - p;
            _shockRing.startColor = _shockRing.endColor = c;
            yield return null;
        }
        _shockRing.enabled = false;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Build
    // ════════════════════════════════════════════════════════════

    void BuildMaterials()
    {
        _pMat = particleMaterial;
        if (_pMat == null)
        {
            _pMat = new Material(FindShader());
            _pMat.mainTexture = MakeSoftDot(64);
        }

        _lMat = lineMaterial;
        if (_lMat == null) _lMat = new Material(FindShader());
    }

    static Shader FindShader()
    {
        // Sprites/Default ใช้ได้ทั้ง Built-in และ URP, รองรับ vertex color + ความโปร่งใส
        Shader s = Shader.Find("Sprites/Default");
        if (s == null) s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (s == null) s = Shader.Find("Particles/Standard Unlit");
        return s;
    }

    static Texture2D MakeSoftDot(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                float a = Mathf.Clamp01(1f - d);
                a *= a;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        return tex;
    }

    void BuildAll()
    {
        _root = new GameObject("TempleBlessingVFX_Root").transform;
        _root.SetParent(null); // อยู่นอก Player เพื่อไม่หมุนตามตัวละคร (แค่ตามตำแหน่ง)

        BuildRuneCircle();
        _shockRing = CreateLine("ShockRing", _root, true, 48);
        _shockRing.enabled = false;

        _aura = BuildAura(_root, 1f);
        _beam = BuildBeam();
        _healBurst = BuildBurst("HealBurst", healColor, 0.35f, 2.2f, 1.0f, -0.6f);
        _endBurst = BuildBurst("EndBurst", mainColor, 0.9f, 3f, 0.8f, 0.2f);

        if (fistPoints != null && fistPoints.Length > 0)
        {
            _fistGlows = new ParticleSystem[fistPoints.Length];
            for (int i = 0; i < fistPoints.Length; i++)
            {
                if (fistPoints[i] == null) continue;
                _fistGlows[i] = BuildFistGlow(fistPoints[i]);
            }
        }

        if (usePointLight)
        {
            var lgo = new GameObject("BlessingLight");
            lgo.transform.SetParent(_root, false);
            lgo.transform.localPosition = Vector3.up * 1.2f;
            _light = lgo.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = mainColor;
            _light.range = lightRange;
            _light.intensity = 0f;
            _light.shadows = LightShadows.None;
        }
    }

    void OnDestroy()
    {
        if (_root) Destroy(_root.gameObject);
    }

    // ── วงอักขระวิหาร: วงนอก + วงใน + ดาวหกแฉก (สามเหลี่ยม 2 อัน) ──
    void BuildRuneCircle()
    {
        _runeRoot = new GameObject("RuneCircle").transform;
        _runeRoot.SetParent(_root, false);

        var outer = CreateLine("Outer", _runeRoot, true, 64);
        SetCircle(outer, runeRadius, 64);

        var inner = CreateLine("Inner", _runeRoot, true, 48);
        SetCircle(inner, runeRadius * 0.72f, 48);

        var triA = CreateLine("TriA", _runeRoot, true, 3);
        SetPolygon(triA, runeRadius * 0.7f, 3, 90f);

        var triB = CreateLine("TriB", _runeRoot, true, 3);
        SetPolygon(triB, runeRadius * 0.7f, 3, -90f);

        // ขีดอักขระ 8 จุดรอบวงนอก
        var ticks = new LineRenderer[8];
        for (int i = 0; i < 8; i++)
        {
            ticks[i] = CreateLine("Tick" + i, _runeRoot, false, 2);
            float a = i * Mathf.PI * 2f / 8f;
            Vector3 dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            ticks[i].SetPosition(0, dir * runeRadius * 0.8f);
            ticks[i].SetPosition(1, dir * runeRadius * 1.15f);
        }

        _runeLines = new LineRenderer[4 + ticks.Length];
        _runeLines[0] = outer; _runeLines[1] = inner; _runeLines[2] = triA; _runeLines[3] = triB;
        for (int i = 0; i < ticks.Length; i++) _runeLines[4 + i] = ticks[i];

        SetRuneAlpha(0f);
    }

    void SetRuneAlpha(float a)
    {
        if (_runeLines == null) return;
        for (int i = 0; i < _runeLines.Length; i++)
        {
            Color c = (i == 0 || i == 1) ? mainColor : runeColor;
            c.a = a;
            _runeLines[i].startColor = _runeLines[i].endColor = c;
        }
    }

    LineRenderer CreateLine(string n, Transform parent, bool loop, int count)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.loop = loop;
        lr.positionCount = count;
        lr.widthMultiplier = runeLineWidth;
        lr.material = _lMat;
        lr.alignment = LineAlignment.TransformZ;    // นอนราบกับพื้น
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.numCornerVertices = 2;
        return lr;
    }

    // หมายเหตุ: LineRenderer หมุน X=90 แล้ว → วาดจุดบนระนาบ XY ของ local จะนอนบนพื้นโลก
    static void SetCircle(LineRenderer lr, float radius, int seg)
    {
        lr.positionCount = seg;
        for (int i = 0; i < seg; i++)
        {
            float a = i * Mathf.PI * 2f / seg;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }

    static void SetPolygon(LineRenderer lr, float radius, int sides, float startDeg)
    {
        lr.positionCount = sides;
        for (int i = 0; i < sides; i++)
        {
            float a = (startDeg + i * 360f / sides) * Mathf.Deg2Rad;
            lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }

    // ── Particle builders ──
    ParticleSystem NewPS(string n, Transform parent)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = _pMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    Gradient FadeGradient(Color c)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(c, 0.3f), new GradientColorKey(c, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    // ละอองแสงลอยขึ้นรอบตัวตลอดบัฟ
    ParticleSystem BuildAura(Transform parent, float scale)
    {
        var ps = NewPS("Aura", parent);
        var main = ps.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f * scale, 0.16f * scale);
        main.startColor = mainColor;
        main.gravityModifier = -0.15f;

        var em = ps.emission; em.rateOverTime = 28f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 0.7f * scale;
        sh.rotation = new Vector3(-90f, 0f, 0f);   // พุ่งขึ้น

        var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeGradient(mainColor);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.8f;
        return ps;
    }

    // เสาแสงพุ่งขึ้นจากพื้นตอนบัฟเริ่ม
    ParticleSystem BuildBeam()
    {
        var ps = NewPS("LightBeam", _root);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startColor = mainColor;

        var em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, 50), new ParticleSystem.Burst(0.15f, 30) });

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Circle;
        sh.radius = 0.45f;
        sh.rotation = new Vector3(-90f, 0f, 0f);

        var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeGradient(mainColor);

        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = 0.08f;
        r.lengthScale = 2f;
        return ps;
    }

    // ระเบิดละออง (ใช้กับฮีล / จบบัฟ) — เรียก Emit() เอง
    ParticleSystem BuildBurst(string n, Color c, float radius, float speed, float life, float gravity)
    {
        var ps = NewPS(n, _root);
        ps.transform.localPosition = Vector3.up * 1f;
        var main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = c;
        main.gravityModifier = gravity;

        var em = ps.emission; em.rateOverTime = 0f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = radius;

        var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeGradient(c);
        var size = ps.sizeOverLifetime; size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        return ps;
    }

    // หมัดเรืองแสง — ติดกับ bone มือ
    ParticleSystem BuildFistGlow(Transform hand)
    {
        var ps = NewPS("FistGlow", hand);
        var main = ps.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = mainColor;

        var em = ps.emission; em.rateOverTime = 35f;

        var sh = ps.shape;
        sh.shapeType = ParticleSystemShapeType.Sphere;
        sh.radius = 0.08f;

        var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeGradient(mainColor);
        return ps;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    // 🧪 TEST — เทสเอฟเฟกต์อย่างเดียว ไม่ยุ่งกับสเตตัส
    // ════════════════════════════════════════════════════════════
    [ContextMenu("🧪 VFX - Play Activate")] void T1() => PlayActivate();
    [ContextMenu("🧪 VFX - Play Heal")] void T2() => PlayHeal();
    [ContextMenu("🧪 VFX - Play End")] void T3() => PlayEnd();
    [ContextMenu("🧪 VFX - Warning (เหลือ 10%)")] void T4() => SetRemainingRatio(0.1f);
}