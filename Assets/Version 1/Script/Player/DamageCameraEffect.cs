using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using MOMO.Player;

namespace MOMO.Camera
{
    /// <summary>
    /// ย้อมจอสีแดง + Vignette + กล้องสั่นตอนโดนโจมตี
    /// ใช้ได้ทุกแมพเพราะผูกกับ PlayerHealth.OnDamaged (ไม่ต้องมี SwimmingControllerHybrid)
    /// ติดบน Main Camera ของแมพนั้นๆ
    /// </summary>
    public class DamageCameraEffect : MonoBehaviour
    {
        [Header("=== References ===")]
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private PlayerHealth playerHealth;

        [Header("=== Damage Color ===")]
        [SerializeField] private Color damageColor = new Color(1f, 0f, 0f, 1f);
        [SerializeField] private float damageColorIntensity = 0.8f;
        [SerializeField] private float damageVignetteIntensity = 0.6f;
        [SerializeField] private float damageEffectDuration = 0.3f;
        [SerializeField] private AnimationCurve damageFadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("=== Camera Shake ===")]
        [SerializeField] private bool enableCameraShake = true;
        [SerializeField] private float shakeIntensity = 0.3f;
        [SerializeField] private float shakeDuration = 0.2f;

        [Header("=== Debug ===")]
        [SerializeField] private bool showDebugLogs = false;

        private ColorAdjustments colorAdjustments;
        private Vignette vignette;

        private bool isDamageEffectActive = false;
        private float damageEffectTimer = 0f;
        private float shakeTimer = 0f;
        private Vector3 originalCameraPosition;

        void Start()
        {
            if (postProcessVolume == null)
                postProcessVolume = FindObjectOfType<Volume>();

            if (postProcessVolume == null)
            {
                Debug.LogError("❌ [DamageCameraEffect] No Volume found in scene!");
                return;
            }

            if (playerHealth == null)
                playerHealth = FindObjectOfType<PlayerHealth>();

            if (playerHealth == null)
            {
                Debug.LogError("❌ [DamageCameraEffect] PlayerHealth not found!");
                return;
            }

            originalCameraPosition = transform.localPosition;

            if (!postProcessVolume.profile.TryGet(out colorAdjustments))
                colorAdjustments = postProcessVolume.profile.Add<ColorAdjustments>();

            if (!postProcessVolume.profile.TryGet(out vignette))
                vignette = postProcessVolume.profile.Add<Vignette>();

            colorAdjustments.active = true;
            colorAdjustments.colorFilter.overrideState = true;
            colorAdjustments.colorFilter.value = Color.white;

            vignette.active = true;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0f;
            vignette.color.overrideState = true;
            vignette.color.value = Color.black;

            playerHealth.OnDamaged += HandleDamaged;
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.OnDamaged -= HandleDamaged;
        }

        private void HandleDamaged()
        {
            isDamageEffectActive = true;
            damageEffectTimer = 0f;

            if (enableCameraShake)
                shakeTimer = shakeDuration;

            if (showDebugLogs)
                Debug.Log("🩸 [DamageCameraEffect] Damage effect triggered");
        }

        void Update()
        {
            if (colorAdjustments == null) return;

            if (isDamageEffectActive)
            {
                float progress = damageFadeCurve.Evaluate(damageEffectTimer / damageEffectDuration);
                float colorWeight = damageColorIntensity * progress;
                float vignetteWeight = damageVignetteIntensity * progress;

                colorAdjustments.colorFilter.value = Color.Lerp(Color.white, damageColor, colorWeight);
                vignette.intensity.value = vignetteWeight;

                damageEffectTimer += Time.deltaTime;

                if (damageEffectTimer >= damageEffectDuration)
                {
                    isDamageEffectActive = false;
                    colorAdjustments.colorFilter.value = Color.white;
                    vignette.intensity.value = 0f;
                }
            }

            if (enableCameraShake && shakeTimer > 0f)
            {
                float shakeProgress = shakeTimer / shakeDuration;
                Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shakeIntensity * shakeProgress;
                transform.localPosition = originalCameraPosition + offset;

                shakeTimer -= Time.deltaTime;
                if (shakeTimer <= 0f)
                    transform.localPosition = originalCameraPosition;
            }
        }
    }
}