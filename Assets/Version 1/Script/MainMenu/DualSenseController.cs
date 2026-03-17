using UnityEngine;
using UnityEngine.InputSystem;

namespace MomoGame.MainMenu
{
    /// <summary>
    /// DualSense Haptic Feedback Controller
    /// จัดการ Haptic feedback สำหรับ PS5 Controller
    /// </summary>
    public class DualSenseController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField][Range(0f, 1f)] private float globalIntensityMultiplier = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;
        
        private Gamepad gamepad;
        
        private void Awake()
        {
            gamepad = Gamepad.current;
            
            if (gamepad == null)
            {
                Debug.LogWarning("[DualSenseController] No gamepad detected!");
            }
            else if (showDebugLogs)
            {
                Debug.Log($"[DualSenseController] Gamepad detected: {gamepad.name}");
            }
        }
        
        /// <summary>
        /// เล่น Haptic Feedback
        /// </summary>
        public void TriggerHaptic(float intensity, float duration)
        {
            if (!enableHaptics) return;
            
            gamepad = Gamepad.current;
            
            if (gamepad == null) return;
            
            float adjustedIntensity = Mathf.Clamp01(intensity * globalIntensityMultiplier);
            
            // Set motor speeds (low frequency, high frequency)
            gamepad.SetMotorSpeeds(adjustedIntensity, adjustedIntensity);
            
            // Stop after duration
            StartCoroutine(StopHapticAfterDelay(duration));
            
            if (showDebugLogs)
            {
                Debug.Log($"[DualSenseController] Haptic: {adjustedIntensity:F2} for {duration}s");
            }
        }
        
        /// <summary>
        /// เล่น Haptic แบบ Left/Right แยกกัน
        /// </summary>
        public void TriggerHaptic(float leftIntensity, float rightIntensity, float duration)
        {
            if (!enableHaptics) return;
            
            gamepad = Gamepad.current;
            
            if (gamepad == null) return;
            
            float adjustedLeft = Mathf.Clamp01(leftIntensity * globalIntensityMultiplier);
            float adjustedRight = Mathf.Clamp01(rightIntensity * globalIntensityMultiplier);
            
            gamepad.SetMotorSpeeds(adjustedLeft, adjustedRight);
            
            StartCoroutine(StopHapticAfterDelay(duration));
        }
        
        /// <summary>
        /// หยุด Haptic
        /// </summary>
        public void StopHaptic()
        {
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
        }
        
        private System.Collections.IEnumerator StopHapticAfterDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            StopHaptic();
        }
        
        /// <summary>
        /// Haptic Pulse (สั้นๆ เบาๆ)
        /// </summary>
        public void Pulse(float intensity = 0.3f)
        {
            TriggerHaptic(intensity, 0.1f);
        }
        
        /// <summary>
        /// Haptic Impact (แรง สั้น)
        /// </summary>
        public void Impact(float intensity = 0.8f)
        {
            TriggerHaptic(intensity, 0.05f);
        }
        
        public void SetHapticsEnabled(bool enabled)
        {
            enableHaptics = enabled;
            
            if (!enabled)
            {
                StopHaptic();
            }
        }
        
        public void SetIntensityMultiplier(float multiplier)
        {
            globalIntensityMultiplier = Mathf.Clamp01(multiplier);
        }
        
        private void OnDestroy()
        {
            StopHaptic();
        }
        
        private void OnApplicationQuit()
        {
            StopHaptic();
        }
    }
}