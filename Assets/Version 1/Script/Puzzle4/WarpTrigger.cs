using UnityEngine;
using UnityEngine.InputSystem;

public class WarpTrigger : MonoBehaviour
{
    [Header("References")]
    public WaterValvePuzzleManager puzzleManager;
    //public MirrorInteractionManager puzzle2Manager;
    
    [Header("Input Action Name")]
    public string interactActionName = "InteractValve"; // ✅ เปลี่ยนเป็น "InteractValve"
    
    private bool playerInRange = false;
    private PlayerInput playerInput;
    private InputAction interactAction;

    void Start()
    {
        // Find player input component
        playerInput = FindObjectOfType<PlayerInput>();
        
        if (playerInput != null)
        {
            // Get interact action from Player ActionMap
            var playerMap = playerInput.actions.FindActionMap("Player");
            if (playerMap != null)
            {
                interactAction = playerMap.FindAction(interactActionName);
                if (interactAction != null)
                {
                    interactAction.performed += OnPlayerInteract;
                    Debug.Log($"[WarpTrigger] ✅ Found '{interactActionName}' action in Player ActionMap");
                }
                else
                {
                    Debug.LogError($"[WarpTrigger] ❌ '{interactActionName}' action not found in Player ActionMap");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnPlayerInteract;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            
            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║     🚪 PUZZLE ENTRANCE                    ║");
            Debug.Log("║     Press ⭕ Circle to enter puzzle       ║");
            Debug.Log("╚═══════════════════════════════════════════╝");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("[Warp Trigger] Left puzzle entrance area");
        }
    }

    void OnPlayerInteract(InputAction.CallbackContext context)
    {
        if (playerInRange && puzzleManager != null)
        {
            Debug.Log("[⭕ Circle] Warping to puzzle area...");
            
            // Enter puzzle area
            puzzleManager.EnterPuzzle();
        }

        
    }
}