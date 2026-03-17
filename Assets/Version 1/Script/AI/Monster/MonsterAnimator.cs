using UnityEngine;
using UnityEngine.AI;

namespace MOMO.AI.Monster
{
    [RequireComponent(typeof(Animator))]
    public class MonsterAnimator : MonoBehaviour
    {
        [Header("References")]
        public Animator animator;
        public NavMeshAgent agent;
        public MonsterAI_Hybrid monsterAI;
        
        [Header("Animation Settings")]
        [Tooltip("ความเร็วที่เริ่มเล่น Walk animation")]
        public float walkSpeedThreshold = 0.1f;
        
        [Tooltip("ความเร็วที่เริ่มเล่น Run animation")]
        public float runSpeedThreshold = 3f;
        
        [Tooltip("ความนุ่มนวลในการเปลี่ยน Speed")]
        public float speedDampTime = 0.1f;
        
        [Header("Attack Settings")]
        [Tooltip("ช่วงเวลาในการทำ Damage (% ของ Animation)")]
        [Range(0f, 1f)]
        public float attackDamagePoint = 0.5f;
        
        // Animation Parameters
        const string SPEED = "Speed";
        const string ATTACK_TRIGGER = "AttackTrigger";
        const string IS_ATTACKING = "IsAttacking";
        const string IS_HIT = "IsHit";
        const string IS_DEAD = "IsDead";
        
        // Animation State Names
        const string ATTACK_STATE = "Attack01";
        const string HIT_STATE = "GetHit";
        const string DIE_STATE = "Die";
        const string IDLE_STATE = "IdleNormal";
        
        bool hasDealtDamage = false;
        
        void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            
            if (agent == null)
                agent = GetComponent<NavMeshAgent>();
            
            if (monsterAI == null)
                monsterAI = GetComponent<MonsterAI_Hybrid>();
        }
        
        void Update()
        {
            if (animator == null || agent == null) return;
            
            UpdateMovementAnimation();
            CheckAttackDamagePoint();
        }
        
        /// <summary>
        /// Update Animation การเดิน/วิ่ง
        /// </summary>
        void UpdateMovementAnimation()
        {
            // คำนวณความเร็วจริง
            float speed = agent.velocity.magnitude;
            
            // ถ้า Monster หยุดอยู่กับที่
            if (agent.isStopped)
            {
                speed = 0f;
            }
            
            // Set Speed parameter (แบบนุ่มนวล)
            animator.SetFloat(SPEED, speed, speedDampTime, Time.deltaTime);
        }
        
        /// <summary>
        /// เล่น Animation โจมตี
        /// </summary>
        public void PlayAttack()
        {
            if (animator == null) return;
            
            animator.SetTrigger(ATTACK_TRIGGER);
            animator.SetBool(IS_ATTACKING, true);
            hasDealtDamage = false;
            
            Debug.Log("<color=cyan>[Animator]</color> Playing Attack Animation");
        }
        
        /// <summary>
        /// ตรวจสอบช่วงเวลาที่ควรทำ Damage
        /// </summary>
        void CheckAttackDamagePoint()
        {
            if (hasDealtDamage) return;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // ถ้าอยู่ใน Attack state และถึงจุดที่ต้องทำ Damage
            if (stateInfo.IsName(ATTACK_STATE) && stateInfo.normalizedTime >= attackDamagePoint)
            {
                hasDealtDamage = true;
                OnAttackHit();
            }
        }
        
        /// <summary>
        /// ทำ Damage ให้ผู้เล่น
        /// </summary>
        void OnAttackHit()
        {
            Debug.Log("<color=yellow>[Animator]</color> Attack Hit Point!");
            
            if (monsterAI != null)
            {
                monsterAI.DealDamageToPlayer();
            }
        }
        
        /// <summary>
        /// จบ Animation โจมตี
        /// </summary>
        public void OnAttackEnd()
        {
            if (animator == null) return;
            
            animator.SetBool(IS_ATTACKING, false);
            hasDealtDamage = false;
            
            Debug.Log("<color=cyan>[Animator]</color> Attack Animation Ended");
        }
        
        /// <summary>
        /// เล่น Animation โดนโจมตี
        /// </summary>
        public void PlayHit()
        {
            if (animator == null) return;
            
            animator.SetTrigger(IS_HIT);
            
            Debug.Log("<color=orange>[Animator]</color> Playing Hit Animation");
        }
        
        /// <summary>
        /// เล่น Animation ตาย
        /// </summary>
        public void PlayDeath()
        {
            if (animator == null) return;
            
            animator.SetBool(IS_DEAD, true);
            animator.SetFloat(SPEED, 0);
            
            Debug.Log("<color=red>[Animator]</color> Playing Death Animation");
        }
        
        /// <summary>
        /// ตรวจสอบว่ากำลังโจมตีอยู่หรือไม่
        /// </summary>
        public bool IsAttacking()
        {
            if (animator == null) return false;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(ATTACK_STATE) || animator.GetBool(IS_ATTACKING);
        }
        
        /// <summary>
        /// ตรวจสอบว่ากำลังโดนโจมตีอยู่หรือไม่
        /// </summary>
        public bool IsGettingHit()
        {
            if (animator == null) return false;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(HIT_STATE);
        }
        
        /// <summary>
        /// ตรวจสอบว่า Animation โจมตีจบแล้วหรือยัง
        /// </summary>
        public bool IsAttackAnimationFinished()
        {
            if (animator == null) return true;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return !stateInfo.IsName(ATTACK_STATE) || stateInfo.normalizedTime >= 0.95f;
        }
        
        /// <summary>
        /// ตรวจสอบว่า Animation ตายจบแล้วหรือยัง
        /// </summary>
        public bool IsDeathAnimationFinished()
        {
            if (animator == null) return true;
            
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(DIE_STATE) && stateInfo.normalizedTime >= 0.95f;
        }
    }
}