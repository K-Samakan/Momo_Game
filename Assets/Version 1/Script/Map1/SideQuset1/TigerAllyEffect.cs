using UnityEngine;

public class TigerAllyEffect : MonoBehaviour
{
    [Header("Tiger Ally Settings")]
    public int tigerBonusDamage = 3;
    [Tooltip("ค่านี้ใช้แค่แสดงผล/อ้างอิง — ตัวนับคอมโบจริงอยู่ที่ BossMap1BT._comboCount")]
    public int comboRequirement = 5;

    private bool isActive = false;

    [Header("VFX")]
    public GameObject tigerAttackVFX;
    public GameObject tigerImpactVFX;
    public GameObject tigerAllyObject;

    [Header("Sound Effect")]
    public AudioSource audioSource;              // ปล่อยว่างได้ — จะสร้างให้อัตโนมัติถ้าไม่มี
    public AudioClip[] tigerAttackSounds;        // ใส่ได้หลายเสียง จะสุ่มเล่น
    [Range(0f, 1f)]
    public float soundVolume = 0.8f;

    [Header("Damage Number")]
    public GameObject damageNumberPrefab;
    public Vector3 damageNumberOffset = new Vector3(0f, 2.5f, 0f);

    [Header("UI Popup")]
    public TigerAssistPopupUI assistPopup;

    [Header("🧪 Debug")]
    [Tooltip("แสดงข้อความ 'TIGER ASSIST!' กลางจอตอนบัฟลง — ปิดก่อน build จริง")]
    public bool showDebugOverlay = false;
    private string _debugOverlayText = "";
    private float _debugOverlayTimer = 0f;
    private const float DEBUG_OVERLAY_DURATION = 2.5f;

    void Awake()
    {
        // ✅ ถ้าไม่ได้ผูก AudioSource ใน Inspector ให้หาเองหรือสร้างใหม่อัตโนมัติ
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // ════════════════════════════════════════════════════════════
    #region Public API
    // ════════════════════════════════════════════════════════════

    public void EnableTigerAlly()
    {
        isActive = true;
        if (tigerAllyObject) tigerAllyObject.SetActive(true);

        Debug.Log($"<color=#00FF7F>[TigerAlly] t={Time.time:F1}s | ✅ เสือพร้อมช่วยโจมตีบอสแล้ว! (ต้องตีครบ {comboRequirement} ครั้งต่อ 1 บัฟ)</color>");
    }

    /// <summary>
    /// ✅ ไม่นับคอมโบซ้ำอีกชั้น — BossMap1BT นับครบ 5 ดาบมาให้แล้วก่อนเรียกมาที่นี่
    /// เรียกครั้งนี้ = ครบเงื่อนไขแล้ว ให้เสือช่วยตีทันที
    /// </summary>
    public void OnPlayerAttackBoss(BossMap1Health bossHealth)
    {
        if (!isActive || bossHealth == null) return;

        Debug.Log($"<color=#00BFFF>[TigerAlly] t={Time.time:F1}s | 🐯 ได้รับสัญญาณครบ 5 ดาบจาก Boss แล้ว เตรียมช่วยตี</color>");

        TigerAssistAttack(bossHealth);
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Core Logic
    // ════════════════════════════════════════════════════════════

    private void TigerAssistAttack(BossMap1Health bossHealth)
    {
        Vector3 bossPos = bossHealth.transform.position;

        // ✅ ใช้ TakeDamageBypassCooldown แทน TakeDamage ธรรมดา
        bossHealth.TakeDamageBypassCooldown(tigerBonusDamage);

        Debug.Log(
            $"<color=#FFD700>━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
            $"🐯 [TIGER ASSIST TRIGGERED] t={Time.time:F2}s\n" +
            $"→ Bonus DMG: +{tigerBonusDamage} to {bossHealth.name}\n" +
            $"━━━━━━━━━━━━━━━━━━━━━━━━━━</color>"
        );

        if (showDebugOverlay)
        {
            _debugOverlayText = $"🐯 TIGER ASSIST!  +{tigerBonusDamage} DMG\n(t={Time.time:F1}s)";
            _debugOverlayTimer = DEBUG_OVERLAY_DURATION;
        }

        if (tigerAttackVFX)
            Instantiate(tigerAttackVFX, bossPos, Quaternion.identity);

        if (tigerImpactVFX)
            StartCoroutine(SpawnDelayed(tigerImpactVFX, bossPos + Vector3.up, 0.1f));

        // ✅ เล่นเสียง Tiger Attack
        PlayTigerAttackSound();

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

        assistPopup?.ShowPopup();
        HapticManager.Instance?.PlayPattern(HapticPatternType.Short);
    }

    /// <summary>
    /// ✅ สุ่มเล่นเสียง Tiger Attack จาก Array (ถ้ามีแค่ 1 เสียงก็เล่นอันเดียวเสมอ)
    /// </summary>
    private void PlayTigerAttackSound()
    {
        if (audioSource == null || tigerAttackSounds == null || tigerAttackSounds.Length == 0)
            return;

        AudioClip clip = tigerAttackSounds[Random.Range(0, tigerAttackSounds.Length)];
        audioSource.PlayOneShot(clip, soundVolume);
    }

    private System.Collections.IEnumerator SpawnDelayed(
        GameObject prefab, Vector3 pos, float delay)
    {
        yield return new WaitForSeconds(delay);
        Instantiate(prefab, pos, Quaternion.identity);
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Debug Overlay
    // ════════════════════════════════════════════════════════════

    void Update()
    {
        // 🧪 นับถอยหลัง overlay ให้เฟดหายไปเอง
        if (_debugOverlayTimer > 0f)
            _debugOverlayTimer -= Time.deltaTime;
    }

    void OnGUI()
    {
        if (!showDebugOverlay || _debugOverlayTimer <= 0f) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = new Color(1f, 0.85f, 0f, Mathf.Clamp01(_debugOverlayTimer));

        float w = 500f, h = 80f;
        Rect rect = new Rect((Screen.width - w) / 2f, 60f, w, h);
        GUI.Label(rect, _debugOverlayText, style);
    }

    #endregion
}