using UnityEngine;

public class TigerAllyEffect : MonoBehaviour
{
    [Header("Tiger Ally Settings")]
    public int tigerBonusDamage = 3;
    public int comboRequirement = 5;

    private bool isActive = false;
    private int currentCombo = 0;

    [Header("VFX")]
    public GameObject tigerAttackVFX;
    public GameObject tigerImpactVFX;
    public GameObject tigerAllyObject;

    [Header("Damage Number")]
    public GameObject damageNumberPrefab;
    public Vector3 damageNumberOffset = new Vector3(0f, 2.5f, 0f);

    [Header("UI Popup")]
    public TigerAssistPopupUI assistPopup;

    public void EnableTigerAlly()
    {
        isActive = true;
        if (tigerAllyObject) tigerAllyObject.SetActive(true);
        Debug.Log("[TigerAlly] เสือพร้อมช่วยโจมตีบอสแล้ว!");
    }

    public void OnPlayerAttackBoss(MonsterHealth bossHealth)
    {
        if (!isActive) return;

        currentCombo++;
        Debug.Log($"[TigerAlly] Combo: {currentCombo}/{comboRequirement}");

        if (currentCombo >= comboRequirement)
        {
            TigerAssistAttack(bossHealth);
            currentCombo = 0;
        }
    }

    private void TigerAssistAttack(MonsterHealth bossHealth)
    {
        Vector3 bossPos = bossHealth.transform.position;

        // 1) ดีลดาเมจ
        bossHealth.TakeDamage(tigerBonusDamage, transform.position);

        // 2) VFX Slash
        if (tigerAttackVFX)
            Instantiate(tigerAttackVFX, bossPos, Quaternion.identity);

        // 3) VFX Impact (หน่วง 0.1s)
        if (tigerImpactVFX)
            StartCoroutine(SpawnDelayed(tigerImpactVFX, bossPos + Vector3.up, 0.1f));

        // 4) Damage Number
        if (damageNumberPrefab)
        {
            GameObject numObj = Instantiate(
                damageNumberPrefab,
                bossPos + damageNumberOffset,
                Quaternion.identity);

            Camera cam = Camera.main;
            if (cam != null)
            {
                numObj.transform.LookAt(cam.transform);
                numObj.transform.Rotate(0f, 180f, 0f);
            }
            numObj.GetComponent<TigerDamageNumber>()?.Setup(tigerBonusDamage);
        }

        // 5) UI Popup
        assistPopup?.ShowPopup();

        // 6) Haptic
        HapticManager.Instance?.PlayPattern(HapticPatternType.Short);

        Debug.Log($"[TigerAlly] เสือโจมตี! +{tigerBonusDamage} DMG");
    }

    private System.Collections.IEnumerator SpawnDelayed(
        GameObject prefab, Vector3 pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        Instantiate(prefab, pos, Quaternion.identity);
    }
}