using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Speed Boost Pad - แบบเกมแข่งรถ
/// โดนส่วนไหนก็ดูดเข้าเส้น → พุ่งไปตามแผ่น → Launch ออกปลายทาง
/// </summary>
public class SpeedBoostPad : MonoBehaviour
{
    [Header("=== Path Settings ===")]
    [SerializeField] private Transform startPoint;        // จุดเริ่มต้นแผ่น
    [SerializeField] private Transform endPoint;          // จุดสิ้นสุดแผ่น
    [SerializeField] private List<Transform> midPoints = new List<Transform>(); // จุดกลาง (โค้ง)
    
    [Header("=== Speed Settings ===")]
    [SerializeField] private float boostSpeed = 25f;      // ความเร็วบนแผ่น (คงที่)
    [SerializeField] private float snapSpeed = 20f;       // ความเร็วดูดเข้าเส้น
    [SerializeField] private float snapDuration = 0.2f;   // เวลาดูดเข้าเส้น
    
    [Header("=== Launch Settings ===")]
    [SerializeField] private float launchForce = 30f;     // แรงพุ่งออก
    [SerializeField] private Vector3 launchAngle = new Vector3(0, 45, 0); // มุมพุ่ง
    [SerializeField] private bool usePadDirection = true; // ใช้ทิศแผ่น
    
    [Header("=== Path Generation ===")]
    [SerializeField] private int pathResolution = 50;     // ความละเอียดเส้น
    [SerializeField] [Range(0f, 1f)] private float curvature = 0.5f; // ความโค้ง
    
    [Header("=== Visual ===")]
    [SerializeField] private LineRenderer pathLine;
    [SerializeField] private Color normalColor = Color.cyan;
    [SerializeField] private Color activeColor = Color.yellow;
    [SerializeField] private float lineWidth = 0.8f;
    
    [Header("=== Effects ===")]
    [SerializeField] private ParticleSystem enterEffect;
    [SerializeField] private ParticleSystem trailEffect;
    [SerializeField] private ParticleSystem launchEffect;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip boostSound;
    [SerializeField] private AudioClip launchSound;
    
    [Header("=== Haptic ===")]
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private float enterHapticIntensity = 0.4f;
    [SerializeField] private float launchHapticIntensity = 0.7f;
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    // Generated Path
    private List<Vector3> pathPoints = new List<Vector3>();
    private List<Vector3> pathDirections = new List<Vector3>();
    private float totalPathLength = 0f;

    // State
    private GameObject activePlayer;
    private bool isPlayerActive = false;

    // Components
    private AudioSource audioSource;
    private BoxCollider triggerCollider;

    #region Initialization

    private void Awake()
    {
        SetupComponents();
    }

    private void Start()
    {
        GeneratePath();
        SetupVisuals();
    }

    private void SetupComponents()
    {
        // Audio Source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        // Trigger Collider
        triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider == null)
        {
            triggerCollider = gameObject.AddComponent<BoxCollider>();
        }
        triggerCollider.isTrigger = true;

        // Auto-find points ถ้าไม่ได้ตั้งค่า
        if (startPoint == null || endPoint == null)
        {
            AutoFindPoints();
        }
    }

    private void AutoFindPoints()
    {
        Transform[] children = GetComponentsInChildren<Transform>();
        
        foreach (Transform child in children)
        {
            if (child == transform) continue;

            if (child.name.Contains("Start") && startPoint == null)
            {
                startPoint = child;
            }
            else if (child.name.Contains("End") && endPoint == null)
            {
                endPoint = child;
            }
            else if (child.name.Contains("Mid") || child.name.Contains("Point"))
            {
                if (!midPoints.Contains(child))
                {
                    midPoints.Add(child);
                }
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[SpeedBoostPad] Auto-found: Start={startPoint?.name}, End={endPoint?.name}, Mid Points={midPoints.Count}");
        }
    }

    private void GeneratePath()
    {
        pathPoints.Clear();
        pathDirections.Clear();
        totalPathLength = 0f;

        if (startPoint == null || endPoint == null)
        {
            Debug.LogError("[SpeedBoostPad] Start/End points not assigned!");
            return;
        }

        // สร้างเส้นทาง
        List<Vector3> controlPoints = new List<Vector3> { startPoint.position };
        controlPoints.AddRange(midPoints.ConvertAll(t => t.position));
        controlPoints.Add(endPoint.position);

        // Generate smooth path
        for (int i = 0; i < pathResolution; i++)
        {
            float t = i / (float)(pathResolution - 1);
            Vector3 point = GetPointOnPath(controlPoints, t);
            pathPoints.Add(point);

            // คำนวณทิศทาง
            if (i < pathResolution - 1)
            {
                float tNext = (i + 1) / (float)(pathResolution - 1);
                Vector3 nextPoint = GetPointOnPath(controlPoints, tNext);
                Vector3 direction = (nextPoint - point).normalized;
                pathDirections.Add(direction);
            }
            else
            {
                // จุดสุดท้ายใช้ทิศเดียวกับจุดก่อนหน้า
                pathDirections.Add(pathDirections[pathDirections.Count - 1]);
            }

            // คำนวณระยะทาง
            if (i > 0)
            {
                totalPathLength += Vector3.Distance(pathPoints[i - 1], point);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"✅ [SpeedBoostPad] Generated {pathPoints.Count} points, Length: {totalPathLength:F2}m");
        }
    }

    private Vector3 GetPointOnPath(List<Vector3> points, float t)
    {
        if (points.Count == 2)
        {
            // Straight line
            return Vector3.Lerp(points[0], points[1], t);
        }
        else if (points.Count == 3)
        {
            // Quadratic Bezier
            float u = 1f - t;
            return u * u * points[0] + 
                   2f * u * t * points[1] + 
                   t * t * points[2];
        }
        else if (points.Count >= 4)
        {
            // Cubic Bezier (ใช้ 4 จุดแรก)
            float u = 1f - t;
            return u * u * u * points[0] + 
                   3f * u * u * t * points[1] + 
                   3f * u * t * t * points[2] + 
                   t * t * t * points[3];
        }

        return points[0];
    }

    private void SetupVisuals()
    {
        if (pathLine == null) return;

        pathLine.positionCount = pathPoints.Count;
        pathLine.startWidth = lineWidth;
        pathLine.endWidth = lineWidth;
        pathLine.startColor = normalColor;
        pathLine.endColor = normalColor;

        for (int i = 0; i < pathPoints.Count; i++)
        {
            pathLine.SetPosition(i, pathPoints[i]);
        }
    }

    #endregion

    #region Trigger

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (isPlayerActive) return;

        ActivateBoost(other.gameObject);
    }

    #endregion

    #region Boost Logic

    private void ActivateBoost(GameObject player)
    {
        activePlayer = player;
        isPlayerActive = true;

        // เช็คสถานะ (ว่ายน้ำหรือเดิน)
        SwimmingControllerHybrid swimmer = player.GetComponent<SwimmingControllerHybrid>();
        bool wasSwimming = swimmer != null && swimmer.IsSwimming();

        // เริ่ม Boost
        BoostController controller = player.GetComponent<BoostController>();
        if (controller == null)
        {
            controller = player.AddComponent<BoostController>();
        }

        controller.StartBoost(
            pathPoints,
            pathDirections,
            boostSpeed,
            snapSpeed,
            snapDuration,
            wasSwimming
        );

        // Subscribe to completion event
        controller.OnBoostComplete += OnPlayerBoostComplete;

        // Effects
        PlayEnterEffects();

        if (showDebugLogs)
        {
            Debug.Log($"🚀 [SpeedBoostPad] Player entered boost | Was Swimming: {wasSwimming}");
        }
    }

    private void OnPlayerBoostComplete(bool wasSwimming)
    {
        if (activePlayer == null) return;

        // Launch
        LaunchPlayer(wasSwimming);

        // Cleanup
        BoostController controller = activePlayer.GetComponent<BoostController>();
        if (controller != null)
        {
            controller.OnBoostComplete -= OnPlayerBoostComplete;
        }

        activePlayer = null;
        isPlayerActive = false;

        if (showDebugLogs)
        {
            Debug.Log($"🎯 [SpeedBoostPad] Player completed boost");
        }
    }

    private void LaunchPlayer(bool returnToSwimming)
    {
        CharacterController cc = activePlayer.GetComponent<CharacterController>();
        if (cc == null) return;

        // คำนวณทิศ Launch
        Vector3 launchDir;
        if (usePadDirection && pathDirections.Count > 0)
        {
            launchDir = pathDirections[pathDirections.Count - 1];
            
            // เพิ่มมุมยก
            Quaternion angleRotation = Quaternion.Euler(launchAngle);
            launchDir = angleRotation * launchDir;
        }
        else
        {
            launchDir = Quaternion.Euler(launchAngle) * Vector3.forward;
        }

        // Apply Launch
        BoostController controller = activePlayer.GetComponent<BoostController>();
        if (controller != null)
        {
            controller.Launch(launchDir, launchForce, returnToSwimming);
        }

        // Effects
        PlayLaunchEffects();

        if (showDebugLogs)
        {
            Debug.Log($"💥 [SpeedBoostPad] Launched player: {launchDir} x {launchForce}");
        }
    }

    #endregion

    #region Effects

    private void PlayEnterEffects()
    {
        // Particles
        if (enterEffect != null)
        {
            enterEffect.Play();
        }

        if (trailEffect != null)
        {
            trailEffect.Play();
        }

        // Sound
        if (enterSound != null)
        {
            audioSource.PlayOneShot(enterSound, 0.7f);
        }

        if (boostSound != null)
        {
            audioSource.clip = boostSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Visual
        if (pathLine != null)
        {
            pathLine.startColor = activeColor;
            pathLine.endColor = activeColor;
        }

        // Haptic
        if (enableHaptics && activePlayer != null)
        {
            TriggerHaptic(activePlayer, enterHapticIntensity, 0.2f);
        }
    }

    private void PlayLaunchEffects()
    {
        // Particles
        if (launchEffect != null && activePlayer != null)
        {
            launchEffect.transform.position = activePlayer.transform.position;
            launchEffect.Play();
        }

        if (trailEffect != null)
        {
            trailEffect.Stop();
        }

        // Sound
        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (launchSound != null && activePlayer != null)
        {
            AudioSource.PlayClipAtPoint(launchSound, activePlayer.transform.position, 0.8f);
        }

        // Visual
        if (pathLine != null)
        {
            pathLine.startColor = normalColor;
            pathLine.endColor = normalColor;
        }

        // Haptic
        if (enableHaptics && activePlayer != null)
        {
            TriggerHaptic(activePlayer, launchHapticIntensity, 0.3f);
        }
    }

    private void TriggerHaptic(GameObject player, float intensity, float duration)
    {
        MOMO.Player.PlayerHealth health = player.GetComponent<MOMO.Player.PlayerHealth>();
        if (health != null)
        {
            health.TestVibration(intensity, duration);
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw Control Points
        Gizmos.color = Color.yellow;
        if (startPoint != null)
        {
            Gizmos.DrawWireSphere(startPoint.position, 0.3f);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(startPoint.position + Vector3.up * 0.5f, "START");
            #endif
        }

        if (endPoint != null)
        {
            Gizmos.DrawWireSphere(endPoint.position, 0.3f);
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(endPoint.position + Vector3.up * 0.5f, "END");
            #endif
        }

        Gizmos.color = Color.green;
        for (int i = 0; i < midPoints.Count; i++)
        {
            if (midPoints[i] != null)
            {
                Gizmos.DrawWireSphere(midPoints[i].position, 0.2f);
            }
        }

        // Draw Generated Path
        if (pathPoints.Count > 1)
        {
            Gizmos.color = normalColor;
            for (int i = 0; i < pathPoints.Count - 1; i++)
            {
                Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);

                // Draw Arrows
                if (i % 5 == 0 && i < pathDirections.Count)
                {
                    Vector3 arrowStart = pathPoints[i];
                    Vector3 arrowEnd = arrowStart + pathDirections[i] * 0.5f;
                    Gizmos.DrawRay(arrowStart, pathDirections[i] * 0.5f);
                }
            }
        }
    }

    #endregion
}