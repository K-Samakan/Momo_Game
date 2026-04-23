using UnityEngine;

namespace MOMO.Combat
{
    /// <summary>
    /// ติดบน Monster แต่ละตัวเพื่อบอกขนาด
    /// ถ้าไม่ติด จะใช้ Bounds อัตโนมัติ
    /// </summary>
    public class MonsterSizeProfile : MonoBehaviour
    {
        public enum MonsterSize { Small, Medium, Large }

        [Header("Size Classification")]
        [Tooltip("กำหนดเองหรือปล่อยให้ระบบตรวจ Auto")]
        public MonsterSize sizeOverride = MonsterSize.Medium;
        public bool useAutoDetect = true;

        [Header("Auto Detect Threshold")]
        [Tooltip("ถ้าสูงน้อยกว่านี้ = Small")]
        public float smallHeightThreshold = 1.2f;
        [Tooltip("ถ้าสูงน้อยกว่านี้ = Medium")]
        public float mediumHeightThreshold = 2.0f;

        private Collider _col;

        void Awake()
        {
            _col = GetComponent<Collider>();
        }

        /// <summary>
        /// คืนค่า MonsterSize จริงที่ใช้งาน
        /// </summary>
        public MonsterSize GetSize()
        {
            if (!useAutoDetect) return sizeOverride;

            float height = GetMonsterHeight();

            if (height < smallHeightThreshold)   return MonsterSize.Small;
            if (height < mediumHeightThreshold)   return MonsterSize.Medium;
            return MonsterSize.Large;
        }

        public float GetMonsterHeight()
        {
            if (_col != null) return _col.bounds.size.y;
            return GetComponent<Renderer>()?.bounds.size.y ?? 1.5f;
        }

        void OnDrawGizmosSelected()
        {
            float h = Application.isPlaying ? GetMonsterHeight()
                : (GetComponent<Collider>()?.bounds.size.y ?? 1.5f);

            Gizmos.color = h < smallHeightThreshold ? Color.green
                         : h < mediumHeightThreshold ? Color.yellow : Color.red;

            Vector3 top = transform.position + Vector3.up * h;
            Gizmos.DrawWireSphere(top, 0.15f);
            Gizmos.DrawLine(transform.position, top);
        }
    }
}