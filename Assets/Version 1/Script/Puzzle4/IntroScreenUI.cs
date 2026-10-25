// ============================================================
// FILE: IntroScreenUI.cs
// PATH: Assets/Version 1/Script/Puzzle4/IntroScreenUI.cs
// ============================================================
// ✅ CHANGELOG (ใช้ร่วมกัน Map 2 / Map 3 / Map 4)
//   [FIX] Show() — ถ้าวางสคริปต์ไว้บน Panel ตัวเดียวกัน Awake() จะปิดตัวเองไปด้วย
//                 → StartCoroutine ไม่ได้ → Intro ไม่ขึ้น  ⇒ ตอนนี้เปิดตัวเองก่อนเล่น
//   [FIX] Show() — ถ้า parent ถูกปิด (เช่นเป็นลูกของ HUD หลักที่ถูกซ่อน) จะ log error บอกชัดเจน
//   [NEW] introPanel ว่าง → ใช้ gameObject ตัวเอง (เหมือน PuzzleClearUI)
//   [NEW] TotalDuration — เวลารวม fadeIn + display + fadeOut ให้ Manager รอได้พอดี
// ============================================================
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

    /// <summary>✅ NEW: เวลารวมของ Intro (ให้ Manager รอได้พอดี)</summary>
    public float TotalDuration => fadeInDuration + displayDuration + fadeOutDuration;

    void Awake()
    {
        // ✅ NEW: ถ้าไม่ได้ลาก introPanel → ใช้ตัวเอง
        if (introPanel == null)
            introPanel = gameObject;

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
        if (introPanel == null)
        {
            Debug.LogError("[IntroScreenUI] ❌ ไม่มี introPanel!");
            return;
        }

        // ✅ FIX: สคริปต์อยู่บน Panel ตัวเอง → Awake ปิดตัวเองไปแล้ว → เปิดกลับก่อนรัน Coroutine
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // ✅ FIX: parent ถูกปิดอยู่ (เช่น Intro เป็นลูกของ HUD หลักที่ถูกซ่อน) → แสดงไม่ได้
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError($"[IntroScreenUI] ❌ '{gameObject.name}' อยู่ใต้ parent ที่ถูกปิด " +
                           $"(เช่น HUD หลัก) — ย้าย Intro UI ออกมาไว้ใน Canvas ที่ไม่ถูกซ่อน");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(ShowSequence());
        Debug.Log($"[IntroScreenUI] ▶ Show '{gameObject.name}' ({TotalDuration:F1}s)");
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

        Debug.Log("[IntroScreenUI] ✅ Intro sequence completed");

        // ซ่อน Panel (ทำท้ายสุด — ถ้าสคริปต์อยู่บน Panel เอง Coroutine จะหยุดตรงนี้)
        introPanel.SetActive(false);
    }
}