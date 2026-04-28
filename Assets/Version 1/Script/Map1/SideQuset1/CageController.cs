using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using ithappy.Animals_FREE;

public class CageController : MonoBehaviour
{
    [Header("Cage Stats")]
    public int cageHP = 3;
    private int currentHP;

    [Header("Tiger In Cage")]
    public GameObject tigerInCage;           // ← ลาก Tiger_001 (Child) มาใส่
    public float tigerRunDuration = 3f;
    public float tigerFadeOutDuration = 1f;

    [Header("Tiger Audio")]
    public AudioSource tigerAudioSource;
    public AudioClip tigerRoarClip;

    [Header("VSE")]
    public float vseIntensity = 0.8f;
    public float vseInterval = 2f;

    [Header("UI")]
    public Slider hpBar;
    public GameObject hpBarObject;

    [Header("Effects")]
    public Animator cageAnimator;
    public GameObject breakVFX;
    public GameObject hitVFX;

    private bool isBroken = false;
    private Coroutine vseCoroutine;

    void Start()
    {
        currentHP = cageHP;
        if (hpBarObject) hpBarObject.SetActive(false);

        // เริ่ม VSE Loop สีเหลือง
        vseCoroutine = StartCoroutine(VSELoop());

        // เล่นเสียงเสือร้อง
        if (tigerAudioSource && tigerRoarClip)
        {
            tigerAudioSource.clip = tigerRoarClip;
            tigerAudioSource.loop = true;
            tigerAudioSource.Play();
        }
    }

    IEnumerator VSELoop()
    {
        while (!isBroken)
        {
            VisualSoundEffectManager.Instance?.ShowIcon(
                transform.position, vseIntensity, "Event");
            yield return new WaitForSeconds(vseInterval);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isBroken) return;

        currentHP -= dmg;
        currentHP = Mathf.Clamp(currentHP, 0, cageHP);

        if (hpBarObject) hpBarObject.SetActive(true);
        if (hpBar) hpBar.value = (float)currentHP / cageHP;

        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
        if (cageAnimator) cageAnimator.SetTrigger("Hit");

        HapticManager.Instance?.Vibrate(0.3f, 0.3f, 0.1f);

        Debug.Log($"[Cage] HP เหลือ: {currentHP}/{cageHP}");

        if (currentHP <= 0)
            StartCoroutine(BreakCage());
    }

    IEnumerator BreakCage()
    {
        isBroken = true;

        // หยุด VSE และเสียง
        if (vseCoroutine != null) StopCoroutine(vseCoroutine);
        if (tigerAudioSource) tigerAudioSource.Stop();

        // แยก Tiger ออกจาก Parent ก่อน Destroy กรง
        if (tigerInCage != null)
            tigerInCage.transform.SetParent(null);

        if (cageAnimator) cageAnimator.SetTrigger("Break");
        if (breakVFX) Instantiate(breakVFX, transform.position, Quaternion.identity);

        HapticManager.Instance?.PlayPattern(HapticPatternType.Boom);

        CageQuestManager.Instance?.OnCageFreed();

        if (hpBarObject) hpBarObject.SetActive(false);

        // ซ่อนกรง
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr) mr.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Tiger วิ่งออกไปแล้วหาย
        if (tigerInCage != null)
            yield return StartCoroutine(TigerEscape());

        Destroy(gameObject);
    }

    IEnumerator TigerEscape()
    {
        CreatureMover mover = tigerInCage.GetComponent<CreatureMover>();
        if (mover != null)
        {
            mover.enabled = true;

            float elapsed = 0f;
            while (elapsed < tigerRunDuration)
            {
                if (tigerInCage == null) yield break;
                mover.RunForward();
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(tigerRunDuration);
        }

        yield return StartCoroutine(FadeOutTiger());
        if (tigerInCage != null) Destroy(tigerInCage);
    }

    IEnumerator FadeOutTiger()
    {
        if (tigerInCage == null) yield break;

        SkinnedMeshRenderer[] renderers =
            tigerInCage.GetComponentsInChildren<SkinnedMeshRenderer>();

        // ✅ Debug ดูว่าเจอ Renderer ไหมและ Material ชื่ออะไร
        Debug.Log($"[FadeOut] พบ SkinnedMeshRenderer: {renderers.Length}");
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
                Debug.Log($"[FadeOut] Material: {mat.name} | Surface: {mat.GetFloat("_Surface")}");
        }

        float elapsed = 0f;
        while (elapsed < tigerFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / tigerFadeOutDuration);

            foreach (var r in renderers)
            {
                foreach (var mat in r.materials)
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
            }
            yield return null;
        }
    }
}