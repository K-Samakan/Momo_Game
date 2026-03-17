using UnityEngine;

public class WaterAnimatorTest : MonoBehaviour
{
    public Animator animator;

    private bool hasWater = false;

    void Update()
    {
        // W = เปิดน้ำ
        if (Input.GetKeyDown(KeyCode.W))
        {
            hasWater = true;
            animator.SetBool("HasWater", hasWater);
            Debug.Log("💧 WATER ON");
        }

        // S = เล่นย้อน (เฉพาะตอนมีน้ำ)
        if (Input.GetKeyDown(KeyCode.S))
        {
            if (hasWater)
            {
                hasWater = false;
                animator.SetBool("HasWater", hasWater);
                 
                Debug.Log("⏪ REVERSE / WATER OFF");
            }
            else
            {
                Debug.Log("ℹ️ Already no water");
            }
        }
    }
}
