using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TigerAssistPopupUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform popupRect;
    public CanvasGroup canvasGroup;
    public Image tigerPortrait;
    public TextMeshProUGUI assistText;

    [Header("Position")]
    public Vector2 hiddenPosition = new Vector2(400f, 60f);
    public Vector2 visiblePosition = new Vector2(-20f, 60f);

    [Header("Timing")]
    public float slideInDuration = 0.18f;
    public float holdDuration = 1.2f;
    public float fadeOutDuration = 0.3f;

    private Coroutine _currentAnim;

    private void Awake()
    {
        popupRect.anchoredPosition = hiddenPosition;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void ShowPopup()
    {
        if (_currentAnim != null)
            StopCoroutine(_currentAnim);

        gameObject.SetActive(true);
        _currentAnim = StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float t = 0f;
        canvasGroup.alpha = 1f;

        while (t < slideInDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.SmoothStep(0f, 1f, t / slideInDuration);
            popupRect.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, p);
            yield return null;
        }
        popupRect.anchoredPosition = visiblePosition;

        yield return new WaitForSeconds(holdDuration);

        t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = 1f - (t / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        popupRect.anchoredPosition = hiddenPosition;
        gameObject.SetActive(false);
    }
}