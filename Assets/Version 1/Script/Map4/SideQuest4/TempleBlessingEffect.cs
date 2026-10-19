// ============================================================
// FILE: TempleBlessingEffect.cs
// PATH: Assets/Version 1/Script/Map4/SideQuest4/TempleBlessingEffect.cs
// ============================================================
using UnityEngine;
using System.Collections;
using TMPro;
using MOMO.Player;

/// <summary>
/// รางวัลภารกิจรอง Map4 — "พรวิหารโบราณ"
/// โครงจาก TigerAllyEffect (Map1) + การแสดงผลแบบ OasisShieldEffect (Map2/3):
///   • blessingVFX = GameObject ลูกของ Player → SetActive(true) ตอนบัฟ / false ตอนหมด
///     (เหมือน shieldVFX ของโล่ แต่ใช้เอฟเฟกต์ฮีลแบบ loop แทน)
///   • ทุก 15 วิ ฮีล +1 → PlayerHealth.Heal() เล่น healEffect เดิมให้อัตโนมัติ
///
/// การทำงาน (ตอนสู้บอส Map4):
///   เข้าห้องบอส → รอ 3 วิ → บัฟ 30 วิ (ATK+2, HP+1 ทุก 15 วิ) → คูลดาวน์ 60 วิ → วนซ้ำ
///   หยุดเมื่อบอสหรือผู้เล่นตาย
/// </summary>
public class TempleBlessingEffect : MonoBehaviour
{
    [Header("Temple Blessing Settings")]
    public float buffDuration = 30f;
    public float buffCooldown = 60f;
    public int atkBonus = 2;
    public int healAmount = 1;
    public float healInterval = 15f;
    [Tooltip("ฮีลทันทีตอนบัฟเริ่ม แล้วค่อยฮีลซ้ำทุก healInterval")]
    public bool healOnStart = true;
    [Tooltip("หน่วงก่อนบัฟรอบแรก (ตรงกับ Intro บอส 3 วิ)")]
    public float firstBuffDelay = 3f;

    private bool isActive = false;          // = เควสสำเร็จแล้ว

    [Header("References")]
    public PlayerHealth playerHealth;
    public SmartAttackSystem smartAttackSystem;
    public BossMap4Health bossHealth;

    [Header("VFX (แบบเดียวกับ shieldVFX ของ Map2/3)")]
    [Tooltip("เอฟเฟกต์ฮีลแบบ Loop ที่เป็นลูกของ Player — SetActive ตามสถานะบัฟ (ปิดไว้ใน Scene)")]
    public GameObject blessingVFX;

    [Tooltip("(ไม่บังคับ) เอฟเฟกต์ตอนฮีลแต่ละครั้ง — ปล่อยว่าง = ใช้ healEffect เดิมของ PlayerHealth")]
    public GameObject healTickVFX;

    [Tooltip("กะพริบ blessingVFX ช่วงท้ายบัฟ — เตือนทางภาพว่าบัฟใกล้หมด")]
    public bool blinkBeforeEnd = true;
    public float blinkStartTime = 5f;
    public float blinkInterval = 0.25f;

    [Header("Sound Effect")]
    public AudioSource audioSource;               // ปล่อยว่างได้ — สร้างให้อัตโนมัติ
    public AudioClip[] blessingStartSounds;
    public AudioClip blessingEndSound;
    [Range(0f, 1f)] public float soundVolume = 0.8f;

    [Header("UI Popup")]
    public TigerAssistPopupUI assistPopup;        // ใส่รูป "TEMPLE BLESSING!"
    [Tooltip("(ไม่บังคับ) ข้อความบอกเวลาบัฟ/คูลดาวน์ — ช่วยผู้เล่นที่ไม่ได้ยินเสียง")]
    public TextMeshProUGUI statusText;
    public Color buffColor = new Color(0.5f, 1f, 0.55f);
    public Color cooldownColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("🧪 Debug")]
    public bool showDebugOverlay = false;
    public bool enableDebugKey = true;
    public KeyCode debugActivateKey = KeyCode.F8;
    public KeyCode debugDeactivateKey = KeyCode.F7;
    [Tooltip("ลดเลือดผู้เล่นเพื่อเทสฮีล (เลือดเต็มจะฮีลไม่ขึ้น)")]
    public KeyCode debugDamageKey = KeyCode.F5;
    public int debugDamageAmount = 2;
    private string _debugOverlayText = "";
    private float _debugOverlayTimer = 0f;
    private const float DEBUG_OVERLAY_DURATION = 2.5f;

    private bool _bossFightActive = false;
    private bool _isBuffActive = false;
    private int _appliedBonus = 0;
    private Coroutine _cycleRoutine;
    private Coroutine _testRoutine;

    public bool IsBuffActive => _isBuffActive;

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (smartAttackSystem == null && playerHealth != null)
            smartAttackSystem = playerHealth.GetComponent<SmartAttackSystem>();

        if (blessingVFX) blessingVFX.SetActive(false);
        if (statusText) statusText.gameObject.SetActive(false);
    }

    void OnDisable() => RemoveBuff(false);

    // ════════════════════════════════════════════════════════════
    #region Public API
    // ════════════════════════════════════════════════════════════

    /// <summary>เรียกจาก AnchorQuestManager ตอนเควสสำเร็จ (เหมือน EnableTigerAlly)</summary>
    public void EnableTempleBlessing()
    {
        isActive = true;
        Debug.Log("<color=#7CFC00>[TempleBlessing] ✅ ปลดล็อกพรวิหารแล้ว — จะทำงานตอนสู้บอส Map4</color>");

        if (_bossFightActive && _cycleRoutine == null)
            _cycleRoutine = StartCoroutine(BlessingCycle());
    }

    /// <summary>เรียกจาก BossMap4RoomTrigger ตอนเข้าห้องบอส (เหมือน OasisShieldEffect)</summary>
    public void SetBossFightActive(bool active)
    {
        _bossFightActive = active;

        if (active && isActive && _cycleRoutine == null)
            _cycleRoutine = StartCoroutine(BlessingCycle());
        else if (!active)
            StopCycle();
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Core Logic
    // ════════════════════════════════════════════════════════════

    IEnumerator BlessingCycle()
    {
        yield return new WaitForSeconds(firstBuffDelay);

        while (_bossFightActive && IsFightOngoing())
        {
            yield return BuffRoutine();

            float cd = buffCooldown;
            while (cd > 0f && _bossFightActive && IsFightOngoing())
            {
                cd -= Time.deltaTime;
                SetStatus($"Blessing ready in {Mathf.CeilToInt(cd)}s", cooldownColor);
                yield return null;
            }
        }

        SetStatus(null, cooldownColor);
        _cycleRoutine = null;
    }

    IEnumerator BuffRoutine()
    {
        ApplyBuff();

        // 💚 ฮีลครั้งแรกทันทีที่ติดบัฟ
        if (healOnStart) DoHeal();

        float t = 0f;
        float nextHeal = healInterval;
        float blinkTimer = 0f;

        while (t < buffDuration && IsFightOngoing())
        {
            t += Time.deltaTime;
            float remain = buffDuration - t;

            if (t >= nextHeal)
            {
                nextHeal += healInterval;
                DoHeal();
            }

            // ⚠️ กะพริบช่วงท้าย (เหมือนไอเทมใกล้หมดในเกมทั่วไป)
            if (blinkBeforeEnd && blessingVFX && remain <= blinkStartTime)
            {
                blinkTimer += Time.deltaTime;
                if (blinkTimer >= blinkInterval)
                {
                    blinkTimer = 0f;
                    blessingVFX.SetActive(!blessingVFX.activeSelf);
                }
            }

            SetStatus($"TEMPLE BLESSING  ATK+{atkBonus}  {Mathf.CeilToInt(remain)}s", buffColor);
            yield return null;
        }

        RemoveBuff(true);
    }

    bool IsFightOngoing()
    {
        bool playerAlive = playerHealth == null || playerHealth.IsAlive();
        bool bossAlive = bossHealth == null || bossHealth.IsAlive();
        return playerAlive && bossAlive;
    }

    void StopCycle()
    {
        if (_cycleRoutine != null) StopCoroutine(_cycleRoutine);
        _cycleRoutine = null;
        RemoveBuff(true);
        SetStatus(null, cooldownColor);
    }

    void ApplyBuff()
    {
        if (_isBuffActive) return;
        _isBuffActive = true;

        if (smartAttackSystem != null)
        {
            _appliedBonus = atkBonus;
            smartAttackSystem.punchDamage += _appliedBonus;
            smartAttackSystem.punchDamage2 += _appliedBonus;
        }
        else Debug.LogWarning("[TempleBlessing] ⚠️ ไม่พบ SmartAttackSystem — ATK+2 จะไม่ทำงาน");

        if (blessingVFX) blessingVFX.SetActive(true);
        PlayRandom(blessingStartSounds);
        assistPopup?.ShowPopup();
        HapticManager.Instance?.PlayPattern(HapticPatternType.PuzzleComplete);

        ShowOverlay($"🏛️ TEMPLE BLESSING!\nATK +{atkBonus} for {buffDuration:0}s");
        Debug.Log($"<color=#7CFC00>[TempleBlessing] 🏛️ บัฟเริ่ม {buffDuration}s | ATK+{atkBonus} | HP+{healAmount}/{healInterval}s</color>");
    }

    void RemoveBuff(bool playFx)
    {
        if (!_isBuffActive) return;
        _isBuffActive = false;

        if (smartAttackSystem != null && _appliedBonus != 0)
        {
            smartAttackSystem.punchDamage = Mathf.Max(1, smartAttackSystem.punchDamage - _appliedBonus);
            smartAttackSystem.punchDamage2 = Mathf.Max(1, smartAttackSystem.punchDamage2 - _appliedBonus);
        }
        _appliedBonus = 0;

        if (blessingVFX) blessingVFX.SetActive(false);

        if (playFx)
        {
            if (blessingEndSound && audioSource) audioSource.PlayOneShot(blessingEndSound, soundVolume);
            HapticManager.Instance?.PlayPattern(HapticPatternType.Short);
        }

        Debug.Log("[TempleBlessing] ⏳ บัฟหมดเวลา");
    }

    void DoHeal()
    {
        if (playerHealth == null || !playerHealth.IsAlive()) return;

        int before = playerHealth.currentHealth;

        // PlayerHealth.Heal() เล่น healEffect + healSound + สั่นจอยให้อยู่แล้ว (ถ้าเลือดไม่เต็ม)
        playerHealth.Heal(healAmount);

        int after = playerHealth.currentHealth;
        if (after > before)
            Debug.Log($"<color=#7CFC00>[TempleBlessing] 💚 HEAL สำเร็จ: {before} → {after}/{playerHealth.maxHealth}</color>");
        else
            Debug.Log($"<color=#FFA500>[TempleBlessing] ⚪ ถึงรอบฮีลแล้ว แต่เลือดเต็มอยู่ ({after}/{playerHealth.maxHealth}) — ไม่ได้ฮีล</color>");

        if (healTickVFX)
        {
            var fx = Instantiate(healTickVFX, playerHealth.transform.position, Quaternion.identity);
            fx.transform.SetParent(playerHealth.transform);
            Destroy(fx, 2f);
        }
    }

    void PlayRandom(AudioClip[] clips)
    {
        if (audioSource == null || clips == null || clips.Length == 0) return;
        audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)], soundVolume);
    }

    void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        bool show = !string.IsNullOrEmpty(msg);
        statusText.gameObject.SetActive(show);
        if (show) { statusText.text = msg; statusText.color = color; }
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Debug (เหมือน TigerAllyEffect / OasisShieldEffect)
    // ════════════════════════════════════════════════════════════

    void Update()
    {
        if (_debugOverlayTimer > 0f) _debugOverlayTimer -= Time.deltaTime;

        if (!enableDebugKey) return;
        if (Input.GetKeyDown(debugActivateKey)) TEST_ForceActivateBuff();
        if (Input.GetKeyDown(debugDeactivateKey)) TEST_ForceDeactivateBuff();
        if (Input.GetKeyDown(debugDamageKey)) TEST_DamagePlayer();
    }

    [ContextMenu("🧪 TEST - Damage Player (เพื่อเทสฮีล)")]
    public void TEST_DamagePlayer()
    {
        if (playerHealth == null) return;
        int before = playerHealth.currentHealth;
        playerHealth.TakeDamage(debugDamageAmount);
        Debug.Log($"[TempleBlessing][TEST] 🩸 ลดเลือด {before} → {playerHealth.currentHealth}/{playerHealth.maxHealth}");
    }

    void ShowOverlay(string msg)
    {
        if (!showDebugOverlay) return;
        _debugOverlayText = msg;
        _debugOverlayTimer = DEBUG_OVERLAY_DURATION;
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
        style.normal.textColor = new Color(0.5f, 1f, 0.55f, Mathf.Clamp01(_debugOverlayTimer));

        float w = 500f, h = 80f;
        GUI.Label(new Rect((Screen.width - w) / 2f, 60f, w, h), _debugOverlayText, style);
    }

    [ContextMenu("🧪 TEST - Force Activate Blessing (1 round)")]
    public void TEST_ForceActivateBuff()
    {
        if (_isBuffActive) { Debug.Log("[TempleBlessing][TEST] บัฟเปิดอยู่แล้ว"); return; }
        Debug.Log("[TempleBlessing][TEST] ⚡ บังคับเปิดบัฟ 1 รอบ (ข้ามเงื่อนไขเควส/ห้องบอส)");
        _testRoutine = StartCoroutine(BuffRoutine());
    }

    [ContextMenu("🧪 TEST - Force Deactivate Blessing")]
    public void TEST_ForceDeactivateBuff()
    {
        if (_testRoutine != null) StopCoroutine(_testRoutine);
        _testRoutine = null;
        StopCycle();
        Debug.Log("[TempleBlessing][TEST] ⚡ บังคับปิดบัฟ");
    }

    #endregion
}