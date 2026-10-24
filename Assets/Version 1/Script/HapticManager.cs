using UnityEngine;
using UnityEngine.InputSystem;
using UniSense;
using UniSense.LowLevel;
using Unity.VisualScripting;
using System.Collections;

public enum HapticPatternType
{
    Enemy,  // ศัตรู
    Shoot,  // การยิง
    Event,  // เหตุการณ์
    Boom,    // ระเบิด
    Short,
    Long,
    Success,
    Fail,
    BlockPush, // การผลักบล็อก
    IceSlide,  // การเลื่อนบนน้ำแข็ง
    Warning,    // เตือนบล็อกใกล้แตก
    
    // เพิ่ม Pattern ใหม่สำหรับ Mirror Puzzle
    MirrorCheck,     // ตรวจสอบกระจก
    MirrorDanger,    // กระจกอันตราย (หลอก)
    MirrorSafe,      // กระจกปลอดภัย (จริง)
    MirrorRotate,    // หมุนกระจก
    LightReflect,    // แสงสะท้อน
    PuzzleComplete   // แก้ puzzle สำเร็จ
}

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance;

    [Header("Haptic Settings")]
    public float globalHapticMultiplier = 1f;
    public bool enableAdaptiveTriggers = true;
    public bool enableHapticFeedback = true;

    [Header("Mirror Puzzle Settings")]
    public float mirrorCheckSensitivity = 1.0f;
    public float dangerIntensityMultiplier = 1.2f;
    public bool enableMirrorHaptics = true;

    [Header("Push Count Feedback Settings")]
    public bool enablePushCountFeedback = true;
    public float[] pushCountIntensities = { 0.15f, 0.25f, 0.4f, 0.6f, 0.8f }; // ความแรงสำหรับ push count 0-4
    public float[] pushCountFrequencies = { 8f, 12f, 16f, 20f, 25f }; // ความถี่สำหรับ push count 0-4
    public float explosionIntensity = 1.0f;
    public float continuousUpdateInterval = 0.1f; // ความถี่ในการอัปเดตการสั่นแบบต่อเนื่อง

    private DualSenseGamepadHID dualSenseGamepad;
    private bool isPlayingPattern = false;
    private Coroutine currentPatternCoroutine;

    // เก็บสถานะ trigger ปัจจุบัน
    private DualSenseTriggerState currentLeftTriggerState;
    private DualSenseTriggerState currentRightTriggerState;
    private bool isDualSenseConnected = false;

    // สำหรับ Mirror Puzzle
    private bool isMirrorCheckActive = false;
    private float mirrorCheckStartTime = 0f;

    // สำหรับ Continuous Push Count Feedback
    private bool isContinuousFeedbackActive = false;
    private Coroutine continuousFeedbackCoroutine;
    private int currentContinuousPushCount = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(this.gameObject);
        else
            Instance = this;

        //DontDestroyOnLoad(this.gameObject);
        
        InitializeDualSense();
    }

    void InitializeDualSense()
    {
        dualSenseGamepad = DualSenseGamepadHID.FindFirst();
        
        if (dualSenseGamepad != null)
        {
            isDualSenseConnected = true;
            Debug.Log("DualSense Controller ตรวจพบและเชื่อมต่อสำเร็จ");
            InitializeDefaultTriggerStates();
        }
        else
        {
            isDualSenseConnected = false;
            Debug.LogWarning("ไม่พบ DualSense Controller");
        }
    }

    private void Update()
    {
        // ตรวจสอบการเชื่อมต่อ DualSense อย่างต่อเนื่อง
        if (!isDualSenseConnected)
        {
            dualSenseGamepad = DualSenseGamepadHID.FindFirst();
            if (dualSenseGamepad != null)
            {
                isDualSenseConnected = true;
                InitializeDefaultTriggerStates();
                Debug.Log("DualSense Controller เชื่อมต่อใหม่");
            }
        }
    }

    private void InitializeDefaultTriggerStates()
    {
        currentLeftTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.NoResistance
        };

        currentRightTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.NoResistance
        };

        ApplyTriggerStates();
    }

    private void ApplyTriggerStates()
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        try
        {
            dualSenseGamepad.SetGamepadState(new DualSenseGamepadState
            {
                LeftTrigger = currentLeftTriggerState,
                RightTrigger = currentRightTriggerState
            });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"ไม่สามารถตั้งค่า Adaptive Trigger ได้: {e.Message}");
        }
    }

    public void Vibrate(float leftIntensity, float rightIntensity, float duration)
    {
        if (!enableHapticFeedback) return;

        leftIntensity *= globalHapticMultiplier;
        rightIntensity *= globalHapticMultiplier;

        leftIntensity = Mathf.Clamp01(leftIntensity);
        rightIntensity = Mathf.Clamp01(rightIntensity);

        if (dualSenseGamepad != null)
        {
            dualSenseGamepad.SetMotorSpeeds(leftIntensity, rightIntensity);
        }
        else if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(leftIntensity, rightIntensity);
        }
        else
        {
            return;
        }

        StartCoroutine(StopVibrationAfter(duration));
    }

    // ฟังก์ชันสำหรับการสั่นแบบต่อเนื่อง (ไม่มีระยะเวลาจำกัด)
    public void VibrateConstant(float leftIntensity, float rightIntensity)
    {
        if (!enableHapticFeedback) return;

        leftIntensity *= globalHapticMultiplier;
        rightIntensity *= globalHapticMultiplier;

        leftIntensity = Mathf.Clamp01(leftIntensity);
        rightIntensity = Mathf.Clamp01(rightIntensity);

        if (dualSenseGamepad != null)
        {
            dualSenseGamepad.SetMotorSpeeds(leftIntensity, rightIntensity);
        }
        else if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(leftIntensity, rightIntensity);
        }
    }

    // หยุดการสั่นทันที
    public void StopVibration()
    {
        if (dualSenseGamepad != null)
        {
            dualSenseGamepad.SetMotorSpeeds(0f, 0f);
        }
        else if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    public void PlayPattern(HapticPatternType pattern)
    {
        if (!enableHapticFeedback) return;

        // หยุด pattern ที่เล่นอยู่
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
        }

        currentPatternCoroutine = StartCoroutine(PlayPatternCoroutine(pattern));
    }

    private IEnumerator PlayPatternCoroutine(HapticPatternType patternType)
    {
        if (isPlayingPattern) yield break;
        isPlayingPattern = true;

        switch (patternType)
        {
            case HapticPatternType.BlockPush:
                // รูปแบบสำหรับการผลักบล็อก
                Vibrate(0.6f, 0.6f, 0.1f);
                yield return new WaitForSeconds(0.1f);
                Vibrate(0.3f, 0.3f, 0.05f);
                break;

            case HapticPatternType.IceSlide:
                // รูปแบบสำหรับการเลื่อนบนน้ำแข็ง
                for (float t = 0; t < 0.8f; t += 0.1f)
                {
                    float intensity = Mathf.Sin(t * Mathf.PI * 4) * 0.4f + 0.2f;
                    Vibrate(intensity, intensity, 0.05f);
                    yield return new WaitForSeconds(0.05f);
                }
                break;

            case HapticPatternType.Warning:
                // รูปแบบเตือนภัย
                for (int i = 0; i < 5; i++)
                {
                    Vibrate(0.8f, 0.8f, 0.08f);
                    yield return new WaitForSeconds(0.12f);
                }
                break;

            case HapticPatternType.Success:
                // รูปแบบความสำเร็จ
                Vibrate(0f, 1f, 0.15f);
                yield return new WaitForSeconds(0.15f);
                Vibrate(1f, 0f, 0.15f);
                yield return new WaitForSeconds(0.15f);
                Vibrate(1f, 1f, 0.3f);
                break;

            case HapticPatternType.Fail:
                // รูปแบบความล้มเหลว
                for (int i = 0; i < 4; i++)
                {
                    Vibrate(1f, 1f, 0.1f);
                    yield return new WaitForSeconds(0.15f);
                }
                break;

            case HapticPatternType.Boom:
                // รูปแบบระเบิด
                Vibrate(1f, 1f, 0.4f);
                yield return new WaitForSeconds(0.4f);
                Vibrate(0.6f, 0.6f, 0.2f);
                break;

            case HapticPatternType.Short:
                Vibrate(1f, 1f, 0.2f);
                break;

            case HapticPatternType.Long:
                Vibrate(1f, 1f, 0.7f);
                break;

            // ========== Pattern ใหม่สำหรับ Mirror Puzzle ==========
            
            case HapticPatternType.MirrorCheck:
                // รูปแบบสำหรับเริ่มตรวจสอบกระจก - สั่นเบาๆ แล้วเพิ่มขึ้น
                for (float t = 0; t < 1f; t += 0.1f)
                {
                    float intensity = Mathf.Lerp(0.1f, 0.4f, t) * mirrorCheckSensitivity;
                    Vibrate(intensity, intensity, 0.08f);
                    yield return new WaitForSeconds(0.12f);
                }
                break;

            case HapticPatternType.MirrorDanger:
                // รูปแบบเตือนอันตราย - สั่นรุนแรงแบบไม่สม่ำเสมอ
                for (int i = 0; i < 8; i++)
                {
                    float randomIntensity = Random.Range(0.6f, 1.0f) * dangerIntensityMultiplier;
                    float leftIntensity = Random.Range(0.3f, randomIntensity);
                    float rightIntensity = Random.Range(0.3f, randomIntensity);
                    
                    Vibrate(leftIntensity, rightIntensity, 0.06f);
                    yield return new WaitForSeconds(Random.Range(0.08f, 0.15f));
                }
                break;

            case HapticPatternType.MirrorSafe:
                // รูปแบบปลอดภัย - สั่นอ่อนแบบจังหวะ
                for (int i = 0; i < 4; i++)
                {
                    float safeIntensity = 0.2f * mirrorCheckSensitivity;
                    Vibrate(safeIntensity, safeIntensity, 0.1f);
                    yield return new WaitForSeconds(0.15f);
                    Vibrate(safeIntensity * 0.5f, safeIntensity * 0.5f, 0.05f);
                    yield return new WaitForSeconds(0.1f);
                }
                break;

            case HapticPatternType.MirrorRotate:
                // รูปแบบการหมุน - สั่นเป็นคลื่น
                float rotateIntensity = 0.3f;
                Vibrate(rotateIntensity, 0f, 0.08f);
                yield return new WaitForSeconds(0.08f);
                Vibrate(0f, rotateIntensity, 0.08f);
                yield return new WaitForSeconds(0.08f);
                Vibrate(rotateIntensity, rotateIntensity, 0.05f);
                break;

            case HapticPatternType.LightReflect:
                // รูปแบบการสะท้อนแสง - สั่นรวดเร็วสั้นๆ
                for (int i = 0; i < 3; i++)
                {
                    Vibrate(0.4f, 0.4f, 0.03f);
                    yield return new WaitForSeconds(0.05f);
                }
                break;

            case HapticPatternType.PuzzleComplete:
                // รูปแบบความสำเร็จพิเศษ - เฉลิมฉลอง
                
                // ขั้นที่ 1: การสร้างความตื่นเต้น
                for (int i = 0; i < 5; i++)
                {
                    float intensity = Mathf.Lerp(0.3f, 0.8f, (float)i / 4f);
                    Vibrate(intensity, intensity, 0.1f);
                    yield return new WaitForSeconds(0.12f);
                }
                
                yield return new WaitForSeconds(0.2f);
                
                // ขั้นที่ 2: ระเบิดแห่งความสุข
                Vibrate(1f, 1f, 0.3f);
                yield return new WaitForSeconds(0.3f);
                
                // ขั้นที่ 3: การฉลองสลับซ้าย-ขวา
                for (int i = 0; i < 6; i++)
                {
                    if (i % 2 == 0)
                    {
                        Vibrate(0.8f, 0.2f, 0.15f); // ซ้าย
                    }
                    else
                    {
                        Vibrate(0.2f, 0.8f, 0.15f); // ขวา
                    }
                    yield return new WaitForSeconds(0.18f);
                }
                
                // ขั้นที่ 4: จบด้วยการสั่นยาวแบบค่อยๆ ลดลง
                for (float t = 0; t < 1f; t += 0.1f)
                {
                    float intensity = Mathf.Lerp(0.8f, 0.1f, t);
                    Vibrate(intensity, intensity, 0.1f);
                    yield return new WaitForSeconds(0.1f);
                }
                break;

            default:
                // รูปแบบเริ่มต้น
                Vibrate(0.5f, 0.5f, 0.2f);
                break;
        }

        isPlayingPattern = false;
        currentPatternCoroutine = null;
    }

    private IEnumerator StopVibrationAfter(float time)
    {
        yield return new WaitForSeconds(time);

        if (dualSenseGamepad != null)
        {
            dualSenseGamepad.SetMotorSpeeds(0f, 0f);
        }
        else if (Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }
    }

    // ========== ระบบ Continuous Push Count Feedback ==========

    /// <summary>
    /// เริ่มการสั่นแบบต่อเนื่องตาม Push Count
    /// </summary>
    /// <param name="pushCount">Push count ปัจจุบัน (0-4)</param>
    public void StartContinuousPushFeedback(int pushCount)
    {
        if (!enableHapticFeedback || !enablePushCountFeedback) return;

        // หยุด continuous feedback ที่เล่นอยู่
        StopContinuousPushFeedback();

        pushCount = Mathf.Clamp(pushCount, 0, 4);
        currentContinuousPushCount = pushCount;
        isContinuousFeedbackActive = true;
        
        continuousFeedbackCoroutine = StartCoroutine(ContinuousPushFeedbackCoroutine(pushCount));
        
        Debug.Log($"🎮 เริ่มการสั่นแบบต่อเนื่อง - Push Count: {pushCount}");
    }

    /// <summary>
    /// หยุดการสั่นแบบต่อเนื่อง
    /// </summary>
    public void StopContinuousPushFeedback()
    {
        if (isContinuousFeedbackActive && continuousFeedbackCoroutine != null)
        {
            StopCoroutine(continuousFeedbackCoroutine);
            continuousFeedbackCoroutine = null;
        }
        
        isContinuousFeedbackActive = false;
        currentContinuousPushCount = -1;
        StopVibration(); // หยุดการสั่นทันที
        
        Debug.Log("🎮 หยุดการสั่นแบบต่อเนื่อง");
    }

    /// <summary>
    /// Coroutine สำหรับการสั่นแบบต่อเนื่อง
    /// </summary>
    private IEnumerator ContinuousPushFeedbackCoroutine(int pushCount)
    {
        float intensity = pushCountIntensities[pushCount] * globalHapticMultiplier;
        float frequency = pushCountFrequencies[pushCount];
        float time = 0f;
        
        while (isContinuousFeedbackActive)
        {
            switch (pushCount)
            {
                case 0: // ไม่มีการผลัก - สั่นอ่อนสม่ำเสมอ
                    VibrateConstant(intensity, intensity);
                    break;
                    
                case 1: // ผลัก 1 ครั้ง - สั่นอ่อนแบบจังหวะ
                    float pulse1 = (Mathf.Sin(time * frequency) + 1f) * 0.5f * intensity;
                    VibrateConstant(pulse1, pulse1);
                    break;
                    
                case 2: // ผลัก 2 ครั้ง - สั่นปานกลางแบบคลื่น
                    float wave2 = (Mathf.Sin(time * frequency) + 1f) * 0.5f * intensity;
                    float pulse2 = (Mathf.Sin(time * frequency * 2f) + 1f) * 0.3f * intensity;
                    float combined2 = Mathf.Min(wave2 + pulse2, 1f);
                    VibrateConstant(combined2, combined2);
                    break;
                    
                case 3: // ผลัก 3 ครั้ง - สั่นแรงแบบไม่สม่ำเสมอ
                    float base3 = intensity * 0.7f;
                    float variation3 = Mathf.Sin(time * frequency) * intensity * 0.3f;
                    float noise3 = (Mathf.PerlinNoise(time * 2f, 0f) - 0.5f) * intensity * 0.2f;
                    float combined3 = Mathf.Clamp01(base3 + variation3 + noise3);
                    VibrateConstant(combined3, combined3);
                    break;
                    
                case 4: // ผลัก 4 ครั้ง - สั่นแรงมากแบบเตือนอันตราย
                    float baseIntense = intensity * 0.8f;
                    float chaotic = Random.Range(-0.3f, 0.3f) * intensity;
                    float warning = (Mathf.Sin(time * frequency * 1.5f) + 1f) * 0.5f * intensity * 0.4f;
                    
                    float leftIntense = Mathf.Clamp01(baseIntense + chaotic + warning);
                    float rightIntense = Mathf.Clamp01(baseIntense - chaotic + warning);
                    
                    VibrateConstant(leftIntense, rightIntense);
                    break;
            }
            
            time += continuousUpdateInterval;
            yield return new WaitForSeconds(continuousUpdateInterval);
        }
        
        StopVibration();
    }

    /// <summary>
    /// อัปเดต Push Count สำหรับการสั่นแบบต่อเนื่อง
    /// </summary>
    public void UpdateContinuousPushCount(int newPushCount)
    {
        if (isContinuousFeedbackActive && currentContinuousPushCount != newPushCount)
        {
            StartContinuousPushFeedback(newPushCount); // รีสตาร์ทด้วย push count ใหม่
        }
    }

    /// <summary>
    /// เล่น Haptic Feedback แบบระเบิดเมื่อ Block ถูกทำลาย
    /// </summary>
    public void PlayBlockExplosionFeedback()
    {
        if (!enableHapticFeedback) return;
        
        // หยุดการสั่นแบบต่อเนื่อง
        StopContinuousPushFeedback();
        
        // หยุด pattern ที่เล่นอยู่
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
            currentPatternCoroutine = null;
        }
        
        StartCoroutine(PlayExplosionPattern());
    }

    /// <summary>
    /// Coroutine สำหรับ pattern การระเบิด
    /// </summary>
    private IEnumerator PlayExplosionPattern()
    {
        if (isPlayingPattern) yield break;
        isPlayingPattern = true;
        
        Debug.Log("💥 เล่น Block Explosion Haptic Pattern");
        
        // ขั้นที่ 1: การสะสมพลัง (Build-up)
        for (float t = 0; t < 0.3f; t += 0.05f)
        {
            float buildIntensity = Mathf.Lerp(0.2f, 0.8f, t / 0.3f) * explosionIntensity * globalHapticMultiplier;
            Vibrate(buildIntensity, buildIntensity, 0.04f);
            yield return new WaitForSeconds(0.05f);
        }
        
        yield return new WaitForSeconds(0.1f);
        
        // ขั้นที่ 2: การระเบิด (Main Explosion)
        float maxIntensity = explosionIntensity * globalHapticMultiplier;
        Vibrate(maxIntensity, maxIntensity, 0.4f);
        
        yield return new WaitForSeconds(0.4f);
        
        // ขั้นที่ 3: คลื่นกระแทก (Shockwaves)
        for (int i = 0; i < 3; i++)
        {
            float shockIntensity = Mathf.Lerp(0.8f, 0.3f, (float)i / 2f) * explosionIntensity * globalHapticMultiplier;
            
            // สั่นซ้าย-ขวาสลับกัน
            if (i % 2 == 0)
            {
                Vibrate(shockIntensity, shockIntensity * 0.5f, 0.15f);
            }
            else
            {
                Vibrate(shockIntensity * 0.5f, shockIntensity, 0.15f);
            }
            
            yield return new WaitForSeconds(0.2f);
        }
        
        // ขั้นที่ 4: การสั่นสะเทือนหลัง (Aftershock)
        for (int i = 0; i < 5; i++)
        {
            float aftershockIntensity = Mathf.Lerp(0.4f, 0.1f, (float)i / 4f) * explosionIntensity * globalHapticMultiplier;
            Vibrate(aftershockIntensity, aftershockIntensity, 0.08f);
            yield return new WaitForSeconds(0.12f);
        }
        
        isPlayingPattern = false;
        Debug.Log("✅ Block Explosion Haptic Pattern เสร็จสิ้น");
    }

    /// <summary>
    /// เล่น Haptic Feedback ทันทีสำหรับการเพิ่ม Push Count (ไม่ใช่แบบต่อเนื่อง)
    /// </summary>
    public void PlayPushCountIncrementFeedback(int pushCount)
    {
        if (!enableHapticFeedback || !enablePushCountFeedback) return;

        pushCount = Mathf.Clamp(pushCount, 0, 4);
        float intensity = pushCountIntensities[pushCount] * globalHapticMultiplier;
        
        // เล่นการสั่นสั้นๆ เมื่อ push count เพิ่ม
        switch (pushCount)
        {
            case 0:
                Vibrate(intensity, intensity, 0.1f);
                break;
            case 1:
                Vibrate(intensity, intensity, 0.15f);
                break;
            case 2:
                Vibrate(intensity, intensity, 0.2f);
                break;
            case 3:
                Vibrate(intensity * 1.1f, intensity * 1.1f, 0.25f);
                break;
            case 4:
                Vibrate(intensity * 1.2f, intensity * 1.2f, 0.3f);
                break;
        }
        
        Debug.Log($"🎮 Push Count Increment Haptic: Level {pushCount}");
    }

    // ========== Methods เดิมที่ปรับปรุง ==========

    /// <summary>
    /// เล่น Haptic Feedback สำหรับการตรวจสอบกระจก
    /// </summary>
    /// <param name="progress">ความก้าวหน้าการตรวจสอบ (0-1)</param>
    /// <param name="isDangerous">กระจกหลอกหรือไม่</param>
    public void PlayMirrorCheckFeedback(float progress, bool isDangerous)
    {
        if (!enableHapticFeedback || !enableMirrorHaptics) return;
        
        if (isDangerous)
        {
            // กระจกหลอก: สั่นแรงขึ้นเรื่อยๆ พร้อมเตือนอันตราย
            float dangerIntensity = Mathf.Lerp(0.3f, 1.0f, progress * progress) * dangerIntensityMultiplier;
            
            if (progress > 0.85f)
            {
                // ใกล้ระเบิดมาก - สั่นเตือนรุนแรงมาก
                PlayPattern(HapticPatternType.Warning);
            }
            else if (progress > 0.7f)
            {
                // ใกล้ระเบิด - สั่นเตือนรุนแรง
                float warningIntensity = Mathf.Lerp(0.8f, 1.0f, (progress - 0.7f) / 0.3f);
                PlayBlockWarning(warningIntensity * dangerIntensityMultiplier);
            }
            else if (progress > 0.5f)
            {
                // เริ่มอันตราย - ใช้ MirrorDanger pattern
                if (!isPlayingPattern)
                {
                    PlayPattern(HapticPatternType.MirrorDanger);
                }
            }
            else
            {
                // อันตรายเล็กน้อย - สั่นแบบไม่สม่ำเสมอ
                float randomFactor = Random.Range(0.8f, 1.2f);
                float intensity = dangerIntensity * randomFactor * mirrorCheckSensitivity;
                Vibrate(intensity * 0.7f, intensity, 0.1f);
            }
        }
        else
        {
            // กระจกจริง: สั่นอ่อนๆ สม่ำเสมอ แบบปลอดภัย
            float safeIntensity = (0.15f + Mathf.Sin(progress * Mathf.PI * 3) * 0.1f) * mirrorCheckSensitivity;
            
            if (progress > 0.8f)
            {
                // ใกล้เสร็จ - ให้ feedback บวก
                safeIntensity *= 1.3f;
            }
            
            Vibrate(safeIntensity, safeIntensity, 0.08f);
        }
    }

    /// <summary>
    /// เล่น Haptic Feedback สำหรับการหมุนกระจก
    /// </summary>
    /// <param name="rotationSpeed">ความเร็วการหมุน (degrees/second)</param>
    public void PlayMirrorRotationFeedback(float rotationSpeed)
    {
        if (!enableHapticFeedback || !enableMirrorHaptics) return;
        
        float intensity = Mathf.Clamp01(Mathf.Abs(rotationSpeed) / 90f) * 0.4f * mirrorCheckSensitivity;
        
        if (rotationSpeed > 0)
        {
            // หมุนขวา - เน้นมอเตอร์ขวา
            Vibrate(intensity * 0.3f, intensity, 0.05f);
        }
        else if (rotationSpeed < 0)
        {
            // หมุนซ้าย - เน้นมอเตอร์ซ้าย
            Vibrate(intensity, intensity * 0.3f, 0.05f);
        }
    }

    /// <summary>
    /// เล่น Haptic Feedback เมื่อแสงสะท้อน
    /// </summary>
    public void PlayLightReflectionFeedback()
    {
        if (!enableHapticFeedback || !enableMirrorHaptics) return;
        PlayPattern(HapticPatternType.LightReflect);
    }

    /// <summary>
    /// เล่น Haptic Feedback เมื่อแก้ Puzzle สำเร็จ
    /// </summary>
    public void PlayPuzzleCompleteFeedback()
    {
        if (!enableHapticFeedback) return;
        PlayPattern(HapticPatternType.PuzzleComplete);
    }

    /// <summary>
    /// เริ่มการตรวจสอบกระจก
    /// </summary>
    public void StartMirrorCheck()
    {
        if (!enableMirrorHaptics) return;
        
        isMirrorCheckActive = true;
        mirrorCheckStartTime = Time.time;
        PlayPattern(HapticPatternType.MirrorCheck);
    }

    /// <summary>
    /// หยุดการตรวจสอบกระจก
    /// </summary>
    public void StopMirrorCheck()
    {
        isMirrorCheckActive = false;
        StopAllHaptics();
    }

    /// <summary>
    /// เล่น Haptic สำหรับการระเบิดของกระจกหลอก
    /// </summary>
    public void PlayMirrorExplosionFeedback()
    {
        if (!enableHapticFeedback) return;
        
        // หยุด pattern ที่เล่นอยู่ก่อน
        StopAllHaptics();
        
        // เล่น explosion pattern
        PlayPattern(HapticPatternType.Boom);
    }

    /// <summary>
    /// เล่น Haptic สำหรับการเปิดใช้งานกระจกจริง
    /// </summary>
    public void PlayMirrorActivationFeedback()
    {
        if (!enableHapticFeedback || !enableMirrorHaptics) return;
        PlayPattern(HapticPatternType.MirrorSafe);
    }

    // เมธอดสำหรับ block warning ที่ปรับปรุงแล้ว
    public void PlayBlockWarning(float intensity)
    {
        if (!enableHapticFeedback || isPlayingPattern) return;
        
        // intensity 0-1, ยิ่งมากยิ่งแรง
        float vibrationStrength = Mathf.Lerp(0.2f, 1.0f, intensity);
        float duration = Mathf.Lerp(0.1f, 0.25f, intensity);
        
        Vibrate(vibrationStrength, vibrationStrength, duration);
    }

    // ตั้งค่า adaptive trigger สำหรับบล็อกที่เสียหาย
    public void SetDamagedBlockTrigger(float damageRatio)
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        // คำนวณค่าสำหรับ trigger effect
        byte frequency = (byte)Mathf.Lerp(8, 25, damageRatio);
        byte startForce = (byte)Mathf.Lerp(50, 150, damageRatio);
        byte middleForce = (byte)Mathf.Lerp(100, 200, damageRatio);
        byte endForce = (byte)Mathf.Lerp(150, 255, damageRatio);

        var damagedTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.EffectEx,
            EffectEx = new DualSenseEffectExProperties
            {
                StartPosition = (byte)Mathf.Lerp(0, 50, damageRatio * 0.5f),
                KeepEffect = true,
                BeginForce = startForce,
                MiddleForce = middleForce,
                EndForce = endForce,
                Frequency = frequency
            }
        };

        currentLeftTriggerState = damagedTriggerState;
        currentRightTriggerState = damagedTriggerState;
        ApplyTriggerStates();
    }

    // ตั้งค่า trigger สำหรับการผลักบล็อกปกติ
    public void SetPushBlockTrigger(float pushStrength = 0.7f)
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        byte resistance = (byte)(pushStrength * 180);
        
        var pushTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.EffectEx,
            EffectEx = new DualSenseEffectExProperties
            {
                StartPosition = 20,
                KeepEffect = true,
                BeginForce = 0,
                MiddleForce = resistance,
                EndForce = 255,
                Frequency = 0
            }
        };

        currentLeftTriggerState = pushTriggerState;
        currentRightTriggerState = pushTriggerState;
        ApplyTriggerStates();
    }

    // ตั้งค่า trigger สำหรับพื้นผิวน้ำแข็ง
    public void SetIceTrigger()
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        var iceTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.EffectEx,
            EffectEx = new DualSenseEffectExProperties
            {
                StartPosition = 0,
                KeepEffect = true,
                BeginForce = 30,
                MiddleForce = 80,
                EndForce = 120,
                Frequency = 15 // ความถี่สูงเพื่อจำลองความลื่น
            }
        };

        currentLeftTriggerState = iceTriggerState;
        currentRightTriggerState = iceTriggerState;
        ApplyTriggerStates();
    }

    /// <summary>
    /// ตั้งค่า Adaptive Trigger สำหรับ Mirror Puzzle (เก่า - ยังทำให้เกิดการสั่น)
    /// </summary>
    /// <param name="mirrorType">ประเภทของกระจก</param>
    /// <param name="rotationResistance">ความต้านทานในการหมุน</param>
    public void SetMirrorTrigger(bool isRealMirror, float rotationResistance = 0.5f)
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        if (isRealMirror)
        {
            // กระจกจริง: ให้ความรู้สึกนุ่มนวลในการหมุน
            byte resistance = (byte)(rotationResistance * 120);
            
            var realMirrorTrigger = new DualSenseTriggerState
            {
                EffectType = DualSenseTriggerEffectType.EffectEx,
                EffectEx = new DualSenseEffectExProperties
                {
                    StartPosition = 10,
                    KeepEffect = true,
                    BeginForce = 20,
                    MiddleForce = resistance,
                    EndForce = (byte)(resistance + 30),
                    Frequency = 5 // ความถี่ต่ำ = นุ่มนวล
                }
            };

            currentLeftTriggerState = realMirrorTrigger;
            currentRightTriggerState = realMirrorTrigger;
        }
        else
        {
            // กระจกหลอก: ให้ความรู้สึกแข็งและไม่เสถียร
            var fakeMirrorTrigger = new DualSenseTriggerState
            {
                EffectType = DualSenseTriggerEffectType.EffectEx,
                EffectEx = new DualSenseEffectExProperties
                {
                    StartPosition = 5,
                    KeepEffect = true,
                    BeginForce = 100,
                    MiddleForce = 200,
                    EndForce = 255,
                    Frequency = 25 // ความถี่สูง = ไม่เสถียร
                }
            };

            currentLeftTriggerState = fakeMirrorTrigger;
            currentRightTriggerState = fakeMirrorTrigger;
        }

        ApplyTriggerStates();
    }

    /// <summary>
    /// ตั้งค่าแรงต้านการกดแบบต่อเนื่องสำหรับ Mirror Puzzle (ใหม่ - ไม่มีการสั่น)
    /// </summary>
    /// <param name="resistance">ความต้านทานการกด (0.0-1.0)</param>
    public void SetTriggerResistance(float resistance)
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        resistance = Mathf.Clamp01(resistance);
        
        if (resistance == 0.0f)
        {
            // ไม่มีแรงต้าน - กลับเป็นปกติ
            InitializeDefaultTriggerStates();
            Debug.Log($"🔧 Reset triggers to normal (no resistance)");
            return;
        }

        // คำนวณค่าแรงต้านแบบต่อเนื่อง (ไม่มีการสั่น)
        byte resistanceForce = (byte)(resistance * 200); // 0-200 แทน 0-255 เพื่อไม่ให้หนักเกินไป
        
        var resistanceTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.EffectEx,
            EffectEx = new DualSenseEffectExProperties
            {
                StartPosition = 0,      // เริ่มแรงต้านทันทีที่กด
                KeepEffect = true,      // คงแรงต้านไว้
                BeginForce = (byte)(resistanceForce * 0.3f),  // แรงเริ่มต้นเบา
                MiddleForce = resistanceForce,                // แรงหลักตามที่กำหนด
                EndForce = (byte)(resistanceForce * 1.2f),    // แรงสุดท้ายหนักขึ้นเล็กน้อย
                Frequency = 0           // ไม่มีการสั่น (frequency = 0)
            }
        };

        currentLeftTriggerState = resistanceTriggerState;
        currentRightTriggerState = resistanceTriggerState;
        ApplyTriggerStates();
        
        Debug.Log($"🔧 Set continuous trigger resistance: {resistance:F2} (force: {resistanceForce})");
    }

    /// <summary>
    /// ตั้งค่าแรงต้านการกดแบบต่อเนื่องแยกซ้าย-ขวา
    /// </summary>
    /// <param name="leftResistance">ความต้านทาน L2 (0.0-1.0)</param>
    /// <param name="rightResistance">ความต้านทาน R2 (0.0-1.0)</param>
    public void SetTriggerResistance(float leftResistance, float rightResistance)
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        leftResistance = Mathf.Clamp01(leftResistance);
        rightResistance = Mathf.Clamp01(rightResistance);

        // ตั้งค่า L2
        if (leftResistance == 0.0f)
        {
            currentLeftTriggerState = new DualSenseTriggerState
            {
                EffectType = DualSenseTriggerEffectType.NoResistance
            };
        }
        else
        {
            byte leftForce = (byte)(leftResistance * 200);
            currentLeftTriggerState = new DualSenseTriggerState
            {
                EffectType = DualSenseTriggerEffectType.EffectEx,
                EffectEx = new DualSenseEffectExProperties
                {
                    StartPosition = 0,
                    KeepEffect = true,
                    BeginForce = (byte)(leftForce * 0.3f),
                    MiddleForce = leftForce,
                    EndForce = (byte)(leftForce * 1.2f),
                    Frequency = 0
                }
            };
        }

        // ตั้งค่า R2
        if (rightResistance == 0.0f)
        {
            currentRightTriggerState = new DualSenseTriggerState
            {
                EffectType = DualSenseTriggerEffectType.NoResistance
            };
        }
        else
        {
            byte rightForce = (byte)(rightResistance * 200);
            currentRightTriggerState = new DualSenseTriggerState
            {
                EffectType = DualSenseTriggerEffectType.EffectEx,
                EffectEx = new DualSenseEffectExProperties
                {
                    StartPosition = 0,
                    KeepEffect = true,
                    BeginForce = (byte)(rightForce * 0.3f),
                    MiddleForce = rightForce,
                    EndForce = (byte)(rightForce * 1.2f),
                    Frequency = 0
                }
            };
        }

        ApplyTriggerStates();
        
        Debug.Log($"🔧 Set trigger resistance - L2: {leftResistance:F2}, R2: {rightResistance:F2}");
    }

    // ========== Bow Tension (FireBowController / ธนูไฟ Map3) ==========
    private int _lastBowForce = -1;

    /// <summary>
    /// Adaptive Trigger R2 สำหรับง้างธนู — ใช้โปรไฟล์ EffectEx แบบเดียวกับ Mirror Puzzle (Map2)
    /// resistance 0-1 (0.65 = ความหนักเท่ากระจก Map2, 1 = หนักสุด)
    /// ส่งไปจอยเฉพาะตอนค่าแรงเปลี่ยน (forceSend = true บังคับส่ง เช่นตอนเริ่มเล็ง/หลังระบบอื่นรีเซ็ตไก)
    /// </summary>
    public void SetBowTension(float resistance, bool forceSend = false)
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        resistance = Mathf.Clamp01(resistance);
        if (resistance <= 0f) { ReleaseBowTension(); return; }

        int force = Mathf.RoundToInt(resistance * 200f);   // 0-200 เหมือน SetTriggerResistance()
        if (!forceSend && force == _lastBowForce) return;
        _lastBowForce = force;

        currentRightTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.EffectEx,
            EffectEx = new DualSenseEffectExProperties
            {
                StartPosition = 0,                                   // ต้านทันทีที่เริ่มกด
                KeepEffect = true,                                   // ค้างแรงต้านไว้
                BeginForce = (byte)(force * 0.3f),                   // ช่วงต้นไกเบา
                MiddleForce = (byte)force,                           // ช่วงกลางตามที่กำหนด
                EndForce = (byte)Mathf.Min(255, force * 1.2f),       // ปลายไกหนักขึ้น
                Frequency = 0                                        // ไม่สั่น
            }
        };
        ApplyTriggerStates();
    }

    /// <summary>ปล่อย R2 กลับเป็นไกปกติ (ไม่มีแรงต้าน)</summary>
    public void ReleaseBowTension()
    {
        _lastBowForce = -1;
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        currentRightTriggerState = new DualSenseTriggerState
        {
            EffectType = DualSenseTriggerEffectType.NoResistance
        };
        ApplyTriggerStates();
    }

    public void ResetAdaptiveTriggers()
    {
        if (!enableAdaptiveTriggers || dualSenseGamepad == null) return;

        InitializeDefaultTriggerStates();
    }

    // เมธอดสำหรับตั้งค่าทั่วไป
    public void SetHapticEnabled(bool enabled)
    {
        enableHapticFeedback = enabled;
        if (!enabled)
        {
            StopAllHaptics();
        }
    }

    public void SetAdaptiveTriggersEnabled(bool enabled)
    {
        enableAdaptiveTriggers = enabled;
        if (!enabled)
        {
            ResetAdaptiveTriggers();
        }
    }

    public void SetGlobalHapticMultiplier(float multiplier)
    {
        globalHapticMultiplier = Mathf.Clamp01(multiplier);
    }

    /// ตั้งค่าความไวของ Mirror Haptics
    public void SetMirrorHapticSensitivity(float sensitivity)
    {
        mirrorCheckSensitivity = Mathf.Clamp(sensitivity, 0.1f, 2.0f);
    }

    /// ตั้งค่าการเปิด/ปิด Mirror Haptics
    public void SetMirrorHapticsEnabled(bool enabled)
    {
        enableMirrorHaptics = enabled;
        if (!enabled && isMirrorCheckActive)
        {
            StopMirrorCheck();
        }
    }

    /// ตั้งค่าตัวคูณความแรงสำหรับการเตือนอันตราย
    public void SetDangerIntensityMultiplier(float multiplier)
    {
        dangerIntensityMultiplier = Mathf.Clamp(multiplier, 0.5f, 2.0f);
    }

    /// <summary>
    /// ตั้งค่าความแรงสำหรับแต่ละระดับ push count
    /// </summary>
    /// <param name="level">ระดับ (0-4)</param>
    /// <param name="intensity">ความแรง (0.0-1.0)</param>
    public void SetPushCountIntensity(int level, float intensity)
    {
        if (level >= 0 && level < pushCountIntensities.Length)
        {
            pushCountIntensities[level] = Mathf.Clamp01(intensity);
            Debug.Log($"🔧 Set push count intensity level {level}: {intensity:F2}");
        }
    }

    /// <summary>
    /// ตั้งค่าความถี่สำหรับแต่ละระดับ push count
    /// </summary>
    /// <param name="level">ระดับ (0-4)</param>
    /// <param name="frequency">ความถี่</param>
    public void SetPushCountFrequency(int level, float frequency)
    {
        if (level >= 0 && level < pushCountFrequencies.Length)
        {
            pushCountFrequencies[level] = Mathf.Max(1f, frequency);
            Debug.Log($"🔧 Set push count frequency level {level}: {frequency:F1}");
        }
    }

    /// <summary>
    /// เปิด/ปิดระบบ Push Count Feedback
    /// </summary>
    public void SetPushCountFeedbackEnabled(bool enabled)
    {
        enablePushCountFeedback = enabled;
        if (!enabled)
        {
            StopContinuousPushFeedback();
        }
        Debug.Log($"🔧 Push Count Feedback: {(enabled ? "Enabled" : "Disabled")}");
    }

    /// <summary>
    /// ตั้งค่าความแรงการระเบิด
    /// </summary>
    public void SetExplosionIntensity(float intensity)
    {
        explosionIntensity = Mathf.Clamp(intensity, 0.1f, 2.0f);
        Debug.Log($"🔧 Explosion Intensity: {explosionIntensity:F2}");
    }

    /// <summary>
    /// ตั้งค่าความถี่การอัปเดตการสั่นแบบต่อเนื่อง
    /// </summary>
    public void SetContinuousUpdateInterval(float interval)
    {
        continuousUpdateInterval = Mathf.Clamp(interval, 0.05f, 0.5f);
        Debug.Log($"🔧 Continuous Update Interval: {continuousUpdateInterval:F3}s");
    }

    private void StopAllHaptics()
    {
        // หยุด pattern coroutines
        if (currentPatternCoroutine != null)
        {
            StopCoroutine(currentPatternCoroutine);
            currentPatternCoroutine = null;
        }

        // หยุด continuous feedback
        StopContinuousPushFeedback();

        // หยุดการสั่น
        StopVibration();

        isPlayingPattern = false;
    }

    private void OnDisable()
    {
        StopAllHaptics();
        ResetAdaptiveTriggers();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            StopAllHaptics();
        }
    }

    // เมธอดตรวจสอบสถานะ
    public bool IsDualSenseConnected()
    {
        return isDualSenseConnected;
    }

    public bool IsPlayingHapticPattern()
    {
        return isPlayingPattern;
    }

    public bool IsMirrorCheckActive()
    {
        return isMirrorCheckActive;
    }

    public bool IsContinuousFeedbackActive()
    {
        return isContinuousFeedbackActive;
    }

    /// <summary>
    /// ได้รับระยะเวลาที่ตรวจสอบกระจกแล้ว
    /// </summary>
    public float GetMirrorCheckDuration()
    {
        if (!isMirrorCheckActive) return 0f;
        return Time.time - mirrorCheckStartTime;
    }
    
    /// <summary>
    /// รีเซ็ตการตั้งค่าทั้งหมดเป็นค่าเริ่มต้น
    /// </summary>
    public void ResetToDefaults()
    {
        globalHapticMultiplier = 1f;
        mirrorCheckSensitivity = 1f;
        dangerIntensityMultiplier = 1.2f;
        enableHapticFeedback = true;
        enableAdaptiveTriggers = true;
        enableMirrorHaptics = true;
        enablePushCountFeedback = true;
        explosionIntensity = 1.0f;
        continuousUpdateInterval = 0.1f;
        
        // รีเซ็ต intensity arrays
        pushCountIntensities = new float[] { 0.15f, 0.25f, 0.4f, 0.6f, 0.8f };
        pushCountFrequencies = new float[] { 8f, 12f, 16f, 20f, 25f };
        
        StopAllHaptics();
        ResetAdaptiveTriggers();
    }

    /// <summary>
    /// บันทึกการตั้งค่าลง PlayerPrefs
    /// </summary>
    public void SaveHapticSettings()
    {
        PlayerPrefs.SetFloat("HapticGlobalMultiplier", globalHapticMultiplier);
        PlayerPrefs.SetFloat("HapticMirrorSensitivity", mirrorCheckSensitivity);
        PlayerPrefs.SetFloat("HapticDangerMultiplier", dangerIntensityMultiplier);
        PlayerPrefs.SetFloat("HapticExplosionIntensity", explosionIntensity);
        PlayerPrefs.SetFloat("HapticContinuousUpdateInterval", continuousUpdateInterval);
        
        PlayerPrefs.SetInt("HapticEnabled", enableHapticFeedback ? 1 : 0);
        PlayerPrefs.SetInt("AdaptiveTriggersEnabled", enableAdaptiveTriggers ? 1 : 0);
        PlayerPrefs.SetInt("MirrorHapticsEnabled", enableMirrorHaptics ? 1 : 0);
        PlayerPrefs.SetInt("PushCountFeedbackEnabled", enablePushCountFeedback ? 1 : 0);
        
        // บันทึก intensity arrays
        for (int i = 0; i < pushCountIntensities.Length; i++)
        {
            PlayerPrefs.SetFloat($"PushCountIntensity_{i}", pushCountIntensities[i]);
            PlayerPrefs.SetFloat($"PushCountFrequency_{i}", pushCountFrequencies[i]);
        }
        
        PlayerPrefs.Save();
    }

    /// <summary>
    /// โหลดการตั้งค่าจาก PlayerPrefs
    /// </summary>
    public void LoadHapticSettings()
    {
        globalHapticMultiplier = PlayerPrefs.GetFloat("HapticGlobalMultiplier", 1f);
        mirrorCheckSensitivity = PlayerPrefs.GetFloat("HapticMirrorSensitivity", 1f);
        dangerIntensityMultiplier = PlayerPrefs.GetFloat("HapticDangerMultiplier", 1.2f);
        explosionIntensity = PlayerPrefs.GetFloat("HapticExplosionIntensity", 1.0f);
        continuousUpdateInterval = PlayerPrefs.GetFloat("HapticContinuousUpdateInterval", 0.1f);
        
        enableHapticFeedback = PlayerPrefs.GetInt("HapticEnabled", 1) == 1;
        enableAdaptiveTriggers = PlayerPrefs.GetInt("AdaptiveTriggersEnabled", 1) == 1;
        enableMirrorHaptics = PlayerPrefs.GetInt("MirrorHapticsEnabled", 1) == 1;
        enablePushCountFeedback = PlayerPrefs.GetInt("PushCountFeedbackEnabled", 1) == 1;
        
        // โหลด intensity arrays
        for (int i = 0; i < pushCountIntensities.Length; i++)
        {
            pushCountIntensities[i] = PlayerPrefs.GetFloat($"PushCountIntensity_{i}", pushCountIntensities[i]);
            pushCountFrequencies[i] = PlayerPrefs.GetFloat($"PushCountFrequency_{i}", pushCountFrequencies[i]);
        }
    }

    /// <summary>
    /// ทดสอบ Continuous Push Feedback ทุกระดับ (สำหรับ Debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestContinuousPushFeedback()
    {
        StartCoroutine(TestContinuousSequence());
    }

    private IEnumerator TestContinuousSequence()
    {
        Debug.Log("🎮 เริ่มทดสอบ Continuous Push Feedback...");
        
        for (int level = 0; level < 5; level++)
        {
            Debug.Log($"▶️ ทดสอบ Continuous Level: {level} (กดปุ่มใดก็ได้เพื่อหยุด)");
            StartContinuousPushFeedback(level);
            
            // รอ 3 วินาทีหรือจนกว่าจะกดปุ่ม
            float testTime = 0f;
            while (testTime < 3f && isContinuousFeedbackActive)
            {
                if (Input.anyKeyDown)
                {
                    StopContinuousPushFeedback();
                    break;
                }
                yield return new WaitForSeconds(0.1f);
                testTime += 0.1f;
            }
            
            if (isContinuousFeedbackActive)
            {
                StopContinuousPushFeedback();
            }
            
            yield return new WaitForSeconds(1f);
        }
        
        Debug.Log("💥 ทดสอบ Explosion Pattern...");
        PlayBlockExplosionFeedback();
        
        while (isPlayingPattern)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        Debug.Log("✅ ทดสอบ Continuous Push Feedback เสร็จสิ้น!");
    }

    /// <summary>
    /// ทดสอบ Trigger Resistance (สำหรับ Debug)
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestTriggerResistance()
    {
        StartCoroutine(TestResistanceSequence());
    }

    private IEnumerator TestResistanceSequence()
    {
        Debug.Log("🎮 เริ่มทดสอบ Trigger Resistance...");
        
        // ทดสอบแรงต้านต่างๆ
        float[] resistanceLevels = { 0.0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
        
        foreach (float resistance in resistanceLevels)
        {
            Debug.Log($"🔧 ทดสอบแรงต้าน: {resistance:F1}");
            SetTriggerResistance(resistance);
            yield return new WaitForSeconds(2f);
        }
        
        // รีเซ็ตกลับเป็นปกติ
        Debug.Log("🔧 รีเซ็ตกลับเป็นปกติ");
        ResetAdaptiveTriggers();
        
        Debug.Log("✅ ทดสอบ Trigger Resistance เสร็จสิ้น!");
    }

    /// <summary>
    /// แสดงข้อมูล Debug ของ HapticManager
    /// </summary>
    public void LogDebugInfo()
    {
        Debug.Log($"=== HapticManager Debug Info ===");
        Debug.Log($"DualSense Connected: {isDualSenseConnected}");
        Debug.Log($"Haptic Enabled: {enableHapticFeedback}");
        Debug.Log($"Adaptive Triggers Enabled: {enableAdaptiveTriggers}");
        Debug.Log($"Mirror Haptics Enabled: {enableMirrorHaptics}");
        Debug.Log($"Push Count Feedback Enabled: {enablePushCountFeedback}");
        Debug.Log($"Global Multiplier: {globalHapticMultiplier}");
        Debug.Log($"Mirror Sensitivity: {mirrorCheckSensitivity}");
        Debug.Log($"Danger Multiplier: {dangerIntensityMultiplier}");
        Debug.Log($"Explosion Intensity: {explosionIntensity}");
        Debug.Log($"Continuous Update Interval: {continuousUpdateInterval}");
        Debug.Log($"Is Playing Pattern: {isPlayingPattern}");
        Debug.Log($"Is Mirror Check Active: {isMirrorCheckActive}");
        Debug.Log($"Is Continuous Feedback Active: {isContinuousFeedbackActive}");
        Debug.Log($"Current Continuous Push Count: {currentContinuousPushCount}");
        if (isMirrorCheckActive)
        {
            Debug.Log($"Mirror Check Duration: {GetMirrorCheckDuration():F2}s");
        }
        Debug.Log($"Push Count Intensities: [{string.Join(", ", pushCountIntensities)}]");
        Debug.Log($"Push Count Frequencies: [{string.Join(", ", pushCountFrequencies)}]");
        Debug.Log($"================================");
    }
}