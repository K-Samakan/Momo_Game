using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI แถบเลือดบอสด่าน 3 — หุ่นยนต์ Snowman
/// HP = 40 (4 ช่อง × 10 HP)
/// </summary>
public class BossMap3UI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public Image[] barFills;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI hpText;

    [Header("Colors")]
    public Color colorFull = new Color(0.3f, 0.7f, 1f); // ฟ้าน้ำแข็ง
    public Color colorEmpty = new Color(0.267f, 0.267f, 0.255f);

    [Header("Settings")]
    public float fadeInDuration = 0.8f;
    public float fadeOutDuration = 0.5f;
    public int maxHP = 40;

    private int BarCount => barFills != null ? barFills.Length : 4;
    private int HpPerBar => maxHP / BarCount;

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

    public void HideBossUI() { StartCoroutine(HideSequence()); }

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
            int slotMin = slot * hpPerBar;
            int slotMax = slotMin + hpPerBar;
            if (currentHP >= slotMax) { barFills[slot].fillAmount = 1f; barFills[slot].color = colorFull; }
            else if (currentHP <= slotMin) { barFills[slot].fillAmount = 1f; barFills[slot].color = colorEmpty; }
            else { barFills[slot].fillAmount = (float)(currentHP - slotMin) / hpPerBar; barFills[slot].color = colorFull; }
        }
    }

    private IEnumerator FadeCanvas(float from, float to, float duration)
    {
        float t = 0f; canvasGroup.alpha = from;
        while (t < duration) { t += Time.deltaTime; canvasGroup.alpha = Mathf.Lerp(from, to, t / duration); yield return null; }
        canvasGroup.alpha = to;
    }
}