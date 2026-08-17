using UnityEngine;
using UnityEngine.UI;
using Momo.UITutorial.Core;
using Momo.UITutorial.Data;

namespace Momo.UITutorial.Popups
{
    public class TutorialPopupUI : BasePopupUI
    {
        [Header("Tutorial UI Refs")]
        [SerializeField] private Image pageImage;
        [SerializeField] private Text pageDescription; // หรือ TMP_Text
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Transform dotContainer;
        [SerializeField] private GameObject dotPrefab;

        private TutorialPageData currentData;
        private int currentIndex;
        private Image[] dots;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(OnBack);
            nextButton.onClick.AddListener(OnNext);
        }

        /// <summary>
        /// เรียกจาก TutorialManager พร้อมข้อมูล ScriptableObject
        /// </summary>
        public void ShowTutorial(TutorialPageData data)
        {
            currentData = data;
            currentIndex = 0;

            BuildDots();
            RefreshPage();
            Open();
        }

        private void BuildDots()
        {
            foreach (Transform child in dotContainer) Destroy(child.gameObject);

            dots = new Image[currentData.pages.Length];
            for (int i = 0; i < currentData.pages.Length; i++)
            {
                var dotObj = Instantiate(dotPrefab, dotContainer);
                dots[i] = dotObj.GetComponent<Image>();
            }
        }

        private void RefreshPage()
        {
            var page = currentData.pages[currentIndex];
            pageImage.sprite = page.image;
            pageDescription.text = page.description;

            // ปุ่ม Back ปิดใช้งานหน้าแรก, Next เปลี่ยนเป็น "ปิด" หน้าสุดท้าย
            backButton.interactable = currentIndex > 0;

            // อัปเดต Dot Indicator (สีเข้ม = หน้าปัจจุบัน)
            for (int i = 0; i < dots.Length; i++)
                dots[i].color = (i == currentIndex) ? Color.white : new Color(1, 1, 1, 0.35f);
        }

        private void OnBack()
        {
            if (currentIndex <= 0) return;
            currentIndex--;
            RefreshPage();
        }

        private void OnNext()
        {
            if (currentIndex < currentData.pages.Length - 1)
            {
                currentIndex++;
                RefreshPage();
            }
            else
            {
                // หน้าสุดท้าย → ปิด Popup + บันทึกว่าดูแล้ว
                Momo.UITutorial.Managers.TutorialManager.Instance
                    .MarkTutorialAsSeen(currentData.tutorialId);
                Close();
            }
        }
    }
}