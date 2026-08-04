using UnityEngine;

/// <summary>
/// BossMap4Tester — กดปุ่มทดสอบสกิลของ Boss Map4 (หุ่นยนต์หมึกยักษ์)
///
/// ติดบน BossRoot → ลาก BossMap4BT + BossMap4Health ใส่
///
/// ปุ่มกด:
///   F1 = 🐙 TentacleLunge (พุ่งหนวด ATK=3)
///   F2 = 🌊 WaveJump (กระโดดคลื่นน้ำ 5 ครั้ง ATK=3/ครั้ง)
///   F3 = 💫 Stagger (สตั้นธรรมดา)
///   F4 = 🪝 จำลองโดนเบ็ดยืด 1 ครั้ง (กดติดกัน 5 ครั้ง → ดาเมจเพิ่ม 3)
///   F5 = 💀 Force Death (บังคับตาย)
///
/// ⚠️ ลบออกหรือปิด GameObject นี้ก่อน Build จริง!
/// </summary>
public class BossMap4Tester : MonoBehaviour
{
    public BossMap4BT boss;
    public BossMap4Health health;

    void Update()
    {
        if (boss == null || health == null) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            boss.DebugForceLunge();
            Debug.Log("[Tester] F1 → TentacleLunge");
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            boss.DebugForceWaveJump();
            Debug.Log("[Tester] F2 → WaveJump");
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            boss.DebugForceStagger();
            Debug.Log("[Tester] F3 → Stagger");
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            health.OnHookHit();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            health.TakeDamage(9999);
        }
    }

    void OnGUI()
    {
        int w = 360, h = 210;
        int x = 10, y = 10;
        GUI.Box(new Rect(x, y, w, h), "");

        var titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        titleStyle.normal.textColor = Color.cyan;
        labelStyle.normal.textColor = Color.yellow;

        float lx = x + 10;
        float ly = y + 8;
        float lineH = 22;

        GUI.Label(new Rect(lx, ly, w, lineH), "🐙 Boss Map4 Tester", titleStyle);
        ly += lineH + 4;

        GUI.Label(new Rect(lx, ly, w, lineH), "F1  →  🐙 TentacleLunge", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F2  →  🌊 WaveJump (x5)", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F3  →  💫 Stagger", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F4  →  🪝 จำลองโดนเบ็ดยืด (กด 5 ครั้งติด)", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F5  →  💀 Force Death", labelStyle); ly += lineH + 4;

        if (boss != null)
        {
            GUI.Label(new Rect(lx, ly, w, lineH), $"State: {boss.CurrentState}  |  Dist: {boss.DistanceToPlayer:F1}", labelStyle);
            ly += lineH;
        }
        if (health != null)
        {
            GUI.Label(new Rect(lx, ly, w, lineH), $"HP: {health.GetCurrentHP()}/{health.maxHP}  |  Hook streak: {health.HookHitStreak}", labelStyle);
        }
    }
}