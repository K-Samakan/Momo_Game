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

            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;

            SphereCollider col = GetComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            rb.useGravity = true;
            rb.linearDamping = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            Invoke(nameof(RecordStartPosition), 0.8f);
        }

        void RecordStartPosition()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out var hit, 10f))
            {
                startPosition = hit.point + Vector3.up * 0.15f;
                transform.position = startPosition;
            }
            else
            {
                startPosition = transform.position + Vector3.up * 0.15f;
                transform.position = startPosition;
            }

            rb.useGravity = false;
            rb.isKinematic = true;  // ← Kinematic หลังลงพื้นแล้ว
        }

        void Update()
        {
            if (isBeingCollected) return;

            // ── ดูดเข้าหาผู้เล่น ──
            if (player != null && magnetRange > 0)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= magnetRange)
                {
                    // ✅ ใช้ MovePosition แทน velocity (รองรับ Kinematic)
                    Vector3 dir = (player.position - transform.position).normalized;
                    Vector3 newPos = transform.position + dir * magnetSpeed * Time.deltaTime;
                    rb.MovePosition(newPos);
                    return; // ไม่ลอยขณะถูกดูด
                }
            }

            // ── ลอยขึ้นลง ──
            if (floatAnimation && startPosition != Vector3.zero)
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
                transform.position = new Vector3(
                    transform.position.x,
                    newY,
                    transform.position.z
                );
            }

            // ── หมุนรอบตัวเอง ──
            transform.Rotate(Vector3.up, 90f * Time.deltaTime);

            // ── หมดเวลา ──
            if (lifetime > 0 && Time.time - spawnTime >= lifetime)
                DestroySoul(false);
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"Soul triggered by: {other.name} tag: {other.tag}");

            if (!other.CompareTag("Player") || isBeingCollected) return;

            var playerHealth = other.GetComponent<MOMO.Player.PlayerHealth>();
            Debug.Log($"PlayerHealth found: {playerHealth != null}");

            if (playerHealth == null) return;
            if (playerHealth.currentHealth >= playerHealth.maxHealth) return;

            isBeingCollected = true;
            playerHealth.Heal(1);
            PlayPickupEffects();
            DestroySoul(true);
        }

        void PlayPickupEffects()
        {
            if (pickupEffect != null)
            {
                GameObject effect = Instantiate(
                    pickupEffect, transform.position, Quaternion.identity);
                Destroy(effect, 2f);
            }

            if (pickupSound != null)
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }

        void DestroySoul(bool wasCollected)
        {
            Destroy(gameObject);
        }

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