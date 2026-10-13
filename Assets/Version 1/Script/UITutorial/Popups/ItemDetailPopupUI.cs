using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Momo.UITutorial.Core;
using Momo.UITutorial.Data;

namespace Momo.UITutorial.Popups
{
    public class ItemDetailPopupUI : BasePopupUI
    {
        [Header("Item Detail UI Refs")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text hpBonusText;
        [SerializeField] private TMP_Text atkBonusText;
        [SerializeField] private TMP_Text detailEffectText;
        [SerializeField] private Button closeButton;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
        }

        public void ShowItem(ItemData item)
        {
            itemIcon.sprite = item.itemIcon;
            itemNameText.text = item.itemName;
            detailEffectText.text = item.detailEffect;

            // ✅ HP Bonus — ซ่อนทั้งบรรทัดถ้า item.showHpBonus == false
            if (item.showHpBonus)
            {
                hpBonusText.gameObject.SetActive(true);
                hpBonusText.text = $"HP + {item.hpBonus}";
            }
            else
            {
                hpBonusText.gameObject.SetActive(false);
            }

            // ✅ ATK Bonus — ซ่อนทั้งบรรทัดถ้า item.showAtkBonus == false
            if (item.showAtkBonus)
            {
                atkBonusText.gameObject.SetActive(true);
                atkBonusText.text = $"ATK + {item.atkBonus}";
            }
            else
            {
                atkBonusText.gameObject.SetActive(false);
            }

            Open();
        }
    }
}