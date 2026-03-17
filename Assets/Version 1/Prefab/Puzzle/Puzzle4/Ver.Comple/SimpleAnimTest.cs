using UnityEngine;

public class SimpleAnimTest : MonoBehaviour
{
    public string stateName = "Water_Vertical_State";
    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
        Debug.Log("SimpleAnimTest Awake on " + gameObject.name + ", animator = " + animator);
    }

    void Start()
    {
        Debug.Log("SimpleAnimTest Start on " + gameObject.name + ", play " + stateName);
        animator.Play(stateName, 0, 0f);
    }
}