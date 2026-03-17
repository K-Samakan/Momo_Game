using UnityEngine;
using UnityEngine.InputSystem;

public class FullResetButton : MonoBehaviour
{
    [Header("References")]
    public WaterValvePuzzleManager puzzleManager;

    private bool playerInRange = false;
    private InputAction resetAction;

    void Start()
    {
        // Get reset action from Puzzle4 ActionMap
        PlayerInput playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
        {
            var puzzle4Map = playerInput.actions.FindActionMap("Puzzle4");
            if (puzzle4Map != null)
            {
                resetAction = puzzle4Map.FindAction("FullResetPuzzle");
                if (resetAction != null)
                {
                    resetAction.performed += OnReset;
                }
            }
        }
    }

    void OnDestroy()
    {
        if (resetAction != null)
        {
            resetAction.performed -= OnReset;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            Debug.Log("╔═══════════════════════════════════════════╗");
            Debug.Log("║     🔄 FULL RESET BUTTON                  ║");
            Debug.Log("║     Press ✕ X to reset entire puzzle     ║");
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

    void OnReset(InputAction.CallbackContext context)
    {
        if (playerInRange && puzzleManager != null)
        {
            Debug.Log("[✕ Full Reset Button] Resetting entire puzzle...");
            puzzleManager.FullResetPuzzle();
        }
    }
}