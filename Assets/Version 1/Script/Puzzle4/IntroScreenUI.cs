using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class IntroScreenUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject introPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 3f;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        // เพิ่ม CanvasGroup ถ้ายังไม่มี
        canvasGroup = introPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = introPanel.AddComponent<CanvasGroup>();
        }

        // ซ่อน Panel ตอนเริ่ม
        HideImmediate();
    }

    /// <summary>
    /// แสดง Intro Screen
    /// </summary>
    public void Show()
    {
        StartCoroutine(ShowSequence());
    }

    /// <summary>
    /// ซ่อนทันที (ไม่มี Animation)
    /// </summary>
    public void HideImmediate()
    {
        if (introPanel != null)
        {
            introPanel.SetActive(false);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    /// <summary>
    /// Coroutine แสดง Intro
    /// </summary>
    IEnumerator ShowSequence()
    {
        // เปิด Panel
        introPanel.SetActive(true);
        canvasGroup.alpha = 0f;

        // Fade In
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // แสดงตามเวลาที่กำหนด
        yield return new WaitForSeconds(displayDuration);

        // Fade Out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // ซ่อน Panel
        introPanel.SetActive(false);

        Debug.Log("[IntroScreenUI] ✅ Intro sequence completed");
    }
}