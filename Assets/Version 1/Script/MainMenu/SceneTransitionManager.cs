using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace MomoGame.MainMenu
{
    /// <summary>
    /// จัดการการเปลี่ยน Scene พร้อม Fade Effect
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 1f;
        [SerializeField] private Color fadeColor = Color.black;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("Loading Screen")]
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Slider loadingBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        
        [Header("Settings")]
        [SerializeField] private bool autoFadeIn = true;
        [SerializeField] private float minimumLoadTime = 1f;
        
        private static SceneTransitionManager instance;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (fadeImage == null)
            {
                CreateFadeCanvas();
            }
            
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
        }
        
        private void Start()
        {
            if (autoFadeIn)
            {
                StartCoroutine(FadeIn());
            }
        }
        
        private void CreateFadeCanvas()
        {
            GameObject canvasObj = new GameObject("FadeCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            GameObject fadeObj = new GameObject("FadeImage");
            fadeObj.transform.SetParent(canvasObj.transform, false);
            fadeImage = fadeObj.AddComponent<Image>();
            fadeImage.color = fadeColor;
            
            RectTransform rt = fadeImage.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            DontDestroyOnLoad(canvasObj);
        }
        
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            yield return StartCoroutine(FadeOut());
            
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(true);
            }
            
            float startTime = Time.realtimeSinceStartup;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;
            
            while (!operation.isDone)
            {
                float progress = Mathf.Clamp01(operation.progress / 0.9f);
                
                if (loadingBar != null)
                    loadingBar.value = progress;
                
                if (loadingText != null)
                    loadingText.text = $"Loading... {Mathf.RoundToInt(progress * 100)}%";
                
                if (operation.progress >= 0.9f)
                {
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed < minimumLoadTime)
                    {
                        yield return new WaitForSeconds(minimumLoadTime - elapsed);
                    }
                    
                    if (loadingBar != null)
                        loadingBar.value = 1f;
                    
                    if (loadingText != null)
                        loadingText.text = "Loading... 100%";
                    
                    yield return new WaitForSeconds(0.5f);
                    
                    operation.allowSceneActivation = true;
                }
                
                yield return null;
            }
            
            if (loadingScreen != null)
            {
                loadingScreen.SetActive(false);
            }
            
            yield return StartCoroutine(FadeIn());
        }
        
        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            Color startColor = fadeColor;
            startColor.a = 0f;
            Color endColor = fadeColor;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeCurve.Evaluate(elapsed / fadeDuration);
                fadeImage.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
            
            fadeImage.color = endColor;
        }
        
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            Color startColor = fadeColor;
            Color endColor = fadeColor;
            endColor.a = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = fadeCurve.Evaluate(elapsed / fadeDuration);
                fadeImage.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
            
            fadeImage.color = endColor;
        }
    }
}