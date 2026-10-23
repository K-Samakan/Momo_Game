using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(AudioSource))]
public class BossMap1Audio : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] footstepClips;
    public AudioClip[] slamClips;
    public AudioClip[] spinClips;
    public AudioClip[] hurtClips;
    public AudioClip deathClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float footstepVolume = 0.8f;
    [Range(0f, 1f)] public float attackVolume = 1f;
    [Range(0f, 1f)] public float hurtVolume = 0.9f;
    [Range(0f, 1f)] public float deathVolume = 1f;

    [Header("Footstep")]
    public NavMeshAgent agent;
    public float stepInterval = 0.55f;
    public float moveThreshold = 0.2f;

    [Header("Hurt")]
    public float hurtCooldown = 0.1f;

    [Header("Pitch Variation")]
    public float pitchMin = 0.92f;
    public float pitchMax = 1.08f;

    private AudioSource _sfx;
    private float _stepTimer;
    private float _lastHurtTime = -999f;
    private bool _isDead;

    private void Awake()
    {
        _sfx = GetComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.spatialBlend = 1f; // 3D

        if (agent == null) agent = GetComponentInParent<NavMeshAgent>();
    }

    private void Update()
    {
        if (_isDead) return;

        bool moving = agent != null && agent.enabled &&
                      agent.velocity.sqrMagnitude > moveThreshold * moveThreshold;

        if (moving)
        {
            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                PlayFootstep();
                _stepTimer = stepInterval;
            }
        }
        else
        {
            _stepTimer = 0f; // เริ่มเดินแล้วได้ยินก้าวแรกทันที
        }
    }

    // ── Animation Event ─────────────────────────────────────
    public void PlayFootstep() => PlayRandom(footstepClips, footstepVolume);
    public void PlaySlam() => PlayRandom(slamClips, attackVolume);
    public void PlaySpin() => PlayRandom(spinClips, attackVolume);

    // ── เรียกจาก BossMap1Health ──────────────────────────────
    public void PlayHurt()
    {
        if (_isDead || Time.time - _lastHurtTime < hurtCooldown) return;
        _lastHurtTime = Time.time;
        PlayRandom(hurtClips, hurtVolume);
    }

    public void PlayDeath()
    {
        if (_isDead) return;
        _isDead = true;

        _sfx.Stop();
        _sfx.pitch = 1f;
        if (deathClip != null) _sfx.PlayOneShot(deathClip, deathVolume);
    }

    // ── Helper ──────────────────────────────────────────────
    private void PlayRandom(AudioClip[] clips, float volume)
    {
        if (_isDead || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        _sfx.pitch = Random.Range(pitchMin, pitchMax);
        _sfx.PlayOneShot(clip, volume);
    }
}