using UnityEngine;
using UnityEngine.InputSystem;

public class SokobanTPSController : MonoBehaviour
{
    [Header("Player Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f;
    
    [Header("Push Settings")]
    public float pushForce = 500f;
    public float pushRange = 2f;
    public float pushDuration = 0.5f;
    public LayerMask blockLayer = 1 << 6; // Layer สำหรับ blocks
    
    [Header("Camera")]
    public Transform cameraTarget;
    public Vector3 cameraOffset = new Vector3(0, 5, -7);
    
    private CharacterController characterController;
    private Camera playerCamera;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isPushing = false;
    private bool isL2Pressed = false;
    private bool isR2Pressed = false;
    private float currentPushTime = 0f;
    private PushableBlock currentBlock;
    
    // Input Actions
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction pushModeAction;
    private InputAction pushActionAction;
    
    void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        
        // กำหนด Input Actions ตาม Action Map ที่มี
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        pushModeAction = playerInput.actions["PushMode"];
        pushActionAction = playerInput.actions["PushAction"];
    }
    
    void OnEnable()
    {
        pushModeAction.performed += OnPushModePressed;
        pushModeAction.canceled += OnPushModeReleased;
        pushActionAction.performed += OnPushActionPressed;
        pushActionAction.canceled += OnPushActionReleased;
    }
    
    void OnDisable()
    {
        pushModeAction.performed -= OnPushModePressed;
        pushModeAction.canceled -= OnPushModeReleased;
        pushActionAction.performed -= OnPushActionPressed;
        pushActionAction.canceled -= OnPushActionReleased;
    }
    
    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandlePushing();
        UpdateCamera();
    }
    
    void HandleMovement()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        
        if (!isPushing)
        {
            Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);
            move = transform.TransformDirection(move);
            move.y = -9.81f; // Gravity
            
            characterController.Move(move * moveSpeed * Time.deltaTime);
        }
    }
    
    void HandleRotation()
    {
        lookInput = lookAction.ReadValue<Vector2>();
        
        if (!isPushing)
        {
            transform.Rotate(0, lookInput.x * rotationSpeed * Time.deltaTime, 0);
        }
    }
    
    void HandlePushing()
    {
        // ตรวจสอบว่ากด L2 (PushMode) และ R2 (PushAction) พร้อมกัน
        bool bothPressed = isL2Pressed && isR2Pressed;
        
        if (bothPressed && moveInput.magnitude > 0.1f)
        {
            if (!isPushing)
            {
                StartPushing();
            }
            else
            {
                ContinuePushing();
            }
        }
        else
        {
            if (isPushing)
            {
                StopPushing();
            }
        }
    }
    
    void StartPushing()
    {
        // หา block ที่อยู่ในระยะผลัก
        Vector3 pushDirection = new Vector3(moveInput.x, 0, moveInput.y);
        pushDirection = transform.TransformDirection(pushDirection).normalized;
        
        RaycastHit hit;
        if (Physics.Raycast(transform.position, pushDirection, out hit, pushRange, blockLayer))
        {
            PushableBlock block = hit.collider.GetComponent<PushableBlock>();
            if (block != null)
            {
                isPushing = true;
                currentBlock = block;
                currentPushTime = 0f;
                
                // เริ่มการผลัก
                block.StartPush(pushDirection, pushForce);
            }
        }
    }
    
    void ContinuePushing()
    {
        currentPushTime += Time.deltaTime;
        
        if (currentBlock != null)
        {
            Vector3 pushDirection = new Vector3(moveInput.x, 0, moveInput.y);
            pushDirection = transform.TransformDirection(pushDirection).normalized;
            
            // อัปเดตการผลัก
            currentBlock.UpdatePush(pushDirection, pushForce, currentPushTime);
            
            // เคลื่อนที่ตาม block
            Vector3 moveDirection = pushDirection * moveSpeed * 0.5f * Time.deltaTime;
            moveDirection.y = -9.81f * Time.deltaTime;
            characterController.Move(moveDirection);
        }
    }
    
    void StopPushing()
    {
        isPushing = false;
        currentPushTime = 0f;
        
        if (currentBlock != null)
        {
            currentBlock.StopPush();
            currentBlock = null;
        }
    }
    
    void UpdateCamera()
    {
        if (cameraTarget != null && playerCamera != null)
        {
            Vector3 targetPosition = cameraTarget.position + cameraOffset;
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, 
                                                          targetPosition, 
                                                          Time.deltaTime * 5f);
            
            playerCamera.transform.LookAt(cameraTarget.position + Vector3.up);
        }
    }
    
    void OnPushModePressed(InputAction.CallbackContext context)
    {
        isL2Pressed = true;
    }
    
    void OnPushModeReleased(InputAction.CallbackContext context)
    {
        isL2Pressed = false;
    }
    
    void OnPushActionPressed(InputAction.CallbackContext context)
    {
        isR2Pressed = true;
    }
    
    void OnPushActionReleased(InputAction.CallbackContext context)
    {
        isR2Pressed = false;
    }
    
    void OnDrawGizmosSelected()
    {
        // แสดงระยะการผลัก
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, pushRange);
    }}