using UnityEngine;
using UnityEngine.Video;   // ← เพิ่ม

namespace MOMO.Cutscene
{
    public enum SlideType { Image, Video }   // ← เพิ่ม

    [CreateAssetMenu(fileName = "NewCutsceneSlide", menuName = "MOMO/Cutscene Slide")]
    public class CutsceneSlideData : ScriptableObject
    {
        [Header("=== ชนิดสไลด์ ===")]
        public SlideType type = SlideType.Image;   // ← เลือกรูป/วิดีโอ

        [Header("=== ภาพนิ่ง (ใช้เมื่อ type = Image) ===")]
        public Sprite image;

        [Header("=== วิดีโอ (ใช้เมื่อ type = Video) ===")]
        public VideoClip videoClip;
        public bool loopVideo = false;

        [Header("=== เสียงประจำสไลด์ ===")]
        public AudioClip sfx;
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        public bool loopSfx = false;

        [Header("=== Subtitle (พิมพ์ทีละตัว) ===")]
        [TextArea(2, 6)] public string subtitleText;
        public float typeSpeed = 0.04f;

        [Header("=== Auto Advance ===")]
        public float autoAdvanceDelay = 5f;

        [Header("=== การสั่น (ตอนสไลด์ขึ้น) ===")]
        public bool playHapticOnShow = true;
        [Range(0f, 1f)] public float hapticLeft = 0.4f;
        [Range(0f, 1f)] public float hapticRight = 0.4f;
        public float hapticDuration = 0.2f;

        [Header("=== Fade ===")]
        public float fadeInDuration = 0.4f;
    }
}