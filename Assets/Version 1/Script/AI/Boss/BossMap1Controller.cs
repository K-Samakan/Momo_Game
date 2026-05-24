using System.Collections;
using UnityEngine;

public class BossMap1Controller : MonoBehaviour
{
    public enum BossState { Idle, Intro, Slam, Spin, Stagger, Dizzy, Dead }
    private BossState _state = BossState.Idle;

    [Header("Settings")]
    public float introDelay = 3f;
    public float slamStuckDuration = 1.5f;
    public float spinDizzyDuration = 3f;
    public float staggerDuration = 5f;

    [Header("References")]
    public Animator animator;
    public Transform player;
    public BossMap1Health health;

    private int _spinCount = 0;
    private int _comboCount = 0;

    private TigerAllyEffect _tigerAllyEffect;
    private MonsterHealth _monsterHealth;

    private static readonly int HashSlam = Animator.StringToHash("Slam");
    private static readonly int HashDizzy = Animator.StringToHash("Dizzy");
    private static readonly int HashStagger = Animator.StringToHash("Stagger");
    private static readonly int HashDead = Animator.StringToHash("Dead");

    private void Start()
    {
        _tigerAllyEffect = FindFirstObjectByType<TigerAllyEffect>();
        _monsterHealth = GetComponent<MonsterHealth>();
    }

    public void OnPlayerEnterBossRoom()
    {
        if (_state != BossState.Idle) return;
        _state = BossState.Intro;
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        yield return new WaitForSeconds(introDelay);
        StartCoroutine(BossAILoop());
    }

    private IEnumerator BossAILoop()
    {
        while (_state != BossState.Dead)
        {
            if (_state == BossState.Stagger || _state == BossState.Dizzy)
            {
                yield return null;
                continue;
            }

            if (Random.value < 0.7f)
                yield return StartCoroutine(SlamPattern());
            else
                yield return StartCoroutine(SpinPattern());

            yield return new WaitForSeconds(1f);
        }
    }

    private IEnumerator SlamPattern()
    {
        _state = BossState.Slam;
        FacePlayer();

        for (int i = 1; i <= 3; i++)
        {
            animator.SetTrigger(HashSlam);
            yield return new WaitForSeconds(i < 3 ? 0.8f : 1.0f);

            if (i == 3)
            {
                animator.SetBool("SlamStuck", true);
                yield return new WaitForSeconds(slamStuckDuration);
                animator.SetBool("SlamStuck", false);
            }
        }
        _state = BossState.Idle;
    }

    private IEnumerator SpinPattern()
    {
        _state = BossState.Spin;
        _spinCount = 0;

        animator.SetBool("IsSpinning", true);

        while (_spinCount < 5)
        {
            _spinCount++;
            MoveTowardPlayer(4f);
            yield return new WaitForSeconds(0.5f);
        }

        animator.SetBool("IsSpinning", false);

        _state = BossState.Dizzy;
        animator.SetTrigger(HashDizzy);
        yield return new WaitForSeconds(spinDizzyDuration);

        _state = BossState.Idle;
    }

    public void OnHitByPlayer()
    {
        _comboCount++;
        if (_comboCount >= 5)
        {
            _comboCount = 0;
            StartCoroutine(StaggerBoss());

            if (_tigerAllyEffect != null && _monsterHealth != null)
                _tigerAllyEffect.OnPlayerAttackBoss(_monsterHealth);
        }
    }

    private IEnumerator StaggerBoss()
    {
        _state = BossState.Stagger;
        animator.SetTrigger(HashStagger);
        yield return new WaitForSeconds(staggerDuration);
        _state = BossState.Idle;
    }

    public void OnDead()
    {
        _state = BossState.Dead;
        animator.SetTrigger(HashDead);
        StopAllCoroutines();
        health.bossUI.HideBossUI();
    }

    private void FacePlayer()
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    private void MoveTowardPlayer(float speed)
    {
        if (player == null) return;
        Vector3 dir = (player.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        FacePlayer();
    }
}