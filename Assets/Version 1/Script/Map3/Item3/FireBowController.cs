// ============================================================
// FILE: FireBowController.cs
// PATH: Assets/Version 1/Script/Map3/Item3/FireBowController.cs
// รับผิดชอบ: aim mode, ชาร์จแบบ Genshin, adaptive trigger, ยิงธนู, crosshair, aim camera
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class FireBowController : MonoBehaviour, IAttackSfxProvider
{
    [Header("SFX (Map3 Bow)")]
    public AttackSfxSet attackSfx;
    public AudioClip chargeReadyClip;
    public AttackSfxSet AttackSfx => attackSfx;
    public int SfxPriority => 30;
    private PlayerCombatAudio _combatAudio;

    [Header("Arrow")]
    public GameObject arrowProjectilePrefab;
    public Transform arrowSpawnPoint;      // ปลายธนู / มือขวา (ใช้แค่ตำแหน่ง spawn ไม่ใช้ทิศทางแล้ว)
    public float arrowSpeed = 40f;
    public int baseArrowDamage = 1;        // = ATK ปกติ

    [Header("Aim Direction (สำคัญ)")]
    [Tooltip("กล้องหลักของผู้เล่น — ใช้คำนวณทิศยิงแทนทิศของ mesh ธนู เพื่อให้ยิงตรงไปทางที่กล้องมองเสมอ")]
    public Camera playerCamera;

    [Tooltip("Layer ที่ลูกธนูเล็งโดนได้ (เอา Layer ของ Player ออก)")]
    public LayerMask aimLayerMask = ~0;
    [Tooltip("ระยะเล็งไกลสุด ถ้าไม่โดนอะไรจะยิงไปจุดนี้")]
    public float aimMaxDistance = 200f;
    [Tooltip("ยิงเร็ว (□) ก็ไปตรงกลางจอเหมือนกัน")]
    public bool useCrosshairForQuickShot = true;
    [Tooltip("ปิดแรงโน้มถ่วงของลูกธนู เพื่อให้พุ่งตรงไปที่ crosshair พอดี")]
    public bool arrowUseGravity = false;

    [Header("Fire Charge (เหมือน Genshin)")]
    [Tooltip("เวลากด R2 ค้างขั้นต่ำ (วินาที) ก่อนจะกลายเป็นธนูไฟ")]
    public float chargeThreshold = 1.2f;
    public int fireIgniteTicks = 2;        // ติดไฟ DOT กี่ครั้ง
    public float igniteTickInterval = 0.6f;

    [Header("VFX / Visual")]
    [Tooltip("VFX ไฟสีแดงที่ตัวธนู — โผล่เมื่อชาร์จครบ (ธนูตัว mesh หลักติดมืออยู่แล้วตลอด ไม่ต้องสลับ)")]
    public GameObject fireGlowVFX;
    public ParticleSystem chargingParticles; // optional: อนุภาคระหว่างชาร์จ (ยังไม่ครบ)

    [Tooltip("ไฮไลต์สีแดงที่ตัวธนู + ลูกธนูที่พาดสาย (ใส่ BowChargeHighlight ไว้ที่ bowMesh แล้วลากมาใส่)")]
    public BowChargeHighlight chargeHighlight;

    [Header("UI")]
    public BowReticleUI bowReticleUI;      // Crosshair กลางจอ

    [Header("Adaptive Trigger R2 (โปรไฟล์เดียวกับ Mirror Puzzle Map2 — กระจก = 0.65)")]
    [Tooltip("ใส่แรงต้าน R2 ไว้ตั้งแต่สวมธนู (ก่อนกด) — ทำให้รู้สึกแรงต้านตั้งแต่จังหวะแรกที่กดลง เหมือนตอนเดินเข้าใกล้กระจก")]
    public bool preloadTriggerWhenEquipped = true;
    [Tooltip("แรงต้านรอไว้ตอนถือธนูแต่ยังไม่ง้าง")]
    [Range(0f, 1f)] public float triggerIdleResistance = 0.35f;
    [Tooltip("แรงต้านตอนเริ่มง้าง (เบาสุด)")]
    [Range(0f, 1f)] public float triggerMinResistance = 0.4f;
    [Tooltip("แรงต้านตอนชาร์จเต็ม (หนักสุด) — ค้างไว้จนกว่าจะปล่อย R2")]
    [Range(0f, 1f)] public float triggerMaxResistance = 1f;
    [Tooltip("ความโค้งของการเพิ่มแรง (1 = เพิ่มเท่า ๆ กัน, 2 = เบานานแล้วหนักเร็วช่วงท้าย, 0.6 = หนักเร็วช่วงแรก)")]
    [Range(0.3f, 3f)] public float resistanceCurvePower = 1.2f;

    public enum TriggerTestMode
    {
        Off,               // ใช้ระบบไล่แรงตามชาร์จปกติ
        FixedMirror,       // แรงต้านคงที่แบบกระจก Map2 (SetTriggerResistance 0.65)
        TriggerVibrate,    // ไกสั่นกระตุกต้านนิ้ว (EffectEx + Frequency) แบบ DualSenseTriggerRumble
        OnOffStutter       // แรงต้านแบบกระจก เปิด/ปิดสลับเร็ว ๆ = รู้สึกกึก ๆ
    }

    [Header("🧪 TEST — ทดสอบว่า Adaptive Trigger ทำงานจริงไหม")]
    [Tooltip("Off = ระบบปกติ | FixedMirror = หนักคงที่แบบกระจก | TriggerVibrate = ไกสั่นกระตุก | OnOffStutter = แรงต้านเปิดปิดกึก ๆ")]
    public TriggerTestMode triggerTestMode = TriggerTestMode.TriggerVibrate;
    [Tooltip("FixedMirror / OnOffStutter: ค่าเดียวกับกระจก Map2 = 0.65")]
    [Range(0f, 1f)] public float testResistance = 0.65f;
    [Tooltip("TriggerVibrate: ความแรงของการกระตุก")]
    [Range(0f, 1f)] public float testVibrateStrength = 1f;
    [Tooltip("TriggerVibrate: ความถี่การกระตุก (น้อย = กึก…กึก… ช้า, มาก = ครืดถี่ ๆ)")]
    [Range(1, 40)] public int testVibrateFrequency = 10;
    [Tooltip("OnOffStutter: เปิดแรงต้านนานกี่วินาที / ปิดนานกี่วินาที")]
    public float testStutterOnTime = 0.08f;
    public float testStutterOffTime = 0.08f;
    [Tooltip("ใส่เอฟเฟกต์ทั้ง L2 และ R2 (เปิดไว้ตอนทดสอบ จะได้เทียบได้ว่าไกไหนไม่ทำงาน)")]
    public bool testBothTriggers = true;
    [Tooltip("ทำงานเฉพาะตอนกด R2 ค้าง (ปิด = ทำงานตลอดตั้งแต่สวมธนู)")]
    public bool testOnlyWhileHoldingR2 = false;
    [Tooltip("ส่งคำสั่งซ้ำทุก ๆ กี่วินาที (FixedMirror / TriggerVibrate)")]
    public float testResendInterval = 0.5f;
    [Tooltip("แสดงสถานะ Adaptive Trigger มุมซ้ายบนของจอ")]
    public bool showTriggerDebug = true;

    bool TestActive => triggerTestMode != TriggerTestMode.Off;
    private bool _stutterOn;
    private bool _testEffectApplied;
    private float _testResendTimer;
    private string _lastTriggerCmd = "-";
    private float _lastTriggerCmdTime;

    [Header("Aim Camera (Cinemachine)")]
    [Tooltip("ลาก GameObject ของ Cinemachine Virtual Camera มุม over-the-shoulder แบบ Genshin — ต้องมี Priority สูงกว่ากล้องปกติ")]
    public GameObject aimVirtualCameraObject;

    [Tooltip("ThirdPersonController ของ Player — ใช้ลดความไวกล้อง + หันตัวตามกล้องตอนง้าง (เว้นว่าง = หาอัตโนมัติ)")]
    public ThirdPersonController thirdPersonController;

    [Header("Animator")]
    public string aimBoolParam = "IsAiming";
    public string shootTrigger = "ShootArrow";
    public string shootFireTrigger = "ShootFireArrow";

    // ── Private ──
    private bool _isEquipped = false;
    private bool _isAiming = false;
    private bool _isCharged = false;
    private float _heldTime = 0f;

    private PlayerInput _playerInput;
    private InputAction _aimAction;   // R2 (มี performed/started/canceled)
    private Animator _animator;
    private Transform _playerRoot;

    public bool IsEquipped => _isEquipped;
    public bool IsAiming => _isAiming;
    public float ChargeProgress => _isAiming ? Mathf.Clamp01(_heldTime / chargeThreshold) : 0f;

    [Header("Quick Shot Cooldown (ปุ่มโจมตีธรรมดา □)")]
    public float quickShotCooldown = 0.5f;
    private float _lastQuickShotTime = -999f;

    private InputAction _attackAction; // □ ปุ่มโจมตีธรรมดา — ใช้ยิงธนูแบบไม่ง้าง

    // ─────────────────────────────────────────────
    void Awake()
    {
        _animator = GetComponentInParent<Animator>();

        // ✅ หา Main Camera อัตโนมัติถ้าไม่ได้ลากไว้
        if (playerCamera == null) playerCamera = Camera.main;
        if (thirdPersonController == null) thirdPersonController = GetComponentInParent<ThirdPersonController>();
        if (thirdPersonController == null) thirdPersonController = FindFirstObjectByType<ThirdPersonController>();
        _playerRoot = thirdPersonController != null ? thirdPersonController.transform : transform.root;
        _combatAudio = GetComponentInParent<PlayerCombatAudio>();
        if (_combatAudio == null) _combatAudio = FindFirstObjectByType<PlayerCombatAudio>();
    }

    void OnEnable()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput == null) _playerInput = FindFirstObjectByType<PlayerInput>();
        if (_playerInput == null) { Debug.LogError("[FireBowController] ❌ ไม่พบ PlayerInput!"); return; }

        var map = _playerInput.actions.FindActionMap("Player");

        _aimAction = map?.FindAction("Aim"); // ⚠️ ต้องมี Action ชื่อ "Aim" ผูกกับ R2 ใน Input Actions Asset
        if (_aimAction == null)
        {
            Debug.LogError("[FireBowController] ❌ ไม่พบ Action 'Aim' (R2) — ต้องสร้างใน Input Actions ก่อน!");
        }
        else
        {
            _aimAction.started += OnAimStarted;     // กด R2 ลง
            _aimAction.canceled += OnAimReleased;    // ปล่อย R2
        }

        // ✅ ปุ่มโจมตีธรรมดา (□ = Attack) — เมื่อสวมธนู จะยิงธนูแบบไม่ง้าง/ไม่ติดไฟ แทนการต่อย
        _attackAction = map?.FindAction("Attack");
        if (_attackAction != null)
            _attackAction.performed += OnQuickShotInput;
        else
            Debug.LogError("[FireBowController] ❌ ไม่พบ Action 'Attack' (□)!");
    }

    void OnDisable()
    {
        if (_aimAction != null)
        {
            _aimAction.started -= OnAimStarted;
            _aimAction.canceled -= OnAimReleased;
        }
        if (_attackAction != null)
            _attackAction.performed -= OnQuickShotInput;

        // ✅ ถ้าโดนปิดกลางคันระหว่างง้าง (cutscene / ตาย / เปลี่ยน scene) — คืนทุกอย่าง ไม่ให้กล้องค้างโหมดเล็ง
        if (_isAiming) CancelAim();
        HapticManager.Instance?.ReleaseBowTension();   // ปิดธนู = R2 กลับเป็นไกปกติ
        if (TestActive) HapticManager.Instance?.ResetAdaptiveTriggers();
    }

    // ─────────────────────────────────────────────
    // ปุ่มโจมตีธรรมดา (□) ตอนสวมธนู → ยิงธนูแบบไม่ง้าง ไม่ติดไฟ ไม่ต้องเล็ง
    // ─────────────────────────────────────────────
    void OnQuickShotInput(InputAction.CallbackContext ctx)
    {
        // ❌ ถ้ากำลังง้าง R2 อยู่ ไม่ให้ □ แทรก (กันซ้อน state)
        if (!_isEquipped || _isAiming) return;

        if (Time.time < _lastQuickShotTime + quickShotCooldown) return;
        _lastQuickShotTime = Time.time;

        // ยิงตรงไปข้างหน้าตัวละครทันที ไม่มี aim mode / ไม่มี crosshair / ไม่ติดไฟ
        FireArrow(isFireArrow: false);

        Debug.Log("[FireBowController] 🏹 Quick Shot (ไม่ง้าง, ไม่ติดไฟ) — ยิงจากปุ่มโจมตีธรรมดา");
    }

    void Update()
    {
        // 🧪 TEST: เอฟเฟกต์ไกทดสอบ (ข้ามระบบไล่แรงปกติทั้งหมด)
        if (TestActive && _isEquipped) UpdateTriggerTest();

        if (!_isEquipped || !_isAiming) return;

        _heldTime += Time.deltaTime;

        // ── Adaptive Trigger: เบาสุด → หนักสุดตามเวลาชาร์จ แล้วค้างที่หนักสุด (t ถูก clamp ที่ 1) จนกว่าจะปล่อย ──
        float t = Mathf.Clamp01(_heldTime / chargeThreshold);
        ApplyBowTension(t);

        // ── ไฮไลต์แดงค่อย ๆ เพิ่มตาม % ชาร์จ ──
        chargeHighlight?.SetProgress(t);

        // ── เช็คว่าชาร์จครบหรือยัง ──
        if (!_isCharged && _heldTime >= chargeThreshold)
        {
            _isCharged = true;
            OnChargeComplete();
        }
    }

    // ─────────────────────────────────────────────
    public void OnEquip(int atkFromStat)
    {
        _isEquipped = true;
        baseArrowDamage = Mathf.Max(baseArrowDamage, atkFromStat);
        ApplyIdleTension();   // ✅ สวมธนูแล้ว R2 มีแรงต้านรอไว้เลย
        Debug.Log("[FireBowController] 🏹 Equipped! พร้อมใช้งาน");
    }

    // ─────────────────────────────────────────────
    void OnAimStarted(InputAction.CallbackContext ctx)
    {
        if (!_isEquipped) return;

        _isAiming = true;
        _isCharged = false;
        _heldTime = 0f;

        // ✅ ไม่ต้องสลับ mesh แล้ว — ธนูติดมืออยู่ตลอดตั้งแต่ equip
        if (fireGlowVFX != null) fireGlowVFX.SetActive(false);

        if (_animator != null) _animator.SetBool(Animator.StringToHash(aimBoolParam), true);

        // ✅ โชว์ Bow Reticle ตรงกลางจอทันทีที่เริ่มเล็ง (เหมือน Genshin)
        bowReticleUI?.Show();
        if (chargingParticles != null) chargingParticles.Play();

        // ✅ สลับกล้องเป็นมุม over-the-shoulder แบบ Genshin
        if (aimVirtualCameraObject != null) aimVirtualCameraObject.SetActive(true);

        // ✅ โหมดเล็ง: ลดความไวกล้อง + เปิดมุมก้ม/เงย + หันตัวตามกล้อง
        thirdPersonController?.SetBowAim(true);

        // ✅ เริ่มไฮไลต์ (โชว์ลูกธนูพาดสาย)
        chargeHighlight?.BeginCharge();

        // ✅ เริ่มแรงต้าน R2 เบาสุดทันทีที่กด (forceSend: กันกรณีระบบอื่นรีเซ็ตไกไปแล้ว)
        if (HapticManager.Instance == null)
            Debug.LogWarning("[FireBowController] ⚠️ ไม่มี HapticManager ใน Scene — Adaptive Trigger จะไม่ทำงาน");
        else if (!HapticManager.Instance.IsDualSenseConnected())
            Debug.LogWarning("[FireBowController] ⚠️ HapticManager ยังไม่เจอ DualSense — Adaptive Trigger จะไม่ทำงาน");
        ApplyBowTension(0f, forceSend: true);

        Debug.Log("[FireBowController] 🎯 Start Aiming...");
    }

    void OnChargeComplete()
    {
        // ✅ ธนูติดไฟแดง — พร้อมยิงธนูไฟ
        if (fireGlowVFX != null) fireGlowVFX.SetActive(true);
        bowReticleUI?.SetChargedState(true);
        chargeHighlight?.SetFullyCharged();   // 🔴 ธนู + ลูกธนูแดงจ้า + pulse

        // Haptic feedback แจ้งว่าชาร์จเต็มแล้ว (สั่นสั้น ๆ)
        HapticManager.Instance?.Vibrate(0f, 0.3f, 0.08f);

        _combatAudio?.PlayClip(chargeReadyClip, 0.8f);


        Debug.Log("[FireBowController] 🔥 CHARGED! พร้อมยิงธนูไฟ");
    }

    void OnAimReleased(InputAction.CallbackContext ctx)
    {
        if (!_isEquipped || !_isAiming) return;

        bool shootFire = _isCharged;
        FireArrow(shootFire);
        CancelAim();
    }

    // คืนค่าทุกอย่างหลังปล่อย R2 (หรือโดนขัดจังหวะ)
    void CancelAim()
    {
        _isAiming = false;
        _isCharged = false;
        _heldTime = 0f;

        if (fireGlowVFX != null) fireGlowVFX.SetActive(false);

        if (_animator != null) _animator.SetBool(Animator.StringToHash(aimBoolParam), false);

        bowReticleUI?.Hide();
        if (chargingParticles != null) chargingParticles.Stop();

        // ✅ กลับกล้องปกติ
        if (aimVirtualCameraObject != null) aimVirtualCameraObject.SetActive(false);

        // ✅ ปล่อยแรงต้าน trigger กลับสู่ปกติ
        // ✅ ยิงแล้ว → กลับไปแรงต้านรอ (หรือไกปกติถ้าปิด preload)
        ApplyIdleTension();

        chargeHighlight?.ResetHighlight();
        thirdPersonController?.SetBowAim(false);
    }

    // t = % ชาร์จ (0-1) → แรงต้านจาก min → max แล้วค้างที่ max เมื่อ t = 1
    void ApplyBowTension(float t, bool forceSend = false)
    {
        if (TestActive) { if (forceSend) _testResendTimer = 0f; return; }

        float curved = Mathf.Pow(Mathf.Clamp01(t), resistanceCurvePower);
        float resistance = Mathf.Lerp(triggerMinResistance, triggerMaxResistance, curved);
        HapticManager.Instance?.SetBowTension(resistance, forceSend);
    }

    void ApplyIdleTension()
    {
        if (TestActive && _isEquipped && isActiveAndEnabled) { _testResendTimer = 0f; return; }

        if (_isEquipped && preloadTriggerWhenEquipped && isActiveAndEnabled)
            HapticManager.Instance?.SetBowTension(triggerIdleResistance, forceSend: true);
        else
            HapticManager.Instance?.ReleaseBowTension();
    }

    // ─────────────────────────────────────────────
    // 🧪 TEST effects
    // ─────────────────────────────────────────────
    void UpdateTriggerTest()
    {
        var hm = HapticManager.Instance;
        if (hm == null) { _lastTriggerCmd = "❌ ไม่มี HapticManager"; return; }

        // โหมด "เฉพาะตอนกด R2": ปล่อยแล้วล้างไก
        if (testOnlyWhileHoldingR2 && !_isAiming)
        {
            if (_testEffectApplied)
            {
                hm.ClearTriggersSilent(testBothTriggers);
                _testEffectApplied = false;
                LogCmd("ClearTriggers (ปล่อย R2)");
            }
            return;
        }

        _testResendTimer -= Time.deltaTime;
        if (_testResendTimer > 0f) return;

        switch (triggerTestMode)
        {
            case TriggerTestMode.FixedMirror:
                // ฟังก์ชันเดียวกับ InteractionMirror.SetAdaptiveTrigger() ทุกตัวอักษร (มีผลทั้ง L2/R2)
                hm.SetTriggerResistance(testResistance);
                _testResendTimer = testResendInterval;
                LogCmd($"SetTriggerResistance({testResistance:F2})");
                break;

            case TriggerTestMode.TriggerVibrate:
                hm.SetTriggerVibrationEffect(testVibrateStrength, (byte)testVibrateFrequency, testBothTriggers);
                _testResendTimer = testResendInterval;
                LogCmd($"TriggerVibrate(แรง {testVibrateStrength:F2}, ถี่ {testVibrateFrequency})");
                break;

            case TriggerTestMode.OnOffStutter:
                _stutterOn = !_stutterOn;
                if (!_stutterOn) hm.ClearTriggersSilent(testBothTriggers);
                else if (testBothTriggers) hm.SetTriggerResistance(testResistance);      // L2+R2 โปรไฟล์กระจก
                else hm.SetBowTension(testResistance, forceSend: true);                 // R2 อย่างเดียว โปรไฟล์กระจก
                _testResendTimer = _stutterOn ? testStutterOnTime : testStutterOffTime;
                LogCmd($"Stutter {(_stutterOn ? "ON" : "OFF")}");
                break;
        }
        _testEffectApplied = true;
    }

    void LogCmd(string cmd)
    {
        _lastTriggerCmd = cmd;
        _lastTriggerCmdTime = Time.time;
    }

    void OnGUI()
    {
        if (!showTriggerDebug || !_isEquipped) return;
        var hm = HapticManager.Instance;
        string status =
            $"[Bow Trigger Debug]\n" +
            $"HapticManager : {(hm != null ? "พบ" : "❌ ไม่พบ")}\n" +
            $"DualSense     : {(hm != null && hm.IsDualSenseConnected() ? "เชื่อมต่อ" : "❌ ไม่เจอ")}\n" +
            $"Adaptive On   : {(hm != null && hm.enableAdaptiveTriggers ? "เปิด" : "❌ ปิด")}\n" +
            $"Mode          : {(TestActive ? "TEST " + triggerTestMode : "ไล่แรงตามชาร์จ")}\n" +
            $"คำสั่งล่าสุด   : {_lastTriggerCmd} ({Time.time - _lastTriggerCmdTime:F1}s ที่แล้ว)\n" +
            $"R2 กดอยู่     : {(_isAiming ? $"ใช่ {_heldTime:F1}s" : "ไม่")}";
        GUI.Box(new Rect(10, 10, 330, 140), "");
        GUI.Label(new Rect(18, 14, 320, 135), status);
    }

    // ─────────────────────────────────────────────
    // หาจุดที่ crosshair กลางจอชี้อยู่จริง (ยิงเรย์จากกล้อง)
    // ข้ามตัว Player เอง, trigger collider (เช่นวงตรวจจับของมอนสเตอร์) และของที่อยู่ระหว่างกล้องกับตัวละคร
    // ─────────────────────────────────────────────
    Vector3 GetCrosshairAimPoint()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float skipDist = Vector3.Dot(arrowSpawnPoint.position - ray.origin, ray.direction);

        Vector3 aimPoint = ray.GetPoint(aimMaxDistance);
        float best = float.MaxValue;

        var hits = Physics.RaycastAll(ray, aimMaxDistance, aimLayerMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            if (h.distance <= skipDist) continue;                                              // อยู่ระหว่างกล้องกับมือ
            if (_playerRoot != null && h.collider.transform.IsChildOf(_playerRoot)) continue;  // ตัวเอง
            if (h.distance < best) { best = h.distance; aimPoint = h.point; }
        }
        return aimPoint;
    }

    // ─────────────────────────────────────────────
    void FireArrow(bool isFireArrow)
    {
        if (arrowProjectilePrefab == null || arrowSpawnPoint == null) return;

        if (_animator != null)
            _animator.SetTrigger(Animator.StringToHash(isFireArrow ? shootFireTrigger : shootTrigger));

        // ✅ แก้ปัญหาลูกธนูพุ่งเฉียงขึ้น: ใช้ทิศทางกล้องแทนทิศของ mesh/bone มือ
        //    ยิงจากตำแหน่ง arrowSpawnPoint แต่ "ทิศ" มาจากกล้องเสมอ
        Vector3 fireDirection;
        bool aimed = _isAiming || useCrosshairForQuickShot;
        if (playerCamera != null && aimed)
        {
            // ✅ ยิงจากมือ ไปยังจุดที่ crosshair ชี้จริง (แก้ลูกธนูไม่ตรงเป้า / พุ่งไปแค่ด้านหน้า)
            Vector3 aimPoint = GetCrosshairAimPoint();
            fireDirection = (aimPoint - arrowSpawnPoint.position).normalized;
            if (Vector3.Dot(fireDirection, playerCamera.transform.forward) < 0.2f)
                fireDirection = playerCamera.transform.forward; // กันกรณีเป้าอยู่ชิดมือเกินไป
        }
        else if (playerCamera != null)
        {
            fireDirection = playerCamera.transform.forward;
        }
        else
        {
            // fallback ถ้าไม่มีกล้อง — ใช้ทิศตัวละครแทน (แนวนอนล้วน ไม่มีองศาขึ้น)
            fireDirection = transform.forward;
        }

        Quaternion arrowRotation = Quaternion.LookRotation(fireDirection);

        GameObject arrowGO = Instantiate(arrowProjectilePrefab, arrowSpawnPoint.position, arrowRotation);
        _combatAudio?.PlayFromSet(attackSfx, isFireArrow);
        // 🔴 ลูกธนูที่ยิงตอนชาร์จเต็ม → เรืองแดงระหว่างบิน
        arrowGO.GetComponent<ArrowRedHighlight>()?.Apply(isFireArrow);

        var arrowRb = arrowGO.GetComponent<Rigidbody>();
        if (arrowRb != null)
        {
            arrowRb.useGravity = arrowUseGravity;
            if (!arrowRb.isKinematic)
                arrowRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // ลูกเร็วไม่ทะลุเป้าบาง
            arrowRb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        var projectile = arrowGO.GetComponent<FireArrowProjectile>();
        if (projectile != null)
        {
            projectile.Init(
                damage: baseArrowDamage,
                speed: arrowSpeed,
                isFireArrow: isFireArrow,
                igniteTicks: fireIgniteTicks,
                igniteTickInterval: igniteTickInterval,
                direction: fireDirection
            );
        }

        HapticManager.Instance?.Vibrate(0.1f, isFireArrow ? 0.5f : 0.2f, 0.12f);
        Debug.Log($"[FireBowController] 🏹 ยิงธนู! Fire={isFireArrow} | Charged={_heldTime:F2}s | Dir={fireDirection}");
    }
}