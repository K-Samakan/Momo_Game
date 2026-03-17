using UnityEngine;

public class AirBubbleZone : MonoBehaviour
{
    [Header("=== Oxygen Recovery ===")]
    [SerializeField] private float oxygenRecoveryMultiplier = 2f;
    [SerializeField] private bool instantRecovery = false;
    
    [Header("=== Ground Settings ===")]
    [SerializeField] private Transform bubbleFloor;
    [SerializeField] private float gravityScale = 1f;
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugLogs = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SwimmingControllerHybrid swimmingController = other.GetComponent<SwimmingControllerHybrid>();
            MonoBehaviour thirdPersonController = null; // ← แก้: ใช้ MonoBehaviour
            Rigidbody rb = other.GetComponent<Rigidbody>();
            CharacterController cc = other.GetComponent<CharacterController>();

            // ← เพิ่ม: หา ThirdPersonController แบบ Dynamic
            Component[] components = other.GetComponents<MonoBehaviour>();
            foreach (Component comp in components)
            {
                if (comp.GetType().Name.Contains("ThirdPerson") || 
                    comp.GetType().Name.Contains("StarterAssets"))
                {
                    thirdPersonController = comp as MonoBehaviour;
                    break;
                }
            }

            if (swimmingController != null)
            {
                swimmingController.EnterAirBubble(oxygenRecoveryMultiplier, instantRecovery);

                if (showDebugLogs)
                {
                    Debug.Log($"🫧 Player entered Air Bubble! Oxygen recovery x{oxygenRecoveryMultiplier}");
                }
            }

            // สลับ Controller: ปิด Swimming, เปิด Ground
            if (swimmingController != null)
            {
                swimmingController.enabled = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"🚶 Swimming Controller disabled");
                }
            }

            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = true;
                
                if (showDebugLogs)
                {
                    Debug.Log($"🚶 {thirdPersonController.GetType().Name} enabled");
                }
            }

            // เปิด Gravity
            if (rb != null)
            {
                rb.useGravity = true;
                rb.isKinematic = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"⬇️ Gravity enabled (Rigidbody)");
                }
            }
            else if (cc != null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"⬇️ Using CharacterController gravity");
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SwimmingControllerHybrid swimmingController = other.GetComponent<SwimmingControllerHybrid>();

            if (swimmingController != null)
            {
                if (!instantRecovery)
                {
                    swimmingController.RecoverOxygenInAirBubble(Time.deltaTime);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SwimmingControllerHybrid swimmingController = other.GetComponent<SwimmingControllerHybrid>();
            MonoBehaviour thirdPersonController = null; // ← แก้: ใช้ MonoBehaviour
            Rigidbody rb = other.GetComponent<Rigidbody>();

            // ← เพิ่ม: หา ThirdPersonController แบบ Dynamic
            Component[] components = other.GetComponents<MonoBehaviour>();
            foreach (Component comp in components)
            {
                if (comp.GetType().Name.Contains("ThirdPerson") || 
                    comp.GetType().Name.Contains("StarterAssets"))
                {
                    thirdPersonController = comp as MonoBehaviour;
                    break;
                }
            }

            if (swimmingController != null)
            {
                swimmingController.ExitAirBubble();

                if (showDebugLogs)
                {
                    Debug.Log($"💧 Player left Air Bubble! Oxygen depleting...");
                }
            }

            // สลับ Controller: เปิด Swimming, ปิด Ground
            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"🏊 {thirdPersonController.GetType().Name} disabled");
                }
            }

            if (swimmingController != null)
            {
                swimmingController.enabled = true;
                swimmingController.ForceEnterWater();
                
                if (showDebugLogs)
                {
                    Debug.Log($"🏊 Swimming Controller enabled");
                }
            }

            // ปิด Gravity
            if (rb != null)
            {
                rb.useGravity = false;
                
                if (showDebugLogs)
                {
                    Debug.Log($"🔼 Gravity disabled (Rigidbody)");
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.matrix = transform.localToWorldMatrix;

        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }

        if (bubbleFloor != null)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            Gizmos.DrawCube(bubbleFloor.position, bubbleFloor.localScale);
        }
    }
}