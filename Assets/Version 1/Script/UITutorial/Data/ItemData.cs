using UnityEngine;

namespace Momo.UITutorial.Data
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "Momo/Item/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemName;
        public Sprite itemIcon;
        [TextArea(2, 4)] public string detailEffect; // "Detail Effect" บรรทัดล่างสุดในรูป
        public int hpBonus;
        public int atkBonus;

        [Header("Display Toggles")]
        [Tooltip("ปิดถ้าไอเทมนี้ไม่มี ATK bonus ถาวรจริง (เช่น เจ็ทแพ็คที่ ATK เป็นแค่ดาเมจตอนใช้งาน ไม่ใช่บัฟติดตัว) — บรรทัด 'ATK +X' จะถูกซ่อนไปทั้งบรรทัดใน Popup")]
        public bool showAtkBonus = true;

        [Tooltip("ปิดถ้าไอเทมนี้ไม่มี HP bonus — ซ่อนบรรทัด 'HP +X' ไปทั้งบรรทัด (เผื่อใช้ในอนาคต)")]
        public bool showHpBonus = true;
    }
}