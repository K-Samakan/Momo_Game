using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using Momo.UITutorial.Core;
using Momo.UITutorial.Data;
using Momo.UITutorial.Managers;

namespace Momo.UITutorial.Popups
{
    public class TutorialPopupUI : BasePopupUI
    {
        [Header("Tutorial UI Refs")]
        [SerializeField] private Image pageImage;
        [SerializeField] private TMP_Text pageDescription;
        [SerializeField] private Button backButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Transform dotContainer;
        [SerializeField] private GameObject dotPrefab;

        [Header("Next Button Label (สลับข้อความ Next ↔ Close)")]
        [SerializeField] private TMP_Text nextButtonLabel;

        [Header("Decorative Arrows (ซ่อน/โชว์ตามหน้า)")]
        [SerializeField] private GameObject leftArrowDecor;
        [SerializeField] private GameObject rightArrowDecor;

        private const string NEXT_LABEL = "Next";
        private const string CLOSE_LABEL = "Close";

        private TutorialPageData currentData;
        private int currentIndex;
        private Image[] dots;
        private Color[] dotBaseColors;

        // ── Input Action References ──────────────────────────────
        private InputAction nextAction;
        private InputAction backAction;

        protected override void Awake()
        {
            base.Awake();
            backButton.onClick.AddListener(OnBack);
            nextButton.onClick.AddListener(OnNext);
        }

        private void OnEnable()
        {
            var playerObj = PlayerReference.Instance != null
                ? PlayerReference.Instance.gameObject
                : GameObject.FindGameObjectWithTag("Player");

            var playerInput = playerObj != null ? playerObj.GetComponent<PlayerInput>() : null;
            if (playerInput == null) return;

            nextAction = playerInput.actions.FindAction("PopupUI/Next");
            backAction = playerInput.actions.FindAction("PopupUI/Back");

            if (nextAction != null) nextAction.performed += HandleNextInput;
            if (backAction != null) backAction.performed += HandleBackInput;
        }

        private void OnDisable()
        {
            if (nextAction != null) nextAction.performed -= HandleNextInput;
            if (backAction != null) backAction.performed -= HandleBackInput;
        }

        private void HandleNextInput(InputAction.CallbackContext ctx) => OnNext();
        private void HandleBackInput(InputAction.CallbackContext ctx) => OnBack();

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
            dotBaseColors = new Color[currentData.pages.Length];

            for (int i = 0; i < currentData.pages.Length; i++)
            {
                var dotObj = Instantiate(dotPrefab, dotContainer);
                dots[i] = dotObj.GetComponent<Image>();
                dotBaseColors[i] = dots[i].color;
            }
        }

        private void RefreshPage()
        {
            var page = currentData.pages[currentIndex];
            pageImage.sprite = page.image;
            pageDescription.text = page.description;

            bool isFirstPage = currentIndex == 0;
            bool isLastPage = currentIndex == currentData.pages.Length - 1;

            // ── ปุ่ม Back จริง (Interactable) ──
            backButton.interactable = !isFirstPage;

            // ── ข้อความปุ่ม Next → Close ──
            if (nextButtonLabel != null)
                nextButtonLabel.text = isLastPage ? CLOSE_LABEL : NEXT_LABEL;

            // ── ลูกศรตกแต่งข้างรูป: ซ่อน/โชว์ตามหน้า ──
            if (leftArrowDecor != null)
                leftArrowDecor.SetActive(!isFirstPage);

            if (rightArrowDecor != null)
                rightArrowDecor.SetActive(!isLastPage);

            // ── Dot Indicator ──
            for (int i = 0; i < dots.Length; i++)
            {
                Color c = dotBaseColors[i];
                c.a = (i == currentIndex) ? 1f : 0.35f;
                dots[i].color = c;
            }
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
                TutorialManager.Instance.MarkTutorialAsSeen(currentData.tutorialId);
                Close();
            }
        }
    }
}