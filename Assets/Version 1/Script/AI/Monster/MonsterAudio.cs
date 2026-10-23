using UnityEngine;

namespace MOMO.AI.Monster
{
    [RequireComponent(typeof(AudioSource))]
    public class MonsterAudio : MonoBehaviour
    {
        [Tooltip("ลาก MonsterSFX_Common ไฟล์เดียวกันใส่ทุกตัว")]
        public MonsterSFXProfile profile;

        [Tooltip("กันเสียงโดนตีซ้อนกันถี่เกินไป (วินาที)")]
        public float hitCooldown = 0.08f;

        AudioSource _source;
        float _lastHitTime = -999f;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 1f;                       // 3D เต็มที่
            _source.rolloffMode = AudioRolloffMode.Linear;

            if (profile != null)
            {
                _source.minDistance = profile.minDistance;
                _source.maxDistance = profile.maxDistance;
                if (profile.outputGroup != null)
                    _source.outputAudioMixerGroup = profile.outputGroup;
            }
        }

        public void PlayAttack()
        {
            if (profile == null) return;
            Play(profile.GetRandom(profile.attackClips), profile.attackVolume);
        }

        public void PlayHit()
        {
            if (profile == null) return;
            if (Time.time < _lastHitTime + hitCooldown) return;
            _lastHitTime = Time.time;
            Play(profile.GetRandom(profile.hitClips), profile.hitVolume);
        }

        void Play(AudioClip clip, float volume)
        {
            if (clip == null || _source == null) return;
            _source.pitch = Random.Range(profile.pitchRange.x, profile.pitchRange.y);
            _source.PlayOneShot(clip, volume);
        }
    }
}