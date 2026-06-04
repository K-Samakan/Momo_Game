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

        [Header("Animation Settings")]
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

        bool _hasDealtDamage = false;

        void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            // ❌ ลบออก: if (monsterAI == null) monsterAI = GetComponent<MonsterAI_Hybrid>();
        }

        void Update()
        {
            if (animator == null || agent == null) return;
            UpdateMovementAnimation();
            CheckAttackDamagePoint();
        }

        // ─── Movement ───────────────────────────────────────────

        void UpdateMovementAnimation()
        {
            //float speed = agent.isStopped ? 0f : agent.velocity.magnitude;
            //animator.SetFloat(SPEED, speed, speedDampTime, Time.deltaTime);
        }

        // ✅ เพิ่มใหม่ — MonsterBT เรียกเพื่อ set speed โดยตรง
        public void SetMovementSpeed(float speed)
        {
            if (animator == null) return;
            animator.SetFloat(SPEED, speed, speedDampTime, Time.deltaTime);
        }

        // ─── Attack ─────────────────────────────────────────────

        public void PlayAttack()
        {
            if (animator == null) return;
            animator.SetTrigger(ATTACK_TRIGGER);
            animator.SetBool(IS_ATTACKING, true);
            _hasDealtDamage = false;
        }

        void CheckAttackDamagePoint()
        {
            if (_hasDealtDamage) return;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsName(ATTACK_STATE) && info.normalizedTime >= attackDamagePoint)
            {
                _hasDealtDamage = true;
                animator.SetBool(IS_ATTACKING, false);
            }
        }

        public bool IsAttacking()
        {
            if (animator == null) return false;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(ATTACK_STATE) || animator.GetBool(IS_ATTACKING);
        }

        // ✅ เพิ่มใหม่ — MonsterBT เช็คว่า attack animation จบแล้วหรือยัง
        public bool IsAttackFinished()
        {
            if (animator == null) return true;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(ATTACK_STATE) && info.normalizedTime >= 1f;
        }

        // ─── Hit ────────────────────────────────────────────────

        public void PlayHit()
        {
            if (animator == null) return;
            animator.SetTrigger(IS_HIT);
        }

        // ✅ เพิ่มใหม่ — MonsterBT เช็ค hit stun animation
        public bool IsGettingHit()
        {
            if (animator == null) return false;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(HIT_STATE);
        }

        // ─── Death ──────────────────────────────────────────────

        public void PlayDeath()
        {
            if (animator == null) return;
            animator.SetBool(IS_DEAD, true);
            animator.SetTrigger(DIE_STATE);
        }

        public bool IsDeathAnimationFinished()
        {
            if (animator == null) return true;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            return info.IsName(DIE_STATE) && info.normalizedTime >= 0.95f;
        }
    }
}