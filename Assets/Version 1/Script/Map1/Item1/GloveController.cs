// ============================================================
// FILE: GloveController.cs
// PATH: Assets/Version 1/Script/Map1/Item1/GloveController.cs
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.AI.Monster;

public class GloveController : MonoBehaviour
{
    [Header("Shockwave")]
    [Tooltip("ลาก GloveShockwave Prefab มาใส่")]
    public GameObject shockwavePrefab;

    [Header("Spawn Points")]
    [Tooltip("ลาก houlderOrigin_R มาใส่ — จุด Spawn Shockwave ขวา")]
    public Transform spawnPoint_R;
    [Tooltip("ลาก houlderOrigin_L มาใส่ — จุด Spawn Shockwave ซ้าย")]
    public Transform spawnPoint_L;

    [Header("Damage")]
    public int gloveDamage = 2;

    [Header("Attack Cooldown")]
    public float attackCooldown = 0.8f;

    // ── Private ──
    private bool _isEquipped = false;
    private float _lastAttackTime = -999f;
    private InputAction _attackAction;
    private PlayerInput _playerInput;

    // ─────────────────────────────────────────────
    void OnEnable()
    {
        _playerInput = GetComponentInParent<PlayerInput>();
        if (_playerInput == null)
            _playerInput = FindFirstObjectByType<PlayerInput>();

        if (_playerInput != null)
        {
            var map = _playerInput.actions.FindActionMap("Player");
            _attackAction = map?.FindAction("Attack");
            if (_attackAction != null)
                _attackAction.performed += OnAttackInput;
            else
                Debug.LogError("[GloveController] ❌ ไม่พบ Action 'Attack'!");
        }
        else
        {
            Debug.LogError("[GloveController] ❌ ไม่พบ PlayerInput!");
        }
    }

    void OnDisable()
    {
        if (_attackAction != null)
            _attackAction.performed -= OnAttackInput;
    }

    // ─────────────────────────────────────────────
    public void OnEquip()
    {
        _isEquipped = true;
        Debug.Log("[GloveController] 🥊 Equipped! พร้อมใช้งาน");
    }

    // ─────────────────────────────────────────────
    // Input — ใช้สำหรับ Cooldown check เท่านั้น
    // Shockwave จริงๆ เรียกจาก Animation Event
    // ─────────────────────────────────────────────
    void OnAttackInput(InputAction.CallbackContext context)
    {
        if (!_isEquipped) return;
        if (Time.time < _lastAttackTime + attackCooldown) return;
        _lastAttackTime = Time.time;
        // Animation Event จะเรียก SpawnShockwaveRight/Left เอง
    }

    // ─────────────────────────────────────────────
    // Animation Events — เรียกจาก Animation Clip
    // ─────────────────────────────────────────────
    public void SpawnShockwaveRight()
    {
        if (!_isEquipped) return;
        SpawnShockwave(spawnPoint_R);
        Debug.Log("[GloveController] 👊 Shockwave Right!");
    }

    public void SpawnShockwaveLeft()
    {
        if (!_isEquipped) return;
        SpawnShockwave(spawnPoint_L);
        Debug.Log("[GloveController] 👊 Shockwave Left!");
    }

    void SpawnShockwave(Transform spawnPoint)
    {
        if (shockwavePrefab == null)
        {
            Debug.LogWarning("[GloveController] ⚠️ ไม่พบ shockwavePrefab!");
            return;
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[GloveController] ⚠️ ไม่พบ SpawnPoint!");
            return;
        }

        // Spawn Shockwave ที่ตำแหน่ง spawnPoint
        // หมุนให้หน้าตรงกับทิศที่ตัวละครหัน
        GameObject sw = Instantiate(
            shockwavePrefab,
            spawnPoint.position,
            Quaternion.LookRotation(transform.root.forward)
        );

        var shockwave = sw.GetComponent<GloveShockwave>();
        if (shockwave != null)
            shockwave.Launch(transform.root.forward, gloveDamage);

        HapticManager.Instance?.Vibrate(0.2f, 0.5f, 0.1f);
    }

    // ─────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (spawnPoint_R != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPoint_R.position, 0.15f);
            Gizmos.DrawLine(spawnPoint_R.position,
                spawnPoint_R.position + transform.root.forward * 6f);
        }
        if (spawnPoint_L != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(spawnPoint_L.position, 0.15f);
            Gizmos.DrawLine(spawnPoint_L.position,
                spawnPoint_L.position + transform.root.forward * 6f);
        }
    }
}