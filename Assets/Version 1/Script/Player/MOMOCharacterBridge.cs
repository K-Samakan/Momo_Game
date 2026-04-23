using UnityEngine;
using FS_ThirdPerson;
using StarterAssets;

/// <summary>
/// Bridge script เชื่อม StarterAssets ThirdPersonController 
/// กับ FS Parkour and Climbing System (ICharacter Interface)
/// ใส่ script นี้บน PlayerArmature เดียวกับ ThirdPersonController
/// </summary>
public class MOMOCharacterBridge : MonoBehaviour, ICharacter
{
    // ============================================================
    // References
    // ============================================================
    [Header("References")]
    [SerializeField] ThirdPersonController thirdPersonController;
    [SerializeField] Animator characterAnimator;

    CharacterController characterController;
    StarterAssetsInputs starterAssetsInputs;

    // ============================================================
    // ICharacter — Animator (ต้องมีทั้ง get และ set)
    // ============================================================
    public Animator Animator
    {
        get => characterAnimator;
        set => characterAnimator = value;
    }

    // ============================================================
    // ICharacter — Root Motion
    // ============================================================
    public bool UseRootMotion
    {
        get => characterAnimator != null && characterAnimator.applyRootMotion;
        set { if (characterAnimator != null) characterAnimator.applyRootMotion = value; }
    }

    // ============================================================
    // ICharacter — ทิศทางการเคลื่อนที่จาก Input
    // ============================================================
    public Vector3 MoveDir
    {
        get
        {
            if (starterAssetsInputs == null) return Vector3.zero;
            Vector3 inputDir = new Vector3(starterAssetsInputs.move.x, 0, starterAssetsInputs.move.y).normalized;
            if (Camera.main != null)
            {
                Vector3 camForward = Camera.main.transform.forward;
                Vector3 camRight = Camera.main.transform.right;
                camForward.y = 0f;
                camRight.y = 0f;
                return (camForward.normalized * inputDir.z + camRight.normalized * inputDir.x).normalized;
            }
            return inputDir;
        }
    }

    // ============================================================
    // ICharacter — ตรวจว่าอยู่บนพื้นไหม
    // ============================================================
    public bool IsGrounded => characterController != null && characterController.isGrounded;

    // ============================================================
    // ICharacter — ค่า Gravity ที่ Asset ใช้ในการคำนวณ jump
    // ============================================================
    public float Gravity => -15f;

    // ============================================================
    // ICharacter — ป้องกัน Parkour/Climbing ชั่วคราว
    // ตั้ง = true เมื่อต้องการบล็อก เช่น ระหว่าง Attack
    // ============================================================
    public bool PreventAllSystems { get; set; } = false;

    // ============================================================
    // ICharacter — หน่วงการเริ่ม system
    // ============================================================
    public bool WaitToStartSystem { get; set; } = false;

    // ============================================================
    // ICharacter — FS Asset เรียกเมื่อเริ่ม Parkour/Climbing
    // ============================================================
    public void OnStartSystem(SystemBase system, bool needHandsForAction = false)
    {
        // ปิด ThirdPersonController ชั่วคราว ให้ FS Asset ควบคุมแทน
        if (thirdPersonController != null)
            thirdPersonController.enabled = false;
    }

    // ============================================================
    // ICharacter — FS Asset เรียกเมื่อ action เสร็จสิ้น
    // ============================================================
    public void OnEndSystem(SystemBase system)
    {
        // คืนการควบคุมกลับให้ ThirdPersonController
        if (thirdPersonController != null)
            thirdPersonController.enabled = true;
    }

    // ============================================================
    // Unity Lifecycle
    // ============================================================
    private void Awake()
    {
        characterController  = GetComponent<CharacterController>();
        starterAssetsInputs  = GetComponent<StarterAssetsInputs>();

        if (thirdPersonController == null)
            thirdPersonController = GetComponent<ThirdPersonController>();
        if (characterAnimator == null)
            characterAnimator = GetComponent<Animator>();
    }
}