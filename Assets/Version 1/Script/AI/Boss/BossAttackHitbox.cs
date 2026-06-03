using UnityEngine;

public class BossAttackHitbox : MonoBehaviour
{
    public int damage = 1;
    private bool _isActive = false;

    public void EnableHitbox(int dmg)
    {
        damage = dmg;
        _isActive = true;
    }

    public void DisableHitbox()
    {
        _isActive = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;
        if (!other.CompareTag("Player")) return;

        var ph = other.GetComponent<MOMO.Player.PlayerHealth>();
        ph?.TakeDamage(damage);

        // ปิด Hitbox หลังโดนครั้งแรก
        _isActive = false;

        HapticManager.Instance?.Vibrate(0.3f, 0.6f, 0.15f);
        Debug.Log($"[BossHitbox] ผู้เล่นโดน {damage} DMG");
    }
}