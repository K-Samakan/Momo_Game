using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;   // New Input System
using UnityEngine.Video;
using TMPro;
using System.Collections;

namespace MOMO.Cutscene
{
    /// <summary>
    /// จัดการ Cutscene แบบสไลด์ (รองรับทั้งภาพนิ่งและวิดีโอ)
    ///
    /// การควบคุมด้วย DualSense:
    ///  - ✕ (buttonSouth) กดค้าง  → ข้ามไปสไลด์ถัดไป (ทีละสไลด์)
    ///  - ⭘ (buttonEast) แตะ ขณะพิมพ์ → โชว์ข้อความเต็มทันที
    ///  - ⭘ (buttonEast) แตะ หลังพิมพ์ครบ → ไปสไลด์ถัดไป
    ///  - ไม่กดอะไรหลังพิมพ์ครบนานเกิน autoAdvanceDelay → ไปสไลด์ถัดไปอัตโนมัติ
    /// </summary>
    public class CutsceneManager : MonoBehaviour
    {
        private enum SlideState { Typing, Complete }

        [Header("=== Slides ===")]
        [Tooltip("รายการสไลด์ทั้งหมด เรียงตามลำดับการเล่น")]
        [SerializeField] private CutsceneSlideData[] slides;

        [Header("=== UI: Image (ภาพนิ่ง) ===")]
        [Tooltip("Image UI ที่ใช้แสดงรูปสไลด์")]
        [SerializeField] private Image slideImage;

        [Tooltip("CanvasGroup สำหรับ Fade (ครอบทั้ง Image และ Video)")]
        [SerializeField] private CanvasGroup slideCanvasGroup;

        [Header("=== UI: Video (สำรอง) ===")]
        [Tooltip("RawImage ที่ใช้แสดงวิดีโอ (วางซ้อนตำแหน่งเดียวกับ Image)")]
        [SerializeField] private RawImage videoDisplay;

        [Tooltip("VideoPlayer component")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Header("=== UI: Subtitle ===")]
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private CanvasGroup subtitleGroup;

        [Header("=== Subtitle Highlight BG (พื้นหลังดำโปร่งแสง) ===")]
        [Tooltip("เปิดพื้นหลังไฮไลต์หลังข้อความ (โตตามการพิมพ์)")]
        [SerializeField] private bool useHighlightBG = true;

        [Tooltip("Image พื้นหลังที่จะ resize ตามข้อความ (วางหลัง SubtitleText)")]
        [SerializeField] private RectTransform highlightBG;

        [Tooltip("ระยะขอบพื้นหลังเกินข้อความ (x = ซ้าย-ขวา, y = บน-ล่าง) pixel")]
        [SerializeField] private Vector2 highlightPadding = new Vector2(24f, 12f);

        [Header("=== Audio ===")]
        [Tooltip("AudioSource สำหรับเล่นเสียงประจำสไลด์")]
        [SerializeField] private AudioSource sfxSource;

        [Header("=== Input (Hold to skip) ===")]
        [Tooltip("เวลากด ✕ ค้าง (วินาที) ถึงจะนับว่าข้ามสไลด์")]
        [SerializeField] private float crossHoldThreshold = 0.4f;

        [Header("=== Scene ===")]
        [Tooltip("ชื่อ Scene ถัดไป เมื่อ Cutscene จบ")]
        [SerializeField] private string nextSceneName = "Map1";

        [Tooltip("จอ Loading (GameObject) ที่จะโชว์ตอนรอ scene ถัดไปโหลด")]
        [SerializeField] private GameObject loadingScreen;

        [Tooltip("เวลาโชว์จอ Loading ขั้นต่ำ (วินาที) แม้โหลดเสร็จก่อนก็โชว์ครบเวลานี้")]
        [SerializeField] private float minLoadingTime = 1.5f;

        // ───────────── runtime ─────────────
        private int index = 0;
        private SlideState state;
        private bool isTransitioning = false;
        private bool isFinished = false;   // กันไม่ให้ Update ทำงานหลัง cutscene จบ

        private Coroutine typingCo;
        private float crossHoldTimer = 0f;
        private bool crossSkipFired = false;
        private float autoAdvanceTimer = 0f;

        private RenderTexture videoRT;   // สร้างครั้งเดียวเมื่อมีสไลด์วิดีโอ
        private AsyncOperation preloadOp; // โหลด scene ถัดไปเบื้องหลัง

        // ✕ = buttonSouth, ⭘ = buttonEast (ตรงกับ PS5 บน PC ผ่าน New Input System)
        private Gamepad Pad => Gamepad.current;

        // ═══════════════════════════════════════════════
        void Start()
        {
            if (slides == null || slides.Length == 0)
            {
                Debug.LogError("❌ CutsceneManager: ไม่มี slides!");
                return;
            }

            if (slideImage == null || slideCanvasGroup == null || subtitleText == null)
            {
                Debug.LogError("❌ CutsceneManager: ยังไม่ได้ assign UI references ครบ!");
                return;
            }

            if (loadingScreen != null) loadingScreen.SetActive(false);

            // เริ่มโหลด Map1 เบื้องหลังตั้งแต่ตอนนี้ แต่ยังไม่สลับ scene
            StartPreloadNextScene();

            StartCoroutine(ShowSlide(0));
        }

        /// <summary>โหลด scene ถัดไปเบื้องหลัง (ยังไม่เปิดจนกว่า cutscene จบ)</summary>
        void StartPreloadNextScene()
        {
            if (string.IsNullOrEmpty(nextSceneName)) return;

            preloadOp = SceneManager.LoadSceneAsync(nextSceneName);
            preloadOp.allowSceneActivation = false;   // โหลดเสร็จแต่ยังไม่สลับ
        }

        void Update()
        {
            if (isFinished || isTransitioning || Pad == null) return;
            if (index < 0 || index >= slides.Length) return;   // กัน index เกินขอบ

            HandleCross();          // ✕ ค้าง → ข้ามสไลด์
            HandleCircle();         // ⭘ แตะ → เร่งข้อความ / ไปสไลด์ถัดไป
            HandleAutoAdvance();    // ไม่กดอะไร → เลื่อนเอง
        }

        void OnDestroy()
        {
            if (videoRT != null)
                videoRT.Release();   // คืน VRAM
        }

        // ───────────── Input handlers ─────────────

        /// <summary>✕ (buttonSouth) กดค้างเพื่อข้ามสไลด์ — fire ครั้งเดียวต่อการกด</summary>
        void HandleCross()
        {
            if (Pad.buttonSouth.isPressed)
            {
                crossHoldTimer += Time.unscaledDeltaTime;
                if (crossHoldTimer >= crossHoldThreshold && !crossSkipFired)
                {
                    crossSkipFired = true;
                    Debug.Log("✕ Hold → ข้ามสไลด์");
                    NextSlide();
                }
            }
            else
            {
                crossHoldTimer = 0f;
                crossSkipFired = false;
            }
        }

        /// <summary>⭘ (buttonEast) แตะ — จับขอบขาขึ้น 1 ครั้ง = 1 action</summary>
        void HandleCircle()
        {
            if (!Pad.buttonEast.wasPressedThisFrame) return;

            if (state == SlideState.Typing)
                CompleteSubtitle();   // แตะครั้งแรก: โชว์เต็ม
            else
                NextSlide();          // แตะอีกครั้ง: ไปสไลด์ถัดไป
        }

        /// <summary>ไม่กดอะไร: นับเวลาแล้วเลื่อนเอง (เฉพาะตอน Complete)</summary>
        void HandleAutoAdvance()
        {
            if (state != SlideState.Complete) return;
            if (index < 0 || index >= slides.Length) return;   // กัน index เกิน array

            autoAdvanceTimer += Time.unscaledDeltaTime;
            if (autoAdvanceTimer >= slides[index].autoAdvanceDelay)
            {
                Debug.Log("⏱ Auto advance");
                NextSlide();
            }
        }

        // ───────────── Core flow ─────────────

        IEnumerator ShowSlide(int i)
        {
            isTransitioning = true;
            index = i;
            CutsceneSlideData slide = slides[i];

            // reset state
            state = SlideState.Typing;
            autoAdvanceTimer = 0f;
            crossHoldTimer = 0f;
            crossSkipFired = false;

            // ── พื้นหลัง: branch ตามชนิดสไลด์ ──
            if (slide.type == SlideType.Video && slide.videoClip != null)
            {
                slideImage.gameObject.SetActive(false);
                if (videoDisplay != null) videoDisplay.gameObject.SetActive(true);

                SetupVideoRT();
                videoPlayer.Stop();
                videoPlayer.clip = slide.videoClip;
                videoPlayer.isLooping = slide.loopVideo;
                videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
                videoPlayer.Prepare();
                while (!videoPlayer.isPrepared) yield return null;   // รอวิดีโอโหลด
                videoPlayer.Play();
            }
            else
            {
                if (videoPlayer != null) videoPlayer.Stop();
                if (videoDisplay != null) videoDisplay.gameObject.SetActive(false);
                slideImage.gameObject.SetActive(true);
                slideImage.sprite = slide.image;
            }

            subtitleText.text = "";
            if (highlightBG != null) highlightBG.gameObject.SetActive(false);

            // ── เสียงประจำสไลด์ ──
            if (sfxSource != null && slide.sfx != null)
            {
                sfxSource.Stop();
                sfxSource.clip = slide.sfx;
                sfxSource.volume = slide.sfxVolume;
                sfxSource.loop = slide.loopSfx;
                sfxSource.Play();
            }

            // ── การสั่นตอนสไลด์ขึ้น ──
            if (slide.playHapticOnShow && HapticManager.Instance != null)
            {
                HapticManager.Instance.Vibrate(
                    slide.hapticLeft, slide.hapticRight, slide.hapticDuration);
            }

            // ── fade in ──
            yield return StartCoroutine(FadeCanvas(slideCanvasGroup, 1f, slide.fadeInDuration));
            if (subtitleGroup != null) subtitleGroup.alpha = 1f;

            isTransitioning = false;

            // ── เริ่มพิมพ์ subtitle ──
            typingCo = StartCoroutine(TypeSubtitle(slide));
        }

        /// <summary>typewriter ด้วย TMP maxVisibleCharacters + resize พื้นหลังตามข้อความ</summary>
        IEnumerator TypeSubtitle(CutsceneSlideData slide)
        {
            subtitleText.text = slide.subtitleText;
            subtitleText.maxVisibleCharacters = 0;
            subtitleText.ForceMeshUpdate();
            int total = subtitleText.textInfo.characterCount;

            for (int c = 0; c <= total; c++)
            {
                subtitleText.maxVisibleCharacters = c;
                subtitleText.ForceMeshUpdate();
                UpdateHighlightBG();
                yield return new WaitForSecondsRealtime(slide.typeSpeed);
            }

            OnSubtitleComplete();
        }

        /// <summary>resize Image พื้นหลังให้พอดีกับข้อความที่มองเห็น (โตตามการพิมพ์)</summary>
        void UpdateHighlightBG()
        {
            if (!useHighlightBG || highlightBG == null) return;

            subtitleText.ForceMeshUpdate();
            TMP_TextInfo info = subtitleText.textInfo;
            int visible = subtitleText.maxVisibleCharacters;

            // ยังไม่มีตัวอักษรโผล่ → ซ่อนพื้นหลัง
            if (visible <= 0 || info.characterCount == 0)
            {
                highlightBG.gameObject.SetActive(false);
                return;
            }

            highlightBG.gameObject.SetActive(true);

            // หา bounds ของเฉพาะตัวอักษรที่มองเห็นและไม่ใช่ช่องว่าง
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            bool found = false;
            int count = Mathf.Min(visible, info.characterCount);

            for (int i = 0; i < count; i++)
            {
                if (!info.characterInfo[i].isVisible) continue;
                found = true;
                minX = Mathf.Min(minX, info.characterInfo[i].bottomLeft.x);
                maxX = Mathf.Max(maxX, info.characterInfo[i].topRight.x);
                minY = Mathf.Min(minY, info.characterInfo[i].descender);
                maxY = Mathf.Max(maxY, info.characterInfo[i].ascender);
            }

            if (!found)
            {
                highlightBG.gameObject.SetActive(false);
                return;
            }

            float w = (maxX - minX) + highlightPadding.x * 2f;
            float h = (maxY - minY) + highlightPadding.y * 2f;
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;

            // วางพื้นหลังให้อิงพิกัด local ของ SubtitleText (highlightBG ต้องเป็นลูกของ SubtitleRoot ระดับเดียวกับ text)
            highlightBG.sizeDelta = new Vector2(w, h);
            highlightBG.localPosition = subtitleText.rectTransform.localPosition +
                                        new Vector3(cx, cy, 0f);
        }

        /// <summary>⭘ แตะตอนพิมพ์: โชว์เต็มทันที</summary>
        void CompleteSubtitle()
        {
            if (typingCo != null) StopCoroutine(typingCo);
            subtitleText.maxVisibleCharacters = int.MaxValue;
            subtitleText.ForceMeshUpdate();
            UpdateHighlightBG();
            OnSubtitleComplete();
            Debug.Log("⭘ เร่งข้อความ → เต็ม");
        }

        void OnSubtitleComplete()
        {
            state = SlideState.Complete;
            autoAdvanceTimer = 0f;   // เริ่มนับ auto-advance ตรงนี้
        }

        void NextSlide()
        {
            index++;
            if (index < slides.Length)
                StartCoroutine(ShowSlide(index));
            else
                StartCoroutine(EndCutscene());
        }

        IEnumerator EndCutscene()
        {
            isTransitioning = true;
            isFinished = true;   // หยุด Update ทันที กัน index เกินขอบ

            if (HapticManager.Instance != null) HapticManager.Instance.StopVibration();
            if (sfxSource != null) sfxSource.Stop();
            if (videoPlayer != null) videoPlayer.Stop();

            // fade จอดำลง (ตั้ง 0f ถ้าไม่อยากได้ fade)
            yield return StartCoroutine(FadeCanvas(slideCanvasGroup, 0f, 0.5f));

            Debug.Log($"🎬 Cutscene จบ → เข้า {nextSceneName}");

            // โชว์จอ Loading ระหว่างรอ Map1 โหลด
            if (loadingScreen != null) loadingScreen.SetActive(true);

            float loadingStart = Time.unscaledTime;

            // รอจน Map1 โหลดครบ (async ค้างที่ 0.9 จนกว่าจะ activate)
            if (preloadOp != null)
            {
                while (preloadOp.progress < 0.9f)
                    yield return null;
            }

            // โชว์จอ Loading ให้ครบเวลาขั้นต่ำ แม้โหลดเสร็จก่อน
            while (Time.unscaledTime - loadingStart < minLoadingTime)
                yield return null;

            // เข้า Map1
            if (preloadOp != null)
                preloadOp.allowSceneActivation = true;
            else
                SceneManager.LoadScene(nextSceneName);
        }

        // ───────────── Helpers ─────────────

        IEnumerator FadeCanvas(CanvasGroup cg, float target, float dur)
        {
            if (cg == null) yield break;
            float start = cg.alpha, t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(start, target, t / dur);
                yield return null;
            }
            cg.alpha = target;
        }

        /// <summary>เตรียม RenderTexture สำหรับวิดีโอ (สร้างครั้งเดียว)</summary>
        void SetupVideoRT()
        {
            if (videoRT != null || videoPlayer == null) return;

            videoRT = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            videoRT.name = "CutsceneRT";

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRT;
            videoPlayer.playOnAwake = false;
            videoPlayer.skipOnDrop = true;

            if (videoDisplay != null)
                videoDisplay.texture = videoRT;
        }
    }
}