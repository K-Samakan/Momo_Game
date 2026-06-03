using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossMap1UI : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public Image[] barFills;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI hpText;

    [Header("Colors")]
    public Color colorFull = new Color(0.886f, 0.294f, 0.290f);
    public Color colorEmpty = new Color(0.267f, 0.267f, 0.255f);

    [Header("Settings")]
    public float fadeInDuration = 0.8f;
    public float fadeOutDuration = 0.5f;
    public int maxHP = 50;

    [Header("Editor Test")]
    public bool previewInEditor = false;

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

        for (int slot = 0; slot < 5; slot++)
        {
            // slot 0 = ซ้ายสุด = HP 1-10  (เหลือสุดท้าย)
            // slot 4 = ขวาสุด = HP 41-50  (หมดก่อน)
            int slotHpMin = slot * 10;
            int slotHpMax = slotHpMin + 10;

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
                float pct = (float)(currentHP - slotHpMin) / 10f;
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