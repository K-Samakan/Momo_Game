// Assets/Version 1/Script/Environment/BreakableFloor.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// พื้นที่เหยียบแล้วแตก: สั่นจอย + VSE + CC + เสียง
/// → ยืนได้ช่วงหนึ่ง → กระพริบเตือน → แตกหาย → รอ Respawn → วนลูป
///
/// ต้องมี Collider 2 ตัวบน GameObject เดียวกัน:
///   1) Collider พื้นจริง  (ไม่ติ๊ก Is Trigger)
///   2) Collider ตรวจจับ  (ติ๊ก Is Trigger) สูงกว่าผิวพื้นเล็กน้อย
/// และ GameObject ผู้เล่นต้องมี Tag = "Player"
/// </summary>
public class BreakableFloor : MonoBehaviour
{
    [Header("=== Timing ===")]
    [Tooltip("เวลาที่ยืนได้สบายๆ ก่อนจะเริ่มกระพริบเตือน (วินาที)")]
    public float standTime = 2f;

    [Tooltip("เวลากระพริบเตือน ก่อนพื้นแตกจริง (วินาที)")]
    public float blinkTime = 1f;

    [Tooltip("เวลาที่หายไปก่อนจะกลับมา (วินาที)")]
    public float respawnTime = 10f;

    [Header("=== Blink (กระพริบเตือน) ===")]
    public float blinkSpeed = 8f;

    [Header("=== Haptic ===")]
    [Range(0f, 1f)] public float vibrateIntensity = 0.4f;

    [Header("=== VSE (Visual Sound Effect) ===")]
    [Tooltip("SoundEmitter บน object นี้ (ใช้ดึง soundTag และ baseIntensity)")]
    public SoundEmitter soundEmitter;

    [Tooltip("ต่ออายุไอคอน VSE ทุกกี่วินาที (ต้องน้อยกว่า Display Time ของ VSE Manager = 2.5)")]
    public float vseRefreshInterval = 0.5f;

    [Tooltip("ติ๊ก = VSE ค้างจนพื้นแตก แม้ผู้เล่นเดินออกไปแล้ว | ไม่ติ๊ก = หายเมื่อผู้เล่นเดินออก")]
    public bool keepVSEWhenPlayerLeaves = true;

    [Header("=== CC (Closed Caption) Direction ===")]
    [Tooltip("ติ๊ก = กำหนดคำทิศทางใน CC เอง (แทนการคำนวณทิศจากตำแหน่งพื้น)\nไม่ติ๊ก = คำนวณทิศตามปกติ (Front/Left/Right/Behind)")]
    public bool overrideCCDirection = true;

    [Tooltip("คำที่แสดงใน CC แทนทิศทาง เช่น Underfoot, Below")]
    public string ccDirectionText = "Underfoot";

    [Tooltip("ติ๊ก = ใช้คำที่กำหนดเฉพาะตอนผู้เล่นยืนบนพื้น (เดินออกแล้วกลับไปบอกทิศจริง)\nไม่ติ๊ก = ใช้คำที่กำหนดตลอดเวลา")]
    public bool overrideOnlyWhenStanding = true;

    [Header("=== SFX 1: เสียงหินกำลังจะแตก (เล่นตอนเหยียบเท่านั้น) ===")]
    public AudioClip creakingSound;
    [Range(0f, 1f)] public float creakingVolume = 0.8f;
    [Tooltip("ติ๊ก = เล่นวนซ้ำตลอดช่วงเหยียบ | ไม่ติ๊ก = เล่นครั้งเดียวตอนเหยียบ")]
    public bool loopCreaking = false;
    [Tooltip("0 = เสียงอยู่กลางตัวผู้เล่น (2D) | 1 = เสียงมีทิศทางตามตำแหน่งพื้น (3D)")]
    [Range(0f, 1f)] public float creakingSpatialBlend = 0f;

    [Header("=== SFX 2: เสียงหินแตก (เล่นตอนพื้นหายไป) ===")]
    public AudioClip breakSound;
    [Range(0f, 1f)] public float breakVolume = 1f;
    [Tooltip("0 = เสียงอยู่กลางตัวผู้เล่น (2D) | 1 = เสียงมีทิศทางตามตำแหน่งพื้น (3D)")]
    [Range(0f, 1f)] public float breakSpatialBlend = 0f;

    [Header("=== References ===")]
    public Renderer floorRenderer;

    [Header("=== Debug ===")]
    public bool showDebugLog = true;

    // ─── Audio Sources (สร้างตอนเริ่มเกม) ───────
    private AudioSource _creakAudioSource;
    private AudioSource _breakAudioSource;

    // ─── Colliders ──────────────────────────────
    private Collider _floorCollider;
    private Collider _triggerZone;

    // ─── Internal State ─────────────────────────
    private enum FloorState { Idle, Pressed, Broken }
    private FloorState _state = FloorState.Idle;
    private Coroutine _breakRoutine;
    private Coroutine _vseRoutine;
    private Color _originalColor;
    private bool _playerOnFloor = false;
    private Vector3 _sourcePos; // ตำแหน่งต้นเสียงของ VSE/CC (กึ่งกลางพื้นจริง)

    // ════════════════════════════════════════════
    // Setup
    // ════════════════════════════════════════════
    void Awake()
    {
        if (floorRenderer == null) floorRenderer = GetComponent<Renderer>();
        if (soundEmitter == null) soundEmitter = GetComponent<SoundEmitter>();

        foreach (var c in GetComponents<Collider>())
        {
            if (c.isTrigger) _triggerZone = c;
            else _floorCollider = c;
        }

        if (_triggerZone == null || _floorCollider == null)
        {
            Debug.LogWarning(
                "[BreakableFloor] ต้องมี Collider 2 ตัว " +
                "(พื้นจริง 1 ตัวที่ไม่ติ๊ก Is Trigger + ตัวตรวจจับ 1 ตัวที่ติ๊ก Is Trigger)", this);
        }

        _sourcePos = _floorCollider != null ? _floorCollider.bounds.center : transform.position;

        if (floorRenderer != null)
            _originalColor = floorRenderer.material.color;

        // AudioSource ตัวที่ 1: เสียงกำลังจะแตก
        _creakAudioSource = gameObject.AddComponent<AudioSource>();
        _creakAudioSource.clip = creakingSound;
        _creakAudioSource.loop = loopCreaking;
        _creakAudioSource.volume = creakingVolume;
        _creakAudioSource.spatialBlend = creakingSpatialBlend;
        _creakAudioSource.playOnAwake = false;

        // AudioSource ตัวที่ 2: เสียงหินแตก (แยกตัว เพื่อไม่ให้ถูกตัดตอนหยุดเสียงที่ 1)
        _breakAudioSource = gameObject.AddComponent<AudioSource>();
        _breakAudioSource.loop = false;
        _breakAudioSource.spatialBlend = breakSpatialBlend;
        _breakAudioSource.playOnAwake = false;
    }

    // ════════════════════════════════════════════
    // Trigger เข้า / ออก
    // ════════════════════════════════════════════
    private bool IsPlayer(Collider other)
    {
        return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;

        _playerOnFloor = true;

        // กลับมาเหยียบซ้ำระหว่างที่พื้นกำลังนับถอยหลัง → เปิดเสียงกลับมา + อัปเดต CC ทันที
        if (_state == FloorState.Pressed)
        {
            PlayCreaking();
            SendVSE();
            return;
        }

        if (_state != FloorState.Idle) return;

        Log("ผู้เล่นเหยียบ → เริ่มนับเวลา");
        _state = FloorState.Pressed;

        _breakRoutine = StartCoroutine(BreakSequence());

        StartPressedFeedback();

        _vseRoutine = StartCoroutine(VSEKeepAliveLoop());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;

        _playerOnFloor = false;
        Log("ผู้เล่นเดินออก → หยุดเสียงกำลังจะแตก");

        StopCreaking();

        // อัปเดต CC ทันที ให้เปลี่ยนจาก "Underfoot" เป็นทิศจริง
        if (_state == FloorState.Pressed && keepVSEWhenPlayerLeaves)
            SendVSE();
    }

    // ════════════════════════════════════════════
    // Feedback
    // ════════════════════════════════════════════
    private void StartPressedFeedback()
    {
        // 1) สั่นจอย
        try
        {
            if (HapticManager.Instance != null)
                HapticManager.Instance.Vibrate(vibrateIntensity, vibrateIntensity, 0.15f);
            else
                Log("ไม่พบ HapticManager.Instance ในฉาก");
        }
        catch (System.Exception e) { Debug.LogException(e, this); }

        // 2) ส่ง VSE + CC ครั้งแรก
        SendVSE();

        // 3) เสียงกำลังจะแตก
        PlayCreaking();
    }

    // ─── SFX 1: เสียงกำลังจะแตก ─────────────────
    private void PlayCreaking()
    {
        if (creakingSound == null) { Log("ยังไม่ได้ใส่ Creaking Sound"); return; }
        if (!_creakAudioSource.isPlaying) _creakAudioSource.Play();
    }

    private void StopCreaking()
    {
        if (_creakAudioSource != null && _creakAudioSource.isPlaying)
            _creakAudioSource.Stop();
    }

    // ─── SFX 2: เสียงหินแตก ─────────────────────
    private void PlayBreakSound()
    {
        if (breakSound == null) { Log("ยังไม่ได้ใส่ Break Sound"); return; }
        _breakAudioSource.PlayOneShot(breakSound, breakVolume);
    }

    // ════════════════════════════════════════════
    // VSE + CC
    // ════════════════════════════════════════════
    private string GetVSETag()
        => soundEmitter != null ? soundEmitter.soundTag : "Warning";

    private float GetVSEIntensity()
        => soundEmitter != null ? soundEmitter.baseIntensity : 0.85f;

    private string GetCCDirectionOverride()
    {
        if (!overrideCCDirection) return null;
        if (overrideOnlyWhenStanding && !_playerOnFloor) return null;
        return ccDirectionText;
    }

    private void SendVSE()
    {
        try
        {
            VisualSoundEffectManager.Instance?.KeepIconAlive(_sourcePos, GetVSEIntensity(), GetVSETag());
            InGameCCManager.Instance?.ShowCC(_sourcePos, GetVSETag(), GetCCDirectionOverride());
        }
        catch (System.Exception e) { Debug.LogException(e, this); }
    }

    private IEnumerator VSEKeepAliveLoop()
    {
        while (_state == FloorState.Pressed)
        {
            yield return new WaitForSeconds(vseRefreshInterval);

            if (_state != FloorState.Pressed) break;
            if (!_playerOnFloor && !keepVSEWhenPlayerLeaves) continue;

            SendVSE();
        }
    }

    // ════════════════════════════════════════════
    // ลำดับเหตุการณ์หลัก: ยืนได้ → กระพริบ → แตก → รอ → กลับมา
    // ════════════════════════════════════════════
    private IEnumerator BreakSequence()
    {
        Log($"ช่วงยืนได้ {standTime} วินาที");
        yield return new WaitForSeconds(standTime);

        Log($"เริ่มกระพริบ {blinkTime} วินาที");
        float elapsed = 0f;
        while (elapsed < blinkTime)
        {
            float t = Mathf.PingPong(elapsed * blinkSpeed, 1f);
            floorRenderer.material.color = Color.Lerp(_originalColor, Color.red, t);

            elapsed += Time.deltaTime;
            yield return null;
        }
        floorRenderer.material.color = _originalColor;

        BreakFloor();

        Log($"รอ Respawn {respawnTime} วินาที");
        yield return new WaitForSeconds(respawnTime);

        RespawnFloor();
    }

    private void BreakFloor()
    {
        Log("พื้นแตก!");
        _state = FloorState.Broken; // ทำให้ VSEKeepAliveLoop หยุดเอง

        // ตัดสินใจเรื่องเสียงก่อนรีเซ็ต _playerOnFloor
        StopCreaking();
        PlayBreakSound();

        _playerOnFloor = false;

        // ซ่อนไอคอน VSE ของพื้นนี้ทันที
        try
        {
            VisualSoundEffectManager.Instance?.HideIconsFromSource(_sourcePos, GetVSETag());
        }
        catch (System.Exception e) { Debug.LogException(e, this); }

        // สั่นตอนแตก
        try
        {
            if (HapticManager.Instance != null)
                HapticManager.Instance.PlayPattern(HapticPatternType.Boom);
        }
        catch (System.Exception e) { Debug.LogException(e, this); }

        // TODO: ใส่ Particle Effect แตกตรงนี้ได้

        floorRenderer.enabled = false;
        if (_floorCollider != null) _floorCollider.enabled = false;
        if (_triggerZone != null) _triggerZone.enabled = false;
    }

    private void RespawnFloor()
    {
        Log("พื้นกลับมาแล้ว");
        _state = FloorState.Idle;

        floorRenderer.material.color = _originalColor;
        floorRenderer.enabled = true;
        if (_floorCollider != null) _floorCollider.enabled = true;
        if (_triggerZone != null) _triggerZone.enabled = true;

        _breakRoutine = null;
        _vseRoutine = null;
    }

    private void Log(string message)
    {
        if (showDebugLog)
            Debug.Log($"[BreakableFloor:{name}] {message}", this);
    }
}