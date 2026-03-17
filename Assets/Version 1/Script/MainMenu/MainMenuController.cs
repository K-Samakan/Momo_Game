using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace MomoGame.MainMenu
{
    /// <summary>
    /// Main Menu Controller - จัดการ Main Menu และ Options Panel
    /// ไม่มีระบบ Continue/Save (จะเพิ่มในอนาคต)
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject optionsPanel;

        [Header("Menu Buttons")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button quitButton;

        [Header("Options Buttons")]
        [SerializeField] private Button backButton;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Toggle videoBackgroundToggle;

        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI versionText;

        [Header("Scene Settings")]
        [Tooltip("ชื่อ Scene ที่จะโหลดเมื่อกด New Game")]
        [SerializeField] private string gameplaySceneName = "Stage1_Forest";

        [Header("Background")]
        [SerializeField] private BackgroundManager backgroundManager;

        [Header("Settings")]
        [SerializeField] private bool showVersionInfo = true;
        [SerializeField] private bool enableDebugLogs = false;
        private SceneTransitionManager transitionManager;
        private AudioManager audioManager;
        private MenuNavigator navigator;

        private void Awake()
        {
            Log("Main Menu Controller starting...");

            // Find or create managers
            FindManagers();
        }

        private void Start()
        {
            InitializeMenu();
            SetupButtonListeners();
            SetupOptionsListeners();
            UpdateVersionText();
            PlayBackgroundMusic();

            Log("Main Menu initialized successfully");
        }

        #region Initialization

        /// <summary>
        /// ค้นหา Manager components
        /// </summary>
        private void FindManagers()
        {
            // Scene Transition Manager
            transitionManager = FindObjectOfType<SceneTransitionManager>();
            if (transitionManager == null)
            {
                transitionManager = gameObject.AddComponent<SceneTransitionManager>();
                Log("Created SceneTransitionManager");
            }

            // Audio Manager
            audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                LogWarning("AudioManager not found! Audio will not play.");
            }

            // Menu Navigator
            navigator = GetComponent<MenuNavigator>();
            if (navigator == null)
            {
                navigator = gameObject.AddComponent<MenuNavigator>();
                Log("Created MenuNavigator");
            }

            // Background Manager
            if (backgroundManager == null)
            {
                backgroundManager = FindObjectOfType<BackgroundManager>();
                if (backgroundManager == null)
                {
                    LogWarning("BackgroundManager not found!");
                }
            }
        }

        /// <summary>
        /// เริ่มต้นเมนู - แสดง Main Menu, ซ่อน Options
        /// </summary>
        private void InitializeMenu()
        {
            // แสดง Main Menu Panel
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
            else
                LogError("Main Menu Panel is not assigned!");

            // ซ่อน Options Panel
            if (optionsPanel != null)
                optionsPanel.SetActive(false);
            else
                LogError("Options Panel is not assigned!");

            // ตั้งปุ่มแรกที่เลือก
            if (navigator != null && newGameButton != null)
            {
                navigator.SetFirstSelected(newGameButton.gameObject);
            }
        }

        /// <summary>
        /// ผูก Event Listeners สำหรับปุ่มหลัก
        /// </summary>
        private void SetupButtonListeners()
        {
            if (newGameButton != null)
                newGameButton.onClick.AddListener(OnNewGame);
            else
                LogError("New Game Button is not assigned!");

            if (optionsButton != null)
                optionsButton.onClick.AddListener(OnOptions);
            else
                LogError("Options Button is not assigned!");

            if (quitButton != null)
                quitButton.onClick.AddListener(OnQuit);
            else
                LogError("Quit Button is not assigned!");

            if (backButton != null)
                backButton.onClick.AddListener(OnBackToMainMenu);
            else
                LogError("Back Button is not assigned!");
        }

        /// <summary>
        /// ผูก Event Listeners สำหรับ Options
        /// </summary>
        private void SetupOptionsListeners()
        {
            // BGM Volume Slider
            if (bgmVolumeSlider != null && audioManager != null)
            {
                bgmVolumeSlider.value = audioManager.GetBGMVolume();
                bgmVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            }

            // SFX Volume Slider
            if (sfxVolumeSlider != null && audioManager != null)
            {
                sfxVolumeSlider.value = audioManager.GetSFXVolume();
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            // Video Background Toggle
            if (videoBackgroundToggle != null && backgroundManager != null)
            {
                bool isVideo = backgroundManager.GetBackgroundType() ==
                               BackgroundManager.BackgroundType.Video;
                videoBackgroundToggle.isOn = isVideo;
                videoBackgroundToggle.onValueChanged.AddListener(OnVideoBackgroundToggled);
            }
        }

        /// <summary>
        /// แสดงข้อมูลเวอร์ชัน
        /// </summary>
        private void UpdateVersionText()
        {
            if (versionText != null && showVersionInfo)
            {
                versionText.text = $"Version {Application.version}";
            }
        }

        /// <summary>
        /// เล่นเพลงพื้นหลัง
        /// </summary>
        private void PlayBackgroundMusic()
        {
            if (audioManager != null)
            {
                audioManager.PlayBGM("MainMenuTheme");
            }
        }

        #endregion

        #region Button Callbacks

        /// <summary>
        /// เริ่มเกมใหม่
        /// </summary>
        public void OnNewGame()
        {
            Log("New Game button pressed");

            audioManager?.PlaySFX("MenuSelect");

            if (string.IsNullOrEmpty(gameplaySceneName))
            {
                LogError("Gameplay scene name is not set!");
                return;
            }

            if (audioManager != null)
            {
                audioManager.StopBGM(fade: true); // Fade out แล้วค่อยหยุด
            }

            transitionManager?.LoadScene(gameplaySceneName);
        }

        /// <summary>
        /// เปิด Options Panel
        /// </summary>
        public void OnOptions()
        {
            Log("Options button pressed");

            audioManager?.PlaySFX("MenuSelect");

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);

            if (optionsPanel != null)
                optionsPanel.SetActive(true);

            // ตั้งปุ่มแรกใน Options
            if (navigator != null && backButton != null)
            {
                navigator.SetFirstSelected(backButton.gameObject);
            }
        }

        /// <summary>
        /// กลับไป Main Menu
        /// </summary>
        public void OnBackToMainMenu()
        {
            if (!IsOptionsActive())
            {
                // ถ้าไม่ได้อยู่ใน Options → ไม่ทำอะไร
                return;
            }

            Log("Returning to Main Menu");

            audioManager?.PlaySFX("MenuBack");

            if (optionsPanel != null)
                optionsPanel.SetActive(false);

            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);

            // กลับไปปุ่มแรกใน Main Menu
            if (navigator != null && newGameButton != null)
            {
                navigator.SetFirstSelected(newGameButton.gameObject);
            }
        }

        /// <summary>
        /// ออกจากเกม
        /// </summary>
        public void OnQuit()
        {
            Log("Quit button pressed");

            audioManager?.PlaySFX("MenuSelect");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }

        #endregion

        #region Options Callbacks

        /// <summary>
        /// เปลี่ยนระดับเสียง BGM
        /// </summary>
        private void OnBGMVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetBGMVolume(value);
            }
        }

        /// <summary>
        /// เปลี่ยนระดับเสียง SFX
        /// </summary>
        private void OnSFXVolumeChanged(float value)
        {
            if (audioManager != null)
            {
                audioManager.SetSFXVolume(value);
            }
        }

        /// <summary>
        /// เปลี่ยนประเภท Background (Image/Video)
        /// </summary>
        private void OnVideoBackgroundToggled(bool useVideo)
        {
            if (backgroundManager != null)
            {
                BackgroundManager.BackgroundType type = useVideo
                    ? BackgroundManager.BackgroundType.Video
                    : BackgroundManager.BackgroundType.Image;

                backgroundManager.ChangeBackgroundType(type);
                audioManager?.PlaySFX("MenuSelect");

                Log($"Background changed to: {type}");
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Unsubscribe จาก Events
            if (newGameButton != null)
                newGameButton.onClick.RemoveListener(OnNewGame);

            if (optionsButton != null)
                optionsButton.onClick.RemoveListener(OnOptions);

            if (quitButton != null)
                quitButton.onClick.RemoveListener(OnQuit);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackToMainMenu);

            if (bgmVolumeSlider != null)
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBGMVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (videoBackgroundToggle != null)
                videoBackgroundToggle.onValueChanged.RemoveListener(OnVideoBackgroundToggled);
        }

        #endregion

        #region Debug Helpers

        private void Log(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[MainMenuController] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[MainMenuController] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MainMenuController] {message}");
        }

        public bool IsOptionsActive()
        {
            return optionsPanel != null && optionsPanel.activeSelf;
        }

        #endregion
    }
}