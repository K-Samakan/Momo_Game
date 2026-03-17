using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// ระบบ UI แสดงค่า Oxygen แบบวงกลม พร้อม Icon ปอดและ Glow Effect
/// </summary>
public class OxygenUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image backgroundImage;         // พื้นหลังมืด
    [SerializeField] private Image outerRingImage;          // วงกลม Radial Fill
    [SerializeField] private Image glowEffectImage;         // เรืองแสง
    [SerializeField] private Image lungIconImage;           // Icon ปอด
    [SerializeField] private TextMeshProUGUI oxygenText;    // ข้อความ "O₂"
    [SerializeField] private TextMeshProUGUI valueText;     // ตัวเลข
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Color Settings")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.82f, 0.88f, 1f); // #4DD0E1
    [SerializeField] private Color lowColor = new Color(1f, 0.92f, 0.23f, 1f);      // #FFEB3B (เหลือง)
    [SerializeField] private Color criticalColor = new Color(1f, 0.27f, 0.27f, 1f); // #FF4444 (แดง)
    [SerializeField] private float colorTransitionSpeed = 5f;

    [Header("Thresholds")]
    [SerializeField] private float lowOxygenThreshold = 30f;     // % ที่เริ่มเป็นสีเหลือง
    [SerializeField] private float criticalThreshold = 10f;      // % ที่เป็นสีแดง

    [Header("Blink Settings")]
    [SerializeField] private float blinkSpeed = 0.5f;
    [SerializeField] private float blinkMinAlpha = 0.3f;
    [SerializeField] private float blinkMaxAlpha = 1f;

    [Header("Glow Settings")]
    [SerializeField] private bool enableGlow = true;
    [SerializeField] private float glowPulseSpeed = 2f;
    [SerializeField] private float glowMinAlpha = 0.3f;
    [SerializeField] private float glowMaxAlpha = 0.8f;

    [Header("Visibility Settings")]
    [SerializeField] private bool hideWhenNotInWater = true;

    // References
    private SwimmingControllerHybrid swimmingController;
    
    // State
    private bool isBlinking = false;
    private Coroutine blinkCoroutine;
    private float currentOxygen;
    private float maxOxygen;
    private Color currentColor;
    private Vector3 originalScale;

    #region Initialization

    private void Awake()
    {
        ValidateReferences();
        InitializeUI();
        originalScale = transform.localScale;
    }

    private void Start()
    {
        FindSwimmingController();
    }

    private void ValidateReferences()
    {
        if (outerRingImage == null)
        {
            Debug.LogError("[OxygenUI] Outer Ring Image is not assigned!");
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // Setup Radial Fill
        if (outerRingImage != null)
        {
            outerRingImage.type = Image.Type.Filled;
            outerRingImage.fillMethod = Image.FillMethod.Radial360;
            outerRingImage.fillOrigin = (int)Image.Origin360.Top;
            outerRingImage.fillClockwise = true;
        }
    }

    private void InitializeUI()
    {
        currentColor = normalColor;
        canvasGroup.alpha = hideWhenNotInWater ? 0f : 1f;
        
        if (outerRingImage != null)
        {
            outerRingImage.fillAmount = 1f;
            outerRingImage.color = normalColor;
        }

        if (glowEffectImage != null)
        {
            glowEffectImage.color = new Color(normalColor.r, normalColor.g, normalColor.b, glowMinAlpha);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.16f, 0.16f, 0.24f, 0.78f); // #28283C
        }

        if (lungIconImage != null)
        {
            lungIconImage.color = Color.white;
        }
    }

    private void FindSwimmingController()
    {
        swimmingController = FindObjectOfType<SwimmingControllerHybrid>();
        
        if (swimmingController == null)
        {
            Debug.LogWarning("[OxygenUI] SwimmingControllerHybrid not found!");
        }
        else
        {
            Debug.Log("[OxygenUI] ✅ Connected to SwimmingControllerHybrid");
        }
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (swimmingController == null) return;

        UpdateOxygenValues();
        UpdateVisibility();
        UpdateUI();
        UpdateColorTransition();
        UpdateGlowEffect();
        CheckCriticalState();
    }

    private void UpdateOxygenValues()
    {
        currentOxygen = swimmingController.GetOxygen();
        maxOxygen = swimmingController.GetMaxOxygen();
    }

    private void UpdateVisibility()
    {
        if (!hideWhenNotInWater) return;

        bool shouldShow = swimmingController.IsSwimming();
        float targetAlpha = shouldShow ? 1f : 0f;

        if (!isBlinking)
        {
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * 5f);
        }
    }

    private void UpdateUI()
    {
        // Update Radial Fill
        float fillAmount = maxOxygen > 0 ? currentOxygen / maxOxygen : 0f;
        if (outerRingImage != null)
        {
            outerRingImage.fillAmount = fillAmount;
        }

        // Update Text
        if (valueText != null)
        {
            valueText.text = Mathf.CeilToInt(currentOxygen).ToString();
        }

    }

    private void UpdateColorTransition()
    {
        float oxygenPercent = GetOxygenPercentage();

        // เลือกสีตาม Threshold
        Color targetColor;
        if (currentOxygen <= 0)
        {
            targetColor = criticalColor;
        }
        else if (oxygenPercent <= criticalThreshold)
        {
            targetColor = criticalColor;
        }
        else if (oxygenPercent <= lowOxygenThreshold)
        {
            targetColor = lowColor;
        }
        else
        {
            targetColor = normalColor;
        }

        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * colorTransitionSpeed);

        // Apply Color
        if (outerRingImage != null)
        {
            outerRingImage.color = currentColor;
        }

        if (oxygenText != null)
        {
            oxygenText.color = currentColor;
        }

        if (valueText != null)
        {
            valueText.color = currentColor;
        }
    }

    private void UpdateGlowEffect()
    {
        if (!enableGlow || glowEffectImage == null) return;

        // Pulse Glow
        float pulse = Mathf.Lerp(glowMinAlpha, glowMaxAlpha, (Mathf.Sin(Time.time * glowPulseSpeed) + 1f) / 2f);
        Color glowColor = new Color(currentColor.r, currentColor.g, currentColor.b, pulse);
        glowEffectImage.color = glowColor;
    }

    private void CheckCriticalState()
    {
        bool shouldBlink = currentOxygen <= 0;

        if (shouldBlink && !isBlinking)
        {
            StartBlinking();
        }
        else if (!shouldBlink && isBlinking)
        {
            StopBlinking();
        }
    }

    private float GetOxygenPercentage()
    {
        return maxOxygen > 0 ? (currentOxygen / maxOxygen) * 100f : 0f;
    }

    #endregion

    #region Blink Animation

    private void StartBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }

        isBlinking = true;
        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private void StopBlinking()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        isBlinking = false;
        
        if (!hideWhenNotInWater || swimmingController.IsSwimming())
        {
            canvasGroup.alpha = 1f;
        }
    }

    private IEnumerator BlinkRoutine()
    {
        while (isBlinking)
        {
            // Fade Out
            float timer = 0f;
            while (timer < blinkSpeed)
            {
                timer += Time.deltaTime;
                float t = timer / blinkSpeed;
                canvasGroup.alpha = Mathf.Lerp(blinkMaxAlpha, blinkMinAlpha, t);
                yield return null;
            }

            // Fade In
            timer = 0f;
            while (timer < blinkSpeed)
            {
                timer += Time.deltaTime;
                float t = timer / blinkSpeed;
                canvasGroup.alpha = Mathf.Lerp(blinkMinAlpha, blinkMaxAlpha, t);
                yield return null;
            }
        }
    }

    #endregion

    #region Public Methods

    public void ForceUpdate()
    {
        if (swimmingController != null)
        {
            UpdateOxygenValues();
            UpdateUI();
        }
    }

    public void SetColorTheme(Color normal, Color low, Color critical)
    {
        normalColor = normal;
        lowColor = low;
        criticalColor = critical;
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
    }

    #endregion

    #region Cleanup

    private void OnDestroy()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
        }
    }

    #endregion
}