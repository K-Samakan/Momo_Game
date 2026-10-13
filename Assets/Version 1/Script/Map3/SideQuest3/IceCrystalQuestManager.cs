// ============================================================
// FILE: IceCrystalQuestManager.cs
// PATH: Assets/Version 1/Script/Map3/SideQuest3/IceCrystalQuestManager.cs
// ============================================================
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// จัดการภารกิจรอง "ช่วยวาฬโบราณ" (Map3) — ใช้ UI flow เดียวกับ CageQuestManager.cs (Map1)
///   • นับผลึกน้ำแข็ง 5 จุด
///   • ตอนสำเร็จ → ไฮไลท์เขียวบนป้ายเควส แล้ว fade ป้ายหายไปทั้งก้อน (เหมือน Map1)
///   • ละลายน้ำแข็งที่ขังวาฬ + ปลดล็อก WhaleGuardianEffect (ATK+1, กันช้า, โล่)
/// </summary>
public class IceCrystalQuestManager : MonoBehaviour
{
    public static IceCrystalQuestManager Instance;

    [Header("Quest Settings")]
    public int totalCrystals = 5;
    private int destroyedCrystals = 0;

    [Header("UI")]
    [Tooltip("Text ตัวนับ (บรรทัดเล็กใต้ชื่อเควส) — เช่น SideQuestPopup")]
    public TextMeshProUGUI questCounterText;
    [Tooltip("ข้อความหน้าตัวเลข → \"Crystals melted : 0/5\"")]
    public string counterLabel = "Crystals melted";

    [Tooltip("(ไม่บังคับ) Panel แจ้งเตือนแยก — เว้นว่างได้ถ้าใช้แค่ไฮไลท์บนป้ายเควส")]
    public GameObject questCompletePanel;
    public TextMeshProUGUI questCompleteText;
    public Image panelBackground;

    [Header("Side Quest Popup Complete FX (เหมือน Map1)")]
    [Tooltip("ป้ายเควสทั้งก้อน — จะ fade หายไปตอนจบ (เช่น SideQuest_Root)")]
    public GameObject sideQuestPopupRoot;
    [Tooltip("Panel ไฮไลท์สีเขียวหลังชื่อเควส (Pivot X = 0.5, ปิดไว้ก่อนใน Scene)")]
    public RectTransform highlightPanel;
    public float highlightRevealDuration = 0.4f;

    [Header("Effect Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color successColor = new Color(0f, 0.8f, 0.2f, 0.8f); // เขียว (เหมือน Map1)
    public float textPunchScale = 1.4f;
    public float fadeOutDelay = 2f;
    public float fadeOutDuration = 1f;

    [Header("Whale (Visual ตอนเควสสำเร็จ)")]
    [Tooltip("ก้อนน้ำแข็งที่ขังวาฬ — จะถูกปิดตอนเควสสำเร็จ")]
    public GameObject whaleIcePrison;
    public GameObject whaleFreedVFX;
    public Animator whaleAnimator;
    public string whaleFreedTrigger = "Freed";

    [Header("Whale Guardian (Boss Reward)")]
    public WhaleGuardianEffect whaleGuardianEffect;

    [Header("🧪 Debug / Test")]
    public bool enableDebugKey = true;
    public KeyCode debugCompleteKey = KeyCode.F9;

    public bool IsQuestComplete { get; private set; } = false;

    void Awake()
    {
        Instance = this;
        if (questCompletePanel) questCompletePanel.SetActive(false);
        if (highlightPanel) highlightPanel.gameObject.SetActive(false);
    }

    void Start() => UpdateUI();

    void Update()
    {
        if (enableDebugKey && Input.GetKeyDown(debugCompleteKey))
            TEST_CompleteCrystalQuestInstantly();
    }

    public void OnCrystalDestroyed()
    {
        if (IsQuestComplete) return;

        destroyedCrystals = Mathf.Clamp(destroyedCrystals + 1, 0, totalCrystals);
        UpdateUI();

        if (destroyedCrystals >= totalCrystals)
            CompleteQuest();
    }

    void CompleteQuest()
    {
        IsQuestComplete = true;

        // 🐋 ปลดปล่อยวาฬ
        if (whaleIcePrison) whaleIcePrison.SetActive(false);
        if (whaleFreedVFX && whaleIcePrison)
            Instantiate(whaleFreedVFX, whaleIcePrison.transform.position, Quaternion.identity);
        if (whaleAnimator) whaleAnimator.SetTrigger(whaleFreedTrigger);

        // ✅ ปลดล็อกรางวัล — ยังไม่ทำงานจนกว่าจะเข้าห้องบอส
        if (whaleGuardianEffect) whaleGuardianEffect.EnableGuardian();

        HapticManager.Instance?.PlayPattern(HapticPatternType.PuzzleComplete);

        StartCoroutine(QuestCompleteEffect());
        StartCoroutine(SideQuestPopupCompleteEffect());

        Debug.Log("[Quest] 🐋 ภารกิจช่วยวาฬโบราณสำเร็จ!");
    }

    // ── Panel แจ้งเตือนแยก (ถ้าผูกไว้) ──
    IEnumerator QuestCompleteEffect()
    {
        if (!questCompletePanel) yield break;

        questCompletePanel.SetActive(true);
        if (panelBackground) panelBackground.color = successColor;

        if (questCompleteText)
        {
            questCompleteText.text = "Ancient Whale freed!";
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

        float elapsed = 0f;
        cg.alpha = 1f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        cg.alpha = 0f;
    }

    /// <summary>🧪 TEST ONLY — จบเควสทันที ใช้ flow เดิมทุกขั้น</summary>
    [ContextMenu("🧪 TEST - Complete Ice Crystal Quest Instantly")]
    public void TEST_CompleteCrystalQuestInstantly()
    {
        if (IsQuestComplete)
        {
            Debug.Log("[TEST] เควสช่วยวาฬสำเร็จไปแล้ว");
            return;
        }
        Debug.Log("[TEST] ⚡ บังคับจบ Side Quest วาฬโบราณ");
        while (!IsQuestComplete) OnCrystalDestroyed();
    }

    void UpdateUI()
    {
        if (questCounterText)
            questCounterText.text = $"{counterLabel} : {destroyedCrystals}/{totalCrystals}";
    }
}