using UnityEngine;
using UnityEngine.Video;

namespace MOMO.Cutscene
{
    /// <summary>
    /// ScriptableObject สำหรับเก็บข้อมูล 1 Cutscene Slide
    /// ประกอบด้วย: Video, BGM, CC Subtitle
    /// </summary>
    [CreateAssetMenu(fileName = "NewVideoSlide", menuName = "MOMO/Video Cutscene Slide")]
    public class VideoSlideData : ScriptableObject
    {
        [Header("=== Video ===")]
        [Tooltip("ไฟล์วิดีโอ MP4 (ไม่มีเสียง)")]
        public VideoClip videoClip;
        
        [Header("=== Background Music ===")]
        [Tooltip("ไฟล์เสียง BGM (MP3/WAV)")]
        public AudioClip bgmClip;
        
        [Range(0f, 1f)]
        [Tooltip("ระดับเสียง BGM (0 = เงียบ, 1 = เต็ม)")]
        public float bgmVolume = 0.7f;
        
        [Tooltip("Loop BGM หรือไม่?")]
        public bool loopBGM = true;
        
        [Header("=== CC Subtitle (Closed Caption) ===")]
        [Tooltip("ข้อความ CC Subtitle ภาษาไทย")]
        public CCSubtitleData ccSubtitle;
        
        [Header("=== Timing ===")]
        [Tooltip("ระยะเวลา Fade In (วินาที)")]
        public float fadeInDuration = 0.5f;
        
        [Tooltip("ระยะเวลา Fade Out (วินาที)")]
        public float fadeOutDuration = 0.5f;
    }
    
    /// <summary>
    /// ข้อมูล CC Subtitle แบบ Time-based
    /// รองรับ Subtitle หลายบรรทัดตาม Timeline วิดีโอ
    /// </summary>
    [System.Serializable]
    public class CCSubtitleData
    {
        [Tooltip("รายการ Subtitle แต่ละบรรทัดพร้อม Timing")]
        public SubtitleLine[] lines;
    }
    
    [System.Serializable]
    public class SubtitleLine
    {
        [Tooltip("เวลาเริ่มแสดง (วินาที)")]
        public float startTime;
        
        [Tooltip("เวลาสิ้นสุด (วินาที)")]
        public float endTime;
        
        [TextArea(2, 5)]
        [Tooltip("ข้อความ Subtitle")]
        public string text;
    }
}