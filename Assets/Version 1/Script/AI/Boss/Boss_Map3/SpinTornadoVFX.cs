using UnityEngine;

/// <summary>
/// SpinTornadoVFX — เอฟเฟกต์พายุหมุนรอบตัวแบบ Procedural
/// ใช้ ParticleSystem + Orbital Velocity สร้างวงพลังงานหมุนวน
/// ไม่ต้องพึ่ง VFX Asset ภายนอกเลย — Unity สร้างให้เองด้วยโค้ด
/// 
/// วิธีใช้:
///   var tornado = SpinTornadoVFX.Spawn(bossTransform, radius: 3f, color: greenColor);
///   ... (ระหว่างหมุน) ...
///   tornado.StopAndDestroy();
/// </summary>
public class SpinTornadoVFX : MonoBehaviour
{
    private ParticleSystem _ps;

    public static SpinTornadoVFX Spawn(Transform parent, float radius, Color color)
    {
        var go = new GameObject("SpinTornadoVFX");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.1f, 0f); // ลอยเหนือพื้นนิดเดียว
        var vfx = go.AddComponent<SpinTornadoVFX>();
        vfx.Setup(radius, color);
        return vfx;
    }

    void Setup(float radius, Color color)
    {
        _ps = gameObject.AddComponent<ParticleSystem>();

        // ── Main ──
        var main = _ps.main;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 300;
        main.playOnAwake = false;

        // ── Emission ──
        var emission = _ps.emission;
        emission.rateOverTime = 90f;

        // ── Shape: วงกลมรอบตัวบอส ──
        var shape = _ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0.15f; // เกิดใกล้ขอบวงกลม (เหมือนกำแพงพายุ)
        shape.rotation = new Vector3(-90f, 0f, 0f); // หันขึ้นด้านบน (แนวราบ)

        // ── Velocity: หมุนวน (Orbital) + ลอยขึ้น ──
        var velocityOverLifetime = _ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.orbitalY = new ParticleSystem.MinMaxCurve(220f); // ความเร็วหมุน (องศา/วิ)
        velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1.8f);        // ลอยขึ้นช้าๆ

        // ── Size over Lifetime: หดตัวตอนใกล้ตาย ──
        var sizeOverLifetime = _ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Color over Lifetime: จางเข้า-จางออก ──
        var colorOverLifetime = _ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.85f, 0.25f),
                new GradientAlphaKey(0.6f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = grad;

        // ── Renderer: ใช้ Sprites/Default กันปัญหา shader ข้าม Render Pipeline ──
        var renderer = _ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white; // สีคุมผ่าน colorOverLifetime แทน
        renderer.material = mat;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        _ps.Play();
    }

    /// <summary>หยุดสร้างอนุภาคใหม่ + ทำลายตัวเองหลังอนุภาคที่เหลือตายหมด</summary>
    public void StopAndDestroy(float extraDelay = 1.5f)
    {
        if (_ps != null)
        {
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(gameObject, _ps.main.startLifetime.constantMax + extraDelay);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}