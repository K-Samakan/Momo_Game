// Assets/Version 1/Script/CC/InGameCCManager.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// In-Gameplay Closed Caption Manager
/// แสดง CC ล่างกลางจอ สูงสุด 2 บรรทัด
/// Priority: Enemy/Projectile (0) > Warning (1) > Cage/SideQuest (2)
/// Format: [ Sound Name - Direction ]
/// - Background ขยายตาม Content Size Fitter
/// - มีแค่ Line1 → Line2 ซ่อน → Line1 อยู่กลาง
/// </summary>
public class InGameCCManager : MonoBehaviour
{
    public static InGameCCManager Instance;

    // ─── References ──────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Transform ของ MOMO (ใช้คำนวณทิศ)")]
    public Transform player;

    [Tooltip("Transform ของ Camera หลัก (ใช้คำนวณทิศ)")]
    public Transform cameraTransform;

    // ─── UI References ───────────────────────────────────────────
    [Header("UI References")]
    [Tooltip("CanvasGroup ของ CC Panel ทั้งหมด (ใช้ Fade)")]
    public CanvasGroup ccPanelGroup;

    [Tooltip("Text บรรทัดที่ 1 — Priority สูง (Enemy, Projectile)")]
    public TextMeshProUGUI line1Text;

    [Tooltip("Text บรรทัดที่ 2 — Priority ต่ำ (Warning, Cage, SideQuest)")]
    public TextMeshProUGUI line2Text;

    [Tooltip("Background Image ของ CC Panel")]
    public Image ccBackground;

    // ─── Display Settings ────────────────────────────────────────
    [Header("Display Settings")]
    [Tooltip("เวลาแสดง CC สำหรับ Enemy / Projectile (วินาที)")]
    public float displayTimeHigh = 1.5f;

    [Tooltip("เวลาแสดง CC สำหรับ Warning (วินาที)")]
    public float displayTimeMid = 2.0f;

    [Tooltip("เวลาแสดง CC สำหรับ Cage / SideQuest (วินาที)")]
    public float displayTimeLow = 2.5f;

    [Tooltip("ระยะเวลา Fade In/Out (วินาที)")]
    public float fadeDuration = 0.15f;

    // ─── Sound Label Map ─────────────────────────────────────────
    //[Header("Sound Labels — ตั้งค่าใน Inspector")]
    [System.Serializable]
    public class SoundLabel
    {
        [Tooltip("Tag เช่น Enemy, Cage, Warning, SideQuest")]
        public string tag;
        [Tooltip("ข้อความที่แสดงใน CC เช่น Monster Sound")]
        public string label;
    }
    public List<SoundLabel> soundLabels = new List<SoundLabel>();

    // ─── Private ─────────────────────────────────────────────────
    // Priority map (น้อย = สำคัญมาก)
    private static readonly Dictionary<string, int> TagPriority = new Dictionary<string, int>
    {
        { "Enemy",      0 },
        { "Projectile", 0 },
        { "Warning",    1 },
        { "Cage",       2 },   // ✅ เพิ่ม Cage
        { "SideQuest",  2 },
    };

    // ข้อมูล CC แต่ละ slot
    private class CCEntry
    {
        public string text;
        public float timer;
        public int priority;
    }

    private CCEntry _slot1 = null; // priority สูง (0)
    private CCEntry _slot2 = null; // priority ต่ำ (1-2)

    private Coroutine _fadeCoroutine;
    private bool _isPanelVisible = false;

    // ════════════════════════════════════════════════════════════
    #region Unity Lifecycle
    // ════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (ccPanelGroup != null) ccPanelGroup.alpha = 0f;
        if (line1Text != null) line1Text.text = "";
        if (line2Text != null)
        {
            line2Text.text = "";
            line2Text.gameObject.SetActive(false); // ซ่อน Line2 ตอนเริ่ม
        }
    }

    void Update()
    {
        UpdateSlots();
        UpdateUI();
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Public API
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// เรียกจาก SoundEmitter, CageController หรือ script อื่น
    /// เมื่อมีเสียงเกิดขึ้นจริง
    /// </summary>
    public void ShowCC(Vector3 sourceWorldPos, string tag)
    {
        if (player == null || cameraTransform == null) return;

        int priority = GetPriority(tag);
        string label = GetLabel(tag);
        string direction = GetDirection(sourceWorldPos);
        string fullText = $"[ {label} - {direction} ]";
        float displayTime = GetDisplayTime(priority);

        CCEntry entry = new CCEntry
        {
            text = fullText,
            timer = displayTime,
            priority = priority
        };

        if (priority <= 0)
        {
            // Enemy / Projectile → Slot 1 เสมอ
            _slot1 = entry;
        }
        else
        {
            // Warning / Cage / SideQuest → Slot 2
            // priority สูงกว่า (ตัวเลขน้อยกว่า) แย่งที่ได้
            if (_slot2 == null || priority <= _slot2.priority)
                _slot2 = entry;
        }
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Update Logic
    // ════════════════════════════════════════════════════════════

    void UpdateSlots()
    {
        if (_slot1 != null)
        {
            _slot1.timer -= Time.deltaTime;
            if (_slot1.timer <= 0f) _slot1 = null;
        }

        if (_slot2 != null)
        {
            _slot2.timer -= Time.deltaTime;
            if (_slot2.timer <= 0f) _slot2 = null;
        }
    }

    void UpdateUI()
    {
        bool hasBoth = (_slot1 != null && _slot2 != null);
        bool hasAny = (_slot1 != null || _slot2 != null);

        // ── Line1 ────────────────────────────────────────────────
        if (line1Text != null)
        {
            if (_slot1 != null)
                line1Text.text = _slot1.text;           // มี slot1 → แสดง slot1
            else if (_slot2 != null)
                line1Text.text = _slot2.text;           // มีแค่ slot2 → ขึ้น Line1 แทน
            else
                line1Text.text = "";
        }

        // ── Line2 — แสดงเฉพาะตอนมีทั้ง 2 slot ──────────────────
        if (line2Text != null)
        {
            bool showLine2 = hasBoth;
            line2Text.gameObject.SetActive(showLine2);  // ซ่อน/แสดง Line2
            line2Text.text = showLine2 ? _slot2.text : "";
        }

        // ── Fade Panel ──────────────────────────────────────────
        if (hasAny && !_isPanelVisible)
        {
            _isPanelVisible = true;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadePanel(1f));
        }
        else if (!hasAny && _isPanelVisible)
        {
            _isPanelVisible = false;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadePanel(0f));
        }
    }

    IEnumerator FadePanel(float targetAlpha)
    {
        if (ccPanelGroup == null) yield break;

        float startAlpha = ccPanelGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            ccPanelGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        ccPanelGroup.alpha = targetAlpha;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Helpers
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// คำนวณทิศทาง 4 แบบ: Front / Behind / Left / Right
    /// อิงจาก Camera Forward
    /// </summary>
    string GetDirection(Vector3 sourceWorldPos)
    {
        Vector3 dir = sourceWorldPos - player.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
        if (flatDir == Vector3.zero) return "Front";

        Vector3 localDir = cameraTransform.InverseTransformDirection(flatDir);
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        if (angle > -45f && angle <= 45f) return "Front";
        if (angle > 45f && angle <= 135f) return "Right";
        if (angle > 135f || angle <= -135f) return "Behind";
        return "Left";
    }

    string GetLabel(string tag)
    {
        SoundLabel sl = soundLabels.Find(s => s.tag == tag);
        if (sl != null) return sl.label;

        // Fallback
        switch (tag)
        {
            case "Enemy": return "Monster Sound";
            case "Projectile": return "Projectile";
            case "Warning": return "Warning Sound";
            case "Cage": return "Tiger Roar";
            case "SideQuest": return "Ambient Sound";
            default: return tag;
        }
    }

    int GetPriority(string tag)
        => TagPriority.TryGetValue(tag, out int p) ? p : 99;

    float GetDisplayTime(int priority)
    {
        if (priority <= 0) return displayTimeHigh;
        if (priority == 1) return displayTimeMid;
        return displayTimeLow;
    }

    #endregion
}