using UnityEngine;
using UnityEngine.InputSystem;
public class SokobanTarget : MonoBehaviour
{
     [Header("Target Settings")]
    public Color normalColor = Color.green;
    public Color occupiedColor = Color.blue;
    public GameObject completeEffect;
    
    private Renderer targetRenderer;
    private bool isOccupied = false;
    
    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            targetRenderer.material.color = normalColor;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Block"))
        {
            isOccupied = true;
            if (targetRenderer != null)
            {
                targetRenderer.material.color = occupiedColor;
            }
            
            if (completeEffect != null)
            {
                completeEffect.SetActive(true);
            }
            
            // ตรวจสอบว่าผ่านด่านหรือยัง
            GamePuzzleManager2.Instance?.CheckLevelComplete();
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Block"))
        {
            isOccupied = false;
            if (targetRenderer != null)
            {
                targetRenderer.material.color = normalColor;
            }
            
            if (completeEffect != null)
            {
                completeEffect.SetActive(false);
            }
        }
    }
    
    public bool IsOccupied()
    {
        return isOccupied;
    }
}