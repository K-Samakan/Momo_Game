using UnityEngine;
using StarterAssets;
using System.Collections;

public class JumpPad : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 3.0f; 
    // ⚠️ ค่านี้ไม่ใช่ velocity โดยตรง!
    // ApplyExternalForce ใช้สูตร: velocity = sqrt(jumpForce * -2 * Gravity)
    // Gravity = -15 → jumpForce 3 = กลาง, 5 = สูง, 8 = สูงมาก

    [Header("Cooldown")]
    [SerializeField] private float cooldown = 1.0f;
    private bool _isReady = true;

    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem jumpEffect;
    [SerializeField] private Animator padAnimator;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = true;

    private void Reset()
    {
        // ตรวจสอบ Collider อัตโนมัติเมื่อ Add Component
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (showDebugLog)
            Debug.Log($"[JumpPad] Trigger hit by: {other.name} | Tag: {other.tag}");

        if (!_isReady) 
        {
            if (showDebugLog) Debug.Log("[JumpPad] Not ready (cooldown)");
            return;
        }

        // วิธีที่ 1: ตรวจจาก Tag (เร็วกว่า)
        if (!other.CompareTag("Player")) return;

        // วิธีที่ 2: ดึง Component
        ThirdPersonController player = other.GetComponent<ThirdPersonController>();
        
        // กรณีที่ Collider อยู่ที่ Child Object ของ Player
        if (player == null)
            player = other.GetComponentInParent<ThirdPersonController>();

        if (player == null)
        {
            if (showDebugLog) Debug.LogWarning("[JumpPad] ThirdPersonController NOT found!");
            return;
        }

        if (showDebugLog) Debug.Log($"[JumpPad] Launching player! Force: {jumpForce}");

        // ยิงผู้เล่นขึ้น
        player.ApplyExternalForce(jumpForce);

        // Effects + Haptic
        PlayEffects();
        TriggerHaptic();

        StartCoroutine(Cooldown());
    }

    private void PlayEffects()
    {
        if (jumpEffect != null) jumpEffect.Play();
        if (padAnimator != null) padAnimator.SetTrigger("Launch");
    }

    private void TriggerHaptic()
    {
        if (HapticManager.Instance != null)
            HapticManager.Instance.Vibrate(1.0f, 1.0f, 0.3f);
    }

    private IEnumerator Cooldown()
    {
        _isReady = false;
        yield return new WaitForSeconds(cooldown);
        _isReady = true;
        if (showDebugLog) Debug.Log("[JumpPad] Ready again!");
    }

    // แสดง Gizmo ในหน้าต่าง Scene
    private void OnDrawGizmos()
    {
        Gizmos.color = _isReady ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}