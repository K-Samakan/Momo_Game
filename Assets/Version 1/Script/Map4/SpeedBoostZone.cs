using UnityEngine;
using System.Collections;

/// <summary>
/// Speed Boost แบบโค้ง - คำนวณทิศทางจาก Surface Normal
/// </summary>
[RequireComponent(typeof(MeshCollider))]
public class SpeedBoostZoneCurved : MonoBehaviour
{
    [Header("=== Boost Settings ===")]
    [SerializeField] private float boostForce = 20f;
    [SerializeField] private float boostDuration = 0.5f;
    [SerializeField] private bool useNormalDirection = true;  // ใช้ทิศ Normal ของ Mesh
    [SerializeField] private Vector3 additionalDirection = Vector3.up; // เพิ่มทิศพิเศษ

    [Header("=== Cooldown ===")]
    [SerializeField] private float cooldownTime = 2f;
    
    [Header("=== Effects ===")]
    [SerializeField] private ParticleSystem boostParticles;
    [SerializeField] private AudioClip boostSound;
    [SerializeField] private bool enableHaptics = true;
    [SerializeField] private float hapticIntensity = 0.5f;

    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    private bool isReady = true;
    private MeshCollider meshCollider;
    private Vector3 lastHitNormal;
    private Vector3 lastHitPoint;

    private void Awake()
    {
        meshCollider = GetComponent<MeshCollider>();
        if (meshCollider != null)
        {
            meshCollider.convex = true;
            meshCollider.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (!isReady) return;

        SwimmingControllerHybrid swimmer = other.GetComponent<SwimmingControllerHybrid>();
        if (swimmer == null || !swimmer.IsSwimming()) return;

        // คำนวณทิศทางจาก Raycast
        Vector3 boostDirection = CalculateBoostDirection(other.transform.position);
        
        ApplyBoost(other.gameObject, boostDirection);
    }

    private Vector3 CalculateBoostDirection(Vector3 playerPosition)
    {
        if (!useNormalDirection)
        {
            return additionalDirection.normalized;
        }

        // Raycast หา Surface Normal
        RaycastHit hit;
        Vector3 direction = (transform.position - playerPosition).normalized;
        
        if (Physics.Raycast(playerPosition, direction, out hit, 5f))
        {
            lastHitNormal = hit.normal;
            lastHitPoint = hit.point;

            // ใช้ Normal + Additional Direction
            Vector3 finalDirection = (hit.normal + additionalDirection).normalized;

            if (showDebugLogs)
            {
                Debug.Log($"🌀 Surface Normal: {hit.normal} → Boost Direction: {finalDirection}");
            }

            return finalDirection;
        }

        // Fallback
        return additionalDirection.normalized;
    }

    private void ApplyBoost(GameObject player, Vector3 direction)
    {
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller == null) return;

        StartCoroutine(BoostRoutine(controller, direction));
        PlayEffects();
        
        if (enableHaptics)
        {
            TriggerHaptic(player);
        }

        isReady = false;
        StartCoroutine(CooldownRoutine());

        if (showDebugLogs)
        {
            Debug.Log($"🚀 Boosted in direction: {direction} with force {boostForce}");
        }
    }

    private IEnumerator BoostRoutine(CharacterController controller, Vector3 direction)
    {
        float timer = 0f;
        Vector3 boostVelocity = direction * boostForce;

        while (timer < boostDuration)
        {
            timer += Time.deltaTime;
            float t = 1f - (timer / boostDuration);

            controller.Move(boostVelocity * t * Time.deltaTime);
            yield return null;
        }
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownTime);
        isReady = true;
    }

    private void PlayEffects()
    {
        if (boostParticles != null)
        {
            boostParticles.Play();
        }

        if (boostSound != null)
        {
            AudioSource.PlayClipAtPoint(boostSound, transform.position, 0.7f);
        }
    }

    private void TriggerHaptic(GameObject player)
    {
        MOMO.Player.PlayerHealth health = player.GetComponent<MOMO.Player.PlayerHealth>();
        if (health != null)
        {
            health.TestVibration(hapticIntensity, 0.2f);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        // Draw Mesh Collider
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        if (meshCollider != null && meshCollider.sharedMesh != null)
        {
            Gizmos.DrawWireMesh(meshCollider.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
        }

        // Draw Last Hit Normal
        if (lastHitNormal != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(lastHitPoint, lastHitNormal * 2f);
        }
    }
}