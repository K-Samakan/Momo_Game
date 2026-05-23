// ============================================================
// FILE: GloveShockwave.cs
// ============================================================
using UnityEngine;
using MOMO.AI.Monster;

public class GloveShockwave : MonoBehaviour
{
    [Header("Settings")]
    public float speed = 12f;
    public float maxDistance = 6f;
    public float hitRadius = 0.5f;
    public int damage = 2;
    public float lifetime = 0.5f;

    private Vector3 _startPos;
    private Vector3 _direction;
    private float _timer = 0f;
    private bool _hasHit = false;

    // ─────────────────────────────────────────────
    public void Launch(Vector3 direction, int dmg)
    {
        _startPos = transform.position;
        _direction = direction.normalized;
        damage = dmg;
        _timer = 0f;
        _hasHit = false;
    }

    void Update()
    {
        if (_hasHit) return;

        // ── พุ่งออกไปข้างหน้า ──
        transform.position += _direction * speed * Time.deltaTime;
        _timer += Time.deltaTime;

        // ── ตรวจ Hit ──
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
            {
                DealDamage(hit);
                _hasHit = true;
                DestroyShockwave();
                return;
            }
            if (hit.CompareTag("BreakableWall"))
            {
                hit.GetComponent<BreakableWall>()?.Break();
                _hasHit = true;
                DestroyShockwave();
                return;
            }
        }

        // ── หมดระยะหรือหมดเวลา ──
        float dist = Vector3.Distance(transform.position, _startPos);
        if (dist >= maxDistance || _timer >= lifetime)
            DestroyShockwave();
    }

    void DestroyShockwave()
    {
        // หยุด Particle แล้วรอให้ fade หาย
        var ps = GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Stop();

        Destroy(gameObject, 0.5f);
    }

    void DealDamage(Collider enemyCol)
    {
        MonsterBT bt = enemyCol.GetComponent<MonsterBT>();
        if (bt != null)
        {
            bt.TakeDamage(damage);
            Debug.Log($"[Shockwave] 💥 Hit: {enemyCol.name} | DMG: {damage}");
            HapticManager.Instance?.Vibrate(0.15f, 0.5f, 0.12f);
            return;
        }

        MonsterHealth mh = enemyCol.GetComponent<MonsterHealth>();
        if (mh != null && mh.IsAlive())
        {
            mh.TakeDamage(damage, enemyCol.transform.position);
            Debug.Log($"[Shockwave] 💥 Hit: {enemyCol.name} | DMG: {damage}");
            HapticManager.Instance?.Vibrate(0.15f, 0.5f, 0.12f);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}