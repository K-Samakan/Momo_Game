using System.Collections;
using UnityEngine;

public class DisappearingPlatform : MonoBehaviour
{
    // ─── Timing ───────────────────────────────────────────────────
    [Header("⏱ Timing")]
    public float delayBeforeFade = 0.5f;
    public float fadeOutDuration = 1.0f;
    public float respawnDelay = 3.0f;
    public float fadeInDuration = 1.0f;

    // ─── Warning Blink ────────────────────────────────────────────
    [Header("⚠️ Warning Blink")]
    public bool enableBlink = true;
    public int blinkCount = 3;
    public float blinkInterval = 0.12f;

    // ─── Visual Sound Effect ──────────────────────────────────────
    [Header("🔊 Visual Sound Effect")]
    public bool enableVSE = true;
    public string vseTagStep = "FootStep";   // สีขาว — เสียงเหยียบ
    public string vseTagCrumble = "Warning";    // สีเหลือง — platform กำลังพัง
    public string vseTagRespawn = "Respawn";    // สีเขียว — platform กลับมา
    [Range(0f, 1f)]
    public float vseIntensity = 0.75f;

    // ─── Haptic ───────────────────────────────────────────────────
    [Header("📳 Haptic Feedback")]
    public bool enableHaptic = true;
    public HapticPatternType hapticOnStep = HapticPatternType.Success;
    public HapticPatternType hapticOnFade = HapticPatternType.Boom;

    // ─── Detection ────────────────────────────────────────────────
    [Header("🎯 Detection")]
    public float detectionRadius = 1.0f;
    public LayerMask playerLayer;

    // ─── Components ───────────────────────────────────────────────
    [Header("🔧 Components")]
    public Renderer[] targetRenderers;
    public Collider platformCollider;

    // ─── Private ──────────────────────────────────────────────────
    private bool _isRunning = false;
    private Color[] _originalColors;

    // ══════════════════════════════════════════════════════════════
    #region Unity Lifecycle
    // ══════════════════════════════════════════════════════════════

    void Start()
    {
        // Auto-detect Renderers
        if (targetRenderers == null || targetRenderers.Length == 0)
            targetRenderers = GetComponentsInChildren<Renderer>();

        // Auto-detect Collider
        if (platformCollider == null)
            platformCollider = GetComponent<Collider>();

        // ✅ ต้องไม่เป็น Trigger — ให้ผู้เล่นยืนได้
        if (platformCollider != null)
            platformCollider.isTrigger = false;

        // เก็บสีเดิม
        _originalColors = new Color[targetRenderers.Length];
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] != null)
                _originalColors[i] = targetRenderers[i].material.color;
        }
    }

    void Update()
    {
        if (_isRunning) return;

        // ✅ ตรวจจับ Character Controller ด้วย OverlapSphere
        Collider[] hits = Physics.OverlapSphere(
            transform.position + Vector3.up * 0.5f,
            detectionRadius,
            playerLayer
        );

        if (hits.Length > 0)
        {
            // VSE: เหยียบ
            if (enableVSE && VisualSoundEffectManager.Instance != null)
                VisualSoundEffectManager.Instance.ShowIcon(
                    transform.position, 0.4f, vseTagStep);

            // Haptic: เหยียบ
            if (enableHaptic && HapticManager.Instance != null)
                HapticManager.Instance.PlayPattern(hapticOnStep);

            StartCoroutine(DisappearSequence());
        }
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Public Method (เรียกจากภายนอกได้)
    // ══════════════════════════════════════════════════════════════

    /// <summary>เรียกจาก Player script ได้โดยตรง</summary>
    public void TriggerDisappear()
    {
        if (_isRunning) return;

        if (enableVSE && VisualSoundEffectManager.Instance != null)
            VisualSoundEffectManager.Instance.ShowIcon(
                transform.position, 0.4f, vseTagStep);

        if (enableHaptic && HapticManager.Instance != null)
            HapticManager.Instance.PlayPattern(hapticOnStep);

        StartCoroutine(DisappearSequence());
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Main Sequence
    // ══════════════════════════════════════════════════════════════

    IEnumerator DisappearSequence()
    {
        _isRunning = true;

        // 1. หน่วงก่อน Fade
        yield return new WaitForSeconds(delayBeforeFade);

        // 2. กระพริบเตือน
        if (enableBlink)
            yield return StartCoroutine(BlinkWarning());

        // 3. VSE + Haptic: กำลังหาย
        if (enableVSE && VisualSoundEffectManager.Instance != null)
            VisualSoundEffectManager.Instance.ShowIcon(
                transform.position, 1.0f, vseTagCrumble);

        if (enableHaptic && HapticManager.Instance != null)
            HapticManager.Instance.PlayPattern(hapticOnFade);

        // 4. Fade OUT
        yield return StartCoroutine(FadeAlpha(1f, 0f, fadeOutDuration));

        // 5. ปิด Collider → ผู้เล่นตกได้
        if (platformCollider != null)
            platformCollider.enabled = false;

        // 6. รอ Respawn
        yield return new WaitForSeconds(respawnDelay);

        // 7. Fade IN
        yield return StartCoroutine(FadeAlpha(0f, 1f, fadeInDuration));

        // 8. เปิด Collider กลับมา
        if (platformCollider != null)
            platformCollider.enabled = true;

        // 9. VSE: Respawn
        if (enableVSE && VisualSoundEffectManager.Instance != null)
            VisualSoundEffectManager.Instance.ShowIcon(
                transform.position, 0.6f, vseTagRespawn);

        _isRunning = false;
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Helpers
    // ══════════════════════════════════════════════════════════════

    IEnumerator BlinkWarning()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            SetRenderersAlpha(0.2f);
            yield return new WaitForSeconds(blinkInterval);
            SetRenderersAlpha(1f);
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    IEnumerator FadeAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            SetRenderersAlpha(alpha);
            yield return null;
        }
        SetRenderersAlpha(to);
    }

    void SetRenderersAlpha(float alpha)
    {
        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            Color c = _originalColors[i];
            c.a = alpha;
            targetRenderers[i].material.color = c;
        }
    }

    #endregion

    // ══════════════════════════════════════════════════════════════
    #region Gizmos
    // ══════════════════════════════════════════════════════════════

    void OnDrawGizmos()
    {
        // แสดงพื้นที่ตรวจจับ (สีฟ้า)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(
            transform.position + Vector3.up * 0.5f,
            detectionRadius
        );

        // แสดงขอบ Platform (สีเหลือง)
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    #endregion
}