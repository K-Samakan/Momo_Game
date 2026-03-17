using UnityEngine;

public class ClimbableRock : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  INSPECTOR
    // ─────────────────────────────────────────────
    [Header("Rock Settings")]
    public bool isTopRock = false;          // หินบนสุด → กดกระโดดขึ้น Platform
    public float rockWidth = 4f;            // ความยาวที่ผู้เล่นไต่ได้

    public enum TraverseAxis { Right, Up, Forward }
    public TraverseAxis traverseDirection = TraverseAxis.Right; // เลือกแกนที่หินวางยาว

    [Header("Grab Points")]
    public Transform grabPoint;             // จุดที่ผู้เล่นจะเกาะ (Empty Object หน้าหิน)

    [Header("Neighbor Detection")]
    public float neighborSearchRadius = 3f; // รัศมีหาหินใกล้เคียง

    [Header("Vault Target (เฉพาะ Top Rock)")]
    public Transform vaultTarget;           // จุดขึ้น Platform หลัง Vault

    // ─────────────────────────────────────────────
    //  PUBLIC API
    // ─────────────────────────────────────────────

    /// <summary>ดึง Direction จริงตามแกนที่เลือก</summary>
    public Vector3 GetTraverseDir()
    {
        switch (traverseDirection)
        {
            case TraverseAxis.Up:      return transform.up;
            case TraverseAxis.Forward: return transform.forward;
            default:                   return transform.right;
        }
    }

    /// <summary>หาหินเพื่อนบ้านในทิศที่กำหนดจาก Right Stick</summary>
    public ClimbableRock FindNeighborInDirection(Vector2 stickDir)
    {
        if (stickDir.magnitude < 0.5f) return null;

        // แปลง stick (2D) → world direction
        // X = ซ้าย/ขวา, Y = ขึ้น/ลง
        Vector3 worldDir = new Vector3(stickDir.x, stickDir.y, 0f).normalized;

        Collider[] hits = Physics.OverlapSphere(transform.position, neighborSearchRadius);
        ClimbableRock bestRock = null;
        float bestScore = 0.4f; // threshold dot product

        foreach (var hit in hits)
        {
            ClimbableRock rock = hit.GetComponent<ClimbableRock>();
            if (rock == null || rock == this) continue;

            Vector3 toRock = (rock.transform.position - transform.position).normalized;
            float dot = Vector3.Dot(toRock, worldDir);

            if (dot > bestScore)
            {
                bestScore = dot;
                bestRock = rock;
            }
        }

        return bestRock;
    }

    // ─────────────────────────────────────────────
    //  GIZMOS (ใช้ใน Scene View)
    // ─────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        // แสดงจุด GrabPoint (สีเขียว)
        if (grabPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(grabPoint.position, 0.15f);
        }

        // แสดงเส้นขอบเขตไต่ซ้ายขวา (สีเหลือง)
        Gizmos.color = Color.yellow;
        Vector3 dir = GetTraverseDir();
        Vector3 leftEdge  = transform.position + dir * (-rockWidth / 2f);
        Vector3 rightEdge = transform.position + dir * ( rockWidth / 2f);
        Gizmos.DrawLine(leftEdge, rightEdge);
        Gizmos.DrawSphere(leftEdge,  0.1f);
        Gizmos.DrawSphere(rightEdge, 0.1f);

        // แสดงรัศมีหาหินเพื่อนบ้าน (สีฟ้า)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, neighborSearchRadius);

        // แสดง VaultTarget (สีชมพู) เฉพาะ Top Rock
        if (isTopRock && vaultTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(vaultTarget.position, 0.2f);
            Gizmos.DrawLine(transform.position, vaultTarget.position);
        }
    }
}