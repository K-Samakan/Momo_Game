using UnityEngine;

public class BossMap1Health : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 50;
    private int _currentHP;
    private int _lastSoulDropHP;

    [Header("References")]
    public BossMap1BT bossController;
    public BossMap1UI bossUI;
    public GameObject soulPrefab;

    [Header("Damage Cooldown")]
    public float damageCooldown = 0.1f;
    private float _lastDamageTime = -1f;

    private void Awake()
    {
        _currentHP = maxHP;
        _lastSoulDropHP = maxHP;

        // ✅ ตรวจสอบว่ามี bossController หรือไม่
        if (bossController == null)
        {
            Debug.LogError("❌ BossMap1Health: ไม่ได้ต่อ bossController ใน Inspector!");
        }
        else
        {
            Debug.Log("✅ BossMap1Health: เชื่อมต่อ bossController สำเร็จ");
        }
    }

    /// <summary>
    /// ดาเมจปกติจากผู้เล่น — มี Cooldown กันตีถี่/hitbox ซ้อนกันในดาบเดียว
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (_currentHP <= 0) return;

        // ✅ กัน damage ซ้ำถี่เกินไป
        if (Time.time - _lastDamageTime < damageCooldown) return;
        _lastDamageTime = Time.time;

        ApplyDamageInternal(damage);
    }

    /// <summary>
    /// ✅ ใช้สำหรับดาเมจพิเศษ (เช่น Tiger Assist) ที่ไม่ควรติด Damage Cooldown ปกติ
    /// เพราะมักถูกเรียกในเฟรมเดียวกับที่ผู้เล่นตี (ผ่าน OnHitByPlayer chain)
    /// ถ้าใช้ TakeDamage ธรรมดาจะโดน cooldown ของดาบผู้เล่นบล็อกไปเงียบๆ
    /// </summary>
    public void TakeDamageBypassCooldown(int damage)
    {
        if (_currentHP <= 0) return;

        Debug.Log($"<color=#FFD700>[BossMap1Health] 🐯 รับดาเมจพิเศษ (ไม่ติด cooldown): {damage}</color>");
        ApplyDamageInternal(damage);
    }

    /// <summary>
    /// Logic กลางที่ใช้ร่วมกันทั้ง TakeDamage และ TakeDamageBypassCooldown
    /// </summary>
    private void ApplyDamageInternal(int damage)
    {
        _currentHP = Mathf.Max(0, _currentHP - damage);
        bossUI.UpdateHealthBar(_currentHP);

        // ✅ Debug HP
        Debug.Log($"💔 Boss HP: {_currentHP}/{maxHP}");

        // Soul Drop ทุก HP -10
        if (_lastSoulDropHP - _currentHP >= 10)
        {
            _lastSoulDropHP -= 10;
            DropSouls(5);
        }

        bossController.OnHitByPlayer();

        if (_currentHP <= 0)
        {
            // ✅ Debug ตาย
            Debug.Log("💀 Boss HP = 0 → เรียก OnDead()");
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