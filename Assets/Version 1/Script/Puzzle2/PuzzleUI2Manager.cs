using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PuzzleUI2Manager : MonoBehaviour
{
    [Header("UI References")]
    public Canvas puzzleCanvas;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI mirrorCountText;
    public TextMeshProUGUI healthText;
    public Slider mirrorCheckProgressBar;
    public GameObject controlsPanel;
    public GameObject completionPanel;
    
    [Header("Visual Feedback")]
    public Image dangerOverlay;
    public Image safeOverlay;
    public Color dangerColor = Color.red;
    public Color safeColor = Color.green;
    public AnimationCurve overlayFadeCurve;
    
    [Header("Audio")]
    public AudioSource uiAudioSource;
    public AudioClip buttonClickSound;
    public AudioClip warningSound;
    public AudioClip successChime;

    private MirrorLightPuzzle puzzleManager;
    private HapticManager hapticManager;
    private bool isShowingFeedback = false;
    private Coroutine feedbackCoroutine;

    void Start()
    {
        puzzleManager = FindObjectOfType<MirrorLightPuzzle>();
        hapticManager = HapticManager.Instance;
        
        InitializeUI();
        UpdateMirrorCount();
    }

    void Update()
    {
        UpdateUI();
    }

    void InitializeUI()
    {
        if (puzzleCanvas) puzzleCanvas.gameObject.SetActive(true);
        if (completionPanel) completionPanel.SetActive(false);
        if (mirrorCheckProgressBar) mirrorCheckProgressBar.gameObject.SetActive(false);
        
        if (instructionText)
        {
            instructionText.text = "🔍 คลิกที่กระจกเพื่อตรวจสอบ\n" +
                                  "⚠️ ระวัง! กระจกหลอกจะระเบิด\n" +
                                  "🔄 ใช้ Q/E เพื่อหมุนกระจกจริง\n" +
                                  "🎯 ส่องแสงไปที่สัญลักษณ์พระอาทิตย์";
        }

        if (controlsPanel)
        {
            // แสดงการควบคุม
            TextMeshProUGUI controlsText = controlsPanel.GetComponentInChildren<TextMeshProUGUI>();
            if (controlsText)
            {
                controlsText.text = "🖱️ คลิกซ้าย: เลือกกระจก\n" +
                                   "⌨️ Q: หมุนซ้าย\n" +
                                   "⌨️ E: หมุนขวา\n" +
                                   "⌨️ ESC: ยกเลิกการเลือก\n" +
                                   "🎮 จับจอยแน่นเพื่อรู้สึกถึงประเภทกระจก!";
            }
        }

        // ซ่อน overlay ในตอนเริ่ม
        if (dangerOverlay) 
        {
            dangerOverlay.color = new Color(dangerColor.r, dangerColor.g, dangerColor.b, 0);
            dangerOverlay.gameObject.SetActive(false);
        }
        if (safeOverlay) 
        {
            safeOverlay.color = new Color(safeColor.r, safeColor.g, safeColor.b, 0);
            safeOverlay.gameObject.SetActive(false);
        }
    }

    void UpdateUI()
    {
        if (!puzzleManager) return;

        UpdateMirrorCount();
        UpdateHealthDisplay();
        UpdateStatusText();
        
        if (puzzleManager.IsPuzzleCompleted() && completionPanel && !completionPanel.activeInHierarchy)
        {
            ShowCompletionScreen();
        }
    }

    void UpdateMirrorCount()
    {
        if (!mirrorCountText || !puzzleManager) return;

        int activeMirrors = puzzleManager.GetActiveMirrorsCount();
        int remainingFakes = puzzleManager.GetRemainingFakeMirrors();
        
        mirrorCountText.text = $"🪞 กระจกใช้งานได้: {activeMirrors}/5\n" +
                              $"💣 กระจกหลอกเหลือ: {remainingFakes}";
    }

    void UpdateHealthDisplay()
    {
        if (!healthText) return;
        
        // เชื่อมต่อกับระบบ Health ของเกม
        // ตัวอย่าง:
        // int currentHealth = PlayerHealth.Instance.GetCurrentHealth();
        // int maxHealth = PlayerHealth.Instance.GetMaxHealth();
        
        // สำหรับตัวอย่าง ใช้ค่าคงที่
        int currentHealth = 5; // ควรเปลี่ยนเป็นค่าจริง
        int maxHealth = 5;
        
        healthText.text = $"❤️ HP: {currentHealth}/{maxHealth}";
        
        // เปลี่ยนสีตาม HP
        if (currentHealth <= 1)
        {
            healthText.color = Color.red;
        }
        else if (currentHealth <= 2)
        {
            healthText.color = Color.yellow;
        }
        else
        {
            healthText.color = Color.white;
        }
    }

    void UpdateStatusText()
    {
        if (!statusText) return;

        if (puzzleManager.IsPuzzleCompleted())
        {
            statusText.text = "🎉 แก้ Puzzle สำเร็จ!";
            statusText.color = Color.green;
        }
        else
        {
            statusText.text = "🔍 กำลังแก้ Puzzle...";
            statusText.color = Color.white;
        }
    }

    public void ShowMirrorCheckProgress(float progress, bool isDangerous)
    {
        if (!mirrorCheckProgressBar) return;

        if (!mirrorCheckProgressBar.gameObject.activeInHierarchy)
        {
            mirrorCheckProgressBar.gameObject.SetActive(true);
        }

        mirrorCheckProgressBar.value = progress;

        // เปลี่ยนสี Progress Bar ตามประเภทกระจก
        Image fillImage = mirrorCheckProgressBar.fillRect.GetComponent<Image>();
        if (fillImage)
        {
            if (isDangerous)
            {
                // แสดงสีแดงเมื่ออันตราย
                fillImage.color = Color.Lerp(Color.yellow, Color.red, progress);
            }
            else
            {
                // แสดงสีเขียวเมื่อปลอดภัย
                fillImage.color = Color.Lerp(Color.yellow, Color.green, progress);
            }
        }

        // แสดง Visual Feedback
        if (isDangerous && progress > 0.5f)
        {
            ShowDangerFeedback(progress);
        }
        else if (!isDangerous)
        {
            ShowSafeFeedback(progress);
        }

        // ซ่อน Progress Bar เมื่อตรวจสอบเสร็จ
        if (progress >= 1f)
        {
            StartCoroutine(HideProgressBarAfterDelay(1f));
        }
    }

    public void HideMirrorCheckProgress()
    {
        if (mirrorCheckProgressBar && mirrorCheckProgressBar.gameObject.activeInHierarchy)
        {
            mirrorCheckProgressBar.gameObject.SetActive(false);
        }
        
        HideFeedbackOverlays();
    }

    void ShowDangerFeedback(float intensity)
    {
        if (!dangerOverlay) return;

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine = StartCoroutine(ShowDangerOverlay(intensity));
        
        // เล่นเสียงเตือน
        if (uiAudioSource && warningSound && !uiAudioSource.isPlaying)
        {
            uiAudioSource.PlayOneShot(warningSound, intensity);
        }
    }

    void ShowSafeFeedback(float intensity)
    {
        if (!safeOverlay) return;

        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine = StartCoroutine(ShowSafeOverlay(intensity));
    }

    IEnumerator ShowDangerOverlay(float intensity)
    {
        if (!dangerOverlay) yield break;

        dangerOverlay.gameObject.SetActive(true);
        
        float targetAlpha = Mathf.Lerp(0.1f, 0.5f, intensity);
        float currentAlpha = dangerOverlay.color.a;
        
        float duration = 0.3f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(currentAlpha, targetAlpha, overlayFadeCurve.Evaluate(t / duration));
            dangerOverlay.color = new Color(dangerColor.r, dangerColor.g, dangerColor.b, alpha);
            yield return null;
        }
        
        dangerOverlay.color = new Color(dangerColor.r, dangerColor.g, dangerColor.b, targetAlpha);
    }

    IEnumerator ShowSafeOverlay(float intensity)
    {
        if (!safeOverlay) yield break;

        safeOverlay.gameObject.SetActive(true);
        
        float targetAlpha = Mathf.Lerp(0.05f, 0.2f, intensity);
        float currentAlpha = safeOverlay.color.a;
        
        float duration = 0.2f;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(currentAlpha, targetAlpha, overlayFadeCurve.Evaluate(t / duration));
            safeOverlay.color = new Color(safeColor.r, safeColor.g, safeColor.b, alpha);
            yield return null;
        }
        
        safeOverlay.color = new Color(safeColor.r, safeColor.g, safeColor.b, targetAlpha);
    }

    void HideFeedbackOverlays()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine = StartCoroutine(FadeOutOverlays());
    }

    IEnumerator FadeOutOverlays()
    {
        float duration = 0.5f;
        
        Color dangerStart = dangerOverlay ? dangerOverlay.color : Color.clear;
        Color safeStart = safeOverlay ? safeOverlay.color : Color.clear;
        
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / duration);
            
            if (dangerOverlay)
            {
                dangerOverlay.color = new Color(dangerStart.r, dangerStart.g, dangerStart.b, dangerStart.a * alpha);
            }
            
            if (safeOverlay)
            {
                safeOverlay.color = new Color(safeStart.r, safeStart.g, safeStart.b, safeStart.a * alpha);
            }
            
            yield return null;
        }
        
        if (dangerOverlay) dangerOverlay.gameObject.SetActive(false);
        if (safeOverlay) safeOverlay.gameObject.SetActive(false);
    }

    IEnumerator HideProgressBarAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideMirrorCheckProgress();
    }

    void ShowCompletionScreen()
    {
        if (!completionPanel) return;

        completionPanel.SetActive(true);
        
        // เล่นเสียงสำเร็จ
        if (uiAudioSource && successChime)
        {
            uiAudioSource.PlayOneShot(successChime);
        }

        // อัพเดตข้อความสำเร็จ
        TextMeshProUGUI completionText = completionPanel.GetComponentInChildren<TextMeshProUGUI>();
        if (completionText)
        {
            int activeMirrors = puzzleManager.GetActiveMirrorsCount();
            completionText.text = $"🎉 ยินดีด้วย!\n\n" +
                                 $"คุณแก้ Mirror Light Puzzle สำเร็จ!\n" +
                                 $"ใช้กระจกทั้งหมด {activeMirrors} บาน\n\n" +
                                 $"แสงได้ไปถึงสัญลักษณ์พระอาทิตย์แล้ว! ☀️";
        }
    }

    // Public Methods สำหรับการเรียกใช้จาก MirrorLightPuzzle
    public void OnMirrorSelected(int mirrorIndex, bool isDangerous)
    {
        if (uiAudioSource && buttonClickSound)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }

        if (statusText)
        {
            statusText.text = $"🔍 กำลังตรวจสอบกระจก {mirrorIndex + 1}...";
        }
    }

    public void OnMirrorCheckComplete(bool wasDangerous, bool exploded)
    {
        HideMirrorCheckProgress();
        
        if (exploded)
        {
            if (statusText)
            {
                statusText.text = "💥 กระจกหลอกระเบิด! HP -1";
                statusText.color = Color.red;
            }
            
            // แสดงเอฟเฟคระเบิด
            StartCoroutine(ShowExplosionEffect());
        }
        else
        {
            if (statusText)
            {
                statusText.text = "✅ กระจกจริง! สามารถหมุนได้";
                statusText.color = Color.green;
            }
        }
        
        // รีเซ็ตข้อความหลังจาก 3 วินาที
        StartCoroutine(ResetStatusTextAfterDelay(3f));
    }

    IEnumerator ShowExplosionEffect()
    {
        // แสดงเอฟเฟคแฟลชสีแดง
        if (dangerOverlay)
        {
            dangerOverlay.gameObject.SetActive(true);
            dangerOverlay.color = new Color(1, 0, 0, 0.8f);
            
            yield return new WaitForSeconds(0.1f);
            
            dangerOverlay.color = new Color(1, 0, 0, 0.4f);
            yield return new WaitForSeconds(0.1f);
            
            dangerOverlay.color = new Color(1, 0, 0, 0);
            dangerOverlay.gameObject.SetActive(false);
        }
    }

    IEnumerator ResetStatusTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (statusText && !puzzleManager.IsPuzzleCompleted())
        {
            statusText.text = "🔍 กำลังแก้ Puzzle...";
            statusText.color = Color.white;
        }
    }

    public void OnRestartButtonClicked()
    {
        if (uiAudioSource && buttonClickSound)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
        
        // รีสตาร์ท Puzzle
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    public void OnQuitButtonClicked()
    {
        if (uiAudioSource && buttonClickSound)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
        
        Application.Quit();
    }
}