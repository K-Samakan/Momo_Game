using System.Collections;
using UnityEngine;
using TMPro;

public class TigerDamageNumber : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro damageText;

    [Header("Animation")]
    public float riseSpeed = 2.5f;
    public float lifetime = 0.9f;
    public AnimationCurve scaleCurve;

    private void Start()
    {
        StartCoroutine(Animate());
    }

    public void Setup(int damage)
    {
        damageText.text = $"+{damage}";
        damageText.color = new Color(1f, 0.55f, 0.05f);
        damageText.fontSize = 5f;
    }

    private IEnumerator Animate()
    {
        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 startScale = Vector3.one * 1.4f;

        while (t < lifetime)
        {
            t += Time.deltaTime;
            float progress = t / lifetime;

            transform.position = startPos + Vector3.up * (riseSpeed * t);
            float scaleVal = scaleCurve.Evaluate(progress);
            transform.localScale = startScale * scaleVal;

            if (progress > 0.6f)
            {
                float alpha = 1f - ((progress - 0.6f) / 0.4f);
                Color c = damageText.color;
                c.a = alpha;
                damageText.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}