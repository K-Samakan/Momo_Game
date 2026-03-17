using UnityEngine;

public class State : MonoBehaviour
{
    public virtual string SaveState()
    {
        return null;
    }

    public virtual void LoadState(string lodedJSON)
    {

    }
    
    
}
