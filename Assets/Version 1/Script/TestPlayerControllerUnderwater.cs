using UnityEngine;

public class TestPlayerControllerUnderwater : MonoBehaviour
{
    public float moveSpeed = 5f;        // ความเร็วการเคลื่อนที่
    public float waterMoveSpeed = 2f;   // ความเร็วว่ายน้ำ
    public GameObject underwaterEffects;

    private CharacterController controller;
    private bool isUnderwater = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        float speed = isUnderwater ? waterMoveSpeed : moveSpeed;

        // ใช้ปุ่ม W A S D หรือปุ่มลูกศร
        float h = Input.GetAxis("Horizontal");  // -1 (ซ้าย) ถึง 1 (ขวา)
        float v = Input.GetAxis("Vertical");    // -1 (ถอยหลัง) ถึง 1 (เดินหน้า)

        Vector3 move = transform.right * h + transform.forward * v;
        controller.SimpleMove(move * speed);
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
}
