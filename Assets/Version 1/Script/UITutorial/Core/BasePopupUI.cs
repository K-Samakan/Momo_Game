using UnityEngine;
using UnityEngine.EventSystems;

namespace Momo.UITutorial.Core
{
    /// <summary>
    /// คลาสแม่สำหรับ Popup ที่ต้อง Pause เกม + ทำพื้นหลังมืด + บล็อก Input
    /// ให้ Popup ทุกตัว (Tutorial, Item Detail) สืบทอดจากคลาสนี้
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePopupUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected CanvasGroup canvasGroup;
        [SerializeField] protected UnityEngine.UI.Image dimBackground; // Image สีดำเต็มจอ

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float dimAlphaTarget = 0.75f;
        [SerializeField] private GameObject firstSelectedButton; // สำหรับ Controller/Gamepad navigation

        private float previousTimeScale = 1f;
        private bool isOpen = false;

        protected virtual void Awake()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// เปิด Popup — เรียกจาก Manager
        /// </summary>
        public virtual void Open()
        {
            if (isOpen) return;
            isOpen = true;

            gameObject.SetActive(true);

            // 1) หยุดเวลาเกม
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            // 2) บล็อก Player Input
            PopupInputBlocker.SetPlayerInputEnabled(false);

            // 3) เตรียม UI
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            if (dimBackground != null)
            {
                var c = dimBackground.color;
                c.a = 0f;
                dimBackground.color = c;
            }

            StopAllCoroutines();
            StartCoroutine(FadeRoutine(true));

            // 4) เลือกปุ่มแรกให้ Controller กด "Next/Back/Close" ได้ทันที
            if (firstSelectedButton != null)
                EventSystem.current.SetSelectedGameObject(firstSelectedButton);
        }

        /// <summary>
        /// ปิด Popup — คืนค่าเกมทั้งหมด
        /// </summary>
        public virtual void Close()
        {
            if (!isOpen) return;
            isOpen = false;

            StopAllCoroutines();
            StartCoroutine(FadeRoutine(false, onComplete: () =>
            {
                gameObject.SetActive(false);

                // คืนเวลาเกม
                Time.timeScale = previousTimeScale;

                // คืน Input ให้ผู้เล่น
                PopupInputBlocker.SetPlayerInputEnabled(true);
            }));
        }

        private System.Collections.IEnumerator FadeRoutine(bool fadeIn, System.Action onComplete = null)
        {
            float elapsed = 0f;
            float startAlpha = canvasGroup.alpha;
            float targetAlpha = fadeIn ? 1f : 0f;

            float startDim = dimBackground != null ? dimBackground.color.a : 0f;
            float targetDim = fadeIn ? dimAlphaTarget : 0f;

            while (elapsed < fadeDuration)
            {
                // ใช้ unscaledDeltaTime เพราะ Time.timeScale = 0 แล้ว
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / fadeDuration;

                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

                if (dimBackground != null)
                {
                    var c = dimBackground.color;
                    c.a = Mathf.Lerp(startDim, targetDim, t);
                    dimBackground.color = c;
                }

                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
            if (dimBackground != null)
            {
                var c = dimBackground.color;
                c.a = targetDim;
                dimBackground.color = c;
            }

            if (!fadeIn)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }

            onComplete?.Invoke();
        }
    }
}