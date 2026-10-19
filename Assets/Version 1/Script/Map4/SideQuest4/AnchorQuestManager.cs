// ============================================================
// FILE: AnchorQuestManager.cs
// PATH: Assets/Version 1/Script/Map4/SideQuest4/AnchorQuestManager.cs
// ============================================================
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// จัดการภารกิจรอง "ทำลายสมอโบราณ" (Map4)
/// ✅ ก๊อปโครงจาก CageQuestManager.cs (Map1) ทุกช่อง — setup ใน Inspector เหมือน Map1
///   • นับสมอ 5 จุด แทนกรงเสือ
///   • จบเควส → Panel เขียว + ไฮไลท์เขียวบนป้ายเควส แล้ว fade ป้ายหาย (เหมือน Map1)
///   • ปลดล็อก TempleBlessingEffect (แทน TigerAllyEffect)
/// </summary>
public class AnchorQuestManager : MonoBehaviour
{
    public static AnchorQuestManager Instance;

    [Header("Quest Settings")]
    public int totalAnchors = 5;
    private int destroyedAnchors = 0;

    [Header("UI")]
    public TextMeshProUGUI questCounterText;
    public GameObject questCompletePanel;
    public TextMeshProUGUI questCompleteText;
    public Image panelBackground;

    [Header("Side Quest Popup Complete FX")]
    public GameObject sideQuestPopupRoot;   // ป้ายเควสทั้งก้อน (fade หายตอนจบ)
    public RectTransform highlightPanel;    // Panel สีเขียวหลังชื่อเควส (Pivot X = 0.5)
    public float highlightRevealDuration = 0.4f;

    [Header("Effect Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color successColor = new Color(0f, 0.8f, 0.2f, 0.8f);
    public float textPunchScale = 1.4f;
    public float fadeOutDelay = 2f;
    public float fadeOutDuration = 1f;

    [Header("Temple Blessing (แทน Tiger Ally)")]
    public TempleBlessingEffect templeBlessingEffect;

    [Header("🧪 Debug / Test")]
    public bool enableDebugKey = true;
    [Tooltip("Map1 ใช้ F9 แต่ Map4 F9 ถูก BossMap4Tester ใช้แล้ว → ใช้ F10")]
    public KeyCode debugCompleteKey = KeyCode.F10;

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
            TEST_CompleteAnchorQuestInstantly();
    }

    public void OnAnchorDestroyed()
    {
        if (IsQuestComplete) return;

        destroyedAnchors++;
        destroyedAnchors = Mathf.Clamp(destroyedAnchors, 0, totalAnchors);
        UpdateUI();

        if (destroyedAnchors >= totalAnchors)
            CompleteQuest();
    }

    void CompleteQuest()
    {
        IsQuestComplete = true;
        if (templeBlessingEffect) templeBlessingEffect.EnableTempleBlessing();

        HapticManager.Instance?.PlayPattern(HapticPatternType.PuzzleComplete);

        StartCoroutine(QuestCompleteEffect());
        StartCoroutine(SideQuestPopupCompleteEffect());

        Debug.Log("[Quest] ⚓ ภารกิจทำลายสมอโบราณสำเร็จ!");
    }

    IEnumerator QuestCompleteEffect()
    {
        if (questCompletePanel) questCompletePanel.SetActive(true);
        if (panelBackground) panelBackground.color = successColor;

        if (questCompleteText)
        {
            questCompleteText.text = "Ancient anchors destroyed!";
            yield return StartCoroutine(PunchTextEffect(questCompleteText));
        }

        yield return new WaitForSeconds(fadeOutDelay);
        yield return StartCoroutine(FadeOutPanel());

        if (questCompletePanel) questCompletePanel.SetActive(false);
    }

    /// <summary>
    /// เหมือน Map1:
    /// 1) ซ่อนตัวนับ "0/5"  2) ไฮไลท์เขียวเผยจากกลาง → ซ้าย-ขวา
    /// 3) ค้างไว้ fadeOutDelay  4) Fade ป้ายเควสทั้งก้อนหายไป
    /// </summary>
    IEnumerator SideQuestPopupCompleteEffect()
    {
        if (questCounterText)
            questCounterText.gameObject.SetActive(false);

        if (highlightPanel)
        {
            highlightPanel.gameObject.SetActive(true);
            Vector3 startScale = new Vector3(0f, 1f, 1f);
            Vector3 endScale = Vector3.one;
            highlightPanel.localScale = startScale;

            float elapsed = 0f;
            while (elapsed < highlightRevealDuration)
            {
                elapsed += Time.deltaTime;
                highlightPanel.localScale = Vector3.Lerp(startScale, endScale, elapsed / highlightRevealDuration);
                yield return null;
            }
            highlightPanel.localScale = endScale;
        }

        yield return new WaitForSeconds(fadeOutDelay);

        if (sideQuestPopupRoot)
        {
            CanvasGroup cg = sideQuestPopupRoot.GetComponent<CanvasGroup>();
            if (cg == null) cg = sideQuestPopupRoot.AddComponent<CanvasGroup>();

            cg.alpha = 1f;
            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            cg.alpha = 0f;
            sideQuestPopupRoot.SetActive(false);
        }
    }

    IEnumerator PunchTextEffect(TextMeshProUGUI text)
    {
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 originalScale = Vector3.one;

        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            text.transform.localScale = Vector3.Lerp(originalScale, originalScale * textPunchScale, elapsed / (duration / 2f));
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < duration / 2f)
        {
            elapsed += Time.deltaTime;
            text.transform.localScale = Vector3.Lerp(originalScale * textPunchScale, originalScale, elapsed / (duration / 2f));
            yield return null;
        }

        text.transform.localScale = originalScale;
    }

    IEnumerator FadeOutPanel()
    {
        CanvasGroup cg = questCompletePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = questCompletePanel.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        cg.alpha = 0f;
    }

    [ContextMenu("🧪 TEST - Complete Anchor Quest Instantly")]
    public void TEST_CompleteAnchorQuestInstantly()
    {
        if (IsQuestComplete)
        {
            Debug.Log("[TEST] เควสสมอสำเร็จไปแล้ว ไม่ต้องทำซ้ำ");
            return;
        }

        Debug.Log("[TEST] ⚡ บังคับจบ Side Quest สมอ (ข้ามการตีสมอจริง)");
        while (destroyedAnchors < totalAnchors)
            OnAnchorDestroyed();
    }

    void UpdateUI()
    {
        if (questCounterText)
            questCounterText.text = $"Anchors destroyed : {destroyedAnchors}/{totalAnchors}";
    }
}