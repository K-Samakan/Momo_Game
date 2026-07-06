using UnityEngine;
using UnityEngine.InputSystem;

public class JetpackController : MonoBehaviour
{
    [Header("Jetpack Settings")]
    public float maxEnergy = 100f;
    public float energyDrainRate = 20f;
    public float energyRechargeRate = 10f;
    public float rechargeDelay = 1f;

    [Header("Flight Settings")]
    public float thrustForce = 15f;
    public int maxAirJumps = 1;

    [Header("Magic Blast")]
    public GameObject blastPrefab;
    public Transform blastSpawnPoint;
    public float blastSpeed = 10f;
    private int _blastDamage = 1;

    [Header("VFX")]
    public ParticleSystem jetpackParticles;

    // ── Private ──
    private bool _isEquipped = false;
    private float _currentEnergy;
    private float _lastUsedTime;
    private int _airJumpsRemaining;
    private bool _isGrounded = true;
    private bool _isFlying = false;

    private CharacterController _characterController;
    private PlayerInput _playerInput;
    private InputAction _jumpAction;
    private bool _inputSubscribed = false;

    // ─────────────────────────────────────────
    // เพิ่ม: ให้ movement system อ่าน velocity ได้
    // ─────────────────────────────────────────
    private Vector3 _jetpackVelocity = Vector3.zero;

    /// <summary>
    /// Movement system เรียกฟังก์ชันนี้เพื่อดึง jetpack velocity
    /// แล้วนำไปรวมกับ velocity ของตัวเองก่อน Move() ครั้งเดียว
    /// </summary>
    public Vector3 GetAndConsumeJetpackVelocity()
    {
        Vector3 v = _jetpackVelocity;
        _jetpackVelocity = Vector3.zero;
        return v;
    }

    // ─────────────────────────────────────────
    void Start()
    {
        _currentEnergy = maxEnergy;

        _characterController = GetComponent<CharacterController>();
        if (_characterController == null)
            Debug.LogError("[JetpackController] ❌ ไม่พบ CharacterController!");

        _playerInput = GetComponent<PlayerInput>();
        if (_playerInput == null)
            Debug.LogWarning("[JetpackController] ⚠️ ไม่พบ PlayerInput!");

        this.enabled = false;
    }

    void OnEnable()
    {
        if (!_isEquipped) return;
        SubscribeToInput();
    }

    void OnDisable()
    {
        // ถ้ายัง equipped อยู่ = script อื่นบน GameObject นี้ทำให้ OnDisable ถูกเรียก
        // ไม่ใช่เราปิด Jetpack จริงๆ → ไม่ต้อง unsubscribe
        if (_isEquipped) return;

        UnsubscribeFromInput();
        StopFlying();
    }

    void OnDestroy()
    {
        UnsubscribeFromInput();
    }

    // ─────────────────────────────────────────
    private void SubscribeToInput()
    {
        if (_inputSubscribed) return;
        if (_playerInput == null) return;

        var map = _playerInput.actions.FindActionMap("Player");
        if (map != null)
        {
            _jumpAction = map.FindAction("Jump");
            if (_jumpAction != null)
            {
                _jumpAction.performed += OnJumpInput;    // ✅ เปิด comment
                _jumpAction.canceled += OnJumpCanceled; // ✅ เปิด comment
                _inputSubscribed = true;
                Debug.Log("[JetpackController] ✅ Subscribed to Jump input");
            }
        }
    }

    private void UnsubscribeFromInput()
    {
        if (!_inputSubscribed) return;
        if (_jumpAction != null)
        {
            _jumpAction.performed -= OnJumpInput;
            _jumpAction.canceled -= OnJumpCanceled;
        }
        _inputSubscribed = false;
        Debug.Log("[JetpackController] ❌ Unsubscribed from Jump input");
    }

    // ─────────────────────────────────────────
    public void OnEquip(int blastDamage)
    {
        _isEquipped = true;
        _blastDamage = blastDamage;
        _currentEnergy = maxEnergy;
        _airJumpsRemaining = maxAirJumps;

        this.enabled = true;
        SubscribeToInput();

        if (JetpackUIManager.Instance != null)
        {
            JetpackUIManager.Instance.ShowEnergyBar();
            JetpackUIManager.Instance.UpdateEnergy(_currentEnergy, maxEnergy);
        }

        Debug.Log("[JetpackController] 🚀 Jetpack Equipped!");
    }

    // ─────────────────────────────────────────
    void Update()
    {
        if (!_isEquipped) return;

        CheckGrounded();
        HandleEnergyRecharge();

        if (_isFlying)
            ConsumeEnergy();
    }

    // ─────────────────────────────────────────
    void CheckGrounded()
    {
        bool wasGrounded = _isGrounded;
        _isGrounded = _characterController != null && _characterController.isGrounded;

        if (_isGrounded && !wasGrounded)
            _airJumpsRemaining = maxAirJumps;
    }

    // ─────────────────────────────────────────
    void OnJumpInput(InputAction.CallbackContext context)
    {
        if (!_isEquipped) return;

        if (_isGrounded)
        {
            _airJumpsRemaining = maxAirJumps;
            return; // ไม่แทรกแซง jump ปกติ
        }

        if (_airJumpsRemaining > 0 && _currentEnergy > 0)
        {
            StartFlying();
            _airJumpsRemaining--;
            Debug.Log($"[Jetpack] 🚀 Air Jump! ({_airJumpsRemaining} remaining)");
        }
    }

    void OnJumpCanceled(InputAction.CallbackContext context)
    {
        StopFlying();
    }

    // ─────────────────────────────────────────
    void StartFlying()
    {
        if (_currentEnergy <= 0) return;

        _isFlying = true;
        _lastUsedTime = Time.time;

        if (jetpackParticles != null && !jetpackParticles.isPlaying)
            jetpackParticles.Play();

        SpawnBlast();

        // ✅ แก้ไข: ไม่เรียก _characterController.Move() โดยตรง
        // แต่ set velocity ให้ movement system ไปรวมเอง
        _jetpackVelocity = Vector3.up * thrustForce;

        HapticManager.Instance?.Vibrate(0.1f, 0.3f, 0.05f);
        Debug.Log("[Jetpack] 🚀 Flying!");
    }

    void StopFlying()
    {
        _isFlying = false;
        _jetpackVelocity = Vector3.zero;

        if (jetpackParticles != null && jetpackParticles.isPlaying)
            jetpackParticles.Stop();
    }

    // ─────────────────────────────────────────
    void ConsumeEnergy()
    {
        _currentEnergy -= energyDrainRate * Time.deltaTime;
        _currentEnergy = Mathf.Max(_currentEnergy, 0);

        if (JetpackUIManager.Instance != null)
            JetpackUIManager.Instance.UpdateEnergy(_currentEnergy, maxEnergy);

        if (_currentEnergy <= 0)
        {
            StopFlying();
            Debug.Log("[Jetpack] ⚠️ Out of Energy!");
            HapticManager.Instance?.PlayPattern(HapticPatternType.Warning);
        }
        else
        {
            // ✅ แก้ไข: สะสม velocity ไว้ให้ movement system ดึงไป
            // ไม่เรียก _characterController.Move() ที่นี่อีกต่อไป
            _jetpackVelocity = Vector3.up * thrustForce;
        }
    }

    void HandleEnergyRecharge()
    {
        if (_isFlying) return;
        if (Time.time < _lastUsedTime + rechargeDelay) return;

        if (_currentEnergy < maxEnergy)
        {
            _currentEnergy += energyRechargeRate * Time.deltaTime;
            _currentEnergy = Mathf.Min(_currentEnergy, maxEnergy);

            if (JetpackUIManager.Instance != null)
                JetpackUIManager.Instance.UpdateEnergy(_currentEnergy, maxEnergy);
        }
    }

    // ─────────────────────────────────────────
    void SpawnBlast()
    {
        if (blastPrefab == null || blastSpawnPoint == null) return;

        GameObject blast = Instantiate(
            blastPrefab,
            blastSpawnPoint.position,
            Quaternion.Euler(90, 0, 0)
        );

        var projectile = blast.GetComponent<JetpackBlast>();
        if (projectile != null)
            projectile.Launch(Vector3.down, blastSpeed, _blastDamage);

        Debug.Log("[Jetpack] 💥 Magic Blast spawned!");
    }

    // ─────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (blastSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(blastSpawnPoint.position, 0.2f);
            Gizmos.DrawLine(blastSpawnPoint.position,
                blastSpawnPoint.position + Vector3.down * 3f);
        }
    }
}