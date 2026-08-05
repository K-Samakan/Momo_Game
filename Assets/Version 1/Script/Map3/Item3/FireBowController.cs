// ============================================================
// FILE: FireBowController.cs
// PATH: Assets/Version 1/Script/Map3/Item3/FireBowController.cs
// รับผิดชอบ: aim mode, ชาร์จแบบ Genshin, adaptive trigger, ยิงธนู, crosshair, aim camera
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;

public class FireBowController : MonoBehaviour
{
    [Header("Arrow")]
    public GameObject arrowProjectilePrefab;
    public Transform arrowSpawnPoint;      // ปลายธนู / มือขวา (ใช้แค่ตำแหน่ง spawn ไม่ใช้ทิศทางแล้ว)
    public float arrowSpeed = 40f;
    public int baseArrowDamage = 1;        // = ATK ปกติ

    [Header("Aim Direction (สำคัญ)")]
    [Tooltip("กล้องหลักของผู้เล่น — ใช้คำนวณทิศยิงแทนทิศของ mesh ธนู เพื่อให้ยิงตรงไปทางที่กล้องมองเสมอ")]
    public Camera playerCamera;

    [Header("Fire Charge (เหมือน Genshin)")]
    [Tooltip("เวลากด R2 ค้างขั้นต่ำ (วินาที) ก่อนจะกลายเป็นธนูไฟ")]
    public float chargeThreshold = 1.2f;
    public int fireIgniteTicks = 2;        // ติดไฟ DOT กี่ครั้ง
    public float igniteTickInterval = 0.6f;

    [Header("VFX / Visual")]
    [Tooltip("VFX ไฟสีแดงที่ตัวธนู — โผล่เมื่อชาร์จครบ (ธนูตัว mesh หลักติดมืออยู่แล้วตลอด ไม่ต้องสลับ)")]
    public GameObject fireGlowVFX;
    public ParticleSystem chargingParticles; // optional: อนุภาคระหว่างชาร์จ (ยังไม่ครบ)

    [Header("UI")]
    public BowReticleUI bowReticleUI;      // Crosshair กลางจอ

    [Header("Adaptive Trigger (R2)")]
    [Tooltip("แรงต้านสูงสุดตอนชาร์จเต็ม (0-1)")]
    public float maxTriggerResistance = 1f;
    [Tooltip("ความเร่งของแรงต้าน ยิ่งมากยิ่งรู้สึกฝืดขึ้นเร็วช่วงท้าย (1 = เชิงเส้นตรงไปตรงมา รู้สึกต้านขึ้นตลอด, 2-3 = โค้งเร่งท้าย)")]
    public float resistanceCurvePower = 1.3f;
    [Tooltip("แรงต้านขั้นต่ำตอนเริ่มเล็ง (กันไม่ให้รู้สึกเหมือนไม่มีอะไรเกิดขึ้นช่วงแรก)")]
    [Range(0f, 0.5f)]
    public float minStartResistance = 0.15f;

    [Header("Aim Camera (Cinemachine)")]
    [Tooltip("ลาก GameObject ของ Cinemachine Virtual Camera มุม over-the-shoulder แบบ Genshin — ต้องมี Priority สูงกว่ากล้องปกติ")]
    public GameObject aimVirtualCameraObject;

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

    public bool IsEquipped => _isEquipped;
    public bool IsAiming => _isAiming;

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
        if (!_isEquipped || !_isAiming) return;

        _heldTime += Time.deltaTime;

        // ── Adaptive Trigger: เริ่มต้านตั้งแต่แตะ R2 แล้วค่อย ๆ แรงขึ้นตลอดช่วงจนสุดตอนชาร์จเต็ม ──
        float t = Mathf.Clamp01(_heldTime / chargeThreshold);
        float curved = Mathf.Pow(t, resistanceCurvePower);
        float finalResistance = Mathf.Lerp(minStartResistance, maxTriggerResistance, curved);
        HapticManager.Instance?.SetTriggerResistance(0f, finalResistance);

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

        Debug.Log("[FireBowController] 🎯 Start Aiming...");
    }

    void OnChargeComplete()
    {
        // ✅ ธนูติดไฟแดง — พร้อมยิงธนูไฟ
        if (fireGlowVFX != null) fireGlowVFX.SetActive(true);
        bowReticleUI?.SetChargedState(true);

        // Haptic feedback แจ้งว่าชาร์จเต็มแล้ว (สั่นสั้น ๆ)
        HapticManager.Instance?.Vibrate(0f, 0.3f, 0.08f);

        Debug.Log("[FireBowController] 🔥 CHARGED! พร้อมยิงธนูไฟ");
    }

    void OnAimReleased(InputAction.CallbackContext ctx)
    {
        if (!_isEquipped || !_isAiming) return;

        bool shootFire = _isCharged;
        FireArrow(shootFire);

        // ✅ Reset state (ไม่ต้องสลับ mesh — ธนูติดมืออยู่ตลอด)
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
        HapticManager.Instance?.SetTriggerResistance(0f, 0f);
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
        if (playerCamera != null)
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