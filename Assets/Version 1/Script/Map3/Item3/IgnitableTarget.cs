// ============================================================
// FILE: IgnitableTarget.cs
// PATH: Assets/Version 1/Script/Map3/Item3/IgnitableTarget.cs
// ติดตั้งอัตโนมัติ (AddComponent) บนศัตรูที่โดนธนูไฟ — จัดการ DOT
// ============================================================
using System.Collections;
using UnityEngine;
using MOMO.AI.Monster;

public class IgnitableTarget : MonoBehaviour
{
    [Header("Visual")]
    public GameObject burningVFXPrefab; // ไฟลุกบนตัวมอนสเตอร์ระหว่างติดไฟ

    private bool _isBurning = false;
    private GameObject _activeVFX;

    /// <summary>
    /// เรียกจาก FireArrowProjectile — ยิงซ้ำระหว่างติดไฟจะ "รีเซ็ต" ระยะเวลาการติดไฟใหม่ (เหมือนเกม)
    /// </summary>
    public void ApplyIgnite(int damagePerTick, int ticks, float tickInterval)
    {
        StopAllCoroutines();
        StartCoroutine(BurnRoutine(damagePerTick, ticks, tickInterval));
    }

    IEnumerator BurnRoutine(int damagePerTick, int ticks, float tickInterval)
    {
        _isBurning = true;

        if (burningVFXPrefab != null && _activeVFX == null)
            _activeVFX = Instantiate(burningVFXPrefab, transform.position, Quaternion.identity, transform);

        var monsterBT = GetComponent<MonsterBT>();          // ระบบหลัก (ตายแล้วดรอป Soul)
        var monsterHealth = GetComponent<MonsterHealth>();
        var bossHealth = GetComponentInParent<BossMap3Health>();

        for (int i = 0; i < ticks; i++)
        {
            yield return new WaitForSeconds(tickInterval);

            if (monsterBT != null)
                monsterBT.TakeDamage(damagePerTick);
            else if (monsterHealth != null)
                monsterHealth.TakeDamage(damagePerTick, transform.position);
            else if (bossHealth != null)
                bossHealth.TakeDamage(damagePerTick, isDot: true); // ✅ ไหม้ไม่ทำให้บอสสะดุ้ง (กัน stun-lock)

            Debug.Log($"[IgnitableTarget] 🔥 Burn tick {i + 1}/{ticks} → DMG {damagePerTick}");
        }

        _isBurning = false;
        if (_activeVFX != null) Destroy(_activeVFX);
    }
}