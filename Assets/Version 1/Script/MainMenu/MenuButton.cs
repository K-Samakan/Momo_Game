using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace MomoGame.MainMenu
{
    /// <summary>
    /// Custom Menu Button พร้อม Haptic Feedback และ Visual Effects
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class MenuButton : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptic = true;
        [SerializeField][Range(0f, 1f)] private float hoverHapticIntensity = 0.3f;
        [SerializeField][Range(0f, 1f)] private float clickHapticIntensity = 0.7f;
        [SerializeField] private float hapticDuration = 0.1f;

        [Header("Audio")]
        [SerializeField] private string hoverSoundName = "MenuHover";
        [SerializeField] private string clickSoundName = "MenuSelect";

        [Header("Visual Effects")]
        [SerializeField] private bool enableScaleEffect = true;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float scaleSpeed = 10f;
        [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Color Effects")]
        [SerializeField] private bool enableColorEffect = false;
        [SerializeField] private Image targetImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hoverColor = Color.yellow;

        [Header("Selection Indicator")]
        [SerializeField] private GameObject selectionArrow;
        [SerializeField] private bool enableSelectionArrow = true;

        // Private variables
        private Button button;
        private Vector3 originalScale;
        private AudioManager audioManager;
        private DualSenseController hapticController;
        private bool isSelected = false;

        private void Awake()
        {
            button = GetComponent<Button>();
            originalScale = transform.localScale;

            // Find managers
            audioManager = FindObjectOfType<AudioManager>();
            hapticController = FindObjectOfType<DualSenseController>();

            // Auto-find target image if not set
            if (enableColorEffect && targetImage == null)
            {
                targetImage = GetComponent<Image>();
            }
        }

        #region Event Handlers

        // Mouse hover
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!button.interactable) return;
            OnHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!button.interactable) return;
            OnUnhover();
        }

        // Controller selection
        public void OnSelect(BaseEventData eventData)
        {
            if (!button.interactable) return;
            OnHover();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            if (!button.interactable) return;
            OnUnhover();
        }

        #endregion

        /// <summary>
        /// เมื่อ Hover/Select ปุ่ม
        /// </summary>
        private void OnHover()
        {
            if (isSelected) return;
            isSelected = true;

            // Play audio
            audioManager?.PlaySFX(hoverSoundName);

            // Haptic feedback
            if (enableHaptic && hapticController != null)
            {
                hapticController.TriggerHaptic(hoverHapticIntensity, hapticDuration);
            }

            // Visual scale
            if (enableScaleEffect)
            {
                StopAllCoroutines();
                StartCoroutine(AnimateScale(hoverScale));
            }

            // Color change
            if (enableColorEffect && targetImage != null)
            {
                targetImage.color = hoverColor;
            }

            if (enableSelectionArrow && selectionArrow != null)
            {
                selectionArrow.SetActive(true);
            }
        }

        /// <summary>
        /// เมื่อออกจาก Hover
        /// </summary>
        private void OnUnhover()
        {
            isSelected = false;

            // Reset scale
            if (enableScaleEffect)
            {
                StopAllCoroutines();
                StartCoroutine(AnimateScale(1f));
            }

            // Reset color
            if (enableColorEffect && targetImage != null)
            {
                targetImage.color = normalColor;
            }

            if (enableSelectionArrow && selectionArrow != null)
            {
                selectionArrow.SetActive(false);
            }
        }

        /// <summary>
        /// เมื่อกดปุ่ม (เรียกจาก Button.onClick)
        /// </summary>
        public void OnButtonClick()
        {
            // Play audio
            audioManager?.PlaySFX(clickSoundName);

            // Stronger haptic
            if (enableHaptic && hapticController != null)
            {
                hapticController.TriggerHaptic(clickHapticIntensity, hapticDuration);
            }

            // Punch effect
            if (enableScaleEffect)
            {
                StartCoroutine(PunchEffect());
            }
        }

        /// <summary>
        /// Animate scale
        /// </summary>
        private IEnumerator AnimateScale(float targetScale)
        {
            Vector3 target = originalScale * targetScale;
            float elapsed = 0f;
            float duration = 0.2f;
            Vector3 startScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = scaleCurve.Evaluate(elapsed / duration);
                transform.localScale = Vector3.Lerp(startScale, target, t);
                yield return null;
            }

            transform.localScale = target;
        }

        /// <summary>
        /// Punch effect เมื่อคลิก
        /// </summary>
        private IEnumerator PunchEffect()
        {
            Vector3 currentScale = transform.localScale;
            Vector3 punchScale = currentScale * 0.9f;
            float duration = 0.05f;

            // Compress
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(currentScale, punchScale, elapsed / duration);
                yield return null;
            }

            // Bounce back
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.localScale = Vector3.Lerp(punchScale, currentScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = currentScale;
        }

        private void OnEnable()
        {
            // Subscribe to button click
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnDisable()
        {
            // Reset
            transform.localScale = originalScale;
            isSelected = false;

            if (enableColorEffect && targetImage != null)
            {
                targetImage.color = normalColor;
            }

            // Unsubscribe
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
    }
}