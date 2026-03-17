using UnityEngine;

namespace MOMO.Pickups
{
    [RequireComponent(typeof(SphereCollider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Soul : MonoBehaviour
    {
        [Header("Healing")]
        [Tooltip("จำนวน HP ที่ฟื้นฟู")]
        public int healAmount = 1;
        
        [Header("Visuals")]
        [Tooltip("เอฟเฟกต์เมื่อเก็บ")]
        public GameObject pickupEffect;
        
        [Tooltip("เสียงเมื่อเก็บ")]
        public AudioClip pickupSound;
        
        [Header("Behavior")]
        [Tooltip("ระยะที่ดูดเข้าหาผู้เล่น (0 = ปิด)")]
        public float magnetRange = 3f;
        
        [Tooltip("ความเร็วดูดเข้าหาผู้เล่น")]
        public float magnetSpeed = 5f;
        
        [Tooltip("หมดเวลาหายไป (วินาที, 0 = ไม่หาย)")]
        public float lifetime = 30f;
        
        [Header("Animation")]
        [Tooltip("ลอยขึ้นลงไหม")]
        public bool floatAnimation = true;
        
        [Tooltip("ความสูงที่ลอย")]
        public float floatHeight = 0.3f;
        
        [Tooltip("ความเร็วลอย")]
        public float floatSpeed = 2f;
        
        Transform player;
        Rigidbody rb;
        Vector3 startPosition;
        bool isBeingCollected = false;
        float spawnTime;
        
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            spawnTime = Time.time;
            
            // หา Player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            
            // ตั้งค่า Collider
            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;
            
            // Rigidbody
            rb.useGravity = true;
            rb.drag = 1f;
            
            // รอจนตกถึงพื้น
            Invoke(nameof(RecordStartPosition), 0.5f);
        }
        
        void RecordStartPosition()
        {
            startPosition = transform.position;
        }
        
        void Update()
        {
            // ลอยขึ้นลง
            if (floatAnimation && rb.velocity.magnitude < 0.1f && startPosition != Vector3.zero)
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
                transform.position = new Vector3(
                    transform.position.x,
                    newY,
                    transform.position.z
                );
            }
            
            // หมุนรอบตัวเอง
            transform.Rotate(Vector3.up, 90f * Time.deltaTime);
            
            // ดูดเข้าหาผู้เล่น
            if (player != null && magnetRange > 0 && !isBeingCollected)
            {
                float distance = Vector3.Distance(transform.position, player.position);
                
                if (distance <= magnetRange)
                {
                    Vector3 direction = (player.position - transform.position).normalized;
                    rb.velocity = direction * magnetSpeed;
                }
            }
            
            // หมดเวลา
            if (lifetime > 0 && Time.time - spawnTime >= lifetime)
            {
                DestroySoul(false);
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !isBeingCollected)
            {
                isBeingCollected = true;
                
                // ฟื้นฟู HP
                MOMO.Player.PlayerHealth playerHealth = other.GetComponent<MOMO.Player.PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.Heal(healAmount);
                    Debug.Log($"<color=green>[Soul]</color> Player collected! Healed +{healAmount} HP");
                }
                
                // เอฟเฟกต์และเสียง
                PlayPickupEffects();
                
                // ทำลาย Soul
                DestroySoul(true);
            }
        }
        
        void PlayPickupEffects()
        {
            // เอฟเฟกต์
            if (pickupEffect != null)
            {
                GameObject effect = Instantiate(pickupEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }
            
            // เสียง
            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }
        }
        
        void DestroySoul(bool wasCollected)
        {
            Destroy(gameObject);
        }
        
        // Debug
        void OnDrawGizmosSelected()
        {
            if (magnetRange > 0)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, magnetRange);
            }
        }
    }
}