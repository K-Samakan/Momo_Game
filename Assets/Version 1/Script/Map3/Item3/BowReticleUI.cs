// ============================================================
// FILE: BowReticleUI.cs
// PATH: Assets/Version 1/Script/Map3/Item3/BowReticleUI.cs
// Crosshair กลางจอตอนเล็งธนู — เปลี่ยนสี/ไอคอนเมื่อชาร์จเต็ม (แบบ Genshin)
// ✅ แก้ bug: ต้อง SetActive(true) ตัวเองก่อน ไม่งั้น Update() จะไม่รัน (object inactive)
// ============================================================
using UnityEngine;
using UnityEngine.UI;

public class BowReticleUI : MonoBehaviour
{
    [Header("Root")]
    public CanvasGroup canvasGroup;   // ใส่ CanvasGroup ที่ตัว Reticle เพื่อ fade in/out

    [Header("Sprites")]
    public Image reticleImage;
    public Sprite normalReticle;      // จุดเล็งปกติ (วงกลมบาง)
    public Sprite chargedReticle;     // จุดเล็งตอนชาร์จเต็ม (วงไฟแดง)

    [Header("Fade")]
    public float fadeSpeed = 8f;

    private bool _visible = false;

    void Awake()
    {
        // ✅ เปิด GameObject ไว้ตลอด (ไม่ปิดแล้ว) แต่ alpha = 0 ทำให้มองไม่เห็น
        //    วิธีนี้ทำให้ Update() รันตลอด สามารถ fade ได้ปกติ
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        if (reticleImage != null && normalReticle != null) reticleImage.sprite = normalReticle;
    }

    void Update()
    {
        if (canvasGroup == null) return;
        float target = _visible ? 1f : 0f;
        canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, target, fadeSpeed * Time.deltaTime);
    }

    public void Show()
    {
        // ✅ กันไว้เผื่อใครยัง SetActive(false) ไว้อยู่ — เปิดให้แน่ใจ
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        _visible = true;
        if (reticleImage != null && normalReticle != null) reticleImage.sprite = normalReticle;
    }

    public void Hide()
    {
        _visible = false;
    }

    public void SetChargedState(bool charged)
    {
        if (reticleImage == null) return;
        reticleImage.sprite = charged ? chargedReticle : normalReticle;
    }
}