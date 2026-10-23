using UnityEngine;
using UnityEngine.Audio;

namespace MOMO.AI.Monster
{
    [CreateAssetMenu(fileName = "MonsterSFX_Common", menuName = "MOMO/Monster/SFX Profile")]
    public class MonsterSFXProfile : ScriptableObject
    {
        [Header("Attack (เสียงตอนโจมตี)")]
        public AudioClip[] attackClips;
        [Range(0f, 1f)] public float attackVolume = 0.8f;

        [Header("Hit (เสียงตอนโดนโจมตี)")]
        public AudioClip[] hitClips;
        [Range(0f, 1f)] public float hitVolume = 0.9f;

        [Header("Variation")]
        [Tooltip("สุ่ม pitch กันเสียงซ้ำ")]
        public Vector2 pitchRange = new Vector2(0.92f, 1.08f);

        [Header("3D Sound")]
        public float minDistance = 2f;
        public float maxDistance = 20f;

        [Header("Mixer (ไม่บังคับ)")]
        public AudioMixerGroup outputGroup;

        public AudioClip GetRandom(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}