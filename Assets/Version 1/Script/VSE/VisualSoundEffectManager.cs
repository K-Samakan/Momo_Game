// Assets/Version 1/Script/VisualSoundEffectManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// VSE แบบ Spider-Sense — Ring ติดตาม MOMO บนหน้าจอ
/// icon โผล่บน Radial Ring รอบตัวละคร ชี้ทิศเสียง + แยกสีตาม tag
/// + Haptic ทิศทาง + Priority System (Enemy/Projectile > Warning > SideQuest)
/// + Monster 2 ตัว: ข้างใกล้เต็ม, ข้างไกลลด 50%
/// </summary>
public class VisualSoundEffectManager : MonoBehaviour
{
    public static VisualSoundEffectManager Instance;

    // ─── References ──────────────────────────────────────────────
    [Header("References")]
    public Transform player;
    public Transform cameraTransform;
    public Camera mainCamera;
    public GameObject visualPrefab;
    public RadialLayoutManual radialLayout;
    public RectTransform vseRoot;

    // ─── Settings ────────────────────────────────────────────────
    [Header("Settings")]
    public float displayTime = 2.5f;
    public float updateSpeed = 8f;
    public int poolSize = 10;
    public float screenEdgePadding = 80f;
    public float playerHeightOffset = 1.0f;

    // ─── Haptic ──────────────────────────────────────────────────
    [Header("Haptic")]
    public bool enableHaptic = true;
    [Range(0f, 1f)]
    public float hapticMinIntensity = 0.3f;

    // ─── Visual Types ────────────────────────────────────────────
    [System.Serializable]
    public class VisualType
    {
        public string tagName;
        public Sprite iconSprite;
        public Color iconColor = Color.white;
    }
    public System.Collections.Generic.List<VisualType> visualTypes
        = new System.Collections.Generic.List<VisualType>();

    // ─── Pool ────────────────────────────────────────────────────
    private System.Collections.Generic.List<GameObject> _pool = new System.Collections.Generic.List<GameObject>();
    private System.Collections.Generic.List<RectTransform> _poolRTs = new System.Collections.Generic.List<RectTransform>();
    private System.Collections.Generic.List<Image> _poolImages = new System.Collections.Generic.List<Image>();

    private Dictionary<GameObject, Vector3> _sources = new Dictionary<GameObject, Vector3>();
    private Dictionary<GameObject, float> _timers = new Dictionary<GameObject, float>();
    private Dictionary<GameObject, string> _tags = new Dictionary<GameObject, string>();

    private RectTransform _canvasRT;

    // ─── Haptic Tracking (per-frame) ─────────────────────────────
    private float _frameMaxLeft = 0f;
    private float _frameMaxRight = 0f;
    private string _frameTopTag = "";
    private int _currentActivePriority = 99;

    // Priority: น้อย = สำคัญมาก
    private static readonly Dictionary<string, int> TagPriority = new Dictionary<string, int>
    {
        { "Enemy",      0 },
        { "Projectile", 0 },
        { "Warning",    1 },
        { "SideQuest",  2 },
    };

    // ════════════════════════════════════════════════════════════
    #region Unity Lifecycle
    // ════════════════════════════════════════════════════════════

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (vseRoot != null) _canvasRT = vseRoot.parent as RectTransform;
        CreatePool();
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    void Update() => UpdateIcons();
    void LateUpdate() => MoveRingToPlayer();

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Spider-Sense Ring
    // ════════════════════════════════════════════════════════════

    void MoveRingToPlayer()
    {
        if (mainCamera == null || vseRoot == null || player == null) return;

        Vector3 worldPos = player.position + Vector3.up * playerHeightOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0f) return;

        screenPos.x = Mathf.Clamp(screenPos.x, screenEdgePadding, Screen.width - screenEdgePadding);
        screenPos.y = Mathf.Clamp(screenPos.y, screenEdgePadding, Screen.height - screenEdgePadding);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRT, screenPos, null, out Vector2 localPoint))
            vseRoot.anchoredPosition = localPoint;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Public API
    // ════════════════════════════════════════════════════════════

    public void ShowIcon(Vector3 sourceWorldPos, float intensity, string tagName = "")
    {
        if (visualPrefab == null || player == null ||
            cameraTransform == null || radialLayout == null)
        {
            Debug.LogWarning("[VSE] Reference ขาดหาย — ตรวจสอบ Inspector");
            return;
        }
        if (intensity <= 0f) return;

        int slot = GetAvailableSlot();
        if (slot < 0) slot = GetOldestSlot();

        GameObject go = _pool[slot];
        RectTransform rt = _poolRTs[slot];
        Image img = _poolImages[slot];

        ApplyVisualType(img, tagName, intensity);

        float baseScale = Mathf.Lerp(0.5f, 1.8f, intensity);
        rt.localScale = new Vector3(baseScale, baseScale, 1f);

        go.SetActive(true);
        _sources[go] = sourceWorldPos;
        _timers[go] = displayTime;
        _tags[go] = tagName;
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Update Loop
    // ════════════════════════════════════════════════════════════

    void UpdateIcons()
    {
        // ── Reset haptic tracking ────────────────────────────────
        _frameMaxLeft = 0f;
        _frameMaxRight = 0f;
        _frameTopTag = "";
        _currentActivePriority = 99;

        // ── Pass 1: หา Priority สูงสุดที่ active ────────────────
        for (int i = 0; i < _pool.Count; i++)
        {
            if (!_pool[i].activeSelf) continue;
            if (!_tags.TryGetValue(_pool[i], out string t)) continue;
            int pri = GetPriority(t);
            if (pri < _currentActivePriority) _currentActivePriority = pri;
        }

        // ── Pass 2: อัปเดตตำแหน่ง + สะสม haptic ─────────────────
        for (int i = 0; i < _pool.Count; i++)
        {
            GameObject go = _pool[i];
            if (!go.activeSelf) continue;
            if (!_sources.TryGetValue(go, out Vector3 source)) continue;
            if (!_timers.TryGetValue(go, out float timeLeft)) continue;
            if (!_tags.TryGetValue(go, out string tagName)) continue;

            RectTransform rt = _poolRTs[i];

            // ตำแหน่งบน Ring
            float angle = CalculateAngle(source);
            float angleRad = angle * Mathf.Deg2Rad;
            float radius = radialLayout.radius;
            Vector2 targetPos = new Vector2(Mathf.Sin(angleRad) * radius,
                                            Mathf.Cos(angleRad) * radius);

            rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos,
                                               Time.deltaTime * updateSpeed);
            rt.rotation = Quaternion.Euler(0f, 0f, -angle);

            Vector3 sc = rt.localScale;
            sc.x = (angle > 180f && angle < 360f) ? -Mathf.Abs(sc.x) : Mathf.Abs(sc.x);
            rt.localScale = sc;

            // ── สะสม Haptic ──────────────────────────────────────
            if (enableHaptic)
            {
                int pri = GetPriority(tagName);
                bool canHaptic = pri <= _currentActivePriority;
                // SideQuest จะไม่สั่นถ้ามี priority สูงกว่าอยู่

                if (canHaptic)
                {
                    float absX = Mathf.Abs(sc.x);
                    float intensity = Mathf.InverseLerp(0.5f, 1.8f, absX);
                    if (intensity >= hapticMinIntensity)
                        AccumulateHaptic(source, intensity, tagName);
                }
            }

            // ── นับถอยหลัง ───────────────────────────────────────
            timeLeft -= Time.deltaTime;
            _timers[go] = timeLeft;

            if (timeLeft <= 0f)
            {
                go.SetActive(false);
                _sources.Remove(go);
                _timers.Remove(go);
                _tags.Remove(go);
            }
        }

        // ── Pass 3: ส่ง Haptic 1 ครั้งต่อ frame ─────────────────
        if (enableHaptic && (_frameMaxLeft > 0f || _frameMaxRight > 0f))
            TriggerFinalHaptic(_frameMaxLeft, _frameMaxRight, _frameTopTag);
    }

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Haptic System
    // ════════════════════════════════════════════════════════════

    /// <summary>
    /// สะสม left/right intensity จากแต่ละ VSE
    /// ข้าง dominant = เต็ม / ข้างตรงข้าม = ×0.5
    /// </summary>
    void AccumulateHaptic(Vector3 sourceWorldPos, float intensity, string tagName)
    {
        if (player == null || cameraTransform == null) return;

        Vector3 dir = sourceWorldPos - player.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
        Vector3 localDir = cameraTransform.InverseTransformDirection(flatDir);
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        float absAngle = Mathf.Abs(angle);

        float rightW, leftW;

        if (absAngle < 30f)
        {
            // ตรงหน้า → เท่ากัน
            rightW = 0.5f; leftW = 0.5f;
        }
        else if (absAngle > 150f)
        {
            // ด้านหลัง → สั่นเต็มสองข้าง
            rightW = 1f; leftW = 1f;
        }
        else if (angle > 0f)
        {
            // ขวา
            rightW = Mathf.Clamp01(angle / 90f);
            leftW = 1f - rightW;
        }
        else
        {
            // ซ้าย
            leftW = Mathf.Clamp01(-angle / 90f);
            rightW = 1f - leftW;
        }

        float right = rightW * intensity;
        float left = leftW * intensity;

        // ✅ ข้าง dominant = เต็ม / ข้างรอง = ×0.5
        if (right >= left)
        {
            _frameMaxRight = Mathf.Max(_frameMaxRight, right);
            _frameMaxLeft = Mathf.Max(_frameMaxLeft, left * 0.5f);
        }
        else
        {
            _frameMaxLeft = Mathf.Max(_frameMaxLeft, left);
            _frameMaxRight = Mathf.Max(_frameMaxRight, right * 0.5f);
        }

        // เก็บ tag ของ VSE ที่ intensity สูงสุด
        float total = right + left;
        if (total > 0f && total >= (_frameMaxLeft + _frameMaxRight) * 0.5f)
            _frameTopTag = tagName;
    }

    /// <summary>
    /// ส่ง Haptic จริง 1 ครั้งต่อ frame
    /// </summary>
    void TriggerFinalHaptic(float left, float right, string tagName)
    {
        if (HapticManager.Instance == null) return;

        switch (tagName)
        {
            case "Enemy":
                HapticManager.Instance.Vibrate(left, right, 0.15f);
                break;

            case "Projectile":
                HapticManager.Instance.Vibrate(left * 0.8f, right * 0.8f, 0.08f);
                break;

            case "Warning":
                // Pattern จังหวะเตือน — ไม่ใช้ทิศทาง เพราะ Platform อยู่ใต้เท้า
                HapticManager.Instance.PlayPattern(HapticPatternType.Warning);
                break;

            case "SideQuest":
                // เบา ambient — ✅ จะถูกข้ามถ้ามี Enemy/Projectile/Warning อยู่
                HapticManager.Instance.Vibrate(left * 0.4f, right * 0.4f, 0.2f);
                break;

            default:
                HapticManager.Instance.Vibrate(left * 0.5f, right * 0.5f, 0.1f);
                break;
        }
    }

    int GetPriority(string tag)
        => TagPriority.TryGetValue(tag, out int p) ? p : 99;

    #endregion

    // ════════════════════════════════════════════════════════════
    #region Helpers
    // ════════════════════════════════════════════════════════════

    float CalculateAngle(Vector3 sourceWorldPos)
    {
        Vector3 dir = sourceWorldPos - player.position;
        Vector3 flatDir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
        if (flatDir == Vector3.zero) return 0f;
        Vector3 localDir = cameraTransform.InverseTransformDirection(flatDir);
        float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        return (angle + 360f) % 360f;
    }

    void ApplyVisualType(Image img, string tagName, float intensity)
    {
        if (img == null) return;
        VisualType vt = visualTypes.Find(v => v.tagName == tagName);
        if (vt != null)
        {
            if (vt.iconSprite != null) img.sprite = vt.iconSprite;
            Color c = vt.iconColor;
            c.a = Mathf.Clamp01(intensity);
            img.color = c;
        }
        else
        {
            img.color = new Color(1f, 1f, 1f, Mathf.Clamp01(intensity));
        }
    }

    void CreatePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = Instantiate(visualPrefab, transform);
            go.SetActive(false);
            _pool.Add(go);
            _poolRTs.Add(go.GetComponent<RectTransform>());
            _poolImages.Add(go.GetComponentInChildren<Image>());
        }
    }

    int GetAvailableSlot()
    {
        for (int i = 0; i < _pool.Count; i++)
            if (!_pool[i].activeSelf) return i;
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
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        Gizmos.DrawWireSphere(player.position, radialLayout.radius / 100f);
    }

    #endregion
}