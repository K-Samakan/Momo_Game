using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class UnderwaterCameraEffect : MonoBehaviour
{
    [Header("=== References ===")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private SwimmingControllerHybrid swimmingController;
    
    [Header("=== Underwater Color ===")]
    [SerializeField] private Color underwaterColor = new Color(0.1f, 0.3f, 0.6f, 1f);
    [SerializeField] private float underwaterColorIntensity = 0.7f;
    
    [Header("=== Vignette Settings ===")]
    [SerializeField] private float vignetteIntensity = 0.4f;
    [SerializeField] private float vignetteSmoothness = 0.3f;
    
    [Header("=== Transition Settings ===")]
    [SerializeField] private float fadeSpeed = 2f;
    
    [Header("=== Audio Settings ===")]
    [SerializeField] private AudioClip underwaterAmbience;
    [SerializeField] private float underwaterVolume = 0.5f;
    [SerializeField] private float audioFadeSpeed = 1f;
    
    // ← เพิ่มใหม่: Damage Effect Settings
    [Header("=== Damage Effect Settings ===")]
    [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 1f); // สีแดง
    [SerializeField] private float damageColorIntensity = 0.8f; // ความเข้มสีแดง
    [SerializeField] private float damageVignetteIntensity = 0.6f; // ขอบมืดเมื่อเสียเลือด
    [SerializeField] private float damageEffectDuration = 0.3f; // ระยะเวลาแสดงผล
    [SerializeField] private AnimationCurve damageFadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0); // Fade Out Curve
    
    [Header("=== Camera Shake Settings ===")]
    [SerializeField] private bool enableCameraShake = true;
    [SerializeField] private float shakeIntensity = 0.3f; // ความแรงการสั่น
    [SerializeField] private float shakeDuration = 0.2f; // ระยะเวลาสั่น
    [SerializeField] private float shakeFrequency = 25f; // ความถี่การสั่น
    
    [Header("=== Debug ===")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Post-Processing Components
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;
    
    // Audio Components
    private AudioSource underwaterAudioSource;
    
    // State
    private bool isUnderwater = false;
    private float currentColorWeight = 0f;
    private float currentVignetteWeight = 0f;
    private float currentAudioVolume = 0f;
    
    // ← เพิ่มใหม่: Damage Effect State
    private bool isDamageEffectActive = false;
    private float damageEffectTimer = 0f;
    private Vector3 originalCameraPosition;
    private Transform cameraTransform;
    private float shakeTimer = 0f;
    
    // ← เพิ่มใหม่: Health Tracking
    private int lastKnownHealth = -1;

    void Start()
    {
        // Auto-find Volume
        if (postProcessVolume == null)
        {
            postProcessVolume = FindObjectOfType<Volume>();
            
            if (postProcessVolume == null)
            {
                Debug.LogError("❌ No Volume found! Creating one...");
                CreatePostProcessVolume();
            }
        }
        
        // Auto-find SwimmingController
        if (swimmingController == null)
        {
            swimmingController = FindObjectOfType<SwimmingControllerHybrid>();
            
            if (swimmingController == null)
            {
                Debug.LogError("❌ SwimmingControllerHybrid not found!");
            }
        }
        
        // ← เพิ่มใหม่: Get Camera Transform
        cameraTransform = transform;
        originalCameraPosition = cameraTransform.localPosition;
        
        // Get Post-Processing Components
        if (postProcessVolume != null)
        {
            if (!postProcessVolume.profile.TryGet(out colorAdjustments))
            {
                colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>();
            }
            
            if (!postProcessVolume.profile.TryGet(out vignette))
            {
                vignette = postProcessVolume.profile.Add<Vignette>();
            }
            
            // ตั้งค่าเริ่มต้น
            if (colorAdjustments != null)
            {
                colorAdjustments.active = true;
                colorAdjustments.colorFilter.overrideState = true;
                colorAdjustments.colorFilter.value = Color.white;
            }
            
            if (vignette != null)
            {
                vignette.active = true;
                vignette.intensity.overrideState = true;
                vignette.intensity.value = 0f;
                vignette.smoothness.overrideState = true;
                vignette.smoothness.value = vignetteSmoothness;
                vignette.color.overrideState = true;
                vignette.color.value = Color.black;
            }
            
            if (showDebugLogs)
            {
                Debug.Log("✅ Underwater Camera Effect initialized");
            }
        }
        
        SetupAudio();
        
        // ← เพิ่มใหม่: Initialize Health
        if (swimmingController != null)
        {
            lastKnownHealth = swimmingController.GetCurrentHealth();
        }
    }

    private void SetupAudio()
    {
        if (underwaterAmbience == null)
        {
            Debug.LogWarning("⚠️ Underwater Ambience AudioClip not assigned!");
            return;
        }
        
        underwaterAudioSource = gameObject.AddComponent<AudioSource>();
        underwaterAudioSource.clip = underwaterAmbience;
        underwaterAudioSource.loop = true;
        underwaterAudioSource.playOnAwake = false;
        underwaterAudioSource.volume = 0f;
        underwaterAudioSource.spatialBlend = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log($"🎵 Underwater audio setup: {underwaterAmbience.name}");
        }
    }

    void Update()
    {
        if (swimmingController == null) return;
        
        // ← เพิ่มใหม่: Check Health Change (Damage Detection)
        CheckHealthChange();
        
        // Underwater Effect
        bool wasUnderwater = isUnderwater;
        isUnderwater = swimmingController.IsUnderwater();
        
        if (wasUnderwater != isUnderwater && showDebugLogs)
        {
            Debug.Log(isUnderwater ? "🌊 Underwater - Enabling effects" : "🌤️ Above water - Disabling effects");
        }
        
        // ← แก้: คำนวณ Target Weight (รวม Damage Effect)
        float targetColorWeight;
        float targetVignetteWeight;
        Color targetColor;
        
        if (isDamageEffectActive)
        {
            // ← Damage Effect Active: ใช้สีแดง
            float damageProgress = damageFadeCurve.Evaluate(damageEffectTimer / damageEffectDuration);
            targetColorWeight = damageColorIntensity * damageProgress;
            targetVignetteWeight = damageVignetteIntensity * damageProgress;
            targetColor = Color.Lerp(Color.white, damageColor, targetColorWeight);
            
            // Update Timer
            damageEffectTimer += Time.deltaTime;
            
            if (damageEffectTimer >= damageEffectDuration)
            {
                isDamageEffectActive = false;
                damageEffectTimer = 0f;
                
                if (showDebugLogs)
                {
                    Debug.Log("🩹 Damage effect ended");
                }
            }
        }
        else
        {
            // ← Normal: ใช้สีน้ำเงิน (ถ้าอยู่ใต้น้ำ)
            targetColorWeight = isUnderwater ? underwaterColorIntensity : 0f;
            targetVignetteWeight = isUnderwater ? vignetteIntensity : 0f;
            targetColor = Color.Lerp(Color.white, underwaterColor, targetColorWeight);
        }
        
        float targetAudioVolume = isUnderwater ? underwaterVolume : 0f;
        
        // Lerp ไปหา Target
        currentColorWeight = Mathf.Lerp(currentColorWeight, targetColorWeight, Time.deltaTime * fadeSpeed);
        currentVignetteWeight = Mathf.Lerp(currentVignetteWeight, targetVignetteWeight, Time.deltaTime * fadeSpeed);
        currentAudioVolume = Mathf.Lerp(currentAudioVolume, targetAudioVolume, Time.deltaTime * audioFadeSpeed);
        
        // Apply Effects
        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.value = targetColor;
        }
        
        if (vignette != null)
        {
            vignette.intensity.value = currentVignetteWeight;
        }
        
        UpdateAudio();
        
        // ← เพิ่มใหม่: Camera Shake
        if (enableCameraShake)
        {
            UpdateCameraShake();
        }
        
        if (showDebugLogs && Time.frameCount % 60 == 0)
        {
            Debug.Log($"🎨 Color: {currentColorWeight:F2} | Vignette: {currentVignetteWeight:F2} | Audio: {currentAudioVolume:F2} | Damage: {isDamageEffectActive}");
        }
    }

    // ← เพิ่มใหม่: Check Health Change
    private void CheckHealthChange()
    {
        if (swimmingController == null) return;
        
        int currentHealth = swimmingController.GetCurrentHealth();
        
        // ตรวจจับการเสียเลือด
        if (lastKnownHealth != -1 && currentHealth < lastKnownHealth)
        {
            int damageTaken = lastKnownHealth - currentHealth;
            TriggerDamageEffect(damageTaken);
        }
        
        lastKnownHealth = currentHealth;
    }

    // ← เพิ่มใหม่: Trigger Damage Effect
    private void TriggerDamageEffect(int damageAmount)
    {
        isDamageEffectActive = true;
        damageEffectTimer = 0f;
        
        // เริ่ม Camera Shake
        if (enableCameraShake)
        {
            shakeTimer = shakeDuration;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"🩸 Damage taken: {damageAmount} | Triggering effect");
        }
    }

    // ← เพิ่มใหม่: Camera Shake
    private void UpdateCameraShake()
    {
        if (shakeTimer > 0f)
        {
            // สุ่มตำแหน่งกล้อง
            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                0f
            ) * shakeIntensity;
            
            // ลด Intensity ตามเวลา
            float shakeProgress = shakeTimer / shakeDuration;
            shakeOffset *= shakeProgress;
            
            cameraTransform.localPosition = originalCameraPosition + shakeOffset;
            
            shakeTimer -= Time.deltaTime;
            
            // Reset ตำแหน่งเมื่อหมดเวลา
            if (shakeTimer <= 0f)
            {
                cameraTransform.localPosition = originalCameraPosition;
                shakeTimer = 0f;
            }
        }
    }

    private void UpdateAudio()
    {
        if (underwaterAudioSource == null) return;
        
        underwaterAudioSource.volume = currentAudioVolume;
        
        if (isUnderwater && !underwaterAudioSource.isPlaying)
        {
            underwaterAudioSource.Play();
            
            if (showDebugLogs)
            {
                Debug.Log("🎵 Underwater audio started");
            }
        }
        
        if (!isUnderwater && underwaterAudioSource.isPlaying && currentAudioVolume < 0.01f)
        {
            underwaterAudioSource.Stop();
            
            if (showDebugLogs)
            {
                Debug.Log("🎵 Underwater audio stopped");
            }
        }
    }

    private void CreatePostProcessVolume()
    {
        GameObject volumeObj = new GameObject("Global Volume");
        postProcessVolume = volumeObj.AddComponent<Volume>();
        postProcessVolume.isGlobal = true;
        postProcessVolume.priority = 1;
        
        postProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
        
        Debug.Log("✅ Created new Post-Process Volume");
    }

    public void SetUnderwaterEffect(bool enable)
    {
        isUnderwater = enable;
    }
    
    // ← เพิ่มใหม่: Public Method สำหรับทดสอบ
    public void TestDamageEffect()
    {
        TriggerDamageEffect(1);
    }
    
    void OnDisable()
    {
        if (underwaterAudioSource != null && underwaterAudioSource.isPlaying)
        {
            underwaterAudioSource.Stop();
        }
        
        // ← เพิ่มใหม่: Reset Camera Position
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = originalCameraPosition;
        }
    }
}