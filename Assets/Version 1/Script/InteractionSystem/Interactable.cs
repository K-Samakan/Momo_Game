using UnityEngine;


public interface Interactable
{
    public string InteractionPrompt { get; }

    public bool Interact(Interactor interactor);
}
