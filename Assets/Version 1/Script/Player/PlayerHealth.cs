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

        // ✅ เปลี่ยนเป็น public เพื่อให้ SaveManager Reset ได้
        [HideInInspector] public bool _isDead = false;
        [HideInInspector] public float regenTimer = 0f;
        [HideInInspector] public bool isRegenActive = false;

        private Gamepad gamepad;

        void Start()
        {
            currentHealth = maxHealth;
            _isDead = false;
            regenTimer = 0f;
            isRegenActive = false;
            UpdateHealthUI();

            gamepad = Gamepad.current;
            OnHPChanged?.Invoke(currentHealth, maxHealth);
        }

        void Update()
        {
            HandleRegen();
        }

        public bool IsAlive() => currentHealth > 0 && !_isDead;

        public void Heal(int amount)
        {
            if (currentHealth >= maxHealth) return;

            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            VibrateController(healVibrationIntensity, healVibrationDuration);

            if (healEffect != null)
            {
                var fx = Instantiate(healEffect, transform.position, Quaternion.identity);
                fx.transform.SetParent(transform);
                Destroy(fx, 2f);
            }

            if (healSound != null) AudioSource.PlayClipAtPoint(healSound, transform.position);

            UpdateHealthUI();
            OnHPChanged?.Invoke(currentHealth, maxHealth);
            OnHealed?.Invoke();
        }

        public void TakeDamage(int amount)
        {
            if (_isDead || currentHealth <= 0) return;

            currentHealth = Mathf.Max(currentHealth - amount, 0);
            regenTimer = 0f;
            isRegenActive = false;

            StartCoroutine(PulseVibration(damageVibrationIntensity, damagePulseCount,
                                          damagePulseDuration, damagePauseDuration));

            if (damageEffect != null)
            {
                var fx = Instantiate(damageEffect, transform.position, Quaternion.identity);
                fx.transform.SetParent(transform);
                Destroy(fx, 2f);
            }

            if (damageSound != null) AudioSource.PlayClipAtPoint(damageSound, transform.position);

            UpdateHealthUI();

            var anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("GetHit");

            OnHPChanged?.Invoke(currentHealth, maxHealth);
            OnDamaged?.Invoke();

            if (currentHealth <= 0) Die();
        }

        void Die()
        {
            if (_isDead) return;
            _isDead = true;

            Debug.Log("<color=red>[Player]</color> Died!");
            StartCoroutine(PulseVibration(1.0f, 5, 0.2f, 0.1f));

            var anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("Die");

            var tpc = GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.enabled = false;

            DeathScreenManager.Instance?.ShowDeathScreen();
            OnDied?.Invoke();
        }

        public void FallDeath()
        {
            if (_isDead) return;
            _isDead = true;

            currentHealth = 0;
            UpdateHealthUI();
            OnHPChanged?.Invoke(currentHealth, maxHealth);

            StartCoroutine(PulseVibration(1.0f, 3, 0.3f, 0.1f));

            var anim = GetComponent<Animator>();
            if (anim != null) anim.SetTrigger("DieFall");

            var tpc = GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.enabled = false;

            DeathScreenManager.Instance?.ShowDeathScreen();
            OnDied?.Invoke();
        }

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

        private void VibrateController(float intensity, float duration)
        {
            if (!enableHaptics) return;
            if (gamepad == null) gamepad = Gamepad.current;
            if (gamepad == null) return;

            (gamepad as IDualMotorRumble)?.SetMotorSpeeds(intensity, intensity);
            StartCoroutine(StopVibrationAfterDelay(duration));
        }

        private IEnumerator StopVibrationAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            (gamepad as IDualMotorRumble)?.SetMotorSpeeds(0f, 0f);
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
            => (gamepad as IDualMotorRumble)?.SetMotorSpeeds(0f, 0f);
    }
}