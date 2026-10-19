// ============================================================
// FILE: RockQuestManager.cs
// PATH: Assets/Version 1/Script/Map2/SideQuest2/RockQuestManager.cs
// ============================================================
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// จัดการภารกิจรอง "ฟื้นฟูโอเอซิส" (Map2) — ใช้ UI flow เดียวกับ CageQuestManager.cs (Map1)
///   • นับหิน 5 ก้อน
///   • ตอนสำเร็จ → ไฮไลท์เขียวบนป้ายเควส แล้ว fade ป้ายหายไปทั้งก้อน (เหมือน Map1)
///   • ปลดล็อก OasisShieldEffect (โล่ตอนสู้บอส HP <= 2)
/// </summary>
public class RockQuestManager : MonoBehaviour
{
    public static RockQuestManager Instance;

    [Header("Quest Settings")]
    public int totalRocks = 5;
    private int destroyedRocks = 0;

    [Header("UI")]
    [Tooltip("Text ตัวนับ (บรรทัดเล็กใต้ชื่อเควส)")]
    public TextMeshProUGUI questCounterText;
    [Tooltip("ข้อความหน้าตัวเลข → \"Rocks destroyed : 0/5\"")]
    public string counterLabel = "Rocks destroyed";

    [Tooltip("(ไม่บังคับ) Panel แจ้งเตือนแยก — เว้นว่างได้ถ้าใช้แค่ไฮไลท์บนป้ายเควส")]
    public GameObject questCompletePanel;
    public TextMeshProUGUI questCompleteText;
    public Image panelBackground;

    [Header("Side Quest Popup Complete FX")]
    [Tooltip("ป้ายเควสทั้งก้อน — จะ fade หายไปตอนจบ (เช่น SideQuest_Root)")]
    public GameObject sideQuestPopupRoot;
    [Tooltip("Panel ไฮไลท์สีเขียวหลังชื่อเควส (Pivot X = 0.5, ปิดไว้ก่อนใน Scene)")]
    public RectTransform highlightPanel;
    public float highlightRevealDuration = 0.2f;

    [Header("Effect Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color successColor = new Color(0f, 0.8f, 0.2f, 0.8f); // เขียว (เหมือน Map1)
    public float textPunchScale = 1.4f;
    public float fadeOutDelay = 2f;
    public float fadeOutDuration = 1f;

    [Header("Oasis Shield (Boss Reward)")]
    public OasisShieldEffect oasisShieldEffect;

    [Header("🧪 Debug / Test")]
    public bool enableDebugKey = true;
    public KeyCode debugCompleteKey = KeyCode.F9;

    public bool IsQuestComplete { get; private set; } = false;

    void Awake()
    {
        Instance = this;
        if (questCompletePanel) questCompletePanel.SetActive(false);
        if (highlightPanel) highlightPanel.gameObject.SetActive(false); // ซ่อนไฮไลท์ไว้ก่อน
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        if (enableDebugKey && Input.GetKeyDown(debugCompleteKey))
            TEST_CompleteRockQuestInstantly();
    }

    public void OnRockDestroyed()
    {
        if (IsQuestComplete) return;

        destroyedRocks = Mathf.Clamp(destroyedRocks + 1, 0, totalRocks);
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
        StartCoroutine(SideQuestPopupCompleteEffect());

        Debug.Log("[Quest] ภารกิจฟื้นฟูโอเอซิสสำเร็จ!");
    }

    // ── Panel แจ้งเตือนแยก (ทำงานเฉพาะถ้าผูกไว้) ──
    IEnumerator QuestCompleteEffect()
    {
        if (!questCompletePanel) yield break;

        questCompletePanel.SetActive(true);
        if (panelBackground) panelBackground.color = successColor;

        if (questCompleteText)
        {
            questCompleteText.text = "Oasis restored!";
            yield return StartCoroutine(PunchTextEffect(questCompleteText));
        }

        yield return new WaitForSeconds(fadeOutDelay);
        yield return StartCoroutine(FadeOut(questCompletePanel));
        questCompletePanel.SetActive(false);
    }

    /// <summary>
    /// เหมือน Map1:
    /// 1) ซ่อนตัวนับ "0/5"
    /// 2) ไฮไลท์เขียวเผยจากกลาง → ซ้าย-ขวา
    /// 3) ค้างไว้ fadeOutDelay วิ
    /// 4) Fade ป้ายเควสทั้งก้อนหายไป
    /// </summary>
    IEnumerator SideQuestPopupCompleteEffect()
    {
        if (questCounterText) questCounterText.gameObject.SetActive(false);

        if (highlightPanel)
        {
            var img = highlightPanel.GetComponent<Image>();
            if (img) img.color = successColor;

            highlightPanel.gameObject.SetActive(true);
            Vector3 startScale = new Vector3(0f, 1f, 1f);
            highlightPanel.localScale = startScale;

            float elapsed = 0f;
            while (elapsed < highlightRevealDuration)
            {
                elapsed += Time.deltaTime;
                highlightPanel.localScale = Vector3.Lerp(startScale, Vector3.one, elapsed / highlightRevealDuration);
                yield return null;
            }
            highlightPanel.localScale = Vector3.one;
        }

        yield return new WaitForSeconds(fadeOutDelay);

        if (sideQuestPopupRoot)
        {
            yield return StartCoroutine(FadeOut(sideQuestPopupRoot));
            sideQuestPopupRoot.SetActive(false);
        }
    }

    IEnumerator PunchTextEffect(TextMeshProUGUI text)
    {
        float half = 0.15f, elapsed = 0f;
        Vector3 s = Vector3.one;

        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            text.transform.localScale = Vector3.Lerp(s, s * textPunchScale, elapsed / half);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            text.transform.localScale = Vector3.Lerp(s * textPunchScale, s, elapsed / half);
            yield return null;
        }
        text.transform.localScale = s;
    }

    IEnumerator FadeOut(GameObject target)
    {
        CanvasGroup cg = target.GetComponent<CanvasGroup>();
        if (cg == null) cg = target.AddComponent<CanvasGroup>();

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

    [ContextMenu("🧪 TEST - Complete Rock Quest Instantly")]
    public void TEST_CompleteRockQuestInstantly()
    {
        if (IsQuestComplete)
        {
            Debug.Log("[TEST] เควสฟื้นฟูโอเอซิสสำเร็จไปแล้ว");
            return;
        }

        Debug.Log("[TEST] ⚡ บังคับจบ Side Quest โอเอซิส");
        while (!IsQuestComplete) OnRockDestroyed();
    }

    void UpdateUI()
    {
        if (questCounterText)
            questCounterText.text = $"{counterLabel} : {destroyedRocks}/{totalRocks}";
    }
}