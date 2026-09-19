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

    [Header("Side Quest Popup Complete FX")]
    public GameObject sideQuestPopupRoot;   // ตัว SideQuestPopup ทั้งก้อน (จะ fade หายไปตอนจบ)
    public RectTransform highlightPanel;    // Panel สีเขียวหลัง "Save the Tigers" (Pivot X ต้องเป็น 0.5)
    public float highlightRevealDuration = 0.4f;

    [Header("Effect Settings")]
    public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    public Color successColor = new Color(0f, 0.8f, 0.2f, 0.8f);
    public float textPunchScale = 1.4f;
    public float fadeOutDelay = 2f;
    public float fadeOutDuration = 1f;

    [Header("Tiger Ally")]
    public TigerAllyEffect tigerAllyEffect;

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
            TEST_CompleteCageQuestInstantly();
        }
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
        StartCoroutine(SideQuestPopupCompleteEffect());

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
            questCompleteText.text = "Tiger rescued successfully!";
            yield return StartCoroutine(PunchTextEffect(questCompleteText));
        }

        // รอก่อน Fade Out
        yield return new WaitForSeconds(fadeOutDelay);

        // Fade Out ทั้ง Panel
        yield return StartCoroutine(FadeOutPanel());

        // ซ่อน Panel
        if (questCompletePanel) questCompletePanel.SetActive(false);
    }

    /// <summary>
    /// เอฟเฟกต์ตอน Side Quest Popup (ป้าย "Save the Tigers" / 0-5 บน HUD) จบภารกิจ:
    /// 1) ซ่อน subtitle "0/5" ทันที
    /// 2) ไฮไลท์เขียวเผยออกจากกลาง -> ซ้าย-ขวา (scale.x 0 -> 1, ต้องตั้ง Pivot X = 0.5)
    /// 3) ค้างให้เห็นไฮไลท์ (ใช้ fadeOutDelay เดิม)
    /// 4) Fade ทั้ง SideQuestPopup หายไปทั้งก้อน (ใช้ fadeOutDuration เดิม)
    /// </summary>
    IEnumerator SideQuestPopupCompleteEffect()
    {
        // 1) ซ่อน subtitle "0/5"
        if (questCounterText)
            questCounterText.gameObject.SetActive(false);

        // 2) Reveal ไฮไลท์เขียวจากกลาง -> ซ้าย-ขวา
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
                float t = elapsed / highlightRevealDuration;
                highlightPanel.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            highlightPanel.localScale = endScale;
        }

        // 3) ค้างให้เห็นไฮไลท์ (ใช้ค่าเดิม fadeOutDelay)
        yield return new WaitForSeconds(fadeOutDelay);

        // 4) Fade ทั้ง SideQuestPopup หายไปทั้งก้อน (ใช้ค่าเดิม fadeOutDuration)
        if (sideQuestPopupRoot)
        {
            CanvasGroup popupCanvasGroup = sideQuestPopupRoot.GetComponent<CanvasGroup>();
            if (popupCanvasGroup == null)
                popupCanvasGroup = sideQuestPopupRoot.AddComponent<CanvasGroup>();

            popupCanvasGroup.alpha = 1f;

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                popupCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
                yield return null;
            }
            popupCanvasGroup.alpha = 0f;
            sideQuestPopupRoot.SetActive(false);
        }
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

    /// <summary>
    /// 🧪 TEST ONLY — จบ Side Quest กรงเสือทันที โดยไม่ต้องทุบกรงจริง
    /// เรียก flow เดิมทุกขั้นตอน (UI, Haptic, TigerAlly) เหมือนเล่นจริง
    /// เรียกได้ 2 ทาง: คลิกขวา component นี้ตอน Play Mode > เลือกเมนูนี้
    ///                  หรือกดปุ่ม debugCompleteKey ตอน Runtime
    /// </summary>
    [ContextMenu("🧪 TEST - Complete Cage Quest Instantly")]
    public void TEST_CompleteCageQuestInstantly()
    {
        if (IsQuestComplete)
        {
            Debug.Log("[TEST] เควสกรงเสือสำเร็จไปแล้ว ไม่ต้องทำซ้ำ");
            return;
        }

        Debug.Log("[TEST] ⚡ บังคับจบ Side Quest กรงเสือ (ข้ามการทุบกรงจริง)");

        // เรียกผ่าน path เดิมจนครบ เพื่อให้ UI/Haptic/TigerAlly ทำงานเหมือนของจริง
        while (freedCages < totalCages)
        {
            OnCageFreed();
        }
    }

    void UpdateUI()
    {
        if (questCounterText)
            questCounterText.text = $"Tigers rescued : {freedCages}/{totalCages}";
    }
}