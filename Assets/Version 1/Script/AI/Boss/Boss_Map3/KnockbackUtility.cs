using UnityEngine;
using System.Collections;

/// <summary>
/// KnockbackUtility — ระบบ Knockback กลางที่ใช้ร่วมกันได้ทุก Boss/Projectile
/// 
/// หลักการ: ทุกครั้งที่สร้าง damage ให้ผู้เล่น ควรเรียก knockback คู่กันเสมอ
/// เพื่อความสม่ำเสมอของ combat feel ทั้งเกม
/// 
/// วิธีใช้ (ใน MonoBehaviour ใดก็ได้):
///   KnockbackUtility.ApplyKnockback(this, playerCC, transform.position, knockbackForce);
/// </summary>
public static class KnockbackUtility
{
    /// <summary>
    /// ผลัก CharacterController ของผู้เล่นออกจากจุด sourcePosition
    /// </summary>
    /// <param name="runner">MonoBehaviour ที่จะใช้รัน Coroutine (ปกติคือ "this" ของผู้เรียก)</param>
    /// <param name="cc">CharacterController ของผู้เล่น</param>
    /// <param name="sourcePosition">ตำแหน่งต้นกำเนิดแรงผลัก (เช่น ตำแหน่งบอส หรือตำแหน่งหิมะตอนชน)</param>
    /// <param name="force">ความแรงแนวราบ</param>
    /// <param name="upward">แรงยกขึ้นแนวตั้ง (กันเตะติดพื้น)</param>
    /// <param name="duration">ระยะเวลา knockback (วินาที)</param>
    public static void ApplyKnockback(MonoBehaviour runner, CharacterController cc, Vector3 sourcePosition,
        float force, float upward = 0.3f, float duration = 0.3f)
    {
        if (cc == null || runner == null || !cc.enabled) return;
        runner.StartCoroutine(KnockbackCoroutine(cc, sourcePosition, force, upward, duration));
    }

    private static IEnumerator KnockbackCoroutine(CharacterController cc, Vector3 sourcePosition,
        float force, float upward, float duration)
    {
        Vector3 dir = (cc.transform.position - sourcePosition).normalized;
        dir.y = upward;
        Vector3 totalForce = dir * force;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (cc == null || !cc.enabled) yield break;
            float t = 1f - (elapsed / duration);
            cc.Move(totalForce * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}