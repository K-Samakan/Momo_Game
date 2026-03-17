using UnityEngine;

/// <summary>
/// โซนตายตรงก้นหลุมน้ำวน - ใช้ Box Collider
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class WhirlpoolDeathZone : MonoBehaviour
{
    [Header("=== Death Zone Settings ===")]
    [SerializeField] private Vector3 deathZoneSize = new Vector3(4f, 2f, 4f); // ขนาดโซนตาย (X, Y, Z)
    [SerializeField] private bool instantDeath = true;        // ตายทันที
    
    [Header("=== Visual ===")]
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private Color gizmoColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("=== Audio ===")]
    [SerializeField] private AudioClip deathSound;
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showDebugLogs = true;

    private BoxCollider boxCollider;

    #region Initialization

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = true;
        boxCollider.size = deathZoneSize;
        boxCollider.center = Vector3.zero;
    }

    private void OnValidate()
    {
        // อัปเดต Collider เมื่อแก้ไขใน Inspector
        if (boxCollider != null)
        {
            boxCollider.size = deathZoneSize;
        }
    }

    #endregion

    #region Trigger

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        KillPlayer(other.gameObject);
    }

    #endregion

    #region Death Logic

    private void KillPlayer(GameObject player)
    {
        if (showDebugLogs)
        {
            Debug.LogError($"💀 [WhirlpoolDeath] Player fell into whirlpool death zone!");
            Debug.LogError($"   Position: {player.transform.position}");
        }

        MOMO.Player.PlayerHealth health = player.GetComponent<MOMO.Player.PlayerHealth>();
        if (health != null)
        {
            if (instantDeath)
            {
                // ตายทันที
                health.currentHealth = 0;
                health.UpdateHealthUI();
            }
            else
            {
                // เสียเลือดทั้งหมด
                health.TakeDamage(health.maxHealth);
            }

            if (showDebugLogs)
            {
                Debug.LogError($"☠️ Player HP set to 0 (Whirlpool Death)");
            }
        }
        else
        {
            Debug.LogError("❌ PlayerHealth component not found!");
        }

        // Effects
        PlayDeathEffects(player);

        // Disable Player Input
        DisablePlayer(player);
    }

    private void PlayDeathEffects(GameObject player)
    {
        // Particles
        if (deathParticles != null)
        {
            deathParticles.transform.position = player.transform.position;
            deathParticles.Play();
        }

        // Sound
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, player.transform.position, 1f);
        }
    }

    private void DisablePlayer(GameObject player)
    {
        // Disable Swimming Controller
        SwimmingControllerHybrid swimmer = player.GetComponent<SwimmingControllerHybrid>();
        if (swimmer != null)
        {
            swimmer.enabled = false;
        }

        // Disable Input (optional)
        UnityEngine.InputSystem.PlayerInput playerInput = player.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = false;
        }

        if (showDebugLogs)
        {
            Debug.Log("🚫 Player controls disabled");
        }
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, deathZoneSize);

        // วาดกรอบ
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, deathZoneSize);
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        #if UNITY_EDITOR
        // Label พร้อมข้อมูล
        Vector3 labelPos = transform.position + Vector3.up * (deathZoneSize.y / 2f + 0.5f);
        
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.red;
        style.fontSize = 14;
        style.fontStyle = FontStyle.Bold;
        
        UnityEditor.Handles.Label(labelPos, 
            $"💀 DEATH ZONE\nSize: {deathZoneSize.x}x{deathZoneSize.y}x{deathZoneSize.z}",
            style
        );

        // วาดเส้นบอกขอบเขต
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Vector3 halfSize = deathZoneSize / 2f;
        
        // มุม 8 จุด
        Vector3[] corners = new Vector3[]
        {
            transform.TransformPoint(new Vector3(-halfSize.x, -halfSize.y, -halfSize.z)),
            transform.TransformPoint(new Vector3(halfSize.x, -halfSize.y, -halfSize.z)),
            transform.TransformPoint(new Vector3(halfSize.x, -halfSize.y, halfSize.z)),
            transform.TransformPoint(new Vector3(-halfSize.x, -halfSize.y, halfSize.z)),
            
            transform.TransformPoint(new Vector3(-halfSize.x, halfSize.y, -halfSize.z)),
            transform.TransformPoint(new Vector3(halfSize.x, halfSize.y, -halfSize.z)),
            transform.TransformPoint(new Vector3(halfSize.x, halfSize.y, halfSize.z)),
            transform.TransformPoint(new Vector3(-halfSize.x, halfSize.y, halfSize.z)),
        };

        // วาด X ตรงกลาง
        Gizmos.color = Color.red;
        Gizmos.DrawLine(corners[0], corners[6]); // มุมล่างซ้ายหน้า → มุมบนขวาหลัง
        Gizmos.DrawLine(corners[1], corners[7]); // มุมล่างขวาหน้า → มุมบนซ้ายหลัง
        Gizmos.DrawLine(corners[2], corners[4]); // มุมล่างขวาหลัง → มุมบนซ้ายหน้า
        Gizmos.DrawLine(corners[3], corners[5]); // มุมล่างซ้ายหลัง → มุมบนขวาหน้า
        #endif
    }

    #endregion
}