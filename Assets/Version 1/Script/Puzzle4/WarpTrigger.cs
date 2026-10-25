using UnityEngine;
using UnityEngine.InputSystem;

public class WarpTrigger : MonoBehaviour
{
    [Header("References")]
    public WaterValvePuzzleManager puzzleManager;

    [Header("UI")]
    public GameObject promptUI;   // Canvas_Interaction: "Press ⭕ : Enter puzzle"

    [Header("Input Action Name")]
    public string interactActionName = "Puzzle4";

    private bool playerInRange = false;
    private PlayerInput playerInput;
    private InputAction interactAction;

    void Start()
    {
        // ซ่อนป้ายตอนเริ่ม
        if (promptUI != null) promptUI.SetActive(false);

        playerInput = FindFirstObjectByType<PlayerInput>();

        if (playerInput != null)
        {
            var playerMap = playerInput.actions.FindActionMap("Player");
            if (playerMap != null)
            {
                interactAction = playerMap.FindAction(interactActionName);
                if (interactAction != null)
                {
                    interactAction.performed += OnPlayerInteract;
                    Debug.Log($"[WarpTrigger] ✅ Found '{interactActionName}' action");
                }
                else
                {
                    Debug.LogError($"[WarpTrigger] ❌ '{interactActionName}' action not found");
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
            if (promptUI != null) promptUI.SetActive(true);

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
            if (promptUI != null) promptUI.SetActive(false);
            Debug.Log("[Warp Trigger] Left puzzle entrance area");
        }
    }

    void OnPlayerInteract(InputAction.CallbackContext context)
    {
        if (playerInRange && puzzleManager != null)
        {
            Debug.Log("[⭕ Circle] Warping to puzzle area...");

            // ซ่อนป้ายทันทีตอนวาป (กันป้ายค้างถ้า OnTriggerExit ไม่ทำงานตอนเทเลพอร์ต)
            playerInRange = false;
            if (promptUI != null) promptUI.SetActive(false);

            puzzleManager.EnterPuzzle();
        }
    }
}