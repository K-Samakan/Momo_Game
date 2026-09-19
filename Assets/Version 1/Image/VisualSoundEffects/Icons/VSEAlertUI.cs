using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class VSEAlertUI : MonoBehaviour
{
    public static VSEAlertUI Instance;

    [System.Serializable]
    public class AlertRow
    {
        public string tag;
        public RectTransform panel;      // Mon_panel / Shoot_panel / Side_panel / Plat_panel (Pivot X ต้องเป็น 0)
        public Image icon;
        public TextMeshProUGUI description;
    }

    [Header("Rows (Mon / Shoot / Quest / Plant)")]
    public List<AlertRow> rows = new List<AlertRow>();

    [Header("Fade Settings")]
    [Range(0f, 1f)] public float dimAlpha = 0.35f;
    public float fadeInDuration = 0.25f;
    public float fadeOutDuration = 0.4f;

    private Dictionary<string, Coroutine> activeRoutines = new Dictionary<string, Coroutine>();
    private Dictionary<string, int> activeCount = new Dictionary<string, int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        foreach (var row in rows)
        {
            SetIconAlpha(row.icon, dimAlpha);
            SetTextAlpha(row.description, 0f);
            SetPanelScaleX(row.panel, 0f); // panel เริ่มยุบสนิท มองไม่เห็น
        }
    }

    public void BeginAlert(string tag)
    {
        int newCount = activeCount.TryGetValue(tag, out int c) ? c + 1 : 1;
        activeCount[tag] = newCount;

        if (newCount == 1)  // <-- เพิ่มเงื่อนไขนี้: fade เฉพาะตอนเริ่ม active ครั้งแรก
            Play(tag, dimAlpha, 1f, 0f, 1f, 0f, 1f, fadeInDuration);
    }

    public void EndAlert(string tag)
    {
        if (!activeCount.ContainsKey(tag)) return;
        activeCount[tag] = Mathf.Max(0, activeCount[tag] - 1);
        if (activeCount[tag] > 0) return;

        Play(tag, 1f, dimAlpha, 1f, 0f, 1f, 0f, fadeOutDuration);
    }

    void Play(string tag, float iconFrom, float iconTo, float textFrom, float textTo,
        float panelFrom, float panelTo, float duration)
    {
        AlertRow row = rows.Find(r => r.tag == tag);
        if (row == null) return;

        if (activeRoutines.TryGetValue(tag, out Coroutine existing) && existing != null)
            StopCoroutine(existing);

        activeRoutines[tag] = StartCoroutine(
            FadeRoutine(row, iconFrom, iconTo, textFrom, textTo, panelFrom, panelTo, duration));
    }

    IEnumerator FadeRoutine(AlertRow row, float iconFrom, float iconTo,
        float textFrom, float textTo, float panelFrom, float panelTo, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            SetIconAlpha(row.icon, Mathf.Lerp(iconFrom, iconTo, t));
            SetTextAlpha(row.description, Mathf.Lerp(textFrom, textTo, t));
            SetPanelScaleX(row.panel, Mathf.Lerp(panelFrom, panelTo, t));
            yield return null;
        }
        SetIconAlpha(row.icon, iconTo);
        SetTextAlpha(row.description, textTo);
        SetPanelScaleX(row.panel, panelTo);
    }

    void SetIconAlpha(Image icon, float alpha)
    {
        if (!icon) return;
        Color c = icon.color; c.a = alpha; icon.color = c;
    }

    void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (!text) return;
        Color c = text.color; c.a = alpha; text.color = c;
    }

    void SetPanelScaleX(RectTransform panel, float scaleX)
    {
        if (!panel) return;
        Vector3 s = panel.localScale;
        s.x = scaleX;
        panel.localScale = s;
    }
}