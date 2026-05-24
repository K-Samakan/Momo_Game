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

    private void Awake()
    {
        _currentHP = maxHP;
        _lastSoulDropHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        if (_currentHP <= 0) return;

        _currentHP = Mathf.Max(0, _currentHP - damage);

        // อัปเดต HP Bar
        bossUI.UpdateHealthBar(_currentHP);

        // Soul Drop ทุก HP -10
        if (_lastSoulDropHP - _currentHP >= 10)
        {
            _lastSoulDropHP -= 10;
            DropSouls(5);
        }

        // แจ้ง Combo
        bossController.OnHitByPlayer();

        if (_currentHP <= 0)
            bossController.OnDead();
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