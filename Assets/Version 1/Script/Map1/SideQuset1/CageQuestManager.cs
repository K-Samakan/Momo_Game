using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class CageQuestManager : MonoBehaviour
{
    public static CageQuestManager Instance;

    [Header("Quest Settings")]
    public int totalCages = 5;
    private int freedCages = 0;

    [Header("UI")]
    public TextMeshProUGUI questCounterText;
    public GameObject questCompletePanel;
    public TextMeshProUGUI questCompleteText;
    public Image panelBackground;

    [Header("Effect Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color successColor = new Color(0f, 0.8f, 0.2f, 0.8f);
    public float textPunchScale = 1.4f;
    public float fadeOutDelay = 2f;
    public float fadeOutDuration = 1f;

    [Header("Tiger Ally")]
    public TigerAllyEffect tigerAllyEffect;

    public bool IsQuestComplete { get; private set; } = false;

    void Awake()
    {
        Instance = this;
        if (questCompletePanel) questCompletePanel.SetActive(false);
    }

    void Start()
    {
        UpdateUI();
    }

    public void OnCageFreed()
    {
        freedCages++;
        freedCages = Mathf.Clamp(freedCages, 0, totalCages);
        UpdateUI();

        if (freedCages >= totalCages)
            CompleteQuest();
    }

    void CompleteQuest()
    {
        IsQuestComplete = true;
        if (tigerAllyEffect) tigerAllyEffect.EnableTigerAlly();

        // Haptic เหมือน PuzzleComplete
        HapticManager.Instance?.PlayPattern(HapticPatternType.PuzzleComplete);

        // เริ่ม UI Effect
        StartCoroutine(QuestCompleteEffect());

        Debug.Log("[Quest] ภารกิจสำเร็จ!");
    }

    IEnumerator QuestCompleteEffect()
    {
        // แสดง Panel
        if (questCompletePanel) questCompletePanel.SetActive(true);

        // เปลี่ยน Panel เป็นสีเขียว
        if (panelBackground)
            panelBackground.color = successColor;

        // Text Punch Effect
        if (questCompleteText)
        {
            questCompleteText.text = "ช่วยเหลือเสือออกจากกรงสำเร็จ!";
            yield return StartCoroutine(PunchTextEffect(questCompleteText));
        }

        // รอก่อน Fade Out
        yield return new WaitForSeconds(fadeOutDelay);

        // Fade Out ทั้ง Panel
        yield return StartCoroutine(FadeOutPanel());

        // ซ่อน Panel
        if (questCompletePanel) questCompletePanel.SetActive(false);
    }

    IEnumerator PunchTextEffect(TextMeshProUGUI text)
    {
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 originalScale = Vector3.one;

        // โตขึ้น
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            text.transform.localScale = Vector3.Lerp(originalScale,
                originalScale * textPunchScale, t);
            yield return null;
        }

        elapsed = 0f;

        // กลับเล็ก
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            text.transform.localScale = Vector3.Lerp(originalScale * textPunchScale,
                originalScale, t);
            yield return null;
        }

        text.transform.localScale = originalScale;
    }

    IEnumerator FadeOutPanel()
    {
        float elapsed = 0f;

        CanvasGroup canvasGroup = questCompletePanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = questCompletePanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 1f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
    }

    void UpdateUI()
    {
        if (questCounterText)
            questCounterText.text = $"ช่วยเหลือเสือออกจากกรง : {freedCages}/{totalCages}";
    }
}