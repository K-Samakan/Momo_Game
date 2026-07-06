/// <summary>
/// Interface กลางสำหรับ object ที่สามารถรับดาเมจ + knockback ได้
/// ผู้เล่น (PlayerHealth หรือชื่อ script ที่ใช้อยู่) ต้อง implement interface นี้
/// </summary>
public interface IDamageable
{
    /// <param name="damage">จำนวนดาเมจ</param>
    /// <param name="knockbackDirection">ทิศทาง knockback (normalized, แนวนอน)</param>
    /// <param name="knockbackForce">แรง knockback</param>
    void TakeDamage(int damage, UnityEngine.Vector3 knockbackDirection, float knockbackForce);
}