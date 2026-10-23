using UnityEngine;

/// <summary>
/// ระบบ HP ของบอสด่าน 2 — หุ่นยนต์พ่อมด
/// HP = 40, Soul Drop ทุก -10 HP
/// </summary>
public class BossMap2Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 40;
    private int _currentHP;
    private int _lastSoulDropHP;

    [Header("References")]
    public BossMap2BT bossController;
    public BossMap2UI bossUI;
    public BossSFX bossAudio;
    public GameObject soulPrefab;

    [Header("Damage Cooldown")]
    public float damageCooldown = 0.1f;
    private float _lastDamageTime = -1f;

    private void Awake()
    {
        _currentHP = maxHP;
        _lastSoulDropHP = maxHP;

        if (bossController == null)
            Debug.LogError("❌ BossMap2Health: ไม่ได้ต่อ bossController ใน Inspector!");
        else
            Debug.Log("✅ BossMap2Health: เชื่อมต่อ bossController สำเร็จ");

        if (bossAudio == null)
        {
            bossAudio = GetComponentInChildren<BossSFX>();
            if (bossAudio == null)
                Debug.LogWarning("⚠️ BossMap2Health: ไม่พบ BossSFX → บอสจะไม่มีเสียงโดนตี/ตาย");
        }
    }

    public void TakeDamage(int damage)
    {
        if (_currentHP <= 0) return;

        if (Time.time - _lastDamageTime < damageCooldown) return;
        _lastDamageTime = Time.time;

        _currentHP = Mathf.Max(0, _currentHP - damage);
        bossUI.UpdateHealthBar(_currentHP);

        Debug.Log($"💔 [BossMap2] HP: {_currentHP}/{maxHP}");

        // ✅ เสียงโดนตี (เฉพาะตอนยังไม่ตาย)
        if (_currentHP > 0 && bossAudio != null)
            bossAudio.PlayHurt();

        // Soul Drop ทุก HP -10
        if (_lastSoulDropHP - _currentHP >= 10)
        {
            _lastSoulDropHP -= 10;
            DropSouls(5);
        }

        bossController.OnHitByPlayer();

        if (_currentHP <= 0)
        {
            Debug.Log("💀 [BossMap2] HP = 0 → เรียก OnDead()");

            // ✅ เสียงตาย
            if (bossAudio != null)
                bossAudio.PlayDeath();

            bossController.OnDead();
            bossUI.HideBossUI();
        }
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