using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// ควบคุมหิน 1 ก้อนในภารกิจรอง "ฟื้นฟูโอเอซิส" (Map2)
/// ดัดแปลงจาก CageController.cs (Map1) — โครงสร้างหลักเหมือนเดิม แต่ปรับ:
///   • HP: ทุบ 3 ครั้งพัง (เหมือนกรงเสือ ใช้ SmartAttackSystem ต่อยได้เลย)
///   • เสียง: ใช้เสียงน้ำแทนเสียงเสือร้อง และ "ไม่หยุด" แม้หินพังแล้ว (ต่างจาก Map1)
///   • VSE: ใช้ tag "Rock" — หยุดทำงานทันทีที่หินพัง (เหมือน Map1)
///   • เพิ่ม: โผล่ GameObject น้ำ (waterObject) ตอนหินพังสำเร็จ
/// </summary>
public class RockController : MonoBehaviour
{
    [Header("Rock Stats")]
    public int rockHP = 3;
    private int currentHP;

    [Header("Water Sound (Accessibility) — เล่นตั้งแต่ Start ไม่หยุดแม้หินพัง")]
    public AudioSource waterAudioSource;
    public AudioClip waterFlowClip;

    [Header("VSE")]
    [Tooltip("ความเข้มสูงสุดของไอคอนตอนอยู่ใกล้สุด")]
    public float vseIntensity = 0.8f;
    public float vseInterval = 2f;
    public float vseMaxRadius = 15f;

    [Header("Water Object (Reveal ตอนหินพัง)")]
    [Tooltip("ลากโมเดลน้ำที่ซ่อนไว้ (SetActive=false ใน Scene) มาใส่ตรงนี้")]
    public GameObject waterObject;

    [Header("Accessibility Sound Emit (ตอนหินแตก — เหมือนกรงเสือ)")]
    [Tooltip("SoundEmitter tag = SideQuest — Emit() ครั้งเดียวตอนพัง")]
    public SoundEmitter sfxEmitter;

    [Header("UI")]
    public Slider hpBar;
    public GameObject hpBarObject;

    [Header("Effects")]
    public Animator rockAnimator;
    public GameObject breakVFX;
    public GameObject hitVFX;

    private bool isBroken = false;
    private Coroutine vseCoroutine;

    void Start()
    {
        currentHP = rockHP;
        if (hpBarObject) hpBarObject.SetActive(false);
        if (waterObject) waterObject.SetActive(false);

        // ▶️ เริ่มเล่นเสียงน้ำ (loop) — ไม่หยุดแม้หินพังแล้ว (ต่างจาก tiger roar ใน Map1)
        if (waterAudioSource && waterFlowClip)
        {
            waterAudioSource.clip = waterFlowClip;
            waterAudioSource.loop = true;
            waterAudioSource.Play();
        }

        // ▶️ เริ่ม VSE loop — หยุดทันทีที่หินพัง
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
                float intensity = 1f - Mathf.Clamp01(dist / vseMaxRadius);

                // ขึ้น VSE เฉพาะตอนอยู่ในระยะเท่านั้น
                if (intensity > 0.05f)
                {
                    VisualSoundEffectManager.Instance?.ShowIcon(
                        transform.position, intensity, "Rock");

                    InGameCCManager.Instance?.ShowCC(transform.position, "Rock");
                }
            }

            yield return new WaitForSeconds(vseInterval);
        }
    }

    public void TakeDamage(int dmg)
    {
        if (isBroken) return;

        currentHP -= dmg;
        currentHP = Mathf.Clamp(currentHP, 0, rockHP);

        if (hpBarObject) hpBarObject.SetActive(true);
        if (hpBar) hpBar.value = (float)currentHP / rockHP;

        if (hitVFX) Instantiate(hitVFX, transform.position, Quaternion.identity);
        if (rockAnimator) rockAnimator.SetTrigger("Hit");

        HapticManager.Instance?.Vibrate(0.3f, 0.3f, 0.1f);

        Debug.Log($"[Rock] HP เหลือ: {currentHP}/{rockHP}");

        if (currentHP <= 0)
            StartCoroutine(BreakRock());
    }

    IEnumerator BreakRock()
    {
        isBroken = true;

        // ⛔ หยุดแค่ VSE เท่านั้น — เสียงน้ำปล่อยให้เล่นต่อไปเรื่อยๆ ไม่เรียก .Stop()
        if (vseCoroutine != null) StopCoroutine(vseCoroutine);

        // 💧 โผล่น้ำที่ซ่อนไว้ (โจทย์ที่ขอเพิ่มมา)
        if (waterObject) waterObject.SetActive(true);

        if (rockAnimator) rockAnimator.SetTrigger("Break");
        if (breakVFX) Instantiate(breakVFX, transform.position, Quaternion.identity);

        // 🔊 เสียง accessibility ตอนพัง (แพทเทิร์นเดียวกับกรงเสือ)
        sfxEmitter?.Emit();

        HapticManager.Instance?.PlayPattern(HapticPatternType.Boom);

        RockQuestManager.Instance?.OnRockDestroyed();

        if (hpBarObject) hpBarObject.SetActive(false);

        // ซ่อนตัวหิน (mesh + collider) — ไม่ Destroy GameObject เพราะ waterAudioSource ต้องเล่นต่อ
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr) mr.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        yield return null;
    }
}