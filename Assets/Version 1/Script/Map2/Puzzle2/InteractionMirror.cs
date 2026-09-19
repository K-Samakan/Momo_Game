using UnityEngine;
using System.Collections;

public class InteractionMirror : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationAmount = -22.5f;
    public float holdDuration = 3f;

    [Header("Rotation Limits")]
    public float maxRotationOffset = 45f;

    [Header("Mirror Type")]
    public bool isFakeMirror = false;
    public int maxFakeRotations = 5;
    public int damageAmount = 1;

    [Header("Interaction Settings")]
    public string playerTag = "Player";
    public float interactionRange = 3f;

    [Header("Debug")]
    public bool showDebugInfo = false;

    private bool isRotating = false;
    private Coroutine currentRotationCoroutine;
    private bool canInteract = false;

    // เก็บค่า Y rotation เริ่มต้น
    private float initialYRotation;
    private float minYRotation;
    private float maxYRotation;

    // สำหรับ Fake Mirror
    private int currentFakeRotationCount = 0;
    private bool hasTriggeredDamage = false;
    
    // สำหรับ Haptic Feedback ระหว่างกด
    private bool isL2Pressed = false;
    private bool isR2Pressed = false;
    private Coroutine hapticFeedbackCoroutine;

    private void Start()
    {
        SetupRotationLimits();
        CreateInteractionZone();
        MirrorInteractionManager.Instance.RegisterMirror(this);

        if (isFakeMirror)
        {
            Debug.Log($"💀 {gameObject.name} is a FAKE MIRROR! Rotation limit: {maxFakeRotations}");
        }
        else
        {
            Debug.Log($"✨ {gameObject.name} is a REAL MIRROR");
        }
    }

    private void SetupRotationLimits()
    {
        initialYRotation = transform.eulerAngles.y;
        minYRotation = initialYRotation - maxRotationOffset;
        maxYRotation = initialYRotation + maxRotationOffset;
        minYRotation = NormalizeAngle(minYRotation);
        maxYRotation = NormalizeAngle(maxYRotation);

        Debug.Log($"🎯 {gameObject.name} Rotation Setup:");
        Debug.Log($"   Initial Y: {initialYRotation}°");
        Debug.Log($"   Min Y: {minYRotation}°");
        Debug.Log($"   Max Y: {maxYRotation}°");
    }

    private float NormalizeAngle(float angle)
    {
        angle = angle % 360f;
        if (angle < 0)
            angle += 360f;
        return angle;
    }

    public bool CanRotateInDirection(bool rotateRight)
    {
        if (!CanRotate()) return false;

        if (isFakeMirror && hasTriggeredDamage)
        {
            Debug.Log($"💀 {gameObject.name} has already triggered damage - cannot rotate anymore!");
            return false;
        }

        float currentY = transform.eulerAngles.y;
        float targetY = currentY + (rotateRight ? rotationAmount : -rotationAmount);
        targetY = NormalizeAngle(targetY);

        bool canRotate = IsAngleWithinLimits(targetY);

        if (!canRotate)
        {
            string direction = rotateRight ? "right" : "left";
            Debug.Log($"❌ {gameObject.name} cannot rotate {direction} - would exceed limits");
        }

        return canRotate;
    }

    private bool IsAngleWithinLimits(float angle)
    {
        angle = NormalizeAngle(angle);

        if (minYRotation <= maxYRotation)
        {
            return angle >= minYRotation && angle <= maxYRotation;
        }
        else
        {
            return angle >= minYRotation || angle <= maxYRotation;
        }
    }

    private void CreateInteractionZone()
    {
        GameObject interactionZone = new GameObject("InteractionZone");
        interactionZone.transform.SetParent(transform);
        interactionZone.transform.localPosition = Vector3.zero;
        interactionZone.layer = gameObject.layer;

        SphereCollider collider = interactionZone.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = interactionRange;

        MirrorInteractionZone zone = interactionZone.AddComponent<MirrorInteractionZone>();
        zone.parentMirror = this;
    }

    public void SetInteractionState(bool canInteract)
    {
        this.canInteract = canInteract;

        if (canInteract)
        {
            Debug.Log($"🎯 Can now interact with {gameObject.name}");
            SetAdaptiveTrigger();
            ShowRotationInfo();
            
            // แสดง UI
            if (MirrorUIManager.Instance != null)
            {
                MirrorUIManager.Instance.ShowMirrorInteractionUI(this);
            }
        }
        else
        {
            Debug.Log($"❌ Cannot interact with {gameObject.name}");
            ClearAdaptiveTrigger();
            StopHapticFeedback();
            
            // ซ่อน UI
            if (MirrorUIManager.Instance != null)
            {
                MirrorUIManager.Instance.HideMirrorInteractionUI();
            }
        }
    }

    private void ShowRotationInfo()
    {
        float currentY = transform.eulerAngles.y;
        bool canRotateLeft = CanRotateInDirection(false);
        bool canRotateRight = CanRotateInDirection(true);

        Debug.Log($"📊 {gameObject.name} Rotation Status:");
        Debug.Log($"   Type: {(isFakeMirror ? "💀 FAKE MIRROR" : "✨ REAL MIRROR")}");
        Debug.Log($"   Current Y: {currentY}°");
        Debug.Log($"   Can rotate left (L2): {(canRotateLeft ? "✅" : "❌")}");
        Debug.Log($"   Can rotate right (R2): {(canRotateRight ? "✅" : "❌")}");

        if (isFakeMirror)
        {
            Debug.Log($"   Fake rotations: {currentFakeRotationCount}/{maxFakeRotations}");
            Debug.Log($"   Damage triggered: {(hasTriggeredDamage ? "💀 YES" : "❌ No")}");
        }
    }

    public bool CanRotate()
    {
        return canInteract && !isRotating;
    }

    public void StartRotation(bool rotateRight)
    {
        if (!CanRotateInDirection(rotateRight)) return;

        float rotation = rotateRight ? rotationAmount : -rotationAmount;
        currentRotationCoroutine = StartCoroutine(RotateAfterDelay(rotation));

        string direction = rotateRight ? "right" : "left";
        Debug.Log($"🔄 {gameObject.name} starting rotation {direction}: {rotation}°");
        
        // เริ่ม UI Progress
        if (MirrorUIManager.Instance != null)
        {
            MirrorUIManager.Instance.StartHoldProgress(rotateRight, holdDuration);
        }
        
        // เริ่ม Haptic Feedback
        if (rotateRight)
        {
            isR2Pressed = true;
            OnButtonPressed("R2");
        }
        else
        {
            isL2Pressed = true;
            OnButtonPressed("L2");
        }
    }

    public void StopRotation()
    {
        if (currentRotationCoroutine != null)
        {
            StopCoroutine(currentRotationCoroutine);
            currentRotationCoroutine = null;
            isRotating = false;
            Debug.Log($"⏹️ {gameObject.name} rotation stopped");
        }
        
        // หยุด UI Progress แต่ไม่ซ่อน UI Panel
        if (MirrorUIManager.Instance != null)
        {
            MirrorUIManager.Instance.StopHoldProgress(false);
            MirrorUIManager.Instance.StopHoldProgress(true);
            
            // แสดง UI อีกครั้งหากยัง interact ได้อยู่
            if (canInteract)
            {
                MirrorUIManager.Instance.ShowMirrorInteractionUI(this);
            }
        }
        
        // หยุด Haptic Feedback
        isL2Pressed = false;
        isR2Pressed = false;
        OnButtonReleased();
    }

    private void OnButtonPressed(string buttonName)
    {
        Debug.Log($"🎮 {buttonName} pressed on {gameObject.name}");
        
        if (hapticFeedbackCoroutine != null)
        {
            StopCoroutine(hapticFeedbackCoroutine);
        }
        
        hapticFeedbackCoroutine = StartCoroutine(PlayContinuousHapticFeedback());
    }
    
    private void OnButtonReleased()
    {
        Debug.Log($"🎮 Button released on {gameObject.name}");
        StopHapticFeedback();
    }
    
    private void StopHapticFeedback()
    {
        if (hapticFeedbackCoroutine != null)
        {
            StopCoroutine(hapticFeedbackCoroutine);
            hapticFeedbackCoroutine = null;
        }
        
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.Vibrate(0f, 0f, 0f);
        }
    }
    
    private IEnumerator PlayContinuousHapticFeedback()
    {
        if (HapticManager.Instance == null) yield break;
        
        float intensity = GetContinuousHapticIntensity();
        
        string mirrorType = isFakeMirror ? "FAKE" : "REAL";
        Debug.Log($"🎮 Starting continuous haptic for {mirrorType} mirror: {gameObject.name} (Intensity: {intensity:F2})");
        
        HapticManager.Instance.Vibrate(intensity, intensity, holdDuration);
        
        float hapticStartTime = Time.time;
        while ((isL2Pressed || isR2Pressed) && (Time.time - hapticStartTime < holdDuration))
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.Vibrate(0f, 0f, 0f);
        }
    }
    
    private float GetContinuousHapticIntensity()
    {
        if (!isFakeMirror)
        {
            // Real Mirror: ความแรงคงที่เบาๆ
            return 0.2f;
        }
        else
        {
            // Fake Mirror: เริ่มจากความแรงเดียวกับ Real Mirror แต่เพิ่มขึ้นเมื่อหมุนมากขึ้น
            float baseIntensity = 0.2f; // เริ่มต้นเท่ากับ Real Mirror
            
            if (currentFakeRotationCount == 0)
            {
                return baseIntensity; // ครั้งแรกเท่ากับ Real Mirror
            }
            else
            {
                // เพิ่มความแรงตามจำนวนครั้งที่หมุน
                float progressMultiplier = (float)currentFakeRotationCount / maxFakeRotations;
                float intensity = baseIntensity + (progressMultiplier * 0.7f); // เพิ่มขึ้นถึง 0.9f
                
                // ถ้าใกล้ระเบิด (เหลือ 1 ครั้ง) ให้แรงสุด
                if (currentFakeRotationCount >= maxFakeRotations - 1)
                {
                    intensity = 0.95f; // แรงมากแต่ไม่เต็มที่ เพื่อให้รู้สึกถึงการเตือน
                }
                
                return Mathf.Clamp01(intensity);
            }
        }
    }

    private IEnumerator RotateAfterDelay(float rotationY)
    {
        isRotating = true;

        Debug.Log($"⏰ {gameObject.name} waiting {holdDuration} seconds...");
        yield return new WaitForSeconds(holdDuration);

        if (!canInteract)
        {
            Debug.Log($"❌ {gameObject.name} can no longer interact - canceling rotation");
            isRotating = false;
            currentRotationCoroutine = null;
            yield break;
        }

        Vector3 currentRotation = transform.eulerAngles;
        float newYRotation = currentRotation.y + rotationY;
        newYRotation = NormalizeAngle(newYRotation);

        if (!IsAngleWithinLimits(newYRotation))
        {
            Debug.Log($"❌ {gameObject.name} rotation canceled - would exceed limits");
            isRotating = false;
            currentRotationCoroutine = null;
            yield break;
        }

        transform.rotation = Quaternion.Euler(currentRotation.x, newYRotation, currentRotation.z);
        Debug.Log($"✅ {gameObject.name} rotated to Y: {newYRotation}°");

        // ไม่เล่น Haptic Feedback เมื่อหมุนสำเร็จ - ให้เงียบเพื่อให้ผู้เล่นสังเกตเห็นความแตกต่าง

        if (isFakeMirror)
        {
            HandleFakeMirrorRotation();
        }

        // อัพเดท UI หลังหมุนเสร็จ
        if (MirrorUIManager.Instance != null && canInteract)
        {
            yield return new WaitForSeconds(0.1f);
            MirrorUIManager.Instance.ShowMirrorInteractionUI(this);
        }

        ShowRotationInfo();
        isRotating = false;
        currentRotationCoroutine = null;
    }

    private void HandleFakeMirrorRotation()
    {
        currentFakeRotationCount++;
        Debug.Log($"💀 FAKE MIRROR rotated! Count: {currentFakeRotationCount}/{maxFakeRotations}");

        if (currentFakeRotationCount >= maxFakeRotations && !hasTriggeredDamage)
        {
            hasTriggeredDamage = true;
            TriggerFakeMirrorDamage();
        }
        else if (!hasTriggeredDamage)
        {
            int remaining = maxFakeRotations - currentFakeRotationCount;
            Debug.Log($"⚠️ WARNING! {remaining} more rotations until damage!");
            
            if (canInteract)
            {
                SetAdaptiveTrigger();
            }
        }
    }
    
    private void TriggerFakeMirrorDamage()
    {
        Debug.Log($"💀💀💀 FAKE MIRROR DAMAGE TRIGGERED! 💀💀💀");
        Debug.Log($"🩸 Player loses {damageAmount} HP!");

        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayMirrorExplosionFeedback();
        }

        MirrorInteractionManager.Instance.OnPlayerTakeDamage(damageAmount, gameObject.name);
        PlayDamageEffect();
    }

    private void PlayDamageEffect()
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.Play();
        }

        StartCoroutine(ShakeAndDisappear());
        Debug.Log($"💥 {gameObject.name} damage effects activated!");
    }

    private IEnumerator ShakeAndDisappear()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer == null)
        {
            yield return new WaitForSeconds(1f);
            gameObject.SetActive(false);
            yield break;
        }

        Material originalMaterial = renderer.material;
        Color originalColor = originalMaterial.color;
        float fadeDuration = 2f;
        float elapsed = 0f;

        Debug.Log($"🌫️ {gameObject.name} starting fade out...");

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            Color newColor = originalColor;
            newColor.a = alpha;
            renderer.material.color = newColor;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Color finalColor = originalColor;
        finalColor.a = 0f;
        renderer.material.color = finalColor;

        Debug.Log($"👻 {gameObject.name} has faded out and disappeared!");
        gameObject.SetActive(false);
    }

    // Public methods สำหรับระบบอื่นๆ
    public bool IsFakeMirror()
    {
        return isFakeMirror;
    }

    public int GetFakeRotationCount()
    {
        return currentFakeRotationCount;
    }

    public bool HasTriggeredDamage()
    {
        return hasTriggeredDamage;
    }

    public void ResetFakeMirror()
    {
        if (isFakeMirror)
        {
            currentFakeRotationCount = 0;
            hasTriggeredDamage = false;

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }
            
            transform.localScale = Vector3.one;
            
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f;
                renderer.material.color = color;
            }
            
            Debug.Log($"🔄 {gameObject.name} fake mirror has been reset and reappeared!");
        }
    }

    private void SetAdaptiveTrigger()
    {
        if (HapticManager.Instance == null) return;
        
        float resistance = GetAdaptiveTriggerResistance();
        HapticManager.Instance.SetTriggerResistance(resistance);
        
        string mirrorType = isFakeMirror ? "FAKE" : "REAL";
        Debug.Log($"🔧 Set {mirrorType} Mirror trigger resistance: {resistance:F2}");
    }
    
    private void ClearAdaptiveTrigger()
    {
        if (HapticManager.Instance == null) return;
        
        HapticManager.Instance.SetTriggerResistance(0.0f);
        Debug.Log($"🔧 Cleared trigger resistance - normal button feel");
    }
    
    private float GetAdaptiveTriggerResistance()
    {
        // ทั้งกระจกจริงและปลอมใช้แรงต้านเท่ากันหมด
        // ปรับค่าให้สูงขึ้นเพื่อให้รู้สึกหนักเหมือนผลักกระจกจริง
        return 0.65f;
    }

    private void OnDestroy()
    {
        StopHapticFeedback();
        ClearAdaptiveTrigger();
        
        if (MirrorInteractionManager.Instance != null)
        {
            MirrorInteractionManager.Instance.UnregisterMirror(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        Vector3 center = transform.position;
        float radius = 2f;
        Color baseColor = isFakeMirror ? Color.red : Color.blue;

        Vector3 initialDirection = Quaternion.Euler(0, initialYRotation, 0) * Vector3.forward;
        Gizmos.color = baseColor;
        Gizmos.DrawLine(center, center + initialDirection * radius);

        Vector3 minDirection = Quaternion.Euler(0, minYRotation, 0) * Vector3.forward;
        Gizmos.color = hasTriggeredDamage ? Color.black : Color.yellow;
        Gizmos.DrawLine(center, center + minDirection * radius);

        Vector3 maxDirection = Quaternion.Euler(0, maxYRotation, 0) * Vector3.forward;
        Gizmos.color = hasTriggeredDamage ? Color.black : Color.yellow;
        Gizmos.DrawLine(center, center + maxDirection * radius);

        Vector3 currentDirection = transform.forward;
        Gizmos.color = hasTriggeredDamage ? Color.black : Color.green;
        Gizmos.DrawLine(center, center + currentDirection * radius);
    }
}