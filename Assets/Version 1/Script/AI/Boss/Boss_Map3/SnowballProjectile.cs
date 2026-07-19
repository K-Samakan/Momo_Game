using UnityEngine;

/// <summary>
/// SnowballProjectile v4 — ก้อนหิมะที่บอสปาใส่ผู้เล่น (เวอร์ชันเรียบง่าย)
/// 
/// ฟีเจอร์:
///   ✅ มี Rigidbody (Kinematic) อัตโนมัติ — แก้บั๊ก "ทะลุตัวผู้เล่น"
///      (Unity ไม่ยิง OnTriggerEnter ถ้าทั้งคู่ไม่มี Rigidbody เลย)
///   ✅ ใช้ PlayerHealth.TakeDamage(int, Vector3, float) ตัวที่มีอยู่แล้วในเกม
///      ไม่ต้องแก้ไฟล์ PlayerHealth.cs เลย — ใช้ default duration/damping ของระบบเดิม
///   ✅ Slow effect + ทำลายได้ด้วยการโจมตีของผู้เล่น
/// 
/// Setup Prefab (ไม่บังคับ — โค้ด BossMap3BT จะ Auto-Attach ให้ถ้าลืมใส่):
///   1) Model ก้อนหิมะ
///   2) SphereCollider (Is Trigger ✅)
///   3) Rigidbody (Is Kinematic ✅, Use Gravity ❌)  ← สำคัญมาก!
///   4) SnowballProjectile (script นี้)
/// </summary>
public class SnowballProjectile : MonoBehaviour
{
    [Header("Stats")]
    public int damage = 2;
    public int hp = 1;
    public float slowDuration = 3f;
    public float slowMultiplier = 0.5f;
    public float knockbackForce = 6f;

    [Header("VFX")]
    public GameObject destroyVFX;

    private Vector3 _direction;
    private float _speed;
    private bool _initialized = false;
    private bool _destroyed = false;

    /// <summary>
    /// เรียกจาก BossMap3BT ตอน spawn
    /// </summary>
    public void Init(int dmg, Vector3 dir, float speed, float slowDur, float slowMult, float knockback = 6f)
    {
        damage = dmg;
        _direction = dir.normalized;
        _speed = speed;
        slowDuration = slowDur;
        slowMultiplier = slowMult;
        knockbackForce = knockback;
        _initialized = true;

        EnsurePhysicsComponents();
    }

    /// <summary>
    /// ✅ การันตีว่ามี Collider + Rigidbody ครบ — กันบั๊ก "ทะลุตัวผู้เล่น"
    /// Unity จะไม่ยิง OnTriggerEnter เลยถ้าทั้งสอง Collider ที่ชนกันไม่มี Rigidbody สักตัว
    /// (CharacterController ของผู้เล่นไม่นับเป็น Rigidbody!)
    /// </summary>
    void EnsurePhysicsComponents()
    {
        if (GetComponent<Collider>() == null)
        {
            var col = gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.4f;
        }

        if (GetComponent<Rigidbody>() == null)
        {
            var rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;      // ไม่ให้ Physics เข้ามายุ่งกับการเคลื่อนที่ (เราคุมเองผ่าน transform)
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative; // กัน tunneling ตอนความเร็วสูง
        }
    }

    void Update()
    {
        if (!_initialized || _destroyed) return;
        transform.position += _direction * _speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_destroyed) return;

        if (other.CompareTag("Player"))
        {
            var ph = other.GetComponent<MOMO.Player.PlayerHealth>();
            if (ph != null)
            {
                // ✅ ใช้ Overload เดิมที่มีอยู่แล้วใน PlayerHealth.cs — ไม่ต้องแก้ไฟล์นั้นเลย
                Vector3 knockDir = (other.transform.position - transform.position).normalized;
                knockDir.y = 0.3f; // ยกขึ้นเล็กน้อยกันเตะติดพื้น
                ph.TakeDamage(damage, knockDir, knockbackForce);

                Debug.Log($"[Snowball] ❄️ โดนผู้เล่น! DMG={damage}, Knockback={knockbackForce}");
            }

            // Slow effect (แยกจาก PlayerHealth เพราะเป็นเอฟเฟกต์เฉพาะของหิมะ)
            var slowable = other.GetComponent<SnowballSlowEffect>();
            if (slowable == null) slowable = other.gameObject.AddComponent<SnowballSlowEffect>();
            slowable.ApplySlow(slowMultiplier, slowDuration);

            DestroySnowball();
            return;
        }

        // ชนพื้น/กำแพง (ไม่ใช่ Player และไม่ใช่ Enemy ด้วยกันเอง)
        if (!other.CompareTag("Enemy") && !other.isTrigger)
        {
            DestroySnowball();
        }
    }

    /// <summary>
    /// ผู้เล่นตีหิมะให้แตก — เรียกจาก SmartAttackSystem
    /// </summary>
    public void TakeDamage(int dmg)
    {
        if (_destroyed) return;
        hp -= dmg;
        Debug.Log($"[Snowball] ถูกตี! HP={hp}");
        if (hp <= 0) DestroySnowball();
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