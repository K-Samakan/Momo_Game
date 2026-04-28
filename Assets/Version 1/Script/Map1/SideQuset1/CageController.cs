using UnityEngine;
using UnityEngine.UI;

public class CageController : MonoBehaviour
{
    [Header("Cage Stats")]
    public int cageHP = 3;
    private int currentHP;

    [Header("References")]
    public GameObject tigerPrefab;
    public Transform tigerSpawnPoint;
    public Slider hpBar;
    public GameObject hpBarObject;

    [Header("Effects")]
    public Animator cageAnimator;
    public GameObject breakVFX;
    public GameObject hitVFX;

    private bool isBroken = false;

    void Start()
    {
        currentHP = cageHP;
        if (hpBarObject) hpBarObject.SetActive(false);
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

        // ✅ แก้: ใช้ Vibrate() แทน PlayCageHit()
        // สั่นสั้นเบา = กรงโดนตี
        HapticManager.Instance?.Vibrate(0.3f, 0.3f, 0.1f);

        Debug.Log($"[Cage] HP เหลือ: {currentHP}/{cageHP}");

        if (currentHP <= 0)
            BreakCage();
    }

    void BreakCage()
    {
        isBroken = true;

        if (cageAnimator) cageAnimator.SetTrigger("Break");

        if (tigerPrefab && tigerSpawnPoint)
            Instantiate(tigerPrefab, tigerSpawnPoint.position, Quaternion.identity);

        if (breakVFX) Instantiate(breakVFX, transform.position, Quaternion.identity);

        // ✅ แก้: ใช้ PlayPattern(Boom) แทน PlayCageBreak()
        // สั่นหนัก = กรงพัง
        HapticManager.Instance?.PlayPattern(HapticPatternType.Boom);

        CageQuestManager.Instance?.OnCageFreed();

        if (hpBarObject) hpBarObject.SetActive(false);

        GetComponent<Collider>()?.gameObject.SetActive(false);

        Destroy(gameObject, 2f);
    }
}