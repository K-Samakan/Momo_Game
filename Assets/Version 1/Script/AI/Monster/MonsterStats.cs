using UnityEngine;

namespace MOMO.AI.Monster
{
    [CreateAssetMenu(fileName = "NewMonsterStats", menuName = "MOMO/Monster Stats")]
    public class MonsterStats : ScriptableObject
    {
        [Header("Identity")]
        public string monsterName = "Monster";
        public Sprite icon;
        
        [Header("Combat Stats")]
        [Tooltip("พลังชีวิตสูงสุด")]
        public float maxHealth = 2f;
        
        [Tooltip("พลังโจมตี")]
        public float attackDamage = 1f;
        
        [Tooltip("ระยะโจมตี (เมตร)")]
        public float attackRange = 2f;
        
        [Tooltip("เวลาคูลดาวน์การโจมตี (วินาที)")]
        public float attackCooldown = 1.5f;
        
        [Header("Movement")]
        [Tooltip("ความเร็วเดิน")]
        public float walkSpeed = 2f;
        
        [Tooltip("ความเร็ววิ่งไล่")]
        public float runSpeed = 4f;
        
        [Tooltip("ระยะตรวจจับผู้เล่น (เมตร)")]
        public float detectionRange = 8f;
        
        [Tooltip("ระยะที่จะกลับไปตำแหน่งเดิม (เมตร)")]
        public float leashRange = 15f;
        
        [Header("AI Behavior")]
        [Tooltip("จุดลาดตระเวน (relative to spawn point)")]
        public Vector3[] patrolPoints;
        
        [Tooltip("เวลารอที่จุดลาดตระเวน (วินาที)")]
        public float patrolWaitTime = 2f;
        
        [Tooltip("ระยะที่ถือว่าถึงจุดลาดตระเวนแล้ว")]
        public float waypointReachedDistance = 0.5f;
        
        [Header("Loot Drop")]
        [Tooltip("Prefab ของ Soul ที่จะดรอป")]
        public GameObject soulPrefab;
        
        [Tooltip("จำนวน Soul ที่ดรอป")]
        public int soulDropCount = 1;
        
        [Tooltip("โอกาสดรอป Soul (0-1)")]
        [Range(0f, 1f)]
        public float soulDropChance = 1f;
    }
}