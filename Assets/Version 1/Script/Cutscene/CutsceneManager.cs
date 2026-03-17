using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using TMPro;
using System.Collections;

namespace MOMO.Cutscene
{
    public class CutsceneManager : MonoBehaviour
    {
        [Header("=== Video Slides Data ===")]
        [Tooltip("รายการ Video Slides ทั้งหมด (5 อัน)")]
        [SerializeField] private VideoSlideData[] videoSlides;

        [Header("=== Video Player Components ===")]
        [Tooltip("Video Player Component")]
        [SerializeField] private VideoPlayer videoPlayer;

        [Tooltip("RawImage สำหรับแสดงวิดีโอ")]
        [SerializeField] private RawImage videoDisplay;

        [Tooltip("Canvas Group สำหรับ Fade Video")]
        [SerializeField] private CanvasGroup videoCanvasGroup;

        [Header("=== Audio Components ===")]
        [Tooltip("Audio Source สำหรับเล่น BGM")]
        [SerializeField] private AudioSource bgmSource;

        [Header("=== CC Subtitle UI ===")]
        [Tooltip("Panel สำหรับ CC Subtitle")]
        [SerializeField] private GameObject ccSubtitlePanel;

        [Tooltip("Text สำหรับแสดง CC Subtitle")]
        [SerializeField] private TextMeshProUGUI ccSubtitleText;

        [Tooltip("Canvas Group สำหรับ Fade CC Subtitle")]
        [SerializeField] private CanvasGroup ccSubtitleGroup;

        [Tooltip("Icon [CC] สำหรับแสดงว่าเป็น Closed Caption")]
        [SerializeField] private GameObject ccIcon;

        [Header("=== Button Prompt ===")]
        [Tooltip("รูปปุ่ม Circle (Button Prompt)")]
        [SerializeField] private GameObject buttonPrompt;

        // ลบบรรทัดนี้ออก:
        // [SerializeField] private CanvasGroup buttonPromptGroup;

        [Header("=== Settings ===")]
        [Tooltip("ระยะเวลา Fade ทั่วไป (วินาที)")]
        [SerializeField] private float fadeDuration = 0.5f;

        [Tooltip("ชื่อ Scene ถัดไป (Map1)")]
        [SerializeField] private string nextSceneName = "Map1";

        [Header("=== Haptic Feedback Settings ===")]
        [Tooltip("เปิดใช้งาน Haptic Feedback")]
        [SerializeField] private bool enableHapticFeedback = true;

        [Range(0f, 1f)]
        [Tooltip("ความแรง Haptic เมื่อกดปุ่ม (0-1)")]
        [SerializeField] private float hapticIntensity = 0.3f;

        [Tooltip("ระยะเวลา Haptic (วินาที)")]
        [SerializeField] private float hapticDuration = 0.1f;

        // ===== PRIVATE VARIABLES =====

        private int currentSlideIndex = 0;
        private bool isTransitioning = false;
        private bool videoReady = false;
        private string currentSubtitleText = "";

        // DualSense Controller Input
        private bool CircleButtonPressed => Input.GetButtonDown("PSCircle");

        // ===== UNITY LIFECYCLE =====

        void Start()
        {
            Debug.Log("🎬 CutsceneManager: Starting...");

            // Validate Components
            if (!ValidateComponents())
            {
                Debug.LogError("❌ CutsceneManager: Component validation failed!");
                return;
            }

            // Setup Video Player
            SetupVideoPlayer();

            // Setup CC Subtitle
            SetupCCSubtitle();

            // เริ่มต้น Slide แรก
            StartCoroutine(ShowVideoSlide(0));
        }

        void Update()
        {
            // ตรวจสอบ Input
            if (CircleButtonPressed && !isTransitioning && videoReady)
            {
                OnCirclePressed();
            }

            // อัปเดต CC Subtitle ตาม Timeline วิดีโอ
            if (videoPlayer.isPlaying && currentSlideIndex >= 0 && currentSlideIndex < videoSlides.Length)
            {
                UpdateCCSubtitle((float)videoPlayer.time);
            }
        }

        void OnDestroy()
        {
            // Cleanup
            CleanupVideoPlayer();
        }

        // ===== VALIDATION =====

        bool ValidateComponents()
        {
            bool isValid = true;

            if (videoSlides == null || videoSlides.Length == 0)
            {
                Debug.LogError("❌ Video Slides array is empty!");
                isValid = false;
            }

            if (videoPlayer == null)
            {
                Debug.LogError("❌ Video Player component is missing!");
                isValid = false;
            }

            if (videoDisplay == null)
            {
                Debug.LogError("❌ Video Display (RawImage) is missing!");
                isValid = false;
            }

            if (bgmSource == null)
            {
                Debug.LogWarning("⚠️ BGM Audio Source is missing!");
            }

            if (ccSubtitleText == null)
            {
                Debug.LogWarning("⚠️ CC Subtitle Text is missing!");
            }

            return isValid;
        }

        // ===== VIDEO PLAYER SETUP =====

        void SetupVideoPlayer()
        {
            Debug.Log("🎥 Setting up Video Player...");

            // ตั้งค่า Video Player
            videoPlayer.playOnAwake = false;
            videoPlayer.isLooping = true;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.skipOnDrop = true;

            // สร้าง Render Texture (ถ้ายังไม่มี)
            if (videoPlayer.targetTexture == null)
            {
                RenderTexture rt = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                rt.name = "CutsceneRenderTexture";
                videoPlayer.targetTexture = rt;
                videoDisplay.texture = rt;

                Debug.Log("✅ Created Render Texture: 1920x1080");
            }
            else
            {
                videoDisplay.texture = videoPlayer.targetTexture;
            }

            // Event Callbacks
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.errorReceived += OnVideoError;
            // ไม่ต้องใช้ loopPointReached เพราะ video loop อยู่แล้ว
            // videoPlayer.loopPointReached += OnVideoFinished;

            Debug.Log("✅ Video Player setup complete");
        }

        void CleanupVideoPlayer()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
                videoPlayer.errorReceived -= OnVideoError;
                // videoPlayer.loopPointReached -= OnVideoFinished;

                if (videoPlayer.targetTexture != null)
                {
                    videoPlayer.targetTexture.Release();
                }

                videoPlayer.clip = null;
            }

            if (bgmSource != null)
            {
                bgmSource.clip = null;
            }

            Resources.UnloadUnusedAssets();
        }

        // ===== CC SUBTITLE SETUP =====

        void SetupCCSubtitle()
        {
            if (ccSubtitlePanel != null)
            {
                ccSubtitlePanel.SetActive(true);
            }

            if (ccIcon != null)
            {
                ccIcon.SetActive(true);
            }

            if (ccSubtitleText != null)
            {
                ccSubtitleText.text = "";
            }

            Debug.Log("✅ CC Subtitle setup complete");
        }

        // ===== MAIN LOGIC =====

        void OnCirclePressed()
        {
            Debug.Log($"⭕ Circle pressed - Current slide: {currentSlideIndex + 1}/{videoSlides.Length}");

            // Haptic Feedback
            TriggerHapticFeedback();

            // ไปอันถัดไป
            currentSlideIndex++;

            Debug.Log($"→ Moving to index: {currentSlideIndex}");

            if (currentSlideIndex < videoSlides.Length)
            {
                // เล่น Slide ถัดไป
                Debug.Log($"📺 Loading slide {currentSlideIndex + 1}/{videoSlides.Length}");
                StartCoroutine(ShowVideoSlide(currentSlideIndex));
            }
            else
            {
                // จบแล้ว → โหลด Map1
                Debug.Log($"🎬 All {videoSlides.Length} slides completed!");
                Debug.Log($"🚀 Preparing to load: {nextSceneName}");
                StartCoroutine(LoadNextScene());
            }
        }

        // ===== SLIDE MANAGEMENT =====

        IEnumerator ShowVideoSlide(int index)
        {
            isTransitioning = true;
            videoReady = false;

            Debug.Log($"🎬 Showing slide {index + 1}/{videoSlides.Length}");

            // ซ่อนปุ่ม Prompt
            if (buttonPrompt != null)
                buttonPrompt.SetActive(false);

            // Fade Out ถ้าไม่ใช่อันแรก
            if (index > 0)
            {
                yield return StartCoroutine(FadeOut());
            }

            // โหลดข้อมูล Slide
            VideoSlideData slide = videoSlides[index];

            // Prepare Video
            videoPlayer.clip = slide.videoClip;
            videoPlayer.Prepare();

            Debug.Log("⏳ Preparing video...");

            // รอให้วิดีโอโหลดเสร็จ
            float timeout = 10f;
            float elapsed = 0f;

            while (!videoReady && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!videoReady)
            {
                Debug.LogError($"❌ Video {index + 1} failed to load (timeout)");
                yield break;
            }

            // Reset CC Subtitle
            currentSubtitleText = "";
            if (ccSubtitleText != null)
                ccSubtitleText.text = "";

            // เล่น BGM
            PlayBGM(slide);

            // Fade In
            yield return StartCoroutine(FadeIn(slide.fadeInDuration));

            // เริ่มเล่นวิดีโอ
            videoPlayer.Play();
            Debug.Log($"▶️ Playing video {index + 1}");

            // แสดงปุ่ม Prompt
            if (buttonPrompt != null)
                buttonPrompt.SetActive(true);

            isTransitioning = false;
        }

        // ===== VIDEO CALLBACKS =====

        void OnVideoPrepared(VideoPlayer vp)
        {
            Debug.Log($"✅ Video {currentSlideIndex + 1} prepared successfully");
            videoReady = true;
        }

        void OnVideoError(VideoPlayer vp, string message)
        {
            Debug.LogError($"❌ Video Error: {message}");
            videoReady = true;
        }

        // ไม่ต้องใช้ฟังก์ชันนี้แล้ว (เพราะ video loop)
        /*
        void OnVideoFinished(VideoPlayer vp)
        {
            Debug.Log($"🎬 Video {currentSlideIndex + 1} finished playing");
        }
        */

        // ===== BGM MANAGEMENT =====

        void PlayBGM(VideoSlideData slide)
        {
            if (bgmSource == null)
            {
                Debug.LogError("❌ BGM Source is NULL!");
                return;
            }

            if (slide.bgmClip == null)
            {
                Debug.LogError($"❌ BGM Clip is NULL in Slide {currentSlideIndex + 1}!");
                return;
            }

            Debug.Log($"🎵 Playing BGM: {slide.bgmClip.name}");

            // หยุด BGM เก่าทันที
            if (bgmSource.isPlaying)
            {
                bgmSource.Stop();
            }

            // ตั้งค่า BGM ใหม่
            bgmSource.clip = slide.bgmClip;
            bgmSource.volume = 0f;
            bgmSource.loop = slide.loopBGM;
            bgmSource.Play();

            // Fade In BGM
            StartCoroutine(FadeBGM(slide.bgmVolume, 0.5f));
        }

        IEnumerator FadeBGM(float targetVolume, float duration)
        {
            if (bgmSource == null) yield break;

            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            bgmSource.volume = targetVolume;
        }

        // ===== CC SUBTITLE MANAGEMENT =====

        void UpdateCCSubtitle(float currentTime)
        {
            // ป้องกัน Index Out of Range
            if (currentSlideIndex < 0 || currentSlideIndex >= videoSlides.Length)
            {
                Debug.LogWarning($"⚠️ Invalid slide index: {currentSlideIndex}");
                return;
            }

            VideoSlideData slide = videoSlides[currentSlideIndex];

            // ตรวจสอบว่ามี CC Subtitle หรือไม่
            if (slide.ccSubtitle == null || slide.ccSubtitle.lines == null || slide.ccSubtitle.lines.Length == 0)
                return;

            string newSubtitle = "";

            foreach (SubtitleLine line in slide.ccSubtitle.lines)
            {
                if (currentTime >= line.startTime && currentTime <= line.endTime)
                {
                    newSubtitle = line.text;
                    break;
                }
            }

            // อัปเดตเฉพาะเมื่อเปลี่ยนแปลง
            if (newSubtitle != currentSubtitleText)
            {
                currentSubtitleText = newSubtitle;

                if (ccSubtitleText != null)
                {
                    ccSubtitleText.text = currentSubtitleText;

                    if (!string.IsNullOrEmpty(currentSubtitleText))
                    {
                        Debug.Log($"💬 CC: {currentSubtitleText}");
                    }
                }
            }
        }

        // ===== FADE EFFECTS =====

        IEnumerator FadeIn(float duration)
        {
            Debug.Log("⬆️ Fading in...");

            // Fade Video
            if (videoCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 1f, duration));

            // Fade CC Subtitle
            if (ccSubtitleGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(ccSubtitleGroup, 1f, duration * 0.8f));
        }

        IEnumerator FadeOut()
        {
            Debug.Log("⬇️ Fading out...");

            // Fade CC Subtitle ก่อน
            if (ccSubtitleGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(ccSubtitleGroup, 0f, fadeDuration * 0.5f));

            // Fade Video
            if (videoCanvasGroup != null)
                yield return StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 0f, fadeDuration));

            // หยุดวิดีโอ
            videoPlayer.Stop();
        }

        IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration)
        {
            if (group == null) yield break;

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            group.alpha = targetAlpha;
        }

        // ===== SCENE TRANSITION =====

        IEnumerator LoadNextScene()
        {
            isTransitioning = true;

            Debug.Log("========================================");
            Debug.Log("   ⚡ FAST LOADING NEXT SCENE");
            Debug.Log("========================================");

            // ===== หยุดและซ่อนทุกอย่างทันที =====

            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                videoPlayer.Stop();
                Debug.Log("⏹️ Video stopped");
            }

            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.Stop();
                Debug.Log("🔇 BGM stopped");
            }

            // ซ่อน Button Prompt ทันที
            if (buttonPrompt != null)
            {
                buttonPrompt.SetActive(false);
                Debug.Log("🚫 Button prompt hidden");
            }

            // ===== ตรวจสอบ Scene =====

            if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
            {
                Debug.LogError($"❌ Scene '{nextSceneName}' not found!");
                Debug.LogError("→ Fix: File → Build Settings → Add Scene");
                yield break;
            }

            // ===== เริ่มโหลด Scene แบบ Async =====

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
            asyncLoad.allowSceneActivation = false;

            Debug.Log($"⏳ Loading '{nextSceneName}' in background...");

            // ===== Fade Out เร็ว =====

            float fastFadeDuration = 0.2f;

            // Fade ทั้งหมดพร้อมกัน
            if (videoCanvasGroup != null)
                StartCoroutine(FadeCanvasGroup(videoCanvasGroup, 0f, fastFadeDuration));

            if (ccSubtitleGroup != null)
                StartCoroutine(FadeCanvasGroup(ccSubtitleGroup, 0f, fastFadeDuration));

            Debug.Log($"⬇️ Fading out ({fastFadeDuration}s)...");

            // รอ Fade เสร็จ
            yield return new WaitForSeconds(fastFadeDuration);

            // ===== รอโหลด Scene =====

            // รอให้โหลดเกือบเสร็จ (90%)
            while (asyncLoad.progress < 0.9f)
            {
                yield return null;
            }

            Debug.Log("✅ Scene ready (90%)");

            // ===== เปลี่ยน Scene ทันที =====

            Debug.Log("🚀 Activating scene NOW!");
            asyncLoad.allowSceneActivation = true;

            Debug.Log("========================================");
        }

        // ===== HAPTIC FEEDBACK =====

        void TriggerHapticFeedback()
        {
            if (!enableHapticFeedback)
                return;

            Debug.Log($"📳 Haptic Feedback: Intensity={hapticIntensity}, Duration={hapticDuration}");

            // TODO: Integration with UniSense Plugin
            /*
            // ตัวอย่างการใช้งาน UniSense
            if (DualSenseController.IsConnected)
            {
                DualSenseController.TriggerHaptic(
                    leftMotor: hapticIntensity,
                    rightMotor: hapticIntensity,
                    duration: hapticDuration
                );
            }
            */

            // ทดสอบด้วย Unity's Default Haptic (Mobile)
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}