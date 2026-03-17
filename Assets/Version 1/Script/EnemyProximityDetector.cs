using UnityEngine;


public class EnemyProximityDetector : MonoBehaviour
{
    public float detectionRadius = 7f;
    public string enemyTag = "Enemy";
    public float hapticCooldown = 0.1f;
    public LayerMask WallMask;

    private float hapticTimer;
    private bool isSpecialEventPlaying = false;


    /*void Start()
    {
        HapticManager.Instance?.PlayHapticPattern(HapticPatternType.GhostChill);
    }*/

    void Update()
    {
        hapticTimer -= Time.deltaTime;

        if (hapticTimer <= 0)
        {
            DetectEnemies();
            hapticTimer = hapticCooldown;
        }
    }

    void DetectEnemies()
    {
        Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, detectionRadius);
        float maxLeftIntensity = 0f;
        float maxRightIntensity = 0f;
        bool specialEventDetected = false;


        foreach (Collider col in enemiesInRange)
        {
            string tag = col.tag;

            if (tag != "Enemy" && tag != "Shoot" && tag != "Event" && tag != "Boom") continue;

            Vector3 directionToEnemy = col.transform.position - transform.position;
            float distance = directionToEnemy.magnitude;

            if (Physics.Raycast(transform.position, directionToEnemy.normalized, out RaycastHit hit, distance, WallMask))
                continue;

            float angle = Vector3.SignedAngle(transform.forward, directionToEnemy, Vector3.up);
            float normalizedDistance = Mathf.Clamp01(distance / detectionRadius);
            float intensity = 1f - normalizedDistance;

            if (intensity <= 0f) continue;

            // ส่งตำแหน่งศัตรูและความแรงไปยัง UI
            VisualSoundEffectManager.Instance?.ShowIcon(col.transform.position, intensity, tag);

            /*
            // กำหนดความแรงของ Haptic แต่ละด้าน
            if (angle < -45 && angle > -135) maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);        // ซ้าย
            if (angle > 45 && angle < 135) maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);        // ขวา
            if (Mathf.Abs(angle) <= 45)    // หน้า
            {
                maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity * 0.5f);
                maxRightIntensity = Mathf.Max(maxRightIntensity, intensity * 0.5f);
            }
            if (angle < -135 || angle > 135) // หลัง
            {
                maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
            }*/
            // ตรวจสอบเหตุการณ์พิเศษ (ยิง, เหตุการณ์, ระเบิด)

            // คำนวณการสั่นตามทิศทางสำหรับศัตรู
            if (tag == "Enemy")
            {
                if (angle < -45 && angle > -135) maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                if (angle > 45 && angle < 135) maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                if (Mathf.Abs(angle) <= 45)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity * 0.5f);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity * 0.5f);
                }
                if (angle < -135 || angle > 135)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                }
            }
            if (tag == "Shoot")
            {
                if (angle < -45 && angle > -135) maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                if (angle > 45 && angle < 135) maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                if (Mathf.Abs(angle) <= 45)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity * 0.5f);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity * 0.5f);
                }
                if (angle < -135 || angle > 135)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                }
            }
            if (tag == "Event")
            {
                if (angle < -45 && angle > -135) maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                if (angle > 45 && angle < 135) maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                if (Mathf.Abs(angle) <= 45)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity * 0.5f);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity * 0.5f);
                }
                if (angle < -135 || angle > 135)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                }
            }
            if (tag == "Boom")
            {
                if (angle < -45 && angle > -135) maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                if (angle > 45 && angle < 135) maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                if (Mathf.Abs(angle) <= 45)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity * 0.5f);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity * 0.5f);
                }
                if (angle < -135 || angle > 135)
                {
                    maxLeftIntensity = Mathf.Max(maxLeftIntensity, intensity);
                    maxRightIntensity = Mathf.Max(maxRightIntensity, intensity);
                }
            }
        }

        // สั่นตามทิศทางศัตรู (ถ้าไม่มีเหตุการณ์พิเศษ)
        if (!specialEventDetected && (maxLeftIntensity > 0 || maxRightIntensity > 0))
        {
            HapticManager.Instance?.Vibrate(maxRightIntensity, maxLeftIntensity, hapticCooldown);
        }
        isSpecialEventPlaying = specialEventDetected;
    }
    

    // สั่น Gamepad ตามด้านซ้าย/ขวา
    //HapticManager.Instance?.Vibrate(maxRightIntensity, maxLeftIntensity, 0f);

void OnDrawGizmosSelected()
{
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, detectionRadius);
}
}
