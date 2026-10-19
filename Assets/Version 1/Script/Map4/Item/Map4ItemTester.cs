using UnityEngine;
using MOMO.Player;

/// <summary>
/// Map4ItemTester — สลับสวม/ถอด "เบ็ดยืด" (Map4 Item) แบบชั่วคราว
/// สำหรับทดสอบกับบอสเท่านั้น ไม่แตะ Puzzle / Save / Pickup flag ใด ๆ
///
/// วิธีติดตั้ง:
///  1. สร้าง Empty GameObject ชื่อ "Map4ItemTester" (วางไว้ใกล้ ๆ BossMap4Tester ก็ได้)
///  2. ใส่ Component นี้
///  3. ลาก Reference ทั้ง 3 ช่องด้านล่างใส่ให้ครบ
///
/// ⚠️ ลบออกหรือปิด GameObject นี้ก่อน Build จริง!
///
/// หมายเหตุเรื่อง HP Bonus:
///  - ใช้การ set ค่า maxHealth/currentHealth ตรง ๆ แทนการเรียก PlayerHealth.Heal()
///  - เพราะ Heal() จะยิง event OnHPChanged ซึ่งไป trigger infinite loop บั๊กที่มีอยู่ใน
///    PlayerHealthUI.cs (UpdateHPDisplay -> AddHPSlot ถ้าไม่มี hpSlotPrefab/hpSlotContainer
///    จะวนลูปไม่รู้จบจน Unity ค้าง) — การ set ค่าตรง ๆ ไม่ยิง event นี้เลย จึงปลอดภัย
///  - บั๊กต้นเหตุยังอยู่ใน PlayerHealthUI.cs เหมือนเดิม อันนี้เป็นแค่ workaround ฝั่ง Tester
/// </summary>
public class Map4ItemTester : MonoBehaviour
{/*
    [Header("=== References (ลากจาก Player) ===")]
    [Tooltip("FishingRodController ที่อยู่ใต้ Player")]
    public FishingRodController rodController;

    [Tooltip("Mesh โมเดลเบ็ดยืดที่ติดอยู่กับตัว Player (เช่น mesh_0 ใต้มือ/หลัง Player) — ตัวที่ต้องโชว์/ซ่อน")]
    public GameObject itemMeshObject;

    [Tooltip("PlayerHealth ของผู้เล่น (ถ้าอยากให้ HP บวก/ลบตอน Test ด้วยเหมือนของจริง)")]
    public PlayerHealth playerHealth;

    [Header("=== Auto Equip ===")]
    [Tooltip("ติ๊กไว้ = สวมเบ็ดยืดให้อัตโนมัติทันทีตอนเริ่ม Play ไม่ต้องกด F6")]
    public bool equipOnStart = false;

    [Header("=== Test Settings ===")]
    public KeyCode toggleKey = KeyCode.F6;

    [Tooltip("บวก HP ตอน Test เหมือนของจริงไหม (ปิดไว้ถ้าไม่อยากให้ HP เพี้ยนตอนทดสอบซ้ำ ๆ)")]
    public bool alsoApplyHpBonus = false;
    public int hpBonus = 2;

    [Header("=== Debug Display ===")]
    [Tooltip("ปิดไว้ = ซ่อนข้อความสถานะที่มุมจอ")]
    public bool showStatusText = true;

    [Header("=== Status (Read Only) ===")]
    public bool isTestEquipped = false;

    void Start()
    {
        if (equipOnStart)
        {
            Equip();
            Debug.Log("[Map4ItemTester] 🎣 Auto-Equip ตอนเริ่มเกม (equipOnStart = true)");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            Toggle();
        }
    }

    public void Toggle()
    {
        if (isTestEquipped) Unequip();
        else Equip();
    }

    void Equip()
    {
        if (rodController == null)
        {
            Debug.LogWarning("[Map4ItemTester] ⚠️ ไม่ได้ลาก FishingRodController ใส่!");
            return;
        }

        // 1) บวกค่า ATK/Range/Oxygen
        rodController.OnEquip();

        // 2) โชว์โมเดล
        if (itemMeshObject != null)
            itemMeshObject.SetActive(true);
        else
            Debug.LogWarning("[Map4ItemTester] ⚠️ ไม่ได้ลาก itemMeshObject (mesh_0) ใส่ — จะไม่เห็นโมเดล!");

        // 3) HP (ถ้าเปิดใช้) — ✅ ตั้งค่าตรง ๆ แทนการเรียก Heal()
        //    เพราะ Heal() จะยิง event OnHPChanged ที่ไปชน infinite loop บั๊กใน PlayerHealthUI.cs
        //    (บั๊กนั้นเกิดตอน spawn HP slot ไม่สำเร็จแล้ววนลูปไม่รู้จบ) — เลี่ยงได้โดยไม่ต้อง Heal()
        if (alsoApplyHpBonus && playerHealth != null)
        {
            playerHealth.maxHealth += hpBonus;
            playerHealth.currentHealth = Mathf.Min(playerHealth.currentHealth + hpBonus, playerHealth.maxHealth);

            // อัปเดต UI แบบปลอดภัย (ระบบ healthText/healthSlider เดิมใน PlayerHealth.cs เอง — ไม่ใช่ตัวที่มีบั๊ก)
            playerHealth.UpdateHealthUI();
        }

        isTestEquipped = true;
        Debug.Log("[Map4ItemTester] 🎣 TEST EQUIP — สวมเบ็ดยืดชั่วคราวแล้ว");
    }

    void Unequip()
    {
        if (rodController != null)
            rodController.OnUnequip();

        if (itemMeshObject != null)
            itemMeshObject.SetActive(false);

        if (alsoApplyHpBonus && playerHealth != null)
        {
            playerHealth.maxHealth -= hpBonus;
            playerHealth.currentHealth = Mathf.Min(playerHealth.currentHealth, playerHealth.maxHealth);

            // ✅ ใช้ UpdateHealthUI() แทน Heal()/TakeDamage() เหมือนกัน — ปลอดภัยจากบั๊กเดิม
            playerHealth.UpdateHealthUI();
        }

        isTestEquipped = false;
        Debug.Log("[Map4ItemTester] 🎣 TEST UNEQUIP — ถอดเบ็ดยืดแล้ว");
    }

    void OnGUI()
    {
        if (!showStatusText) return; // ✅ ปิดตรงนี้ก็ไม่ขึ้นข้อความเลย

        GUI.Label(new Rect(10, Screen.height - 40, 400, 30),
            $"[F6] Map4 Item Test: {(isTestEquipped ? "✅ EQUIPPED" : "❌ NOT EQUIPPED")}");
    }*/
}