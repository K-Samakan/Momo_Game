// ============================================================
// FILE: AnchorController.cs
// PATH: Assets/Version 1/Script/Map4/SideQuest4/AnchorController.cs
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ควบคุมสมอ 1 จุด (Map4)
/// ✅ ก๊อปโครงจาก CageController.cs (Map1) — ช่อง Inspector เรียงเหมือนกัน
///   ต่างจาก Map1:
///   • ไม่มีเสือในกรง → มี chainObject (โซ่) ที่จะหายตอนพังแทน (ไม่ใส่ก็ได้)
///   • ดาเมจแยกตามอาวุธ: หมัด = 1 / สวมเบ็ดยืด = 2 (ไม่ใช้ค่า punchDamage จาก SmartAttack)
///
/// ⚠️ Collider อยู่บน GameObject เดียวกับสคริปต์ + Tag = "Anchor" (เหมือนกรงใช้ Tag "Cage")
/// </summary>
public class AnchorController : MonoBehaviour
{
    [Header("Anchor Stats")]
    public int anchorHP = 4;
    [Tooltip("ดาเมจต่อครั้งเมื่อต่อยด้วยหมัดปกติ")]
    public int punchDamage = 1;
    [Tooltip("ดาเมจต่อครั้งเมื่อสวมเบ็ดยืด (แรงกว่าหมัด)")]
    public int hookDamage = 2;
    private int currentHP;

    [Header("Chain (แทน Tiger In Cage)")]
    [Tooltip("โซ่ของสมอ — ปิดตอนพัง (ไม่ใส่ก็ได้)")]
    public GameObject chainObject;

    [Header("Chain Audio (แทน Tiger Audio)")]
    public AudioSource chainAudioSource;
    public AudioClip chainCreakClip;

    [Header("VSE")]
    public float vseIntensity = 0.8f;
    public float vseInterval = 2f;
    public float vseMaxRadius = 15f;

    [Header("UI")]
    public Slider hpBar;
    public GameObject hpBarObject;

    [Header("Effects")]
    public Animator anchorAnimator;
    public GameObject breakVFX;
    public GameObject hitVFX;

    [Header("Fishing Rod (ปล่อยว่างได้ — หาจาก Player เอง)")]
    public FishingRodController rodController;

    private bool isBroken = false;
    private Coroutine vseCoroutine;

    public bool IsBroken => isBroken;
    bool IsHookEquipped => rodController != null && rodController.IsEquipped;

    void Start()
    {
        currentHP = anchorHP;
        if (hpBarObject) hpBarObject.SetActive(false);

        if (rodController == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player) rodController = player.GetComponentInChildren<FishingRodController>(true);
        }

        vseCoroutine = StartCoroutine(VSELoop());

        if (chainAudioSource && chainCreakClip)
        {
            chainAudioSource.clip = chainCreakClip;
            chainAudioSource.loop = true;
            chainAudioSource.Play();
        }
    }

    IEnumerator VSELoop()
    {
        Transform player = GameObject.FindWithTag("Player")?.transform;

        while (!isBroken)
        {
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                float intensity = 1f - Mathf.Clamp01(dist / vseMaxRadius);

                if (intensity > 0.05f)
                {
                    VisualSoundEffectManager.Instance?.ShowIcon(transform.position, intensity, "Anchor");
                    InGameCCManager.Instance?.ShowCC(transform.position, "Anchor");
                }
            }

            yield return new WaitForSeconds(vseInterval);
        }
    }

    /// <summary>เรียกจาก SmartAttackSystem — เลือกดาเมจเองตามอาวุธ</summary>
    public void TakeHit()
    {
        bool hook = IsHookEquipped;
        TakeDamage(hook ? hookDamage : punchDamage, hook);
    }

    public void TakeDamage(int dmg) => TakeDamage(dmg, IsHookEquipped);

    void TakeDamage(int dmg, bool isHook)
    {
        if (isBroken) return;

        currentHP -= dmg;
        currentHP = Mathf.Clamp(currentHP, 0, anchorHP);

        if (hpBarObject) hpBarObject.SetActive(true);
        if (hpBar) hpBar.value = (float)currentHP / anchorHP;

        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
        if (anchorAnimator) anchorAnimator.SetTrigger("Hit");

        // 🎮 เบ็ดยืดสั่นแรงกว่าหมัด — รู้สึกได้โดยไม่ต้องได้ยิน
        if (isHook) HapticManager.Instance?.Vibrate(0.5f, 0.6f, 0.15f);
        else        HapticManager.Instance?.Vibrate(0.3f, 0.3f, 0.1f);

        Debug.Log($"[Anchor] {(isHook ? "🎣 เบ็ด" : "👊 หมัด")} -{dmg} | HP เหลือ: {currentHP}/{anchorHP}");

        if (currentHP <= 0)
            StartCoroutine(BreakAnchor());
    }

    IEnumerator BreakAnchor()
    {
        isBroken = true;

        if (vseCoroutine != null) StopCoroutine(vseCoroutine);
        if (chainAudioSource) chainAudioSource.Stop();

        if (chainObject) chainObject.SetActive(false);

        if (anchorAnimator) anchorAnimator.SetTrigger("Break");
        if (breakVFX) Instantiate(breakVFX, transform.position, Quaternion.identity);

        HapticManager.Instance?.PlayPattern(HapticPatternType.Boom);

        AnchorQuestManager.Instance?.OnAnchorDestroyed();

        if (hpBarObject) hpBarObject.SetActive(false);

        // ซ่อนสมอ (เหมือน Map1 แต่ซ่อนทุก mesh ลูกด้วย เพราะสมอมักมีหลายชิ้น)
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;

        yield return null;
    }
}