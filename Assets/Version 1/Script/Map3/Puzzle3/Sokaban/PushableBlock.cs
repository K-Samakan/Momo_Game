using UnityEngine;
using UnityEngine.InputSystem;
public class PushableBlock : MonoBehaviour
{
    [Header("Block Settings")]
    public float maxHealth = 100f;
    public float damagePerSecond = 10f;
    public float damagePerPush = 5f;
    public GameObject brokenPrefab;
    public AudioClip pushSound;
    public AudioClip breakSound;
    
    [Header("Visual Effects")]
    public ParticleSystem crackEffect;
    public Material[] damageMaterials; // Materials ที่แสดงความเสียหาย
    
    private float currentHealth;
    private Rigidbody rb;
    private AudioSource audioSource;
    private Renderer blockRenderer;
    private bool isPushing = false;
    private int pushCount = 0;
    private float totalPushTime = 0f;
    
    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        blockRenderer = GetComponent<Renderer>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }
    
    public void StartPush(Vector3 direction, float force)
    {
        isPushing = true;
        pushCount++;
        
        // เสียงเมื่อเริ่มผลัก
        if (pushSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(pushSound);
        }
        
        // ให้ความเสียหายเมื่อเริ่มผลัก
        TakeDamage(damagePerPush);
    }
    
    public void UpdatePush(Vector3 direction, float force, float pushTime)
    {
        if (!isPushing) return;
        
        totalPushTime = pushTime;
        
        // ใช้แรงผลัก
        rb.AddForce(direction * force * Time.deltaTime, ForceMode.Force);
        
        // ให้ความเสียหายต่อเนื่อง
        TakeDamage(damagePerSecond * Time.deltaTime);
        
        // Effect ถ้าผลักนานเกินไป
        if (pushTime > 3f && crackEffect != null && !crackEffect.isPlaying)
        {
            crackEffect.Play();
        }
    }
    
    public void StopPush()
    {
        isPushing = false;
        
        // หยุด effect
        if (crackEffect != null && crackEffect.isPlaying)
        {
            crackEffect.Stop();
        }
    }
    
    void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        UpdateVisuals();
        
        if (currentHealth <= 0)
        {
            BreakBlock();
        }
    }
    
    void UpdateVisuals()
    {
        if (damageMaterials.Length == 0 || blockRenderer == null) return;
        
        // เปลี่ยน material ตามความเสียหาย
        float healthPercentage = currentHealth / maxHealth;
        int materialIndex = Mathf.FloorToInt((1f - healthPercentage) * damageMaterials.Length);
        materialIndex = Mathf.Clamp(materialIndex, 0, damageMaterials.Length - 1);
        
        blockRenderer.material = damageMaterials[materialIndex];
    }
    
    void BreakBlock()
    {
        // เสียงแตก
        if (breakSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(breakSound);
        }
        
        // สร้างชิ้นส่วนที่แตก
        if (brokenPrefab != null)
        {
            GameObject broken = Instantiate(brokenPrefab, transform.position, transform.rotation);
            
            // ให้แรงกับชิ้นส่วน
            Rigidbody[] pieces = broken.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody piece in pieces)
            {
                Vector3 randomForce = Random.insideUnitSphere * 300f;
                piece.AddForce(randomForce);
                piece.AddTorque(Random.insideUnitSphere * 100f);
            }
            
            // ทำลายชิ้นส่วนหลังจาก 5 วินาที
            Destroy(broken, 5f);
        }
        
        // ทำลาย block
        Destroy(gameObject, 0.1f);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // ถ้าชนแรงเกินไปก็จะเสียหาย
        if (collision.relativeVelocity.magnitude > 5f)
        {
            TakeDamage(collision.relativeVelocity.magnitude);
        }
    }
}