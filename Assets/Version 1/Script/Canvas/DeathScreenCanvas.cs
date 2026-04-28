using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Death Screen Manager
/// - แสดง Canvas เมื่อผู้เล่นตาย
/// - กดปุ่ม ○ (Button East บน PS5) หรือ E (Keyboard) เพื่อเล่นใหม่
/// - รองรับ Save Point ในอนาคต
/// </summary>
public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager Instance { get; private set; }

    // ────────────────────────────────────────
    // Inspector
    // ────────────────────────────────────────

    [Header("Canvas References")]
    [Tooltip("Panel ทั้งหมดของ Death Screen")]
    public GameObject deathScreenPanel;

    [Tooltip("CanvasGroup สำหรับ Fade")]
    public CanvasGroup canvasGroup;

    [Header("Text References")]
    [Tooltip("ข้อความหลัก เช่น 死")]
    public TextMeshProUGUI titleText;

    [Tooltip("ข้อความรอง เช่น YOU FAILED")]
    public TextMeshProUGUI subtitleText;

    [Tooltip("ข้อความบอกปุ่มกด")]
    public TextMeshProUGUI promptText;

    [Tooltip("รูปปุ่ม ○ ที่ Blink พร้อมกับ Text")]
    public Image promptImage;

    [Header("Fade Settings")]
    public float fadeInDuration  = 1.5f;
    public float fadeOutDuration = 0.8f;
    [Tooltip("หน่วงก่อนแสดง Death Screen (วินาที)")]
    public float showDelay = 1.0f;

    [Header("Input Settings")]
    [Tooltip("KeyCode สำรอง (Keyboard)")]
    public KeyCode respawnKey = KeyCode.E;

    [Header("Respawn Settings")]
    [Tooltip("ชื่อ Scene ที่จะ Reload (ปล่อยว่าง = Reload Scene ปัจจุบัน)")]
    public string respawnSceneName = "";

    // ────────────────────────────────────────
    // Private
    // ────────────────────────────────────────

    private bool _isShowing  = false;
    private bool _canRespawn = false;

    // ────────────────────────────────────────
    // Unity
    // ────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(false);

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (!_isShowing || !_canRespawn) return;

        // ── PS5: Button East = ○ (ขวา) ──
        bool pressedController = false;
        if (Gamepad.current != null)
            pressedController = Gamepad.current.buttonEast.wasPressedThisFrame;

        // ── Keyboard fallback ──
        bool pressedKey = Input.GetKeyDown(respawnKey);

        if (pressedController || pressedKey)
            StartCoroutine(RespawnSequence());
    }

    // ────────────────────────────────────────
    // Public API
    // ────────────────────────────────────────

    /// <summary>
    /// เรียกจาก PlayerHealth.Die()
    /// </summary>
    public void ShowDeathScreen()
    {
        if (_isShowing) return;
        StartCoroutine(DeathSequence());
    }

    // ────────────────────────────────────────
    // Sequences
    // ────────────────────────────────────────

    IEnumerator DeathSequence()
    {
        _isShowing  = true;
        _canRespawn = false;

        // หน่วงให้ Animation ตายเล่นก่อน
        yield return new WaitForSeconds(showDelay);

        // เปิด Panel
        if (deathScreenPanel != null)
            deathScreenPanel.SetActive(true);

        // Fade In
        yield return StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));

        // เปิดให้กด Respawn
        _canRespawn = true;

        // Blink prompt
        StartCoroutine(BlinkPrompt());
    }

    IEnumerator RespawnSequence()
    {
        _canRespawn = false;

        // Fade Out
        yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutDuration));

        // Reload Scene
        string sceneName = string.IsNullOrEmpty(respawnSceneName)
            ? SceneManager.GetActiveScene().name
            : respawnSceneName;

        SceneManager.LoadScene(sceneName);
    }

    IEnumerator FadeCanvas(float from, float to, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        canvasGroup.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = to;
    }

    IEnumerator BlinkPrompt()
    {
        while (_isShowing && _canRespawn)
        {
            // สว่าง
            if (promptText  != null) promptText.alpha = 1f;
            if (promptImage != null)
                promptImage.color = new Color(
                    promptImage.color.r,
                    promptImage.color.g,
                    promptImage.color.b, 1f);

            yield return new WaitForSeconds(0.6f);

            // หรี่
            if (promptText  != null) promptText.alpha = 0.2f;
            if (promptImage != null)
                promptImage.color = new Color(
                    promptImage.color.r,
                    promptImage.color.g,
                    promptImage.color.b, 0.2f);

            yield return new WaitForSeconds(0.4f);
        }
    }
}