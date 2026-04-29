using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager Instance { get; private set; }

    [Header("Canvas References")]
    public GameObject deathScreenPanel;
    public CanvasGroup canvasGroup;

    [Header("Text References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI subtitleText;
    public TextMeshProUGUI promptText;
    public Image promptImage;

    [Header("Fade Settings")]
    public float fadeInDuration = 1.5f;
    public float fadeOutDuration = 0.8f;
    public float showDelay = 1.0f;

    [Header("Input Settings")]
    public KeyCode respawnKey = KeyCode.E;

    [Header("Respawn Settings")]
    public string respawnSceneName = "";

    // ── Private ──────────────────────────────────────────────
    private bool _isShowing = false;
    private bool _canRespawn = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (!_isShowing || !_canRespawn) return;

        bool pressedController = Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        bool pressedKey = Input.GetKeyDown(respawnKey);

        if (pressedController || pressedKey)
            StartCoroutine(RespawnSequence());
    }

    // ─────────────────────────────────────────────────────────
    public void ShowDeathScreen()
    {
        if (_isShowing) return;
        StartCoroutine(DeathSequence());
    }

    // ─────────────────────────────────────────────────────────
    IEnumerator DeathSequence()
    {
        _isShowing = true;
        _canRespawn = false;

        yield return new WaitForSeconds(showDelay);

        if (deathScreenPanel != null) deathScreenPanel.SetActive(true);
        yield return StartCoroutine(FadeCanvas(0f, 1f, fadeInDuration));

        _canRespawn = true;
        StartCoroutine(BlinkPrompt());
    }

    IEnumerator RespawnSequence()
    {
        _canRespawn = false;

        yield return StartCoroutine(FadeCanvas(1f, 0f, fadeOutDuration));

        // ✅ ปิด Panel และ Reset state ก่อน Respawn
        if (deathScreenPanel != null) deathScreenPanel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        _isShowing = false;   // ✅ Bug 3 Fix: Reset ให้ ShowDeathScreen() ถูกเรียกได้อีกครั้ง

        // ✅ ใช้ SaveManager แทน SceneManager.LoadScene
        if (SaveManager.Instance != null)
            SaveManager.Instance.RespawnAtLastSavePoint();
        else
        {
            string sceneName = string.IsNullOrEmpty(respawnSceneName)
                ? SceneManager.GetActiveScene().name
                : respawnSceneName;
            SceneManager.LoadScene(sceneName);
        }
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
            if (promptText != null) promptText.alpha = 1f;
            if (promptImage != null) promptImage.color = new Color(
                promptImage.color.r, promptImage.color.g, promptImage.color.b, 1f);

            yield return new WaitForSeconds(0.6f);

            if (promptText != null) promptText.alpha = 0.2f;
            if (promptImage != null) promptImage.color = new Color(
                promptImage.color.r, promptImage.color.g, promptImage.color.b, 0.2f);

            yield return new WaitForSeconds(0.4f);
        }
    }
}