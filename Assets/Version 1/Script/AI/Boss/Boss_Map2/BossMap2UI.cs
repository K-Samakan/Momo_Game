using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI แถบเลือดบอสด่าน 2 — หุ่นยนต์พ่อมด
/// เปลี่ยนจาก Map1: maxHP 50 → 40 (4 ช่อง แทน 5 ช่อง)
/// โครงสร้างเหมือน BossMap1UI ทุกประการ
/// </summary>
public class BossMap2UI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public Image[] barFills;            // ▼ ใส่ 4 ช่อง (Map1 ใส่ 5 ช่อง)
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI hpText;

    [Header("Colors")]
    public Color colorFull = new Color(0.886f, 0.294f, 0.290f);
    public Color colorEmpty = new Color(0.267f, 0.267f, 0.255f);

    [Header("Settings")]
    public float fadeInDuration = 0.8f;
    public float fadeOutDuration = 0.5f;
    public int maxHP = 40;              // ▼ เปลี่ยนจาก 50 → 40

    [Header("Editor Test")]
    public bool previewInEditor = false;

    // จำนวนช่อง HP bar (คำนวณจาก barFills.Length)
    private int BarCount => barFills != null ? barFills.Length : 4;
    private int HpPerBar => maxHP / BarCount;

    private void OnValidate()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = previewInEditor ? 1f : 0f;
    }

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void ShowBossUI()
    {
        gameObject.SetActive(true);
        UpdateHealthBar(maxHP);
        StopAllCoroutines();
        StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));
    }

    public void HideBossUI()
    {
        StartCoroutine(HideSequence());
    }

    private IEnumerator HideSequence()
    {
        yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutDuration));
        gameObject.SetActive(false);
    }

    public void UpdateHealthBar(int currentHP)
    {
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);
        hpText.text = currentHP > 0 ? $"{currentHP} / {maxHP}" : "DEAD";

        int hpPerBar = HpPerBar;

        for (int slot = 0; slot < BarCount; slot++)
        {
            if (slot >= barFills.Length) break;

            int slotHpMin = slot * hpPerBar;
            int slotHpMax = slotHpMin + hpPerBar;

            if (currentHP >= slotHpMax)
            {
                barFills[slot].fillAmount = 1f;
                barFills[slot].color = colorFull;
            }
            else if (currentHP <= slotHpMin)
            {
                barFills[slot].fillAmount = 1f;
                barFills[slot].color = colorEmpty;
            }
            else
            {
                float pct = (float)(currentHP - slotHpMin) / hpPerBar;
                barFills[slot].fillAmount = pct;
                barFills[slot].color = colorFull;
            }
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
    }
}