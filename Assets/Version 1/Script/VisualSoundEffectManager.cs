// Assets/Version 1/Script/VisualSoundEffectManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// จัดการ Visual Sound Effect UI — แสดงไอคอนบน Radial Ring
/// รับข้อมูลจาก SoundEmitter หรือ script อื่นๆ ผ่าน ShowIcon()
/// </summary>
public class VisualSoundEffectManager : MonoBehaviour
{
    public static VisualSoundEffectManager Instance;

    // ─── References ──────────────────────────────────────────────
    [Header("References")]
    [Tooltip("Transform ของตัวละครผู้เล่น")]
    public Transform player;

    [Tooltip("Transform ของ Camera หลัก (ใช้คำนวณทิศทาง)")]
    public Transform cameraTransform;

    [Tooltip("Prefab ที่มี Image (icon) เป็น child")]
    public GameObject visualPrefab;

    [Tooltip("RadialLayoutManual script สำหรับ radius")]
    public RadialLayoutManual radialLayout;

    // ─── Settings ────────────────────────────────────────────────
    [Header("Settings")]
    [Tooltip("เวลาที่ไอคอนแสดงอยู่บนหน้าจอ (วินาที)")]
    public float displayTime = 2.5f;

    [Tooltip("ความเร็วในการ Lerp ไอคอนเข้าหาตำแหน่ง")]
    public float updateSpeed = 8f;

    [Tooltip("จำนวน icon สูงสุดที่แสดงพร้อมกัน")]
    public int poolSize = 10;

    // ─── Visual Types (กำหนดใน Inspector) ────────────────────────
    [System.Serializable]
    public class VisualType
    {
        [Tooltip("ชื่อ tag ที่ตรงกัน เช่น Enemy, Warning, SideQuest")]
        public string tagName;

        [Tooltip("Sprite ที่แสดงเมื่อ tag นี้ถูก trigger")]
        public Sprite iconSprite;

        [Tooltip("สีของไอคอน (ใช้ tint บน Image)")]
        public Color iconColor = Color.white;
    }
    public List<VisualType> visualTypes = new List<VisualType>();

    // ─── Pool ─────────────────────────────────────────────────────
    // cache RectTransform ไว้ด้วยเพื่อไม่ต้อง GetComponent ทุก frame
    private List<GameObject> _pool = new List<GameObject>();
    private List<RectTransform> _poolRTs = new List<RectTransform>();
    private List<Image> _poolImages = new List<Image>();

    private Dictionary<GameObject, Vector3> _sources = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, float> _timers = new Dictionary<GameObject, float>();

    // ════════════════════════════════════════════════════════════
    #region Unity Lifecycle
    // ════════════════════════════════════════════════════════════

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreatePool();
    }

    void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(visualPrefab, transform);
            go.SetActive(false);

            // Cache component ตั้งแต่ตอน spawn — ไม่ต้อง GetComponent ทุก frame
            RectTransform rt = go.GetComponent<RectTransform>();
            Image img = go.GetComponentInChildren<Image>();

            _pool.Add(go);
            _poolRTs.Add(rt);
            _poolImages.Add(img);
        }
    }

    void Update()
    {
        UpdateIcons();
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Public API
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// เรียกจาก SoundEmitter, DisappearingPlatform ฯลฯ
    /// เมื่อมีเสียงเกิดขึ้นจริง
    /// </summary>
    /// <param name="sourceWorldPos">ตำแหน่ง World Space ของแหล่งเสียง</param>
    /// <param name="intensity">ความแรง 0-1 (กำหนดขนาดและความทึบ)</param>
    /// <param name="tagName">ชื่อ tag เพื่อเลือก Sprite และสี</param>
    public void ShowIcon(Vector3 sourceWorldPos, float intensity, string tagName = "")
    {
        // ── Validate ────────────────────────────────────────────
        if (visualPrefab == null || player == null ||
            cameraTransform == null || radialLayout == null)
        {
            Debug.LogWarning("[VSE] Reference ขาดหาย — ตรวจสอบ Inspector");
            return;
        }

        if (intensity <= 0f) return;

        // ── หา slot ว่างจาก pool ────────────────────────────────
        int slot = GetAvailableSlot();
        if (slot < 0)
        {
            // pool เต็ม → ดึง slot ที่ timer น้อยที่สุดมาใช้
            slot = GetOldestSlot();
        }

        GameObject go = _pool[slot];
        Image img = _poolImages[slot];

        // ── ตั้งค่า Sprite และสีตาม tag ─────────────────────────
        ApplyVisualType(img, tagName, intensity);

        // ── ตั้งค่าขนาดตาม intensity ─────────────────────────────
        // ✅ แก้: scale บน RectTransform ของ icon เอง ไม่ใช่ parent
        float scale = Mathf.Lerp(0.6f, 1.2f, intensity);
        _poolRTs[slot].localScale = Vector3.one * scale;

        // ── เปิดใช้งาน ───────────────────────────────────────────
        go.SetActive(true);
        _sources[go] = sourceWorldPos;
        _timers[go] = displayTime;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Update Loop
    // ════════════════════════════════════════════════════════════

    void UpdateIcons()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            GameObject go = _pool[i];
            if (!go.activeSelf) continue;

            if (!_sources.TryGetValue(go, out Vector3 source)) continue;
            if (!_timers.TryGetValue(go, out float timeLeft)) continue;

            // ── คำนวณมุม ─────────────────────────────────────────
            float angle = CalculateAngle(source);

            // ── คำนวณ target position บน Radial Ring ─────────────
            float radius = radialLayout.radius;
            float angleRad = angle * Mathf.Deg2Rad;
            Vector2 targetPos = new Vector2(
                Mathf.Sin(angleRad) * radius,
                Mathf.Cos(angleRad) * radius
            );

            // ── ✅ Lerp ตำแหน่ง ──────────────────────────────────
            RectTransform rt = _poolRTs[i];
            rt.anchoredPosition = Vector2.Lerp(
                rt.anchoredPosition,
                targetPos,
                Time.deltaTime * updateSpeed
            );

            // ── ✅ หมุนไอคอนให้ชี้ถูกทิศ ─────────────────────────
            rt.rotation = Quaternion.Euler(0f, 0f, -angle);

            // ── ✅ Flip X เฉพาะด้านซ้าย (angle 180-360) ──────────
            // angle 0 = ข้างหน้า, 90 = ขวา, 180 = ข้างหลัง, 270 = ซ้าย
            Vector3 sc = rt.localScale;
            sc.x = (angle > 180f && angle < 360f)
                ? -Mathf.Abs(sc.x)
                : Mathf.Abs(sc.x);
            rt.localScale = sc;

            // ── นับถอยหลัง ──────────────────────────────────────
            timeLeft -= Time.deltaTime;
            _timers[go] = timeLeft;

            if (timeLeft <= 0f)
            {
                go.SetActive(false);
                _sources.Remove(go);
                _timers.Remove(go);
            }
        }
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Helpers
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// ✅ คำนวณมุมที่ถูกต้อง 0-360 องศา อิงจาก Camera Forward
    /// 0   = ข้างหน้า
    /// 90  = ขวา
    /// 180 = ข้างหลัง
    /// 270 = ซ้าย
    /// </summary>
    float CalculateAngle(Vector3 sourceWorldPos)
    {
        // 1) หาทิศทางจาก player → source บน XZ plane
        Vector3 dir = sourceWorldPos - player.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;

        if (flatDir == Vector3.zero) return 0f;

        // 2) แปลงเป็น local space ของ Camera
        Vector3 localDir = cameraTransform.InverseTransformDirection(flatDir);

        // 3) คำนวณมุม (Atan2 → องศา)
        //    localDir.x = ซ้าย/ขวา,  localDir.z = หน้า/หลัง
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;

        // 4) normalize ให้อยู่ใน 0-360
        angle = (angle + 360f) % 360f;

        return angle;
    }

    void ApplyVisualType(Image img, string tagName, float intensity)
    {
        if (img == null) return;

        // ค้นหา VisualType ที่ตรงกับ tag
        VisualType vt = visualTypes.Find(v => v.tagName == tagName);

        if (vt != null)
        {
            if (vt.iconSprite != null)
                img.sprite = vt.iconSprite;

            // ✅ ใส่สีตาม tag พร้อม alpha ตาม intensity
            Color c = vt.iconColor;
            c.a = Mathf.Clamp01(intensity);
            img.color = c;
        }
        else
        {
            // fallback: สีขาวถ้าไม่พบ tag
            img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(intensity));
        }
    }

    int GetAvailableSlot()
    {
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].activeSelf) return i;
        }
        return -1;
    }

    int GetOldestSlot()
    {
        int oldest = 0;
        float minTime = float.MaxValue;

        for (int i = 0; i < _pool.Count; i++)
        {
            if (_timers.TryGetValue(_pool[i], out float t) && t < minTime)
            {
                minTime = t;
                oldest = i;
            }
        }
        return oldest;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Gizmos
    // ════════════════════════════════════════════════════════════

    void OnDrawGizmos()
    {
        if (radialLayout == null || player == null) return;
        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        Gizmos.DrawWireSphere(player.position, radialLayout.radius / 100f);
    }

    #endregion
}