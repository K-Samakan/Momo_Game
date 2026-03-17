using UnityEngine;
using System.Collections;

/// <summary>
/// ระบบน้ำวนที่ดูดผู้เล่นลงหลุม (ใช้ Capsule Collider)
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class WhirlpoolZone : MonoBehaviour
{
    [Header("=== Whirlpool Settings ===")]
    [SerializeField] private Transform centerPoint;
    [SerializeField] private float radius = 10f;
    [SerializeField] private float depth = 20f;
    [SerializeField] private float pullForce = 5f;
    [SerializeField] private float downwardForce = 3f;
    [SerializeField] private AnimationCurve forceCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("=== Rotation Settings ===")]
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private float rotationSpeed = 30f;
    [SerializeField] private bool clockwise = true;

    [Header("=== Speed Reduction ===")]
    [SerializeField] private float swimSpeedMultiplier = 0.5f;
    [SerializeField] private float escapeThreshold = 0.8f;

    [Header("=== Depth Force (ยิ่งลึกยิ่งแรง) ===")]
    [SerializeField] private bool increaseForceWithDepth = true;
    [SerializeField] private float depthForceMultiplier = 1.5f;

    [Header("=== Visual Effects ===")]
    [SerializeField] private ParticleSystem whirlpoolParticles;
    [SerializeField] private GameObject visualMesh;
    [SerializeField] private float visualRotationSpeed = 20f;

    [Header("=== Audio ===")]
    [SerializeField] private AudioSource whirlpoolSound;
    [SerializeField] private AudioClip enterSound;
    [SerializeField] private AudioClip loopSound;

    [Header("=== Haptic Feedback ===")]
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private float hapticIntensity = 0.2f;
    [SerializeField] private float hapticPulseInterval = 0.5f;

    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    private CapsuleCollider capsuleCollider;
    private System.Collections.Generic.List<GameObject> playersInZone = new System.Collections.Generic.List<GameObject>();
    private float hapticTimer = 0f;

    #region Initialization

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        capsuleCollider.isTrigger = true;
        capsuleCollider.radius = radius;
        capsuleCollider.height = depth;
        capsuleCollider.direction = 1;
        //capsuleCollider.center = new Vector3(0, -depth / 2f, 0);

        if (centerPoint == null)
        {
            Transform center = transform.Find("CenterPoint");
            if (center != null)
            {
                centerPoint = center;
            }
            else
            {
                GameObject centerObj = new GameObject("CenterPoint");
                centerObj.transform.parent = transform;
                centerObj.transform.localPosition = Vector3.zero;
                centerPoint = centerObj.transform;
            }
        }

        if (whirlpoolSound == null)
        {
            whirlpoolSound = GetComponent<AudioSource>();
            if (whirlpoolSound == null)
            {
                whirlpoolSound = gameObject.AddComponent<AudioSource>();
                whirlpoolSound.playOnAwake = false;
                whirlpoolSound.loop = true;
                whirlpoolSound.spatialBlend = 1f;
            }
        }
    }

    private void Start()
    {
        if (loopSound != null && whirlpoolSound != null)
        {
            whirlpoolSound.clip = loopSound;
            whirlpoolSound.Play();
        }

        if (whirlpoolParticles != null)
        {
            whirlpoolParticles.Play();
        }
    }

    #endregion

    #region Update

    private void Update()
    {
        if (visualMesh != null)
        {
            float rotation = visualRotationSpeed * Time.deltaTime;
            visualMesh.transform.Rotate(Vector3.up, rotation);
        }

        if (enableHaptics && playersInZone.Count > 0)
        {
            hapticTimer += Time.deltaTime;
            if (hapticTimer >= hapticPulseInterval)
            {
                hapticTimer = 0f;
                TriggerHapticForAllPlayers();
            }
        }
    }

    private void FixedUpdate()
    {
        for (int i = playersInZone.Count - 1; i >= 0; i--)
        {
            if (playersInZone[i] == null)
            {
                playersInZone.RemoveAt(i);
                continue;
            }

            ApplyWhirlpoolForce(playersInZone[i]);
        }
    }

    #endregion

    #region Trigger Events

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        AddPlayer(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        AddPlayer(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RemovePlayer(other.gameObject);
    }

    private void AddPlayer(GameObject player)
    {
        if (playersInZone.Contains(player)) return;

        playersInZone.Add(player);
        OnPlayerEnterWhirlpool(player);
        Debug.Log($"🌀 Player added: {playersInZone.Count} total");
    }

    private void RemovePlayer(GameObject player)
    {
        if (!playersInZone.Contains(player)) return;

        playersInZone.Remove(player);
        OnPlayerExitWhirlpool(player);
        Debug.Log($"🏊 Player removed: {playersInZone.Count} remaining");
    }

    #endregion

    #region Whirlpool Logic

    private void OnPlayerEnterWhirlpool(GameObject player)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🌀 [Whirlpool] Player entered whirlpool zone");
        }

        SwimmingControllerHybrid swimmer = player.GetComponent<SwimmingControllerHybrid>();
        if (swimmer != null)
        {
            swimmer.EnterWhirlpool(swimSpeedMultiplier);
        }

        if (enterSound != null && whirlpoolSound != null)
        {
            whirlpoolSound.PlayOneShot(enterSound, 0.7f);
        }

        if (enableHaptics)
        {
            TriggerHaptic(player, hapticIntensity, 0.3f);
        }
    }

    private void OnPlayerExitWhirlpool(GameObject player)
    {
        if (showDebugLogs)
        {
            Debug.Log($"🏊 [Whirlpool] Player escaped whirlpool");
        }

        SwimmingControllerHybrid swimmer = player.GetComponent<SwimmingControllerHybrid>();
        if (swimmer != null)
        {
            swimmer.ExitWhirlpool();
        }
    }

    private void ApplyWhirlpoolForce(GameObject player)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null) return;

        SwimmingControllerHybrid swimmer = player.GetComponent<SwimmingControllerHybrid>();

        Vector3 playerPos = player.transform.position;
        Vector3 centerPos = centerPoint.position;

        float horizontalDistance = Vector3.Distance(
            new Vector3(playerPos.x, centerPos.y, playerPos.z),
            new Vector3(centerPos.x, centerPos.y, centerPos.z)
        );

        float normalizedHorizontalDistance = Mathf.Clamp01(horizontalDistance / radius);

        float playerDepth = centerPos.y - playerPos.y;
        float normalizedDepth = Mathf.Clamp01(playerDepth / depth);

        Vector3 horizontalDirection = new Vector3(
            centerPos.x - playerPos.x,
            0f,
            centerPos.z - playerPos.z
        ).normalized;

        float pullStrength = pullForce * forceCurve.Evaluate(1f - normalizedHorizontalDistance);

        if (increaseForceWithDepth)
        {
            pullStrength *= 1f + (normalizedDepth * depthForceMultiplier);
        }

        Vector3 pullVector = horizontalDirection * pullStrength;

        float downwardStrength = downwardForce * forceCurve.Evaluate(1f - normalizedHorizontalDistance);

        if (increaseForceWithDepth)
        {
            downwardStrength *= 1f + (normalizedDepth * depthForceMultiplier);
        }

        Vector3 downwardVector = Vector3.down * downwardStrength;

        Vector3 rotationVector = Vector3.zero;
        if (enableRotation && horizontalDistance > 0.1f)
        {
            Vector3 tangent = Vector3.Cross(Vector3.up, horizontalDirection);
            if (!clockwise) tangent = -tangent;

            float rotationStrength = rotationSpeed * (1f - normalizedHorizontalDistance) * 0.1f;
            rotationVector = tangent * rotationStrength;
        }

        Vector3 totalForce = pullVector + downwardVector + rotationVector;

        if (swimmer != null)
        {
            swimmer.ApplyWhirlpoolForce(totalForce);
        }
        else
        {
            controller.Move(totalForce * Time.fixedDeltaTime);
        }

        if (showDebugLogs && Time.frameCount % 120 == 0)
        {
            Debug.Log($"🌀 H-Dist: {horizontalDistance:F2}m | Depth: {playerDepth:F2}m | Pull: {pullStrength:F2} | Down: {downwardStrength:F2} | Total: {totalForce.magnitude:F2}");
        }
    }

    #endregion

    #region Haptics

    private void TriggerHaptic(GameObject player, float intensity, float duration)
    {
        MOMO.Player.PlayerHealth health = player.GetComponent<MOMO.Player.PlayerHealth>();
        if (health != null)
        {
            health.TestVibration(intensity, duration);
        }
    }

    private void TriggerHapticForAllPlayers()
    {
        foreach (GameObject player in playersInZone)
        {
            if (player != null)
            {
                TriggerHaptic(player, hapticIntensity, 0.1f);
            }
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector3 center = centerPoint != null ? centerPoint.position : transform.position;

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        DrawCircle(center, radius, Vector3.up);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        DrawCircle(center - Vector3.up * depth, radius, Vector3.up);

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 topPoint = center + direction * radius;
            Vector3 bottomPoint = (center - Vector3.up * depth) + direction * radius;

            Gizmos.DrawLine(topPoint, bottomPoint);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, 0.5f);
        Gizmos.DrawLine(center, center - Vector3.up * depth);

        Gizmos.color = Color.yellow;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 startPos = center + direction * radius;
            Vector3 midPos = center + direction * radius * 0.5f - Vector3.up * depth * 0.3f;
            Vector3 endPos = center - Vector3.up * depth * 0.5f;

            Gizmos.DrawLine(startPos, midPos);
            Gizmos.DrawLine(midPos, endPos);

            Vector3 arrowDir = (endPos - midPos).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, arrowDir) * 0.3f;
            Gizmos.DrawLine(endPos, endPos - arrowDir * 0.5f + right);
            Gizmos.DrawLine(endPos, endPos - arrowDir * 0.5f - right);
        }

        #if UNITY_EDITOR
        UnityEditor.Handles.Label(center + Vector3.up * 2f,
            $"🌀 Whirlpool\nRadius: {radius}m\nDepth: {depth}m");
        #endif
    }

    private void DrawCircle(Vector3 center, float radius, Vector3 normal)
    {
        int segments = 32;
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Quaternion.AngleAxis(0, normal) * Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i;
            Vector3 newPoint = center + Quaternion.AngleAxis(angle, normal) * Vector3.forward * radius;
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    #endregion
}