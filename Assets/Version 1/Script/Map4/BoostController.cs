using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ควบคุมผู้เล่นเมื่ออยู่บน Speed Boost Pad
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class BoostController : MonoBehaviour
{
    // Event
    public System.Action<bool> OnBoostComplete;

    // Components
    private CharacterController characterController;
    private SwimmingControllerHybrid swimmingController;
    private MonoBehaviour thirdPersonController;
    private Animator animator;

    // State
    private bool isActive = false;
    private bool wasSwimming = false;
    private List<Vector3> pathPoints;
    private List<Vector3> pathDirections;
    private int currentIndex = 0;
    private float currentSpeed;
    private float targetSpeed;

    // Animation Hashes
    private readonly int swimSpeedParam = Animator.StringToHash("SwimSpeed");
    private readonly int isSwimmingParam = Animator.StringToHash("IsSwimming");
    private readonly int speedParam = Animator.StringToHash("Speed");
    private readonly int motionSpeedParam = Animator.StringToHash("MotionSpeed");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        swimmingController = GetComponent<SwimmingControllerHybrid>();
        animator = GetComponent<Animator>();

        // Find Third Person Controller
        Component[] components = GetComponents<MonoBehaviour>();
        foreach (Component comp in components)
        {
            if (comp.GetType().Name.Contains("Controller") &&
                comp != this &&
                comp != swimmingController)
            {
                thirdPersonController = comp as MonoBehaviour;
                break;
            }
        }
    }

    public void StartBoost(
        List<Vector3> points,
        List<Vector3> directions,
        float speed,
        float snapSpeed,
        float snapDuration,
        bool swimming
    )
    {
        pathPoints = points;
        pathDirections = directions;
        targetSpeed = speed;
        wasSwimming = swimming;
        isActive = true;
        currentIndex = 0;
        currentSpeed = 0f;

        // ปิด Controllers
        if (swimmingController != null) swimmingController.enabled = false;
        if (thirdPersonController != null) thirdPersonController.enabled = false;

        // หาจุดใกล้ที่สุด
        currentIndex = FindClosestPointIndex();

        // Snap + Move
        StartCoroutine(SnapAndBoostRoutine(snapSpeed, snapDuration));

        Debug.Log($"[BoostController] Started boost at index {currentIndex}");
    }

    private int FindClosestPointIndex()
    {
        float closestDist = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < pathPoints.Count; i++)
        {
            float dist = Vector3.Distance(transform.position, pathPoints[i]);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    private IEnumerator SnapAndBoostRoutine(float snapSpeed, float snapDuration)
    {
        // Phase 1: Snap to path
        Vector3 targetPos = pathPoints[currentIndex];
        float snapTimer = 0f;

        while (snapTimer < snapDuration)
        {
            snapTimer += Time.deltaTime;
            float t = snapTimer / snapDuration;

            Vector3 newPos = Vector3.Lerp(transform.position, targetPos, t);
            characterController.Move(newPos - transform.position);

            yield return null;
        }

        // Phase 2: Move along path
        yield return BoostMovementRoutine();
    }

    private IEnumerator BoostMovementRoutine()
    {
        while (isActive && currentIndex < pathPoints.Count - 1)
        {
            // Accelerate
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

            // Move to next point
            Vector3 targetPos = pathPoints[currentIndex + 1];
            Vector3 direction = (targetPos - transform.position).normalized;

            characterController.Move(direction * currentSpeed * Time.deltaTime);

            // Rotate
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            // Update Animation
            UpdateAnimation();

            // Check if reached next point
            float dist = Vector3.Distance(transform.position, targetPos);
            if (dist < 0.5f)
            {
                currentIndex++;
            }

            yield return null;
        }

        // Complete
        CompleteBoost();
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float normalizedSpeed = Mathf.Clamp01(currentSpeed / 30f);

        animator.SetFloat(swimSpeedParam, normalizedSpeed);
        animator.SetFloat(speedParam, normalizedSpeed);
        animator.SetFloat(motionSpeedParam, normalizedSpeed * 2f);

        if (wasSwimming)
        {
            animator.SetBool(isSwimmingParam, true);
        }
    }

    private void CompleteBoost()
    {
        isActive = false;
        OnBoostComplete?.Invoke(wasSwimming);

        Debug.Log("[BoostController] Boost complete");
    }

    public void Launch(Vector3 direction, float force, bool returnToSwimming)
    {
        StartCoroutine(LaunchRoutine(direction, force, returnToSwimming));
    }

    private IEnumerator LaunchRoutine(Vector3 direction, float force, bool returnToSwimming)
    {
        float timer = 0f;
        float duration = 0.8f;
        Vector3 velocity = direction.normalized * force;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = 1f - (timer / duration);

            characterController.Move(velocity * t * Time.deltaTime);
            yield return null;
        }

        // ✅ เช็คว่ายังอยู่ในน้ำจริงๆ หรือไม่
        bool actuallyInWater = CheckIfStillInWater();

        // Restore Controller
        RestoreController(actuallyInWater);

        // Destroy self
        Destroy(this);
    }

    // ✅ Method ตรวจสอบว่ายังอยู่ในน้ำหรือไม่ (แบบละเอียด)
    private bool CheckIfStillInWater()
    {
        // วิธี 1: Raycast ลงไปหาน้ำ
        RaycastHit hit;
        bool hitWater = Physics.Raycast(
            transform.position + Vector3.up * 5f,
            Vector3.down,
            out hit,
            10f,
            LayerMask.GetMask("Water")
        );

        if (hitWater)
        {
            float waterSurfaceY = hit.point.y;
            float playerFeetY = transform.position.y;
            bool inWater = playerFeetY < waterSurfaceY;

            Debug.Log($"🔍 [BoostController] Raycast: {inWater} (Player: {playerFeetY:F2}, Surface: {waterSurfaceY:F2})");
            return inWater;
        }

        // วิธี 2: OverlapSphere (ขยายรัศมี)
        Collider[] waterColliders = Physics.OverlapSphere(
            transform.position + Vector3.up * (characterController.height * 0.5f),
            characterController.radius * 5f,
            LayerMask.GetMask("Water")
        );

        bool inWaterBackup = waterColliders.Length > 0;

        Debug.Log($"🔍 [BoostController] OverlapSphere: {inWaterBackup} (Found {waterColliders.Length} colliders)");
        
        return inWaterBackup;
    }

    private void RestoreController(bool returnToSwimming)
    {
        if (returnToSwimming)
        {
            // กลับสู่โหมดว่ายน้ำ
            if (swimmingController != null)
            {
                swimmingController.enabled = true;
            }

            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = false;
            }

            if (animator != null)
            {
                animator.SetBool(isSwimmingParam, true);
            }

            Debug.Log("🏊 [BoostController] Returned to Swimming");
        }
        else
        {
            // กลับสู่บนบก
            if (swimmingController != null)
            {
                // ✅ ออกจากน้ำก่อน (ถ้ายังอยู่)
                if (swimmingController.IsSwimming())
                {
                    swimmingController.ForceExitWater();
                    Debug.Log("🏖️ [BoostController] Force exited water");
                }
                
                swimmingController.enabled = false;
            }

            if (thirdPersonController != null)
            {
                thirdPersonController.enabled = true;
            }

            if (animator != null)
            {
                animator.SetBool(isSwimmingParam, false);
            }

            Debug.Log("🚶 [BoostController] Returned to Ground");
        }
    }

    private void OnDestroy()
    {
        if (isActive)
        {
            CompleteBoost();
        }
    }
}