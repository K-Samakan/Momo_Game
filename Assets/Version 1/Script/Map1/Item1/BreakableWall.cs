using UnityEngine;

/// <summary>
/// กำแพงที่มีสัญลักษณ์รูปหมัด
/// ทำลายได้ด้วยถุงมือลิงเท่านั้น
/// Tag ต้องเป็น "BreakableWall" (Tag 24)
/// </summary>
public class BreakableWall : MonoBehaviour
{
    [Header("VFX")]
    public GameObject breakEffect;

    [Header("Audio")]
    public AudioClip breakSound;

    private bool _isBroken = false;

    public void Break()
    {
        if (_isBroken) return;
        _isBroken = true;

        Debug.Log($"[BreakableWall] 💥 กำแพง '{name}' แตกแล้ว!");

        // Haptic
        HapticManager.Instance?.PlayPattern(HapticPatternType.Boom);

        // เสียง
        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, transform.position);

        // VFX
        if (breakEffect != null)
            Instantiate(breakEffect, transform.position, Quaternion.identity);

        Destroy(gameObject, 0.1f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        if (TryGetComponent<BoxCollider>(out var box))
            Gizmos.DrawWireCube(transform.position, box.size);
    }
}