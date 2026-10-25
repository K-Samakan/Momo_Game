using UnityEngine;

public class MonsterHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Tooltip("Maximum health of the monster")]
    public int maxHealth = 3;

    [Tooltip("Current health")]
    private int currentHealth;

    [Header("Knockback Settings")]
    [Tooltip("Force applied when hit")]
    public float knockbackForce = 5f;

    [Tooltip("Duration of knockback")]
    public float knockbackDuration = 0.3f;

    [Tooltip("Height of knockback (upward force)")]
    public float knockbackUpwardForce = 2f;

    [Header("Visual Feedback")]
    [Tooltip("Flash color when hit")]
    public Color hitColor = Color.red;

    [Tooltip("Flash duration")]
    public float flashDuration = 0.1f;

    [Header("Death Settings")]
    [Tooltip("Delay before destroying the object")]
    public float destroyDelay = 0.5f;

    [Tooltip("Death effect (optional)")]
    public GameObject deathEffect;

    // Private variables
    private bool isInvulnerable = false;
    private bool isKnockedBack = false;
    private Rigidbody rb;
    private Renderer[] renderers;
    private Color[] originalColors;
    private Animator animator;

    // Animation IDs (optional)
    private int _animIDHit;
    private int _animIDDeath;

    private void Start()
    {
        // Initialize health
        currentHealth = maxHealth;

        // Get components
        rb = GetComponent<Rigidbody>();
        renderers = GetComponentsInChildren<Renderer>();
        animator = GetComponent<Animator>();

        // Store original colors
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalColors[i] = renderers[i].material.color;
        }

        // Assign animation IDs (if using Animator)
        if (animator != null)
        {
            _animIDHit = Animator.StringToHash("Hit");
            _animIDDeath = Animator.StringToHash("Death");
        }

        // Ensure Rigidbody exists for knockback
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
    }

    public void TakeDamage(int damage, Vector3 attackerPosition)
    {
        // ถ้ากำลังอยู่ในสถานะ invulnerable ไม่รับดาเมจ
        if (isInvulnerable)
        {
            Debug.Log($"{gameObject.name} is invulnerable!");
            return;
        }

        // ลด HP
        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage! HP: {currentHealth}/{maxHealth}");

        // เล่น Animation ถูกตี (ถ้ามี)
        if (animator != null && HasAnimatorParam(_animIDHit))
        {
            animator.SetTrigger(_animIDHit);
        }

        // Visual feedback (Flash)
        StartCoroutine(FlashEffect());

        // Apply knockback
        Vector3 knockbackDirection = (transform.position - attackerPosition).normalized;
        ApplyKnockback(knockbackDirection);

        // Check if dead
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Set invulnerable briefly
            StartCoroutine(InvulnerabilityFrames(knockbackDuration));
        }
    }

    private bool HasAnimatorParam(int hash)
    {
        foreach (var p in animator.parameters) if (p.nameHash == hash) return true;
        return false;
    }

    private void ApplyKnockback(Vector3 direction)
    {
        if (rb == null || rb.isKinematic) return;   // kinematic (เช่นเดินด้วย NavMesh) ตั้ง velocity ไม่ได้

        isKnockedBack = true;

        // คำนวณแรงกระเด็น
        Vector3 knockback = direction * knockbackForce;
        knockback.y = knockbackUpwardForce; // เพิ่มแรงขึ้น

        // ใช้แรง
        rb.velocity = Vector3.zero; // รีเซ็ตความเร็วก่อน
        rb.AddForce(knockback, ForceMode.Impulse);

        Debug.Log($"{gameObject.name} knocked back!");

        // รีเซ็ตสถานะ knockback
        StartCoroutine(ResetKnockback());
    }

    private System.Collections.IEnumerator ResetKnockback()
    {
        yield return new WaitForSeconds(knockbackDuration);
        isKnockedBack = false;
    }

    private System.Collections.IEnumerator InvulnerabilityFrames(float duration)
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(duration);
        isInvulnerable = false;
    }

    private System.Collections.IEnumerator FlashEffect()
    {
        // เปลี่ยนสีเป็นสีแดง
        foreach (var renderer in renderers)
        {
            renderer.material.color = hitColor;
        }

        yield return new WaitForSeconds(flashDuration);

        // กลับเป็นสีเดิม
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = originalColors[i];
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died!");

        // เล่น Animation ตาย (ถ้ามี)
        if (animator != null)
        {
            animator.SetTrigger(_animIDDeath);
        }

        // Spawn death effect (ถ้ามี)
        if (deathEffect != null)
        {
            Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        // ปิดการชนกัน
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            col.enabled = false;
        }

        GetComponent<MonsterPersistence>()?.RegisterDeath();
        // ทำลาย GameObject
        Destroy(gameObject, destroyDelay);
    }

    // Public methods for external access
    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public bool IsAlive()
    {
        return currentHealth > 0;
    }

    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }

    // Visualization in Editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}