using UnityEngine;
using UnityEngine.InputSystem;

public class PuzzlePlayer3 : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 6f;
    public float jumpForce = 10f;
    public float gridSize = 2f; // แต่ละช่อง = 2 units
    public LayerMask obstacleLayer;
    public LayerMask groundLayer;
    
    [Header("Block Interaction")]
    public LayerMask blockLayer;
    public float interactionRange = 2.5f; // เพิ่มระยะเพื่อให้เข้าถึงบล็อคได้
    
    [Header("Debug")]
    public bool showDebugGizmos = true;
    
    private bool isGrabbing = false;
    private bool isRunning = false;
    private IceBlock nearbyBlock = null;
    private bool isMoving = false;
    private Vector3 targetPosition;
    private Rigidbody rb;
    private bool isGrounded;
    
    void Start()
    {
        // Snap ตำแหน่งเริ่มต้นให้อยู่บน grid
        SnapToGrid();
        targetPosition = transform.position;
        
        rb = GetComponent<Rigidbody>();
        if(rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
    
    void Update()
    {
        HandleMovement();
        CheckNearbyBlocks();
        CheckGrounded();
    }
    
    void SnapToGrid()
    {
        // Snap ตำแหน่งให้อยู่บนจุดกริดที่ใกล้ที่สุด
        float snappedX = Mathf.Round(transform.position.x / gridSize) * gridSize;
        float snappedZ = Mathf.Round(transform.position.z / gridSize) * gridSize;
        transform.position = new Vector3(snappedX, transform.position.y, snappedZ);
    }
    
    void HandleMovement()
    {
        if(isMoving)
        {
            float currentSpeed = isRunning ? runSpeed : walkSpeed;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, currentSpeed * Time.deltaTime);
            
            if(Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }
    
    void CheckNearbyBlocks()
    {
        // หาบล็อคที่อยู่ใกล้ๆ (4 ทิศทางหลัก)
        nearbyBlock = null;
        
        Vector3[] checkDirections = {
            Vector3.right * gridSize,   // ขวา
            Vector3.left * gridSize,    // ซ้าย
            Vector3.forward * gridSize, // หน้า  
            Vector3.back * gridSize     // หลัง
        };
        
        foreach(Vector3 direction in checkDirections)
        {
            Vector3 checkPos = transform.position + direction;
            Collider[] blockHits = Physics.OverlapSphere(checkPos, 0.8f, blockLayer);
            
            if(blockHits.Length > 0)
            {
                IceBlock block = blockHits[0].GetComponent<IceBlock>();
                if(block != null)
                {
                    nearbyBlock = block;
                    break;
                }
            }
        }
        
        // Debug แสดงสถานะการจับ
        if(isGrabbing && nearbyBlock != null)
        {
            Debug.Log($"🤏 กำลังจับ: {nearbyBlock.name}");
        }
    }
    
    void CheckGrounded()
    {
        // เช็คว่าอยู่บนพื้นไหม
        Collider[] groundHits = Physics.OverlapSphere(transform.position + Vector3.down * 0.1f, 0.3f, groundLayer);
        isGrounded = groundHits.Length > 0;
    }
    
    // === INPUT ACTIONS ===
    
    public void OnMove(InputAction.CallbackContext context)
    {
        if(isMoving) return;
        
        Vector2 input = context.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0, input.y);
        
        if(context.performed && direction.magnitude > 0.5f)
        {
            if(isGrabbing && nearbyBlock != null)
            {
                // ผลักบล็อค
                Vector3 pushDir = GetGridDirection(direction);
                if(nearbyBlock.TryPush(pushDir))
                {
                    Debug.Log($"💪 ผลักบล็อค {nearbyBlock.name} ไปทิศ {pushDir}");
                }
                else
                {
                    Debug.Log("❌ ผลักไม่ได้!");
                }
            }
            else
            {
                // เคลื่อนที่ผู้เล่น
                MovePlayer(direction);
            }
        }
    }
    
    public void OnJump(InputAction.CallbackContext context)
    {
        if(context.performed && isGrounded && rb != null)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            Debug.Log("🦘 กระโดด!");
        }
    }
    
    public void OnSprint(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            isRunning = true;
            Debug.Log("🏃 เริ่มวิ่ง");
        }
        else if(context.canceled)
        {
            isRunning = false;
            Debug.Log("🚶 หยุดวิ่ง");
        }
    }
    
    public void OnL2(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            if(nearbyBlock != null)
            {
                isGrabbing = true;
                Debug.Log($"🤏 L2: จับบล็อค {nearbyBlock.name}");
            }
        }
        else if(context.canceled)
        {
            isGrabbing = false;
            Debug.Log("👐 L2: ปล่อยบล็อค");
        }
    }
    
    public void OnR2(InputAction.CallbackContext context)
    {
        if(context.started)
        {
            if(nearbyBlock != null)
            {
                isGrabbing = true;
                Debug.Log($"🤏 R2: จับบล็อค {nearbyBlock.name}");
            }
        }
        else if(context.canceled)
        {
            isGrabbing = false;
            Debug.Log("👐 R2: ปล่อยบล็อค");
        }
    }
    
    public void OnLook(InputAction.CallbackContext context)
    {
        // สำหรับควบคุมกล้อง (ถ้าต้องการ)
        Vector2 lookInput = context.ReadValue<Vector2>();
        // TODO: Implement camera look if needed
    }
    
    // === HELPER METHODS ===
    
    void MovePlayer(Vector3 direction)
    {
        Vector3 gridDir = GetGridDirection(direction);
        Vector3 newPos = transform.position + (gridDir * gridSize);
        
        // เช็คว่าไปได้ไหม
        Collider[] obstacles = Physics.OverlapSphere(newPos, 0.8f, obstacleLayer);
        Collider[] blocks = Physics.OverlapSphere(newPos, 0.8f, blockLayer);
        
        if(obstacles.Length == 0 && blocks.Length == 0) // ไม่มีสิ่งกีดขวาง
        {
            targetPosition = newPos;
            isMoving = true;
            Debug.Log($"🚶 เดินไป {newPos}");
        }
        else
        {
            Debug.Log("❌ เดินไปไม่ได้ - มีสิ่งกีดขวาง");
        }
    }
    
    Vector3 GetGridDirection(Vector3 input)
    {
        // แปลงเป็น 4 ทิศทางหลัก (ขจัดการเคลื่อนไหวแนวทแยง)
        if(Mathf.Abs(input.x) > Mathf.Abs(input.z))
            return input.x > 0 ? Vector3.right : Vector3.left;
        else
            return input.z > 0 ? Vector3.forward : Vector3.back;
    }
    
    // Debug Gizmos
    void OnDrawGizmos()
    {
        if(!showDebugGizmos) return;
        
        // วาดตำแหน่งปัจจุบัน
        Gizmos.color = isGrabbing ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        
        // วาดทิศทางที่เช็คบล็อค
        Gizmos.color = Color.yellow;
        Vector3[] checkDirections = {
            Vector3.right * gridSize,
            Vector3.left * gridSize,  
            Vector3.forward * gridSize,
            Vector3.back * gridSize
        };
        
        foreach(Vector3 direction in checkDirections)
        {
            Vector3 checkPos = transform.position + direction;
            Gizmos.DrawWireSphere(checkPos, 0.3f);
        }
        
        // วาดบล็อคที่จับอยู่
        if(nearbyBlock != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, nearbyBlock.transform.position);
        }
    }
}