using UnityEngine;

public class TestUnderwatercontroller : MonoBehaviour
{/*
     public bool isUnderwater = false;
    private Rigidbody rb;

    public float normalGravity = -9.81f;
    public float underwaterGravity = -2.0f;
    public GameObject underwaterEffects;

    public float moveSpeed = 5f;            // บนบก
    public float underwaterMoveSpeed = 2f;  // ใต้น้ำ
    public float jumpForce = 5f;
    private bool isGrounded = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (underwaterEffects != null)
            underwaterEffects.SetActive(false);
    }

    void Update()
    {
        HandleMovement();    // การควบคุมเดิน/ว่ายน้ำ
        HandleJump();        // การกระโดด/ดีดตัว

        // NOTE: คุณอาจต้องปรับแต่ง animation ตามสภาพน้ำ/บนบกด้วย!
    }

    void FixedUpdate()
    {
        if (isUnderwater)
        {
            rb.drag = 2f;
            Physics.gravity = new Vector3(0, underwaterGravity, 0);
        }
        else
        {
            rb.drag = 0.2f;
            Physics.gravity = new Vector3(0, normalGravity, 0);
        }
    }

    void HandleMovement()
    {
        // รับ Input จากจอย: Left Stick (หรือคีย์บอร์ด)
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // ค่า move ขึ้นอยู่กับว่ากำลังเดินหรือว่ายน้ำ
        float speed = isUnderwater ? underwaterMoveSpeed : moveSpeed;

        // สำหรับว่ายน้ำสามแกน (ขึ้น-ลง) ใช้ Right Stick หรือปุ่ม D-pad
        float moveY = 0f;
        if (isUnderwater)
        {
            // สมมุติ Right Stick Y = ยกตัวขึ้นลง หรือใช้ปุ่ม Jump/Crouch
            // Unity Input: "Jump" = A (Xbox), "Fire3" = LT/LB, "Crouch" = B/Circle
            if (Input.GetButton("Jump"))
                moveY = 1f;
            else if (Input.GetButton("Crouch"))
                moveY = -1f;
        }

        Vector3 move = new Vector3(moveX, moveY, moveZ) * speed;

        // เคลื่อนที่บนบก: ล็อกแกน Y ให้ใช้ค่าของ Rigidbody ปัจจุบัน (ไม่ให้โดดขึ้นเอง)
        if (!isUnderwater) move.y = rb.velocity.y;

        // ใช้เวกเตอร์ใหม่
        rb.velocity = move;
    }

    void HandleJump()
    {
        if (isUnderwater) return; // ว่ายน้ำไม่กระโดด

        if ((Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)) && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isGrounded = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = true;
            RenderSettings.fog = true;
            RenderSettings.fogColor = Color.cyan;
            RenderSettings.fogDensity = 0.08f;
            if (underwaterEffects != null)
                underwaterEffects.SetActive(true);
            Debug.Log("Player ENTER Water trigger.");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Water"))
        {
            isUnderwater = false;
            RenderSettings.fog = false;
            if (underwaterEffects != null)
                underwaterEffects.SetActive(false);
             Debug.Log("Player EXIT Water trigger.");
        }
    }

    void OnCollisionStay(Collision collision)
    {
        // ตรวจจับว่าติดพื้นหรือไม่ (สำหรับการกระโดด)
        if (!isUnderwater && collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }*/
}
