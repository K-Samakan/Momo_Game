using UnityEngine;

/// <summary>
/// ระบบ HP ของบอสด่าน 3 — หุ่นยนต์ Snowman
/// HP = 40, Soul Drop ทุก -10 HP
/// Fire Arrow: เรียก OnFireArrowHit() จากระบบธนูไฟ
/// </summary>
public class BossMap3Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 40;
    private int _currentHP;
    private int _lastSoulDropHP;

    [Header("References")]
    public BossMap3BT bossController;
    public BossMap3UI bossUI;
    public BossSFX bossAudio;
    public GameObject soulPrefab;

    [Header("Damage Cooldown")]
    public float damageCooldown = 0.1f;
    private float _lastDamageTime = -1f;

    private void Awake()
    {
        _currentHP = maxHP;
        _lastSoulDropHP = maxHP;

        if (bossAudio == null)
        {
            bossAudio = GetComponentInChildren<BossSFX>();
            if (bossAudio == null)
                Debug.LogWarning("⚠️ BossMap3Health: ไม่พบ BossSFX → บอสจะไม่มีเสียงโดนตี/ตาย");
        }
    }

    /// <param name="isDot">true = ดาเมจไหม้จากธนูไฟ (IgnitableTarget) — บอสไม่สะดุ้ง/ไม่นับ combo</param>
    public void TakeDamage(int damage, bool isDot = false)
    {
        if (_currentHP <= 0) return;
        if (Time.time - _lastDamageTime < damageCooldown) return;
        _lastDamageTime = Time.time;

        _currentHP = Mathf.Max(0, _currentHP - damage);
        bossUI.UpdateHealthBar(_currentHP);

        Debug.Log($"💔 [BossMap3] HP: {_currentHP}/{maxHP}");

        // ✅ เสียงโดนตี (เฉพาะตอนยังไม่ตาย)
        if (_currentHP > 0 && bossAudio != null)
            bossAudio.PlayHurt();

        // Soul Drop ทุก HP -10
        if (_lastSoulDropHP - _currentHP >= 10)
        {
            _lastSoulDropHP -= 10;
            DropSouls(6);
        }

        bossController.OnHitByPlayer(isDot);

        if (_currentHP <= 0)
        {
            Debug.Log("💀 [BossMap3] HP = 0 → OnDead()");

            // ✅ เสียงตาย
            if (bossAudio != null)
                bossAudio.PlayDeath();

            bossController.OnDead();
            bossUI.HideBossUI();
        }
    }

    /// <summary>
    /// เรียกจากระบบธนูไฟ → ส่งต่อให้ BossMap3BT จัดการ stun
    /// </summary>
    public void OnFireArrowHit()
    {
        if (_currentHP <= 0) return;
        bossController.OnFireArrowHit();
    }

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