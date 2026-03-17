using UnityEngine;
using TMPro;
using System.Collections;

public class PuzzleClearUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        if (clearPanel == null)
            clearPanel = gameObject; // ถ้า script อยู่บน Panel เอง

        canvasGroup = clearPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = clearPanel.AddComponent<CanvasGroup>();
        }

        HideImmediate();
    }

    /// <summary>
    /// แสดงหน้าจอเมื่อ Puzzle สำเร็จ
    /// </summary>
    public void Show()
    {
        StopAllCoroutines();
        StartCoroutine(ShowSequence());
    }

    public void HideImmediate()
    {
        if (clearPanel != null)
            clearPanel.SetActive(false);
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    IEnumerator ShowSequence()
    {
        clearPanel.SetActive(true);
        canvasGroup.alpha = 0f;

        // Fade in
        float t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // รอแสดง
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        clearPanel.SetActive(false);

        Debug.Log("[PuzzleClearUI] ✅ Clear sequence completed");
    }
}