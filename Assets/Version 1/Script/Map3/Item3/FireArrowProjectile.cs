// ============================================================
// FILE: FireArrowProjectile.cs
// PATH: Assets/Version 1/Script/Map3/Item3/FireArrowProjectile.cs
// ✅ [Map3 Side Quest] เพิ่ม: ธนูธรรมดาโดนผลึกน้ำแข็ง → TakeDamage (ธนูไฟใช้ IMeltable เดิม = ละลายทันที)
// ============================================================
using UnityEngine;
using MOMO.AI.Monster;

[RequireComponent(typeof(Rigidbody))]
public class FireArrowProjectile : MonoBehaviour
{
    [Header("VFX")]
    public GameObject fireTrailVFX;   // ต่อไฟระหว่างบิน (เฉพาะ fire arrow)
    public GameObject hitVFX;
    public GameObject fireHitVFX;     // ระเบิดไฟตอนชน (เฉพาะ fire arrow)
    public GameObject iceMeltVFX;     // ตอนชนน้ำแข็งแล้วละลาย

    [Header("Lifetime")]
    public float maxLifeTime = 6f;

    private int _damage;
    private float _speed;
    private bool _isFireArrow;
    private int _igniteTicks;
    private float _igniteTickInterval;
    private Rigidbody _rb;
    private bool _hasHit = false;

    public void Init(int damage, float speed, bool isFireArrow, int igniteTicks, float igniteTickInterval, Vector3 direction)
    {
        _damage = damage;
        _speed = speed;
        _isFireArrow = isFireArrow;
        _igniteTicks = igniteTicks;
        _igniteTickInterval = igniteTickInterval;

        _rb = GetComponent<Rigidbody>();
        _rb.linearVelocity = direction.normalized * speed;
        transform.rotation = Quaternion.LookRotation(direction);

        if (fireTrailVFX != null) fireTrailVFX.SetActive(isFireArrow);

        Destroy(gameObject, maxLifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hasHit) return;

        // ── ละลายน้ำแข็ง (เฉพาะธนูไฟ) — รวมผลึกน้ำแข็ง Side Quest (IceCrystalController : IMeltable) ──
        var meltable = other.GetComponent<IMeltable>();
        if (_isFireArrow && meltable != null)
        {
            meltable.Melt();
            if (iceMeltVFX != null) Instantiate(iceMeltVFX, transform.position, Quaternion.identity);
            HapticManager.Instance?.Vibrate(0.2f, 0.2f, 0.15f);
            ResolveHit();
            return;
        }

        // ── ✅ [Map3 Side Quest] ธนูธรรมดาโดนผลึกน้ำแข็ง → นับ 1 ฮิต ──
        var crystal = other.GetComponentInParent<IceCrystalController>();
        if (crystal != null && !crystal.IsBroken)
        {
            crystal.TakeDamage(_damage);
            ResolveHit();
            return;
        }

        // ── โดนบอส Map3 (จุดอ่อนธนูไฟ — เมธอดนี้เตรียมไว้แล้วใน BossMap3BT) ──
        var bossMap3 = other.GetComponentInParent<BossMap3BT>();
        if (bossMap3 != null)
        {
            var bossHealth = other.GetComponentInParent<BossMap3Health>();
            bossHealth?.TakeDamage(_damage);

            if (_isFireArrow)
                bossMap3.OnFireArrowHit();

            ApplyIgniteIfNeeded(other.gameObject);
            ResolveHit();
            return;
        }

        // ── โดนมอนสเตอร์ (ระบบหลัก MonsterBT) — ตายแล้วดรอป Soul เหมือนโจมตีปกติ ──
        //    ต้องเช็คก่อน MonsterHealth เหมือน SmartAttackSystem / GloveShockwave
        //    (เดิมยิงเข้า MonsterHealth อย่างเดียว → มอนตายด้วยระบบเก่าที่ไม่ดรอป Soul)
        var monsterBT = other.GetComponentInParent<MonsterBT>();
        if (monsterBT != null)
        {
            monsterBT.TakeDamage(_damage);
            ApplyIgniteIfNeeded(monsterBT.gameObject);
            ResolveHit();
            return;
        }

        // ── มอนสเตอร์ที่มีแค่ระบบเก่า (MonsterHealth) ──
        var monster = other.GetComponent<MonsterHealth>();
        if (monster != null)
        {
            monster.TakeDamage(_damage, transform.position);
            ApplyIgniteIfNeeded(other.gameObject);
            ResolveHit();
            return;
        }

        // ── โดนกำแพง/พื้น ──
        if (!other.isTrigger && !other.CompareTag("Player"))
        {
            ResolveHit();
        }
    }

    void ApplyIgniteIfNeeded(GameObject target)
    {
        if (!_isFireArrow) return;

        var ignitable = target.GetComponent<IgnitableTarget>();
        if (ignitable == null) ignitable = target.AddComponent<IgnitableTarget>();

        // ความเสียหายต่อเนื่อง 2 ครั้ง ครั้งละ ATK = 1 หน่วย (ตามสเปค)
        ignitable.ApplyIgnite(damagePerTick: 1, ticks: _igniteTicks, tickInterval: _igniteTickInterval);
    }

    void ResolveHit()
    {
        _hasHit = true;

        GameObject vfx = _isFireArrow && fireHitVFX != null ? fireHitVFX : hitVFX;
        if (vfx != null) Instantiate(vfx, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}

/// <summary>
/// Interface สำหรับ object ที่ละลายได้ด้วยธนูไฟ (เช่น กำแพงน้ำแข็ง, ประตูน้ำแข็ง, ผลึกน้ำแข็ง Side Quest ใน Map3)
/// </summary>
public interface IMeltable
{
    void Melt();
}