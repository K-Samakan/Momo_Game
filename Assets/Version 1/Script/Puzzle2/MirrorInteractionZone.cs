using UnityEngine;

public class MirrorInteractionZone : MonoBehaviour
{
    [HideInInspector]
    public InteractionMirror parentMirror;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(parentMirror.playerTag))
        {
            Debug.Log($"🚶‍♂️ Player entered {parentMirror.gameObject.name} interaction zone");
            MirrorInteractionManager.Instance.SetActiveMirror(parentMirror);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(parentMirror.playerTag))
        {
            Debug.Log($"🚶‍♂️ Player left {parentMirror.gameObject.name} interaction zone");
            MirrorInteractionManager.Instance.ClearActiveMirror(parentMirror);
        }
    }
}