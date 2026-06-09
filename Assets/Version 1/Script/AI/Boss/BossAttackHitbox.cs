using UnityEngine;
using System.Collections;

public class BossAttackHitbox : MonoBehaviour
{
    public int damage = 1;
    private bool _isActive = false;
    private SphereCollider _col;

    [Header("Knockback")]
    public float knockbackForce = 8f;
    public float knockbackUpward = 2f;
    public float knockbackDuration = 0.3f;

    void Awake()
    {
        _col = GetComponent<SphereCollider>();
    }

    public void EnableHitbox(int dmg)
    {
        damage = dmg;
        _isActive = true;

        // ✅ เช็คผู้เล่นที่อยู่ข้างใน collider แล้ว (กรณี OnTriggerEnter ไม่ยิง)
        if (_col != null)
        {
            float worldRadius = _col.radius * transform.lossyScale.x;
            Vector3 worldCenter = transform.TransformPoint(_col.center);
            Collider[] hits = Physics.OverlapSphere(worldCenter, worldRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    ApplyDamage(hit);
                    break;
                }
            }
        }
    }

    public void DisableHitbox()
    {
        _isActive = false;
    }

    // ✅ จับผู้เล่นที่เพิ่งเข้ามา (ปกติ)
    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;
        if (!other.CompareTag("Player")) return;
        ApplyDamage(other);
    }

    void ApplyDamage(Collider player)
    {
        if (!_isActive) return;

        // 1) ดาเมจ
        var ph = player.GetComponent<MOMO.Player.PlayerHealth>();
        ph?.TakeDamage(damage);

        // 2) Knockback
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
            StartCoroutine(DoKnockback(cc, player.transform));

        // 3) ปิด hitbox กันโดนซ้ำ
        _isActive = false;

        // 4) Haptic
        HapticManager.Instance?.Vibrate(0.3f, 0.6f, 0.15f);
        Debug.Log($"[BossHitbox] ผู้เล่นโดน {damage} DMG + Knockback");
    }

    IEnumerator DoKnockback(CharacterController cc, Transform player)
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        Vector3 force = dir * knockbackForce + Vector3.up * knockbackUpward;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            if (cc == null || !cc.enabled) yield break;
            float t = 1f - (elapsed / knockbackDuration);
            cc.Move(force * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}