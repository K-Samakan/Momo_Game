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
    }
}