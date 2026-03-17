using UnityEngine;

public class Door : MonoBehaviour, Interactable
{

    [SerializeField] private string _promt;

    public string InteractionPrompt =>_promt;

    public bool Interact(Interactor interactor)
    {
        var inventory = interactor.GetComponent<Inventory>();

        if (inventory == null) return false;
        

        if (inventory.CorrectPuzzle)
        {
            Debug.Log("Opening door!");
            return true;
        }

        
        Debug.Log("Solve the Puzzle!");
        return false;

    }
}
   
