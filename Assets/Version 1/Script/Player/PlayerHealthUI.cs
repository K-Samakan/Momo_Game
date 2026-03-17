using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MOMO.Player; // ✅ เพิ่มบรรทัดนี้

namespace MOMO.UI // ✅ ใส่ namespace ให้เข้ากับโปรเจกต์
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("References")]
        public PlayerHealth playerHealth; // ✅ ตอนนี้หาเจอแล้ว

        [Header("HP Slots")]
        public Image[] hpSlots;
        public Color slotActiveColor = Color.green;
        public Color slotEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("HP Text")]
        public TextMeshProUGUI hpCounterText;

        [Header("Damage Flash Effect")]
        public Image damageFlashImage;
        public float flashDuration = 0.15f;
        public int flashCount = 3;
        public Color flashColor = new Color(1f, 0f, 0f, 0.4f);

        [Header("Heal Effect")]
        public Color healFlashColor = new Color(0f, 1f, 0f, 0.2f);

        [Header("Slot Animation")]
        public float slotScalePunchAmount = 0.3f;

        private int currentDisplayHP;
        private Coroutine flashCoroutine;

        void Start()
        {
            // Auto หา PlayerHealth ถ้าไม่ได้ลาก
            if (playerHealth == null)
                playerHealth = FindObjectOfType<PlayerHealth>();

            if (playerHealth == null)
            {
                Debug.LogError("❌ [PlayerHealthUI] ไม่พบ PlayerHealth!");
                return;
            }

            // Subscribe Events
            playerHealth.OnHPChanged += UpdateHPDisplay;
            playerHealth.OnDamaged   += PlayDamageEffect;
            playerHealth.OnHealed    += PlayHealEffect;

            // ซ่อน Flash เริ่มต้น
            if (damageFlashImage != null)
                SetFlashAlpha(0);

            // แสดงผลเริ่มต้น
            currentDisplayHP = playerHealth.currentHealth;
            UpdateHPDisplay(playerHealth.currentHealth, playerHealth.maxHealth);
        }

        void OnDestroy()
        {
            if (playerHealth == null) return;
            playerHealth.OnHPChanged -= UpdateHPDisplay;
            playerHealth.OnDamaged   -= PlayDamageEffect;
            playerHealth.OnHealed    -= PlayHealEffect;
        }

        // ===== อัปเดต Slot + ตัวเลข =====
        private void UpdateHPDisplay(int current, int max)
        {
            // ✅ อัปเดตตัวเลข
            if (hpCounterText != null)
                hpCounterText.text = $"{current}/{max}";

            // ✅ อัปเดต Slots
            for (int i = 0; i < hpSlots.Length; i++)
            {
                if (hpSlots[i] == null) continue;

                bool isActive = i < current;
                hpSlots[i].color = isActive ? slotActiveColor : slotEmptyColor;

                // Punch เฉพาะ Slot ที่เพิ่งเปลี่ยน
                if (i == current && current < currentDisplayHP)
                    StartCoroutine(SlotPunchScale(hpSlots[i].transform, false)); // เสีย HP

                if (i == current - 1 && current > currentDisplayHP)
                    StartCoroutine(SlotPunchScale(hpSlots[i].transform, true));  // ฟื้น HP
            }

            currentDisplayHP = current;
        }

        // ===== Flash Effects =====
        private void PlayDamageEffect()
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashEffect(flashColor, flashCount));
        }

        private void PlayHealEffect()
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashEffect(healFlashColor, 1));
        }

        private IEnumerator FlashEffect(Color color, int count)
        {
            if (damageFlashImage == null) yield break;

            for (int i = 0; i < count; i++)
            {
                // Fade In
                yield return StartCoroutine(FadeFlash(color, 0f, color.a));
                // Fade Out
                yield return StartCoroutine(FadeFlash(color, color.a, 0f));
            }

            SetFlashAlpha(0);
        }

        private IEnumerator FadeFlash(Color color, float fromAlpha, float toAlpha)
        {
            float t = 0;
            while (t < flashDuration)
            {
                t += Time.deltaTime;
                float alpha = Mathf.Lerp(fromAlpha, toAlpha, t / flashDuration);
                damageFlashImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
        }

        private void SetFlashAlpha(float alpha)
        {
            if (damageFlashImage == null) return;
            Color c = damageFlashImage.color;
            c.a = alpha;
            damageFlashImage.color = c;
        }

        // ===== Slot Punch Scale =====
        private IEnumerator SlotPunchScale(Transform slot, bool isHeal)
        {
            Vector3 originalScale = slot.localScale;
            float targetScale = isHeal
                ? 1f + slotScalePunchAmount
                : 1f - slotScalePunchAmount * 0.5f;

            float duration = 0.1f;

            // Scale ออก
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                slot.localScale = originalScale * Mathf.Lerp(1f, targetScale, t / duration);
                yield return null;
            }

            // กลับ Normal
            for (float t = 0; t < duration * 1.5f; t += Time.deltaTime)
            {
                slot.localScale = originalScale * Mathf.Lerp(targetScale, 1f, t / (duration * 1.5f));
                yield return null;
            }

            slot.localScale = originalScale;
        }
    }
}