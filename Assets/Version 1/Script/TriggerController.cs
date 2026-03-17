using UnityEngine;
using System.Collections;

public class TriggerController : MonoBehaviour
{
    [Header("Trigger Settings")]
    public float baseTriggerResistance = 0.3f;
    public float maxTriggerResistance = 0.9f;
    public bool enableTriggerEffects = true;
    
    [Header("Debug")]
    public bool showTriggerDebug = false;

    // สำหรับ Mirror Control
    private InteractionMirror currentMirror;
    private bool isL2Active = false;
    private bool isR2Active = false;
    
    // สำหรับ Adaptive Trigger Effect
    private Coroutine triggerEffectCoroutine;

    private void Start()
    {
        // ตั้งค่า Trigger เริ่มต้น
        ResetTriggers();
    }

    public void SetCurrentMirror(InteractionMirror mirror)
    {
        currentMirror = mirror;
        
        if (mirror != null)
        {
            UpdateTriggerForMirror(mirror);
        }
        else
        {
            ResetTriggers();
        }
    }

    public void OnL2Pressed()
    {
        if (currentMirror == null || !currentMirror.CanRotate()) return;
        
        isL2Active = true;
        Debug.Log($"🎮 L2 Trigger pressed - starting resistance effect");
        
        // เริ่ม Trigger Effect
        StartTriggerEffect(false); // Left = false
        
        // เริ่มการหมุน Mirror
        currentMirror.StartRotation(false); // L2 = rotate left
    }

    public void OnL2Released()
    {
        isL2Active = false;
        Debug.Log($"🎮 L2 Trigger released");
        
        // หยุด Trigger Effect
        StopTriggerEffect();
        
        // หยุดการหมุน Mirror
        if (currentMirror != null)
        {
            currentMirror.StopRotation();
        }
    }

    public void OnR2Pressed()
    {
        if (currentMirror == null || !currentMirror.CanRotate()) return;
        
        isR2Active = true;
        Debug.Log($"🎮 R2 Trigger pressed - starting resistance effect");
        
        // เริ่ม Trigger Effect
        StartTriggerEffect(true); // Right = true
        
        // เริ่มการหมุน Mirror
        currentMirror.StartRotation(true); // R2 = rotate right
    }

    public void OnR2Released()
    {
        isR2Active = false;
        Debug.Log($"🎮 R2 Trigger released");
        
        // หยุด Trigger Effect
        StopTriggerEffect();
        
        // หยุดการหมุน Mirror
        if (currentMirror != null)
        {
            currentMirror.StopRotation();
        }
    }

    private void UpdateTriggerForMirror(InteractionMirror mirror)
    {
        if (!enableTriggerEffects || HapticManager.Instance == null) return;

        if (mirror.IsFakeMirror())
        {
            // Fake Mirror: แรงต้านขึ้นตามจำนวนครั้งที่หมุน
            float count = mirror.GetFakeRotationCount();
            float maxCount = mirror.maxFakeRotations;
            float resistance = Mathf.Lerp(baseTriggerResistance, maxTriggerResistance, count / maxCount);
            
            if (showTriggerDebug)
            {
                Debug.Log($"🎮 Fake Mirror Trigger: Count {count}/{maxCount}, Resistance: {resistance:F2}");
            }
            
            HapticManager.Instance.SetMirrorTrigger(false, resistance);
        }
        else
        {
            // Real Mirror: แรงต้านเบาๆ คงที่
            if (showTriggerDebug)
            {
                Debug.Log($"🎮 Real Mirror Trigger: Base resistance {baseTriggerResistance:F2}");
            }
            
            HapticManager.Instance.SetMirrorTrigger(true, baseTriggerResistance);
        }
    }

    private void StartTriggerEffect(bool isRightTrigger)
    {
        if (triggerEffectCoroutine != null)
        {
            StopCoroutine(triggerEffectCoroutine);
        }

        triggerEffectCoroutine = StartCoroutine(ContinuousTriggerEffect(isRightTrigger));
    }

    private void StopTriggerEffect()
    {
        if (triggerEffectCoroutine != null)
        {
            StopCoroutine(triggerEffectCoroutine);
            triggerEffectCoroutine = null;
        }

        // รีเซ็ต Trigger กลับเป็นปกติ
        if (currentMirror != null)
        {
            UpdateTriggerForMirror(currentMirror);
        }
        else
        {
            ResetTriggers();
        }
    }

    private IEnumerator ContinuousTriggerEffect(bool isRightTrigger)
    {
        if (HapticManager.Instance == null || currentMirror == null) yield break;

        float baseResistance = GetCurrentResistance();
        float activeResistance = Mathf.Clamp01(baseResistance + 0.3f); // เพิ่มแรงต้านขณะกด

        string triggerName = isRightTrigger ? "R2" : "L2";
        
        if (showTriggerDebug)
        {
            Debug.Log($"🎮 {triggerName} Continuous Effect: {baseResistance:F2} → {activeResistance:F2}");
        }

        while ((isRightTrigger && isR2Active) || (!isRightTrigger && isL2Active))
        {
            // ตั้งค่าแรงต้านแบบต่อเนื่อง
            if (isRightTrigger)
            {
                HapticManager.Instance.SetMirrorTrigger(currentMirror.IsFakeMirror() ? false : true, activeResistance);
            }
            else
            {
                HapticManager.Instance.SetMirrorTrigger(currentMirror.IsFakeMirror() ? false : true, activeResistance);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private float GetCurrentResistance()
    {
        if (currentMirror == null) return baseTriggerResistance;

        if (currentMirror.IsFakeMirror())
        {
            float count = currentMirror.GetFakeRotationCount();
            float maxCount = currentMirror.maxFakeRotations;
            return Mathf.Lerp(baseTriggerResistance, maxTriggerResistance, count / maxCount);
        }
        else
        {
            return baseTriggerResistance;
        }
    }

    public void ResetTriggers()
    {
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.ResetAdaptiveTriggers();
        }
        
        if (showTriggerDebug)
        {
            Debug.Log($"🎮 Triggers reset to default");
        }
    }

    // Public methods สำหรับการตั้งค่า
    public void SetTriggerEnabled(bool enabled)
    {
        enableTriggerEffects = enabled;
        
        if (!enabled)
        {
            ResetTriggers();
        }
        else if (currentMirror != null)
        {
            UpdateTriggerForMirror(currentMirror);
        }
    }

    public void SetBaseTriggerResistance(float resistance)
    {
        baseTriggerResistance = Mathf.Clamp01(resistance);
        
        if (currentMirror != null)
        {
            UpdateTriggerForMirror(currentMirror);
        }
    }

    public void SetMaxTriggerResistance(float resistance)
    {
        maxTriggerResistance = Mathf.Clamp01(resistance);
        
        if (currentMirror != null)
        {
            UpdateTriggerForMirror(currentMirror);
        }
    }

    private void OnDestroy()
    {
        StopTriggerEffect();
        ResetTriggers();
    }

    // Debug Info
    public void LogTriggerInfo()
    {
        Debug.Log($"=== TriggerController Info ===");
        Debug.Log($"Current Mirror: {(currentMirror != null ? currentMirror.name : "None")}");
        Debug.Log($"L2 Active: {isL2Active}");
        Debug.Log($"R2 Active: {isR2Active}");
        Debug.Log($"Base Resistance: {baseTriggerResistance:F2}");
        Debug.Log($"Max Resistance: {maxTriggerResistance:F2}");
        Debug.Log($"Current Resistance: {GetCurrentResistance():F2}");
        Debug.Log($"Effects Enabled: {enableTriggerEffects}");
        Debug.Log($"==============================");
    }
}