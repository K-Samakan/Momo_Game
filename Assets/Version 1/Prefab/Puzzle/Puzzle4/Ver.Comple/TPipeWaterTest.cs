using UnityEngine;

public class TPipeWaterTest : MonoBehaviour
{
    [Header("Animators ของน้ำแต่ละทิศ")]
    [SerializeField] private Animator verticalAnimator;   // ใส่ GameObject: น้ำไหล
    [SerializeField] private Animator horizontalAnimator; // ใส่ GameObject: น้ำไหล.001

    [Header("ชื่อ State ใน Animator")]
    [SerializeField] private string verticalStateName = "Water_Vertical_State";
    [SerializeField] private string horizontalStateName = "Water_Horizontal_State";

    [Header("สถานะน้ำในท่อ (ปรับจาก Inspector ได้)")]
    [SerializeField] private bool hasWater = true;

    // เอาไว้จำค่าเดิม เพื่อตรวจว่ามีการเปลี่ยนจาก true <-> false หรือไม่
    private bool previousHasWater;

    private void Awake()
    {
        // กันลืม: ถ้าใน Inspector ยังไม่เซ็ต Animator เลย จะลองหาในลูกให้
        if (verticalAnimator == null || horizontalAnimator == null)
        {
            Animator[] anims = GetComponentsInChildren<Animator>();

            if (anims.Length > 0 && verticalAnimator == null)
                verticalAnimator = anims[0];

            if (anims.Length > 1 && horizontalAnimator == null)
                horizontalAnimator = anims[1];
        }
    }

    private void Start()
    {
        Debug.Log("[TPipeWaterTest] Start");
        Debug.Log("verticalAnimator   = " + (verticalAnimator   ? verticalAnimator.gameObject.name   : "NULL"));
        Debug.Log("horizontalAnimator = " + (horizontalAnimator ? horizontalAnimator.gameObject.name : "NULL"));

        // บังคับให้ตอนเริ่มเกมมีการ "เปลี่ยนสถานะ" 1 ครั้ง
        // เพื่อให้อนิเมชันเล่นตามค่า hasWater ที่ตั้งไว้ใน Inspector
        bool fakePrevious = !hasWater;            // ทำเหมือนว่าค่าเดิมตรงข้ามกับปัจจุบัน
        ApplyWaterState(fakePrevious, hasWater);  // จะทำให้ถูกเรียก PlayForward/PlayBackward 1 ครั้ง

        previousHasWater = hasWater;              // จากนี้ไปใช้ค่าจริง
    }

    private void Update()
    {
        // ปุ่มเทสต์: Space = toggle น้ำเข้า/ออก
        if (Input.GetKeyDown(KeyCode.Space))
        {
            hasWater = !hasWater;
            Debug.Log("Toggle hasWater = " + hasWater);

            ApplyWaterState(previousHasWater, hasWater);
            previousHasWater = hasWater;
        }
    }

    /// <summary>
    /// ฟังก์ชันหลัก: ดูว่าค่าน้ำเปลี่ยนจาก wasWater -> isWater อย่างไร แล้วสั่งเล่นทั้งสอง Animator
    /// </summary>
    private void ApplyWaterState(bool wasWater, bool isWater)
    {
        // ถ้าไม่มีการเปลี่ยนค่า (true -> true หรือ false -> false) ก็ไม่ต้องทำอะไร
        if (isWater == wasWater)
            return;

        if (isWater)
        {
            // false -> true : น้ำเริ่มมา
            PlayForward(verticalAnimator, verticalStateName);
            PlayForward(horizontalAnimator, horizontalStateName);
        }
        else
        {
            // true -> false : น้ำหายไป
            PlayBackward(verticalAnimator, verticalStateName);
            PlayBackward(horizontalAnimator, horizontalStateName);
        }
    }

    /// <summary>
    /// เล่นอนิเมชันเดินหน้า ตั้งแต่ต้นคลิป (normalizedTime = 0)
    /// </summary>
    private void PlayForward(Animator animator, string stateName)
    {
        if (animator == null)
        {
            Debug.LogWarning("[TPipeWaterTest] PlayForward: animator is NULL, state = " + stateName);
            return;
        }

        Debug.Log("[TPipeWaterTest] PlayForward: " + animator.gameObject.name + " state = " + stateName);

        animator.speed = 1f;               // เดินหน้า
        animator.Play(stateName, 0, 0f);   // layer 0, เริ่มจากเวลา 0
    }

    /// <summary>
    /// เล่นอนิเมชันย้อนกลับ จากปลายคลิป (normalizedTime = 1)
    /// </summary>
    private void PlayBackward(Animator animator, string stateName)
    {
        if (animator == null)
        {
            Debug.LogWarning("[TPipeWaterTest] PlayBackward: animator is NULL, state = " + stateName);
            return;
        }

        Debug.Log("[TPipeWaterTest] PlayBackward: " + animator.gameObject.name + " state = " + stateName);

        animator.speed = -1f;              // เล่นย้อนกลับ
        animator.Play(stateName, 0, 1f);   // layer 0, เริ่มจากปลายคลิป
    }

    /// <summary>
    /// เอาไว้ให้ระบบอื่นเรียกเปลี่ยนสถานะน้ำ จากโค้ดข้างนอก
    /// </summary>
    public void SetHasWater(bool value)
    {
        if (value == hasWater)
            return;

        ApplyWaterState(hasWater, value);
        previousHasWater = value;
        hasWater = value;
    }
}