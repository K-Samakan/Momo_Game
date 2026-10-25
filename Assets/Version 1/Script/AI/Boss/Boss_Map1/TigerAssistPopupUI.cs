using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TigerAssistPopupUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform popupRect;
    public CanvasGroup canvasGroup;
    public Image tigerPortrait;              // ← ใส่รูปรวม (เสือ+แถบ) ตรงนี้
    public TextMeshProUGUI assistText;       // ← ไม่ต้องผูกก็ได้ ถ้ารูปมีข้อความในตัวแล้ว

    [Header("Position")]
    public Vector2 hiddenPosition = new Vector2(400f, 60f);
    public Vector2 visiblePosition = new Vector2(-20f, 60f);

    [Header("Timing")]
    public float slideInDuration = 0.18f;
    public float holdDuration = 1.2f;
    public float fadeOutDuration = 0.3f;

    // ═══════════════════════════════════════════════════
    // ✅ [NEW] โหมดค้างตามเวลาบัฟ (ใช้กับ Temple Blessing Map4)
    //    ShowPopup() เดิมไม่ถูกแตะ — Map1/2/3 ทำงานเหมือนเดิมทุกอย่าง
    // ═══════════════════════════════════════════════════
    [Header("Sustained Mode (ค้างตามเวลาบัฟ)")]
    [Tooltip("ช่วงท้าย alpha จะค่อยๆ ลดจาก 1 ลงมาเหลือค่านี้ ก่อนหายไป")]
    [Range(0f, 1f)] public float warnMinAlpha = 0.15f;
    [Tooltip("ความถี่กะพริบตอนเริ่มเตือน (ครั้ง/วินาที)")]
    public float blinkStartRate = 2f;
    [Tooltip("ความถี่กะพริบก่อนหมด — ยิ่งใกล้หมดยิ่งกะพริบถี่")]
    public float blinkEndRate = 6f;
    [Tooltip("จุดต่ำสุดของการกะพริบ (สัดส่วนของ alpha ปัจจุบัน) — 0 = ดับสนิท")]
    [Range(0f, 1f)] public float blinkLow = 0.25f;
    [Tooltip("(ไม่บังคับ) ข้อความนับถอยหลัง เช่น \"30s\" — ปล่อยว่างได้")]
    public TextMeshProUGUI countdownText;

    private Coroutine _currentAnim;
    public bool IsShowing => gameObject.activeSelf && canvasGroup != null && canvasGroup.alpha > 0f;

    private void Awake()
    {
        popupRect.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void ShowPopup()
    {
        if (_currentAnim != null)
            StopCoroutine(_currentAnim);

        gameObject.SetActive(true);
        _currentAnim = StartCoroutine(AnimatePopup());
    }

    /// <summary>
    /// ✅ [NEW] แสดงค้างไว้ <paramref name="totalDuration"/> วินาที
    ///   • ช่วงแรก: เลื่อนเข้า แล้วค้างเต็ม 100%
    ///   • <paramref name="warnTime"/> วินาทีสุดท้าย: ค่อยๆ เฟดลง + กะพริบ (ถี่ขึ้นเรื่อยๆ) แล้วหายตอนเวลาหมด
    /// </summary>
    public void ShowPopupSustained(float totalDuration, float warnTime)
    {
        if (_currentAnim != null) StopCoroutine(_currentAnim);
        gameObject.SetActive(true);
        _currentAnim = StartCoroutine(AnimateSustained(totalDuration, Mathf.Clamp(warnTime, 0f, totalDuration)));
    }

    /// <summary>✅ [NEW] ซ่อนทันทีแบบเฟดสั้น (ใช้ตอนบัฟถูกยกเลิกกลางคัน เช่น ผู้เล่นตาย)</summary>
    public void HidePopup()
    {
        if (!gameObject.activeInHierarchy) return;
        if (_currentAnim != null) StopCoroutine(_currentAnim);
        _currentAnim = StartCoroutine(FadeAndHide(canvasGroup.alpha));
    }

    private IEnumerator AnimateSustained(float total, float warn)
    {
        float elapsed = 0f;
        canvasGroup.alpha = 1f;

        // ── เลื่อนเข้า ──
        float t = 0f;
        while (t < slideInDuration)
        {
            t += Time.deltaTime; elapsed += Time.deltaTime;
            popupRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, Mathf.SmoothStep(0f, 1f, t / slideInDuration));
            UpdateCountdown(total - elapsed);
            yield return null;
        }
        popupRect.anchoredPosition = visiblePosition;

        // ── ค้างเต็ม ──
        float warnStart = total - warn;
        while (elapsed < warnStart)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f;
            UpdateCountdown(total - elapsed);
            yield return null;
        }

        // ── ช่วงเตือน: เฟด + กะพริบถี่ขึ้น ──
        float phase = 0f;
        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            float u = warn > 0f ? Mathf.Clamp01((elapsed - warnStart) / warn) : 1f; // 0 → 1

            float envelope = Mathf.Lerp(1f, warnMinAlpha, u);                  // ค่อยๆ จางลง
            phase += Time.deltaTime * Mathf.Lerp(blinkStartRate, blinkEndRate, u);
            float wave = 0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f);        // 1 → 0 → 1
            canvasGroup.alpha = envelope * Mathf.Lerp(blinkLow, 1f, wave);

            UpdateCountdown(total - elapsed);
            yield return null;
        }

        // ── จบ: จางหายสั้นๆ ──
        yield return FadeAndHide(canvasGroup.alpha);
    }

    private IEnumerator FadeAndHide(float fromAlpha)
    {
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, 0f, t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        popupRect.anchoredPosition = hiddenPosition;
        if (countdownText) countdownText.text = "";
        _currentAnim = null;
        gameObject.SetActive(false);
    }

    private void UpdateCountdown(float remain)
    {
        if (countdownText) countdownText.text = $"{Mathf.Max(0, Mathf.CeilToInt(remain))}s";
    }

    private IEnumerator AnimatePopup()
    {
        float t = 0f;
        canvasGroup.alpha = 1f;

        while (t < slideInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / slideInDuration);
            popupRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, p);
            yield return null;
        }
        popupRect.anchoredPosition = visiblePosition;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        popupRect.anchoredPosition = hiddenPosition;
        gameObject.SetActive(false);
    }
}