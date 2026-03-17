using UnityEngine;
using UnityEngine.InputSystem;

public class RepairButton : MonoBehaviour
{
    [Header("References")]
    public WaterValvePuzzleManager puzzleManager;
    
    private bool playerInRange = false;
    private InputAction interactAction;

    void Start()
    {
        // Get interact action from Puzzle4 ActionMap
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            var puzzle4Map = playerInput.actions.FindActionMap("Puzzle4");
            if (puzzle4Map != null)
            {
                interactAction = puzzle4Map.FindAction("InteractValve");
                if (interactAction != null)
                {
                    interactAction.performed += OnInteract;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteract;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║     🔧 REPAIR STATION                     ║");
            Debug.Log("║     Press ⭕ Circle to repair ALL broken  ║");
            Debug.Log("║     valves (hearts NOT restored)          ║");
            Debug.Log("╚═══════════════════════════════════════════╝");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        if (playerInRange && puzzleManager != null)
        {
            Debug.Log("[⭕ Repair Button] Repairing all broken valves...");
            puzzleManager.RepairAllBrokenValves();
        }
    }
}