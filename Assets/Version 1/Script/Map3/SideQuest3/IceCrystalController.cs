// ============================================================
// FILE: IceCrystalController.cs
// PATH: Assets/Version 1/Script/Map3/SideQuest3/IceCrystalController.cs
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ผลึกน้ำแข็ง 1 ก้อนในภารกิจรอง "ช่วยวาฬโบราณ" (Map3)
/// ดัดแปลงจาก RockController.cs (Map2) — โครงสร้างเหมือนเดิม แต่ปรับ:
///   • HP = 3 ครั้ง — หมัด (SmartAttackSystem) / ธนูธรรมดา = 1 ครั้งต่อการโดน
///   • ธนูไฟ = ละลายทันที (implement IMeltable → FireArrowProjectile เรียก Melt() ให้เอง)
///   • เสียง: เสียงผลึกน้ำแข็งสั่น (loop) — "หยุด" ตอนแตก (เหมือนเสียงเสือ Map1 ไม่ใช่น้ำ Map2)
///   • VSE tag "IceCrystal" — หยุดทันทีที่แตก
///
/// Setup: GameObject ต้องมี Tag = "IceCrystal" + Collider (บนตัวเดียวกับสคริปต์นี้)
/// </summary>
public class IceCrystalController : MonoBehaviour, IMeltable
{
    [Header("Crystal Stats")]
    [Tooltip("จำนวนครั้งที่ต้องโดน (หมัด/ธนูธรรมดา) ถึงจะแตก")]
    public int crystalHP = 3;

    [Tooltip("เปิด = นับเป็นครั้ง (1 ฮิต = 1) ไม่สน ATK / ปิด = ใช้ค่าดาเมจจริง")]
    public bool countHitsInsteadOfDamage = true;

    private int currentHP;

    [Header("Crystal Hum Sound (Accessibility) — loop จนกว่าจะแตก")]
    public AudioSource crystalAudioSource;
    public AudioClip crystalHumClip;

    [Header("VSE")]
    public float vseIntensity = 0.8f;
    public float vseInterval = 2f;
    public float vseMaxRadius = 15f;
    public string vseTag = "IceCrystal";

    [Header("Accessibility Sound Emit (ตอนแตก)")]
    [Tooltip("SoundEmitter tag = SideQuest — Emit() ครั้งเดียวตอนแตก")]
    public SoundEmitter sfxEmitter;

    [Header("UI")]
    public Slider hpBar;
    public GameObject hpBarObject;

    [Header("Effects")]
    public Animator crystalAnimator;
    public GameObject hitVFX;
    public GameObject shatterVFX;   // แตกจากหมัด/ธนูธรรมดา
    public GameObject meltVFX;      // ละลายจากธนูไฟ (ไม่ใส่ = ใช้ shatterVFX)

    [Tooltip("ซ่อน object นี้ตอนแตก (ถ้าไม่ใส่ จะปิด Renderer ทั้งหมดในลูกแทน)")]
    public GameObject crystalModel;

    private bool isBroken = false;
    private Coroutine vseCoroutine;

    public bool IsBroken => isBroken;

    void Start()
    {
        currentHP = crystalHP;
        if (hpBarObject) hpBarObject.SetActive(false);

        if (crystalAudioSource && crystalHumClip)
        {
            crystalAudioSource.clip = crystalHumClip;
            crystalAudioSource.loop = true;
            crystalAudioSource.Play();
        }

        vseCoroutine = StartCoroutine(VSELoop());
    }

    IEnumerator VSELoop()
    {
        Transform player = GameObject.FindWithTag("Player")?.transform;

        while (!isBroken)
        {
            if (player != null)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                float intensity = (1f - Mathf.Clamp01(dist / vseMaxRadius)) * vseIntensity;

                if (intensity > 0.05f)
                {
                    VisualSoundEffectManager.Instance?.ShowIcon(transform.position, intensity, vseTag);
                    InGameCCManager.Instance?.ShowCC(transform.position, vseTag);
                }
            }
            yield return new WaitForSeconds(vseInterval);
        }
    }

    // ────────────────────────────────────────
    // หมัด (SmartAttackSystem) + ธนูธรรมดา (FireArrowProjectile)
    // ────────────────────────────────────────
    public void TakeDamage(int dmg)
    {
        if (isBroken) return;

        int applied = countHitsInsteadOfDamage ? 1 : Mathf.Max(1, dmg);
        currentHP = Mathf.Clamp(currentHP - applied, 0, crystalHP);

        if (hpBarObject) hpBarObject.SetActive(true);
        if (hpBar) hpBar.value = (float)currentHP / crystalHP;

        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
        if (crystalAnimator) crystalAnimator.SetTrigger("Hit");

        // แตกร้าว: สั่นแหลมสั้น (high-freq ขวา) ต่างจากหิน Map2
        HapticManager.Instance?.Vibrate(0.15f, 0.45f, 0.08f);

        Debug.Log($"[IceCrystal] HP เหลือ: {currentHP}/{crystalHP}");

        if (currentHP <= 0)
            StartCoroutine(BreakCrystal(melted: false));
    }

    // ────────────────────────────────────────
    // ธนูไฟ → ละลายทันที (IMeltable)
    // ────────────────────────────────────────
    public void Melt()
    {
        if (isBroken) return;
        currentHP = 0;
        Debug.Log("[IceCrystal] 🔥 โดนธนูไฟ → ละลายทันที");
        StartCoroutine(BreakCrystal(melted: true));
    }

    IEnumerator BreakCrystal(bool melted)
    {
        isBroken = true;

        if (vseCoroutine != null) StopCoroutine(vseCoroutine);
        if (crystalAudioSource) crystalAudioSource.Stop();

        if (crystalAnimator) crystalAnimator.SetTrigger(melted ? "Melt" : "Break");

        GameObject vfx = (melted && meltVFX != null) ? meltVFX : shatterVFX;
        if (vfx) Instantiate(vfx, transform.position, Quaternion.identity);

        sfxEmitter?.Emit();
        HapticManager.Instance?.PlayPattern(HapticPatternType.Boom);

        IceCrystalQuestManager.Instance?.OnCrystalDestroyed();

        if (hpBarObject) hpBarObject.SetActive(false);

        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;

        if (crystalModel != null) crystalModel.SetActive(false);
        else foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        yield return null;
    }
}