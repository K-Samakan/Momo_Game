using UnityEngine;

/// <summary>
/// ติดสคริปต์นี้บน fistProjectilePrefab
/// ต้องมี Rigidbody (Use Gravity = false) และ Collider (Is Trigger = true) บน prefab ด้วย
/// </summary>
public class FistProjectile : MonoBehaviour
{
    [Header("Damage")]
    public int damage = 1;
    public float knockbackForce = 8f;

    [Header("Target")]
    [Tooltip("ต้องตรงกับ tag ที่ BossMap2BT ใช้ FindGameObjectWithTag ด้วย (ปกติคือ \"Player\")")]
    public string targetTag = "Player";

    [Header("VFX (optional)")]
    public GameObject hitVFX;
    public float hitVFXDuration = 2f;

    private Vector3 _moveDirection;
    private bool _hasHit = false;

    /// <summary>เรียกจาก BossMap2BT ตอน Spawn เพื่อกำหนดดาเมจ/ทิศ/แรง knockback ของหมัดลูกนี้</summary>
    public void Init(int damage, Vector3 moveDirection, float knockbackForce)
    {
        this.damage = damage;
        _moveDirection = moveDirection;
        this.knockbackForce = knockbackForce;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;

        if (other.CompareTag(targetTag))
        {
            _hasHit = true;

            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector3 knockDir = _moveDirection.sqrMagnitude > 0.01f
                    ? _moveDirection
                    : (other.transform.position - transform.position).normalized;
                knockDir.y = 0f;
                damageable.TakeDamage(damage, knockDir.normalized, knockbackForce);
            }
            else
            {
                Debug.LogWarning($"[FistProjectile] '{other.name}' มี tag '{targetTag}' แต่ยังไม่ implement IDamageable");
            }

            if (hitVFX != null)
                Destroy(Instantiate(hitVFX, transform.position, Quaternion.identity), hitVFXDuration);

            Destroy(gameObject);
        }
    }
}