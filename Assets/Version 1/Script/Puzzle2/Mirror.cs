using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class Mirror : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationAmount = 22.5f;
    public float holdDuration = 1f;
    
    [Header("Input Settings")]
    public InputActionAsset inputActions;
    
    private InputActionMap puzzleActionMap;
    private InputAction l2Action;
    private InputAction r2Action;
    
    private bool isRotating = false;
    private Coroutine currentRotationCoroutine;
    
   private void Awake()
{
    Debug.Log("=== Mirror Debug ===");
    
    if (inputActions == null)
    {
        Debug.LogError("Input Actions Asset is NULL! Please assign it in Inspector.");
        return;
    }
    
    puzzleActionMap = inputActions.FindActionMap("Puzzle2");
    
    if (puzzleActionMap == null)
    {
        Debug.LogError("Puzzle2 Action Map not found!");
        return;
    }
    
    l2Action = puzzleActionMap.FindAction("L2");
    r2Action = puzzleActionMap.FindAction("R2");
    
    if (l2Action == null) 
        Debug.LogError("L2 Action not found!");
    else
        Debug.Log("L2 Action found successfully!");
        
    if (r2Action == null) 
        Debug.LogError("R2 Action not found!");
    else
        Debug.Log("R2 Action found successfully!");
}

private void OnEnable()
{
    Debug.Log("OnEnable called");
    
    if (puzzleActionMap == null || l2Action == null || r2Action == null)
    {
        Debug.LogError("Actions not properly initialized! Cannot enable input.");
        return;
    }
    
    puzzleActionMap.Enable();
    Debug.Log("Action Map enabled");
    
    l2Action.started += OnL2Started;
    l2Action.canceled += OnL2Canceled;
    
    r2Action.started += OnR2Started;
    r2Action.canceled += OnR2Canceled;
    
    Debug.Log("Event subscriptions completed");
}

private void OnL2Started(InputAction.CallbackContext context)
{
    Debug.Log("🎮 L2 PRESSED!");
    
    if (!isRotating)
    {
        Debug.Log("Starting L2 rotation coroutine");
        currentRotationCoroutine = StartCoroutine(RotateAfterDelay(-rotationAmount));
    }
    else
    {
        Debug.Log("Already rotating, ignoring L2");
    }
}

private void OnL2Canceled(InputAction.CallbackContext context)
{
    Debug.Log("🎮 L2 RELEASED!");
    StopCurrentRotation();
}

private void OnR2Started(InputAction.CallbackContext context)
{
    Debug.Log("🎮 R2 PRESSED!");
    
    if (!isRotating)
    {
        Debug.Log("Starting R2 rotation coroutine");
        currentRotationCoroutine = StartCoroutine(RotateAfterDelay(rotationAmount));
    }
    else
    {
        Debug.Log("Already rotating, ignoring R2");
    }
}

private void OnR2Canceled(InputAction.CallbackContext context)
{
    Debug.Log("🎮 R2 RELEASED!");
    StopCurrentRotation();
}

private void StopCurrentRotation()
{
    Debug.Log("Stopping current rotation");
    
    if (currentRotationCoroutine != null)
    {
        StopCoroutine(currentRotationCoroutine);
        currentRotationCoroutine = null;
        isRotating = false;
        Debug.Log("Rotation stopped successfully");
    }
    else
    {
        Debug.Log("No rotation to stop");
    }
}

private IEnumerator RotateAfterDelay(float rotationY)
{
    Debug.Log($"🔄 Starting rotation sequence: {rotationY} degrees");
    isRotating = true;
    
    Debug.Log($"⏰ Waiting {holdDuration} seconds...");
    yield return new WaitForSeconds(holdDuration);
    
    Debug.Log("⏰ Wait completed! Rotating now...");
    
    Vector3 currentRotation = transform.eulerAngles;
    float newYRotation = currentRotation.y + rotationY;
    
    newYRotation = newYRotation % 360f;
    if (newYRotation < 0)
        newYRotation += 360f;
    
    Debug.Log($"🔄 Rotating from Y:{currentRotation.y} to Y:{newYRotation}");
    
    transform.rotation = Quaternion.Euler(currentRotation.x, newYRotation, currentRotation.z);
    
    Debug.Log($"✅ Rotation completed! New Y: {newYRotation}");
    
    isRotating = false;
    currentRotationCoroutine = null;
}
}