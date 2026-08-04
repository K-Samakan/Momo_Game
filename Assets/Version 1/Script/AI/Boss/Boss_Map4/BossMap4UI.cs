using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI แถบเลือดบอสด่าน 4 — หุ่นยนต์หมึกยักษ์
/// HP = 50 (5 ช่อง × 10 HP ตามค่าดีฟอลต์ — ปรับ barFills ได้ตามจำนวนช่องจริงใน Canvas)
/// โครงสร้างเหมือน BossMap3UI ทุกประการ เพื่อลดโอกาส error ตอนต่อสาย Inspector
/// </summary>
public class BossMap4UI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public Image[] barFills;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI hpText;

    [Header("Colors")]
    public Color colorFull = new Color(0.55f, 0.25f, 0.75f); // ม่วงหมึก
    public Color colorEmpty = new Color(0.267f, 0.267f, 0.255f);

    [Header("Settings")]
    public float fadeInDuration = 0.8f;
    public float fadeOutDuration = 0.5f;
    public int maxHP = 50;

    private int BarCount => barFills != null && barFills.Length > 0 ? barFills.Length : 5;
    private int HpPerBar => Mathf.Max(1, maxHP / BarCount);

    private void Awake()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(false);
    }

    public void ShowBossUI()
    {
        gameObject.SetActive(true);
        UpdateHealthBar(maxHP);
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));
    }

    public void HideBossUI() => StartCoroutine(HideSequence());

    private IEnumerator HideSequence()
    {
        yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutDuration));
        gameObject.SetActive(false);
    }

    public void UpdateHealthBar(int currentHP)
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        if (hpText != null)
            hpText.text = currentHP > 0 ? $"{currentHP} / {maxHP}" : "DEAD";

        if (barFills == null) return;

        int hpPerBar = HpPerBar;
        for (int slot = 0; slot < BarCount; slot++)
        {
            if (slot >= barFills.Length || barFills[slot] == null) continue;
            int slotMin = slot * hpPerBar;
            int slotMax = slotMin + hpPerBar;
            if (currentHP >= slotMax) { barFills[slot].fillAmount = 1f; barFills[slot].color = colorFull; }
            else if (currentHP <= slotMin) { barFills[slot].fillAmount = 1f; barFills[slot].color = colorEmpty; }
            else { barFills[slot].fillAmount = (float)(currentHP - slotMin) / hpPerBar; barFills[slot].color = colorFull; }
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float t = 0f;
        if (canvasGroup != null) canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = to;
    }
}