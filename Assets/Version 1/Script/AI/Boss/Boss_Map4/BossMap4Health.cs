using UnityEngine;

/// <summary>
/// ระบบ HP ของบอสด่าน 4 — หุ่นยนต์หมึกยักษ์ (Giant Octopus Robot)
/// HP = 50, Soul Drop ทุก -10 HP (ดรอป 7 ชิ้น)
///
/// จุดอ่อน: เบ็ดยืด (ไอเทมประจำด่าน 4)
///   - เรียก OnHookHit() จากระบบเบ็ดยืด "ทุกครั้ง" ที่โจมตีโดนบอส
///   - ถ้าโดนติดกันครบ hookHitsRequired ครั้ง (ดีฟอลต์ = 5) จะได้รับดาเมจเพิ่ม hookBonusDamage หน่วย
///   - ดาเมจปกติของเบ็ดยืด (ATK+2) ให้เรียก TakeDamage(damage) แยกต่างหาก
///     OnHookHit() มีหน้าที่แค่ "นับ streak" และให้ดาเมจโบนัสเท่านั้น
/// </summary>
public class BossMap4Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 50;
    private int _currentHP;
    private int _lastSoulDropHP;

    [Header("References")]
    public BossMap4BT bossController;
    public BossMap4UI bossUI;
    public BossSFX bossAudio;
    public GameObject soulPrefab;

    [Header("Soul Drop")]
    [Tooltip("ดรอป Soul ทุกครั้งที่เสีย HP ครบตามจำนวนนี้")]
    public int soulDropInterval = 10;
    [Tooltip("จำนวน Soul ที่ดรอปต่อครั้ง (ตามเอกสาร = 7)")]
    public int soulDropCount = 7;

    [Header("Damage Cooldown")]
    [Tooltip("กันโดนดาเมจซ้อนจากหลาย Collider ในเฟรมเดียวกัน")]
    public float damageCooldown = 0.1f;
    private float _lastDamageTime = -1f;

    [Header("🪝 จุดอ่อน: เบ็ดยืด (Hook Weakness)")]
    [Tooltip("จำนวนครั้งที่ต้องโดนเบ็ดยืด \"ติดกัน\" เพื่อรับดาเมจเพิ่ม")]
    public int hookHitsRequired = 5;

    [Tooltip("ดาเมจเพิ่มเติมเมื่อโดนเบ็ดยืดครบตามจำนวน")]
    public int hookBonusDamage = 3;

    [Tooltip("ถ้าไม่โดนเบ็ดยืดซ้ำภายในเวลานี้ (วินาที) จะเริ่มนับ streak ใหม่")]
    public float hookStreakTimeout = 5f;

    private int _hookHitStreak = 0;
    private float _lastHookHitTime = -999f;

    public int HookHitStreak => _hookHitStreak;

    private void Awake()
    {
        _currentHP = maxHP;
        _lastSoulDropHP = maxHP;

        if (bossAudio == null)
        {
            bossAudio = GetComponentInChildren<BossSFX>();
            if (bossAudio == null)
                Debug.LogWarning("⚠️ BossMap4Health: ไม่พบ BossSFX → บอสจะไม่มีเสียงโดนตี/ตาย");
        }
    }

    /// <summary>ดาเมจปกติ — เรียกจากระบบโจมตีทั่วไป</summary>
    public void TakeDamage(int damage)
    {
        if (_currentHP <= 0) return;
        if (Time.time - _lastDamageTime < damageCooldown) return;
        _lastDamageTime = Time.time;

        _currentHP = Mathf.Max(0, _currentHP - damage);
        bossUI?.UpdateHealthBar(_currentHP);

        Debug.Log($"💔 [BossMap4] HP: {_currentHP}/{maxHP}");

        // ✅ เสียงโดนตี (เฉพาะตอนยังไม่ตาย)
        if (_currentHP > 0 && bossAudio != null)
            bossAudio.PlayHurt();

        if (_lastSoulDropHP - _currentHP >= soulDropInterval)
        {
            _lastSoulDropHP -= soulDropInterval;
            DropSouls(soulDropCount);
        }

        bossController?.OnHitByPlayer();

        if (_currentHP <= 0)
        {
            Debug.Log("💀 [BossMap4] HP = 0 → OnDead()");

            // ✅ เสียงตาย
            if (bossAudio != null)
                bossAudio.PlayDeath();

            bossController?.OnDead();
            bossUI?.HideBossUI();
        }
    }

    /// <summary>
    /// เรียกจากระบบเบ็ดยืดทุกครั้งที่โจมตีโดนบอสสำเร็จ
    /// ครบ 5 ครั้งติด (ค่าดีฟอลต์) จะได้ดาเมจเพิ่ม 3 หน่วย
    /// </summary>
    public void OnHookHit()
    {
        if (_currentHP <= 0) return;

        if (Time.time - _lastHookHitTime > hookStreakTimeout)
            _hookHitStreak = 0;

        _lastHookHitTime = Time.time;
        _hookHitStreak++;

        Debug.Log($"[BossMap4] 🪝 เบ็ดยืดโดนบอส! streak = {_hookHitStreak}/{hookHitsRequired}");

        if (_hookHitStreak >= hookHitsRequired)
        {
            _hookHitStreak = 0;
            Debug.Log($"[BossMap4] 🪝💥 จุดอ่อนโดนครบ {hookHitsRequired} ครั้งติด! ดาเมจเพิ่ม {hookBonusDamage} หน่วย!");
            TakeDamage(hookBonusDamage);
        }
    }

    /// <summary>ตัด streak ทันที (ไม่บังคับใช้)</summary>
    public void ResetHookStreak() => _hookHitStreak = 0;

    private void DropSouls(int count)
    {
        if (soulPrefab == null) return;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 1.5f;
            offset.y = Mathf.Abs(offset.y) + 0.3f;
            Instantiate(soulPrefab, transform.position + offset, Quaternion.identity);
        }
    }

    public int GetCurrentHP() => _currentHP;
    public bool IsAlive() => _currentHP > 0;
}