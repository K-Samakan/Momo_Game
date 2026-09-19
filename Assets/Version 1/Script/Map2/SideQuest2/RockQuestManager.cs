using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// จัดการภารกิจรอง "ฟื้นฟูโอเอซิส" (Map2) — ดัดแปลงจาก CageQuestManager.cs (Map1)
/// โครงสร้าง UI/Effect/Debug เหมือนเดิมทุกอย่าง ต่างแค่:
///   • นับหิน (destroyedRocks) แทนกรง
///   • ตอนสำเร็จ ปลดล็อกความสามารถ "โล่ป้องกัน" ผ่าน OasisShieldEffect
///     แทนการเปิด TigerAllyEffect โดยตรง (เพราะโล่ต้องรอเงื่อนไข HP<=2 ตอนสู้บอสอีกที
///     ไม่ได้ enable ทันทีเหมือนเสือ)
/// </summary>
public class RockQuestManager : MonoBehaviour
{
    public static RockQuestManager Instance;

    [Header("Quest Settings")]
    public int totalRocks = 5;
    private int destroyedRocks = 0;

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

    [Header("Oasis Shield (Boss Reward)")]
    public OasisShieldEffect oasisShieldEffect;

    [Header("🧪 Debug / Test")]
    [Tooltip("เปิดถ้าอยากใช้ปุ่มคีย์บอร์ดลัดเทสตอน Runtime (นอกเหนือจาก ContextMenu)")]
    public bool enableDebugKey = true;
    public KeyCode debugCompleteKey = KeyCode.F9;

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

    void Update()
    {
        if (enableDebugKey && Input.GetKeyDown(debugCompleteKey))
        {
            TEST_CompleteRockQuestInstantly();
        }
    }

    public void OnRockDestroyed()
    {
        destroyedRocks++;
        destroyedRocks = Mathf.Clamp(destroyedRocks, 0, totalRocks);
        UpdateUI();

        if (destroyedRocks >= totalRocks)
            CompleteQuest();
    }

    void CompleteQuest()
    {
        IsQuestComplete = true;

        // ✅ ปลดล็อกความสามารถโล่ — ยังไม่ activate ทันที รอเงื่อนไข HP<=2 ตอนสู้บอส
        if (oasisShieldEffect) oasisShieldEffect.EnableShieldCapability();

        HapticManager.Instance?.PlayPattern(HapticPatternType.PuzzleComplete);

        StartCoroutine(QuestCompleteEffect());

        Debug.Log("[Quest] ภารกิจฟื้นฟูโอเอซิสสำเร็จ!");
    }

    IEnumerator QuestCompleteEffect()
    {
        if (questCompletePanel) questCompletePanel.SetActive(true);

        if (panelBackground)
            panelBackground.color = successColor;

        if (questCompleteText)
        {
            questCompleteText.text = "Oasis restored!";
            yield return StartCoroutine(PunchTextEffect(questCompleteText));
        }

        yield return new WaitForSeconds(fadeOutDelay);

        yield return StartCoroutine(FadeOutPanel());

        if (questCompletePanel) questCompletePanel.SetActive(false);
    }

    IEnumerator PunchTextEffect(TextMeshProUGUI text)
    {
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 originalScale = Vector3.one;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / (duration / 2f);
            text.transform.localScale = Vector3.Lerp(originalScale,
                originalScale * textPunchScale, t);
            yield return null;
        }

        elapsed = 0f;

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

    /// <summary>
    /// 🧪 TEST ONLY — จบ Side Quest โอเอซิสทันที โดยไม่ต้องทุบหินจริง
    /// เรียก flow เดิมทุกขั้นตอน (UI, Haptic, OasisShieldEffect) เหมือนเล่นจริง
    /// </summary>
    [ContextMenu("🧪 TEST - Complete Rock Quest Instantly")]
    public void TEST_CompleteRockQuestInstantly()
    {
        if (IsQuestComplete)
        {
            Debug.Log("[TEST] เควสฟื้นฟูโอเอซิสสำเร็จไปแล้ว ไม่ต้องทำซ้ำ");
            return;
        }

        Debug.Log("[TEST] ⚡ บังคับจบ Side Quest โอเอซิส (ข้ามการทุบหินจริง)");

        while (destroyedRocks < totalRocks)
        {
            OnRockDestroyed();
        }
    }

    void UpdateUI()
    {
        if (questCounterText)
            questCounterText.text = $"Rocks destroyed : {destroyedRocks}/{totalRocks}";
    }
}