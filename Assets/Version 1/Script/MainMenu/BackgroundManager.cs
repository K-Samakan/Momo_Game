using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MomoGame.MainMenu
{
    /// <summary>
    /// จัดการ Background - รองรับทั้งรูปภาพและวิดีโอ
    /// </summary>
    public class BackgroundManager : MonoBehaviour
    {
        public enum BackgroundType
        {
            Image,
            Video
        }
        
        [Header("Background Type")]
        [SerializeField] private BackgroundType backgroundType = BackgroundType.Image;
        
        [Header("Image Background")]
        [SerializeField] private RawImage backgroundImage;
        [SerializeField] private Texture imageTexture;
        [SerializeField] private BackgroundParallax parallaxScript;
        
        [Header("Video Background")]
        [SerializeField] private RawImage videoRawImage;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private VideoClip videoClip;
        [SerializeField] private RenderTexture videoRenderTexture;
        
        [Header("Settings")]
        [SerializeField] private bool fadeInOnStart = true;
        [SerializeField] private float fadeDuration = 1f;
        
        private void Awake()
        {
            SetupBackground();
        }
        
        private void Start()
        {
            if (fadeInOnStart)
            {
                StartCoroutine(FadeIn());
            }
        }
        
        private void SetupBackground()
        {
            switch (backgroundType)
            {
                case BackgroundType.Image:
                    SetupImageBackground();
                    break;
                    
                case BackgroundType.Video:
                    SetupVideoBackground();
                    break;
            }
        }
        
        private void SetupImageBackground()
        {
            // Show image
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(true);
                backgroundImage.texture = imageTexture;
            }
            
            // Enable parallax
            if (parallaxScript != null)
            {
                parallaxScript.enabled = true;
            }
            
            // Hide video
            if (videoRawImage != null)
            {
                videoRawImage.gameObject.SetActive(false);
            }
            
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
            
            Debug.Log("[BackgroundManager] Using Image Background");
        }
        
        private void SetupVideoBackground()
        {
            // Hide image
            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(false);
            }
            
            // Disable parallax
            if (parallaxScript != null)
            {
                parallaxScript.enabled = false;
            }
            
            // Show video
            if (videoRawImage != null)
            {
                videoRawImage.gameObject.SetActive(true);
            }
            
            // Setup video player
            if (videoPlayer != null)
            {
                SetupVideoPlayer();
            }
            
            Debug.Log("[BackgroundManager] Using Video Background");
        }
        
        private void SetupVideoPlayer()
        {
            // Create render texture if needed
            if (videoRenderTexture == null)
            {
                videoRenderTexture = new RenderTexture(1920, 1080, 0);
                videoRenderTexture.name = "VideoBackgroundRT";
            }
            
            // Configure video player
            videoPlayer.clip = videoClip;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = videoRenderTexture;
            videoPlayer.isLooping = true;
            videoPlayer.playOnAwake = true;
            videoPlayer.SetDirectAudioMute(0, true);
            
            // Assign to raw image
            if (videoRawImage != null)
            {
                videoRawImage.texture = videoRenderTexture;
            }
            
            // Play
            videoPlayer.Play();
        }
        
        public void ChangeBackgroundType(BackgroundType type)
        {
            backgroundType = type;
            SetupBackground();
        }
        
        public BackgroundType GetBackgroundType()
        {
            return backgroundType;
        }
        
        private System.Collections.IEnumerator FadeIn()
        {
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            canvasGroup.alpha = 0f;
            float elapsed = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        private void OnDestroy()
        {
            if (videoRenderTexture != null)
            {
                videoRenderTexture.Release();
            }
        }
    }
    
    /// <summary>
    /// Parallax effect สำหรับรูปภาพ Background
    /// </summary>
    public class BackgroundParallax : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [SerializeField] private Vector2 parallaxSpeed = new Vector2(0.02f, 0f);
        [SerializeField] private bool enableParallax = true;
        
        private RawImage rawImage;
        
        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            
            if (rawImage == null)
            {
                Debug.LogError("[BackgroundParallax] Requires RawImage component!");
                enabled = false;
            }
        }
        
        private void Update()
        {
            if (!enableParallax || rawImage == null) return;
            
            Vector2 offset = new Vector2(
                Time.time * parallaxSpeed.x,
                Time.time * parallaxSpeed.y
            );
            
            // Loop seamlessly
            offset.x %= 1f;
            offset.y %= 1f;
            
            rawImage.uvRect = new Rect(offset, rawImage.uvRect.size);
        }
        
        public void SetParallaxEnabled(bool enabled)
        {
            enableParallax = enabled;
        }
    }
}