using UnityEngine;

namespace MOMO.Cutscene
{
    /// <summary>
    /// หมุน RectTransform ไปเรื่อย ๆ (ใช้กับไอคอน Loading)
    /// แปะที่ GameObject ของไอคอนได้เลย
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        [Tooltip("ความเร็วหมุน (องศาต่อวินาที) ค่าบวก = ทวนเข็ม, ลบ = ตามเข็ม")]
        [SerializeField] private float speed = -180f;

        void Update()
        {
            // ใช้ unscaledDeltaTime เผื่อ Time.timeScale = 0
            transform.Rotate(0f, 0f, speed * Time.unscaledDeltaTime);
        }
    }
}