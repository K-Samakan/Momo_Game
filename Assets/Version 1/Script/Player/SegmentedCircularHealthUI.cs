using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HP Bar แบบวงกลม แบ่งเป็นข้อๆ พร้อมกระพริบเมื่อโดนดาเมจ
/// ต่อกับ PlayerHealth.cs ของเกม
/// </summary>
public class SegmentedCircularHealthUI : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private Image circularFillImage;
    [SerializeField] private Transform segmentsContainer;
    [SerializeField] private GameObject segmentPrefab;
    
    [Header("=== Segment Settings ===")]
    [SerializeField] private Color fullHealthColor = new Color(0f, 1f, 0f, 1f);   // เขียว
    [SerializeField] private Color emptyHealthColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private float segmentSpacing = 5f;
    
    [Header("=== Flash Effect ===")]
    [SerializeField] private bool enableFlash = true;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private int flashCount = 1;
    
    [Header("=== Animation ===")]
    [SerializeField] private float smoothSpeed = 5f;
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugLogs = true;

    // State
    private List<Image> segmentImages = new List<Image>();
    private float targetFillAmount = 1f;
    private bool isFlashing = false;
    
    // ✅ ใช้ PlayerHealth ที่มีอยู่
    private MOMO.Player.PlayerHealth playerHealth;
    private int maxHealth = 5;
    private int currentHealth = 5;
    private int previousHealth = 5;

    #region Initialization

    private void Awake()
    {
        // หา PlayerHealth
        playerHealth = FindObjectOfType<MOMO.Player.PlayerHealth>();
        
        if (playerHealth != null)
        {
            maxHealth = playerHealth.maxHealth;
            currentHealth = playerHealth.currentHealth;
            previousHealth = currentHealth;
            
            if (showDebugLogs)
            {
                Debug.Log($"✅ [CircularHealthUI] Connected to PlayerHealth | HP: {currentHealth}/{maxHealth}");
            }
        }
        else
        {
            Debug.LogError("❌ [CircularHealthUI] PlayerHealth not found!");
        }

        CreateSegments();
        UpdateHealthDisplay();
    }

    private void CreateSegments()
    {
        // Clear existing
        foreach (Transform child in segmentsContainer)
        {
            Destroy(child.gameObject);
        }
        segmentImages.Clear();

        // Calculate angle per segment
        float anglePerSegment = 360f / maxHealth;
        float startAngle = 90f;

        for (int i = 0; i < maxHealth; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, segmentsContainer);
            Image segmentImage = segment.GetComponent<Image>();
            
            if (segmentImage != null)
            {
                segmentImages.Add(segmentImage);
                
                float angle = startAngle - (anglePerSegment * i);
                segment.transform.localRotation = Quaternion.Euler(0, 0, angle);
                
                segmentImage.color = fullHealthColor;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"✅ [CircularHealthUI] Created {segmentImages.Count} segments");
        }
    }

    #endregion

    #region Update

    private void Update()
    {
        // Smooth fill animation
        if (circularFillImage != null)
        {
            circularFillImage.fillAmount = Mathf.Lerp(
                circularFillImage.fillAmount,
                targetFillAmount,
                Time.deltaTime * smoothSpeed
            );
        }

        // ✅ Monitor health changes
        if (playerHealth != null)
        {
            int newHealth = playerHealth.currentHealth;

            if (newHealth != previousHealth)
            {
                bool tookDamage = newHealth < previousHealth;
                UpdateHealth(newHealth, tookDamage);
                previousHealth = newHealth;
            }
        }
    }

    #endregion

    #region Health Management

    private void UpdateHealth(int newHealth, bool tookDamage)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        targetFillAmount = (float)currentHealth / maxHealth;

        UpdateHealthDisplay();

        if (tookDamage && enableFlash && currentHealth > 0)
        {
            StartCoroutine(FlashEffect());
        }

        if (showDebugLogs)
        {
            Debug.Log($"❤️ [CircularHealthUI] HP: {currentHealth}/{maxHealth}");
        }
    }

    private void UpdateHealthDisplay()
    {
        // Update segments
        for (int i = 0; i < segmentImages.Count; i++)
        {
            if (i < currentHealth)
            {
                segmentImages[i].color = fullHealthColor;
            }
            else
            {
                segmentImages[i].color = emptyHealthColor;
            }
        }

        // Update circular fill
        if (circularFillImage != null)
        {
            circularFillImage.color = fullHealthColor;
        }
    }

    #endregion

    #region Flash Effect

    private IEnumerator FlashEffect()
    {
        if (isFlashing) yield break;
        isFlashing = true;

        Color originalColor = fullHealthColor;

        for (int i = 0; i < flashCount; i++)
        {
            yield return FlashToColor(flashColor, flashDuration / 2f);
            yield return FlashToColor(originalColor, flashDuration / 2f);
        }

        isFlashing = false;
    }

    private IEnumerator FlashToColor(Color targetColor, float duration)
    {
        float elapsed = 0f;
        Color startColor = circularFillImage != null ? circularFillImage.color : fullHealthColor;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Color flashedColor = Color.Lerp(startColor, targetColor, t);

            if (circularFillImage != null)
            {
                circularFillImage.color = flashedColor;
            }

            for (int i = 0; i < currentHealth && i < segmentImages.Count; i++)
            {
                segmentImages[i].color = flashedColor;
            }

            yield return null;
        }
    }

    #endregion

    #region Public API

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;

    #endregion

    #region Debug

    [ContextMenu("Test Take Damage")]
    private void TestTakeDamage()
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(1);
        }
    }

    [ContextMenu("Test Heal")]
    private void TestHeal()
    {
        if (playerHealth != null)
        {
            playerHealth.Heal(1);
        }
    }

    #endregion
}