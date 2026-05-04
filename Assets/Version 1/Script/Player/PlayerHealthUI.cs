using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MOMO.Player;

namespace MOMO.UI
{
    public class PlayerHealthUI : MonoBehaviour
    {
        [Header("References")]
        public PlayerHealth playerHealth;

        [Header("HP Slots")]
        public Image[] hpSlots;
        public Color slotActiveColor = Color.green;
        public Color slotEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        [Header("Dynamic Slot Spawn")]
        [Tooltip("ลาก HPSlot_1 (1 ช่อง) มาเป็น Prefab")]
        public GameObject hpSlotPrefab;
        [Tooltip("ลาก BarContainer มาใส่")]
        public Transform hpSlotContainer;

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

        // ── Private ──
        private int currentDisplayHP;
        private Coroutine flashCoroutine;
        private List<Image> slotList = new List<Image>();

        // ─────────────────────────────────────────────
        void Start()
        {
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();

            if (playerHealth == null)
            {
                Debug.LogError("❌ [PlayerHealthUI] ไม่พบ PlayerHealth!");
                return;
            }

            // ย้าย hpSlots ที่ลาก Inspector ไปใส่ List
            slotList = new List<Image>(hpSlots);

            // Subscribe Events
            playerHealth.OnHPChanged += UpdateHPDisplay;
            playerHealth.OnDamaged += PlayDamageEffect;
            playerHealth.OnHealed += PlayHealEffect;

            if (damageFlashImage != null)
                SetFlashAlpha(0);

            currentDisplayHP = playerHealth.currentHealth;
            UpdateHPDisplay(playerHealth.currentHealth, playerHealth.maxHealth);
        }

        void OnDestroy()
        {
            if (playerHealth == null) return;
            playerHealth.OnHPChanged -= UpdateHPDisplay;
            playerHealth.OnDamaged -= PlayDamageEffect;
            playerHealth.OnHealed -= PlayHealEffect;
        }

        // ─────────────────────────────────────────────
        // อัปเดต Slot + ตัวเลข
        // ─────────────────────────────────────────────
        private void UpdateHPDisplay(int current, int max)
        {
            // ✅ ถ้า Slot ไม่พอ → Spawn เพิ่มจนครบ
            while (slotList.Count < max)
                AddHPSlot();

            // ✅ อัปเดตตัวเลข
            if (hpCounterText != null)
                hpCounterText.text = $"{current}/{max}";

            // ✅ อัปเดต Slot ทั้งหมด
            for (int i = 0; i < slotList.Count; i++)
            {
                if (slotList[i] == null) continue;

                // แสดง/ซ่อนตาม maxHP
                slotList[i].gameObject.SetActive(i < max);

                // สีตาม currentHP
                bool isActive = i < current;
                slotList[i].color = isActive ? slotActiveColor : slotEmptyColor;

                // Punch Animation
                if (i == current && current < currentDisplayHP)
                    StartCoroutine(SlotPunchScale(slotList[i].transform, false)); // เสีย HP

                if (i == current - 1 && current > currentDisplayHP)
                    StartCoroutine(SlotPunchScale(slotList[i].transform, true));  // ฟื้น HP
            }

            currentDisplayHP = current;
        }

        // ─────────────────────────────────────────────
        // Spawn HP Slot เพิ่ม
        // ─────────────────────────────────────────────
        private void AddHPSlot()
        {
            if (hpSlotPrefab == null || hpSlotContainer == null)
            {
                Debug.LogWarning("[PlayerHealthUI] ⚠️ ไม่พบ hpSlotPrefab หรือ hpSlotContainer!");
                return;
            }

            GameObject newSlot = Instantiate(hpSlotPrefab, hpSlotContainer);
            Image slotImage = newSlot.GetComponent<Image>();

            if (slotImage != null)
            {
                slotList.Add(slotImage);
                Debug.Log($"[PlayerHealthUI] ➕ เพิ่ม HP Slot | รวม {slotList.Count} slots");
            }
        }

        // ─────────────────────────────────────────────
        // Flash Effects
        // ─────────────────────────────────────────────
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
                yield return StartCoroutine(FadeFlash(color, 0f, color.a));
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

        // ─────────────────────────────────────────────
        // Slot Punch Scale
        // ─────────────────────────────────────────────
        private IEnumerator SlotPunchScale(Transform slot, bool isHeal)
        {
            Vector3 originalScale = slot.localScale;
            float targetScale = isHeal
                ? 1f + slotScalePunchAmount
                : 1f - slotScalePunchAmount * 0.5f;

            float duration = 0.1f;

            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                slot.localScale = originalScale * Mathf.Lerp(1f, targetScale, t / duration);
                yield return null;
            }

            for (float t = 0; t < duration * 1.5f; t += Time.deltaTime)
            {
                slot.localScale = originalScale * Mathf.Lerp(targetScale, 1f, t / (duration * 1.5f));
                yield return null;
            }

            slot.localScale = originalScale;
        }
    }
}