using System.Collections;
using UnityEngine;

public class DisappearAfterTime : MonoBehaviour
{
    public float timeBeforeDisappear = 2.0f; // เวลาที่จะรอก่อนที่จะทำให้เป็น Trigger (5 วินาที)
    private Collider objectCollider; // Collider ของ GameObject

    private void Start()
    {
        objectCollider = GetComponent<Collider>(); // หาคอมโพเนนต์ Collider ของ GameObject
    }

    private void OnTriggerEnter(Collider other)
    {
        // เมื่อ Player เข้า Trigger ของ GameObject (หรือตรวจสอบ Tag Player)
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered trigger, waiting to disappear...");

            // เรียก Coroutine ที่จะทำให้ Collider เปลี่ยนเป็น Trigger
            StartCoroutine(WaitAndDisappear());
        }
    }

    private IEnumerator WaitAndDisappear()
    {
        // รอเวลา 5 วินาที
        yield return new WaitForSeconds(timeBeforeDisappear);

        // เมื่อครบ 5 วินาที เปลี่ยน Collider เป็น Trigger
        objectCollider.isTrigger = true;

        Debug.Log("Object is now a trigger and can be passed through.");
    }
}
