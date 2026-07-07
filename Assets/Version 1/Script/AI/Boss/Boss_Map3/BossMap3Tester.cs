using UnityEngine;

/// <summary>
/// BossMap3Tester — กดปุ่มทดสอบ Skill ของ Boss Map3 (หุ่นยนต์ Snowman)
/// 
/// ติดบน BossRoot → ลาก BossMap3BT + BossMap3Health ใส่
/// 
/// ปุ่มกด:
///   F1 = SnowballThrow (ปาหิมะ 3 ลูก — เล่น SlamRight chain)
///   F2 = SpinAttack (T-pose หมุน 5 รอบ)
///   F3 = Stagger (สตั้นธรรมดา)
///   F4 = จำลองโดนธนูไฟ 1 ครั้ง (กด 3 ครั้งติดกัน → stun 20 วิ)
///   F5 = Force Death (บังคับตาย)
///   
/// ⚠️ ลบออกหรือปิดก่อน Build จริง!
/// </summary>
public class BossMap3Tester : MonoBehaviour
{
    [Header("References")]
    public BossMap3BT boss;
    public BossMap3Health health;

    [Header("Settings")]
    public bool showOnScreenUI = true;
    public bool enableInBuild = false;

    void Update()
    {
#if UNITY_EDITOR
        HandleInput();
#else
        if (enableInBuild) HandleInput();
#endif
    }

    void HandleInput()
    {
        if (boss == null) return;

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log("[BossTest] ❄️ F1 → SnowballThrow");
            boss.animator.SetTrigger("SlamRight"); // Animator จะ chain SlamRight→SlamLeft→SlamJump เอง
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[BossTest] 🌀 F2 → SpinAttack (T-pose)");
            boss.animator.SetTrigger("Spin");
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[BossTest] 💫 F3 → Stagger");
            boss.animator.SetTrigger("Stagger");
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("[BossTest] 🔥 F4 → จำลองโดนธนูไฟ 1 ครั้ง");
            boss.OnFireArrowHit();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[BossTest] 💀 F5 → Force Death");
            boss.OnDead();
        }
    }

    // ═══════════════════════════════════════════════════
    // On-Screen UI
    // ═══════════════════════════════════════════════════

    void OnGUI()
    {
        if (!showOnScreenUI) return;

#if UNITY_EDITOR
        DrawTestUI();
#else
        if (enableInBuild) DrawTestUI();
#endif
    }

    void DrawTestUI()
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        float w = 280, h = 210;
        float x = 10, y = 10; // มุมซ้ายบน (กันชนกับ Debug ของ BossMap3BT ที่อยู่ขวาบน)

        GUI.Box(new Rect(x, y, w, h), "", boxStyle);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = Color.white } };
        GUIStyle titleStyle = new GUIStyle(labelStyle) { fontStyle = FontStyle.Bold, fontSize = 15, normal = { textColor = Color.cyan } };

        float lx = x + 10, ly = y + 8, lineH = 22;

        GUI.Label(new Rect(lx, ly, w, lineH), "⛄ Boss Map3 Tester", titleStyle); ly += lineH + 4;
        GUI.Label(new Rect(lx, ly, w, lineH), "F1  →  ❄️ SnowballThrow", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F2  →  🌀 SpinAttack (T-pose)", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F3  →  💫 Stagger", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F4  →  🔥 จำลองโดนธนูไฟ (×3=stun)", labelStyle); ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F5  →  💀 Force Death", labelStyle); ly += lineH + 4;

        if (boss != null)
        {
            GUI.Label(new Rect(lx, ly, w, lineH),
                $"State: {boss.CurrentState}  |  Dist: {boss.DistanceToPlayer:F1}", labelStyle);
        }
    }
}