using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;

namespace MOMO.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        public int maxHealth = 5;
        public int currentHealth;

        [Header("UI (Legacy - ของเดิม)")]
        public Text healthText;
        public Slider healthSlider;

        [Header("Visual Feedback")]
        public GameObject healEffect;
        public AudioClip healSound;
        public GameObject damageEffect;
        public AudioClip damageSound;

        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private float healVibrationIntensity = 0.2f;
        [SerializeField] private float healVibrationDuration = 0.15f;
        [SerializeField] private float damageVibrationIntensity = 0.7f;
        [SerializeField] private int damagePulseCount = 3;
        [SerializeField] private float damagePulseDuration = 0.15f;
        [SerializeField] private float damagePauseDuration = 0.05f;

        [Header("Auto Regen")]
        public bool autoRegen = true;
        public float regenDelay = 5f;
        public float regenInterval = 2f;

        // Events
        public event Action<int, int> OnHPChanged;
        public event Action OnDamaged;
        public event Action OnHealed;
        public event Action OnDied;

        // Private
        private Gamepad gamepad;
        private float regenTimer = 0f;
        private bool isRegenActive = false;
        private bool _isDead = false; // ป้องกัน Die() ถูกเรียกซ้ำ

        // ========================================
        void Start()
        {
            currentHealth = maxHealth;
            _isDead = false;
            UpdateHealthUI();

            gamepad = Gamepad.current;
            if (gamepad != null)
                Debug.Log($"✅ [PlayerHealth] Found Gamepad: {gamepad.displayName}");

            OnHPChanged?.Invoke(currentHealth, maxHealth);
        }

        void Update()
        {
            HandleRegen();
        }

        // ========================================
        // Public API
        // ========================================

        /// <summary>
        /// เช็คว่าผู้เล่นยังมีชีวิตอยู่ไหม
        /// </summary>
        public bool IsAlive() => currentHealth > 0 && !_isDead;

        public void Heal(int amount)
        {
            if (currentHealth >= maxHealth)
            {
                Debug.Log("<color=yellow>[Player]</color> HP is already full!");
                return;
            }

            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);

            Debug.Log($"<color=green>[Player]</color> Healed +{amount}! HP: {currentHealth}/{maxHealth}");

            VibrateController(healVibrationIntensity, healVibrationDuration);

            if (healEffect != null)
            {
                GameObject effect = Instantiate(healEffect, transform.position, Quaternion.identity);
                effect.transform.SetParent(transform);
                Destroy(effect, 2f);
            }

            if (healSound != null)
                AudioSource.PlayClipAtPoint(healSound, transform.position);

            UpdateHealthUI();
            OnHPChanged?.Invoke(currentHealth, maxHealth);
            OnHealed?.Invoke();
        }

        public void TakeDamage(int amount)
        {
            if (_isDead || currentHealth <= 0) return;

            currentHealth -= amount;
            currentHealth = Mathf.Max(currentHealth, 0);

            Debug.Log($"<color=red>[Player]</color> Took {amount} damage! HP: {currentHealth}/{maxHealth}");

            // Reset Regen Timer
            regenTimer = 0f;
            isRegenActive = false;

            StartCoroutine(PulseVibration(
                damageVibrationIntensity,
                damagePulseCount,
                damagePulseDuration,
                damagePauseDuration
            ));

            if (damageEffect != null)
            {
                GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
                effect.transform.SetParent(transform);
                Destroy(effect, 2f);
            }

            if (damageSound != null)
                AudioSource.PlayClipAtPoint(damageSound, transform.position);

            UpdateHealthUI();

            // Animation โดนตี
            Animator anim = GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("GetHit");

            OnHPChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke();

            if (currentHealth <= 0)
                Die();
        }

        // ========================================
        // Die
        // ========================================

        void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("<color=red>[Player]</color> Died!");
            StartCoroutine(PulseVibration(1.0f, 5, 0.2f, 0.1f));

            // ── Animation ตายปกติ ──
            Animator anim = GetComponent<Animator>();
            if (anim != null)
                anim.SetTrigger("Die");        // ← ท่าตายปกติ เช่น ล้มลง

            var tpc = GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.enabled = false;

            DeathScreenManager.Instance?.ShowDeathScreen();
            OnDied?.Invoke();
        }

        public void FallDeath()
{
    if (_isDead) return;
    _isDead = true;

    Debug.Log("<color=red>[Player]</color> Fell off the platform!");

    currentHealth = 0;
    UpdateHealthUI();
    OnHPChanged?.Invoke(currentHealth, maxHealth);

    StartCoroutine(PulseVibration(1.0f, 3, 0.3f, 0.1f));

    // ── Animation ตายจากตก ──
    Animator anim = GetComponent<Animator>();
    if (anim != null)
        anim.SetTrigger("DieFall");    // ← ท่าตกหายไป/หมุน

    var tpc = GetComponent<StarterAssets.ThirdPersonController>();
    if (tpc != null) tpc.enabled = false;

    DeathScreenManager.Instance?.ShowDeathScreen();
    OnDied?.Invoke();
}

        // ========================================
        // Auto Regen
        // ========================================

        private void HandleRegen()
        {
            if (!autoRegen || _isDead || currentHealth >= maxHealth) return;

            regenTimer += Time.deltaTime;

            if (!isRegenActive && regenTimer >= regenDelay)
                isRegenActive = true;

            if (isRegenActive && regenTimer >= regenDelay + regenInterval)
            {
                Heal(1);
                regenTimer = regenDelay;
            }
        }

        // ========================================
        // UI
        // ========================================

        public void UpdateHealthUI()
        {
            if (healthText != null)
                healthText.text = $"HP: {currentHealth}/{maxHealth}";

            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
                healthSlider.value = currentHealth;
            }
        }

        // ========================================
        // Haptic
        // ========================================

        private void VibrateController(float intensity, float duration)
        {
            if (!enableHaptics) return;
            if (gamepad == null) gamepad = Gamepad.current;
            if (gamepad == null) return;

            var haptics = gamepad as IDualMotorRumble;
            if (haptics == null) return;

            haptics.SetMotorSpeeds(intensity, intensity);
            StartCoroutine(StopVibrationAfterDelay(duration));
        }

        private IEnumerator StopVibrationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (gamepad != null)
            {
                var haptics = gamepad as IDualMotorRumble;
                haptics?.SetMotorSpeeds(0f, 0f);
            }
        }

        private IEnumerator PulseVibration(float intensity, int pulseCount,
            float pulseDuration, float pauseDuration)
        {
            if (!enableHaptics) yield break;
            if (gamepad == null) gamepad = Gamepad.current;
            if (gamepad == null) yield break;

            var haptics = gamepad as IDualMotorRumble;
            if (haptics == null) yield break;

            for (int i = 0; i < pulseCount; i++)
            {
                haptics.SetMotorSpeeds(intensity, intensity);
                yield return new WaitForSeconds(pulseDuration);
                haptics.SetMotorSpeeds(0f, 0f);

                if (i < pulseCount - 1)
                    yield return new WaitForSeconds(pauseDuration);
            }
        }

        public void TestVibration(float intensity = 0.5f, float duration = 0.3f)
            => VibrateController(intensity, duration);

        public void StopVibration()
        {
            if (gamepad != null)
            {
                var haptics = gamepad as IDualMotorRumble;
                haptics?.SetMotorSpeeds(0f, 0f);
            }
        }
    }
}