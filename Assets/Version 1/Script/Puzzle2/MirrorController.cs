using UnityEngine;

public class MirrorController : MonoBehaviour
{
    private MirrorData mirrorData;
    private MirrorLightPuzzle puzzleManager;
    private Renderer mirrorRenderer;
    private Material originalMaterial;
    private Material highlightMaterial;
    
    [Header("Visual Feedback")]
    public Color realMirrorColor = Color.blue;
    public Color fakeMirrorColor = Color.red;
    public Color neutralColor = Color.gray;
    public Color highlightColor = Color.yellow;

    void Awake()
    {
        mirrorRenderer = GetComponent<Renderer>();
        if (mirrorRenderer)
        {
            originalMaterial = mirrorRenderer.material;
        }
    }

    public void Initialize(MirrorData data, MirrorLightPuzzle manager)
    {
        mirrorData = data;
        puzzleManager = manager;
        UpdateVisualState();
    }

    void UpdateVisualState()
    {
        if (!mirrorRenderer) return;

        Color targetColor = neutralColor;
        
        switch (mirrorData.state)
        {
            case MirrorState.Inactive:
                targetColor = neutralColor;
                break;
                
            case MirrorState.Active:
                targetColor = realMirrorColor;
                break;
                
            case MirrorState.Damaged:
                targetColor = fakeMirrorColor;
                gameObject.SetActive(false); // ซ่อนกระจกที่ระเบิดแล้ว
                break;
        }

        if (mirrorRenderer.material.color != targetColor)
        {
            mirrorRenderer.material.color = targetColor;
        }
    }

    void OnMouseEnter()
    {
        if (mirrorData.isInteractable && mirrorRenderer)
        {
            mirrorRenderer.material.color = highlightColor;
        }
    }

    void OnMouseExit()
    {
        if (mirrorData.isInteractable)
        {
            UpdateVisualState();
        }
    }

    public int GetMirrorIndex() => mirrorData.mirrorID;
    public MirrorData GetMirrorData() => mirrorData;
    public bool IsInteractable() => mirrorData.isInteractable;
}