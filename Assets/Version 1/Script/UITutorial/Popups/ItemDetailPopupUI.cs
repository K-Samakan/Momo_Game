using UnityEngine;
using UnityEngine.UI;
using Momo.UITutorial.Core;
using Momo.UITutorial.Data;

namespace Momo.UITutorial.Popups
{
    public class ItemDetailPopupUI : BasePopupUI
    {
        [Header("Item Detail UI Refs")]
        [SerializeField] private Image itemIcon;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text hpBonusText;
        [SerializeField] private Text atkBonusText;
        [SerializeField] private Text detailEffectText;
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
            hpBonusText.text = $"HP + {item.hpBonus}";
            atkBonusText.text = $"ATK + {item.atkBonus}";
            detailEffectText.text = item.detailEffect;

            Open();
        }
    }
}