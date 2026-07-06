using UnityEngine;

/// <summary>
/// BossMap2Tester — กดปุ่มทดสอบ Skill ของ Boss Map2
/// 
/// ติดบน BossRoot → ลาก BossMap2BT ใส่ช่อง boss
/// 
/// ปุ่มกด:
///   F1 = FistSummon (หมัด 3 ลูก)
///   F2 = AoE Attack (คลื่นพลังรอบตัว)
///   F3 = Stagger (สตั้น)
///   F4 = Force Death (บังคับตาย)
///   F5 = Reset HP (เติม HP เต็ม)
///   
/// ⚠️ ลบออกก่อน Build จริง!
/// </summary>
public class BossMap2Tester : MonoBehaviour
{
    [Header("References")]
    public BossMap2BT boss;
    public BossMap2Health health;

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
            Debug.Log("[BossTest] 👊 F1 → FistSummon");
            ForceSkill("FistSummon");
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            Debug.Log("[BossTest] 💥 F2 → AoE Attack");
            ForceSkill("AoE");
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("[BossTest] 💫 F3 → Stagger");
            ForceSkill("Stagger");
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            Debug.Log("[BossTest] 💀 F4 → Force Death");
            if (boss != null) boss.OnDead();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("[BossTest] 💚 F5 → Reset HP");
            ResetHP();
        }
    }

    void ForceSkill(string skill)
    {
        if (boss == null) return;

        // เรียก Animator Trigger ตรงๆ
        switch (skill)
        {
            case "FistSummon":
                boss.animator.SetTrigger("SlamRight");
                Debug.Log("[BossTest] → Trigger SlamRight (FistSummon chain)");
                break;

            case "AoE":
                boss.animator.SetTrigger("Summon");
                // Spawn VFX ที่พื้น
                AoeGroundIndicator.Spawn(
                    boss.transform.position,
                    boss.aoeRadius,
                    boss.aoeWindUpDelay
                );
                // ทำ damage หลัง wind up
                StartCoroutine(DelayedAoE());
                break;

            case "Stagger":
                boss.animator.SetTrigger("Stagger");
                Debug.Log("[BossTest] → Trigger Stagger");
                break;
        }
    }

    System.Collections.IEnumerator DelayedAoE()
    {
        yield return new WaitForSeconds(boss.aoeWindUpDelay);

        // หาผู้เล่น
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) yield break;

        float dist = Vector3.Distance(boss.transform.position, player.transform.position);
        Debug.Log($"[BossTest] 💥 AoE ระเบิด! ระยะผู้เล่น: {dist:F1} / รัศมี: {boss.aoeRadius}");

        if (dist <= boss.aoeRadius)
        {
            // Damage
            var ph = player.GetComponent<MOMO.Player.PlayerHealth>();
            if (ph != null) ph.TakeDamage(boss.aoeDamage);

            // Knockback
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
                StartCoroutine(DoKnockback(cc, player.transform, boss.transform.position, boss.aoeKnockbackForce));

            // Haptic
            HapticManager.Instance?.Vibrate(0.4f, 0.8f, 0.2f);

            Debug.Log($"[BossTest] 💥 ผู้เล่นโดน AoE! DMG={boss.aoeDamage}");
        }
        else
        {
            Debug.Log("[BossTest] ✅ ผู้เล่นอยู่นอกระยะ AoE");
        }
    }

    System.Collections.IEnumerator DoKnockback(CharacterController cc, Transform player, Vector3 bossPos, float force)
    {
        Vector3 dir = (player.position - bossPos).normalized;
        dir.y = 0.3f;
        Vector3 knockForce = dir * force;

        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (cc == null || !cc.enabled) yield break;
            float t = 1f - (elapsed / duration);
            cc.Move(knockForce * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void ResetHP()
    {
        if (health != null)
        {
            // ใช้ Reflection หรือ public method
            // ถ้า BossMap2Health ไม่มี Reset → ต้องเพิ่ม
            Debug.Log("[BossTest] ⚠️ ต้องเพิ่ม ResetHP() ใน BossMap2Health");
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
        boxStyle.fontSize = 14;
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.normal.textColor = Color.white;

        float w = 260;
        float h = 180;
        float x = Screen.width - w - 10;
        float y = 10;

        GUI.Box(new Rect(x, y, w, h), "", boxStyle);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 13;
        labelStyle.normal.textColor = Color.white;

        GUIStyle titleStyle = new GUIStyle(labelStyle);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.fontSize = 15;
        titleStyle.normal.textColor = Color.yellow;

        float lx = x + 10;
        float ly = y + 8;
        float lineH = 22;

        GUI.Label(new Rect(lx, ly, w, lineH), "🎮 Boss Map2 Tester", titleStyle);
        ly += lineH + 4;

        GUI.Label(new Rect(lx, ly, w, lineH), "F1  →  👊 FistSummon (หมัด 3 ลูก)", labelStyle);
        ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F2  →  💥 AoE Attack (คลื่นพลัง)", labelStyle);
        ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F3  →  💫 Stagger", labelStyle);
        ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F4  →  💀 Force Death", labelStyle);
        ly += lineH;
        GUI.Label(new Rect(lx, ly, w, lineH), "F5  →  💚 Reset HP", labelStyle);
        ly += lineH + 4;

        // แสดงสถานะปัจจุบัน
        if (boss != null)
        {
            string state = boss.CurrentState;
            GUI.Label(new Rect(lx, ly, w, lineH),
                $"State: {state}  |  Dist: {boss.DistanceToPlayer:F1}", labelStyle);
        }
    }
}