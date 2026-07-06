using UnityEngine;
using System.Collections;

/// <summary>
/// SnowballProjectile — ก้อนหิมะที่บอสปาใส่ผู้เล่น
/// 
/// ฟีเจอร์:
///   - ชนผู้เล่น → TakeDamage + ลดความเร็วชั่วคราว (slow)
///   - ผู้เล่นตีทำลายได้ (มี HP ของตัวเอง)
///   - หลบได้ (พุ่งตรงไป ไม่ tracking)
///   
/// Setup Prefab:
///   1) Model ก้อนหิมะ
///   2) SphereCollider (Is Trigger ✅)
///   3) Rigidbody (Use Gravity ❌, Is Kinematic ❌)
///   4) SnowballProjectile (script นี้)
///   5) Tag = "Enemy" (เพื่อให้ SmartAttackSystem ตีได้)
/// </summary>
public class SnowballProjectile : MonoBehaviour
{
    [Header("Stats")]
    public int damage = 2;
    public int hp = 1;
    public float slowDuration = 3f;
    public float slowMultiplier = 0.5f;

    [Header("VFX")]
    public GameObject destroyVFX;

    // ── Private ──
    private Vector3 _direction;
    private float _speed;
    private bool _initialized = false;
    private bool _destroyed = false;

    /// <summary>
    /// เรียกจาก BossMap3BT ตอน spawn
    /// </summary>
    public void Init(int dmg, Vector3 dir, float speed, float slowDur, float slowMult)
    {
        damage = dmg;
        _direction = dir.normalized;
        _speed = speed;
        slowDuration = slowDur;
        slowMultiplier = slowMult;
        _initialized = true;
    }

    void Update()
    {
        if (!_initialized || _destroyed) return;
        transform.position += _direction * _speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_destroyed) return;

        // ── ชนผู้เล่น ──
        if (other.CompareTag("Player"))
        {
            // Damage
            var ph = other.GetComponent<MOMO.Player.PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damage);
                Debug.Log($"[Snowball] ❄️ โดนผู้เล่น! DMG={damage}");
            }

            // Slow effect
            var slowable = other.GetComponent<SnowballSlowEffect>();
            if (slowable != null)
            {
                slowable.ApplySlow(slowMultiplier, slowDuration);
            }
            else
            {
                // Auto-add component ถ้ายังไม่มี
                slowable = other.gameObject.AddComponent<SnowballSlowEffect>();
                slowable.ApplySlow(slowMultiplier, slowDuration);
            }

            // Haptic
            HapticManager.Instance?.Vibrate(0.2f, 0.5f, 0.15f);

            DestroySnowball();
            return;
        }

        // ── ชนพื้น/กำแพง ──
        if (!other.CompareTag("Enemy") && !other.isTrigger)
        {
            DestroySnowball();
        }
    }

    /// <summary>
    /// ผู้เล่นตีหิมะ — เรียกจาก SmartAttackSystem หรือ MonsterBT
    /// </summary>
    public void TakeDamage(int dmg)
    {
        if (_destroyed) return;
        hp -= dmg;
        Debug.Log($"[Snowball] ถูกตี! HP={hp}");

        if (hp <= 0)
        {
            Debug.Log("[Snowball] 💥 หิมะแตก!");
            DestroySnowball();
        }
    }

    void DestroySnowball()
    {
        if (_destroyed) return;
        _destroyed = true;

        if (destroyVFX != null)
        {
            var vfx = Instantiate(destroyVFX, transform.position, Quaternion.identity);
            Destroy(vfx, 2f);
        }

        Destroy(gameObject);
    }
}