// ============================================================
// FILE: FishingRodPickup.cs
// PATH: Assets/Version 1/Script/Map4/Item4/FishingRodPickup.cs
// รูปแบบเดียวกับ GlovePickup.cs (Map1)
// ============================================================
using UnityEngine;
using UnityEngine.InputSystem;
using MOMO.Player;
using Momo.UITutorial.Data;
using Momo.UITutorial.Managers;

public class FishingRodPickup : MonoBehaviour
{
    [Header("=== Item Object (แท่นวาง) ===")]
    [Tooltip("โมเดลเบ็ดยืดบนแท่น — โผล่ตอน puzzle สำเร็จ และหายไปตอนเก็บ")]
    public GameObject fishingRodItemObject;

    [Header("=== Item Visual บนตัวผู้เล่น ===")]
    [Tooltip("ลาก GameObject โมเดลเบ็ดยืดที่ติดอยู่กับตัวผู้เล่น (ใต้มือ/หลัง) — ซ่อนไว้ก่อน จะโชว์ตอนเก็บสำเร็จ")]
    public GameObject rodVisualOnPlayer;

    [Header("=== Item Data (สำหรับ Item Detail Popup) ===")]
    public ItemData itemData;

    [Header("=== UI Prompt ===")]
    public GameObject promptUI;

    [Header("=== Stat Bonuses (ให้ตอนสวมใส่) ===")]
    public int hpBonus = 2;

    [Header("=== References ===")]
    public PlayerInput _playerInput;

    private bool _pickedUp = false;
    private bool _playerInRange = false;
    private bool _pedestalRevealed = false;
    private InputAction _interactAction;

    private PlayerHealth _playerHealth;
    private SmartAttackSystem _smartAttack;
    private FishingRodController _rodController;
    private MonoBehaviour _swimmingController;

    void Start()
    {
        if (fishingRodItemObject != null)
            fishingRodItemObject.SetActive(false);

        if (rodVisualOnPlayer != null)
            rodVisualOnPlayer.SetActive(false); // ✅ ซ่อนไว้จนกว่าจะเก็บสำเร็จ

        if (promptUI != null) promptUI.SetActive(false);

        if (_playerInput == null)
            _playerInput = FindFirstObjectByType<PlayerInput>();

        if (_playerInput == null)
        {
            Debug.LogError("[FishingRodPickup] ❌ ไม่พบ PlayerInput!");
            return;
        }

        var playerMap = _playerInput.actions.FindActionMap("Player");
        if (playerMap != null)
        {
            _interactAction = playerMap.FindAction("Attack");
            if (_interactAction != null)
                _interactAction.performed += OnInteract;
            else
                Debug.LogError("[FishingRodPickup] ❌ ไม่พบ Action 'Attack'!");
        }
        else
        {
            Debug.LogError("[FishingRodPickup] ❌ ไม่พบ ActionMap 'Player'!");
        }
    }

    void Update()
    {
        if (_pickedUp || _pedestalRevealed) return;

        bool puzzleSolved = Inventory.Instance != null && Inventory.Instance.CorrectPuzzle4;
        if (puzzleSolved)
        {
            _pedestalRevealed = true;
            if (fishingRodItemObject != null) fishingRodItemObject.SetActive(true);
            Debug.Log("[FishingRodPickup] ✅ Puzzle4 สำเร็จแล้ว — เบ็ดยืดปรากฏขึ้น!");
        }
    }

    void OnDestroy()
    {
        if (_interactAction != null)
            _interactAction.performed -= OnInteract;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_pickedUp || !_pedestalRevealed || !other.CompareTag("Player")) return;

        _playerInRange = true;
        _playerHealth = other.GetComponent<PlayerHealth>();
        _smartAttack = other.GetComponent<SmartAttackSystem>();
        _rodController = other.GetComponentInChildren<FishingRodController>(true);
        _swimmingController = other.GetComponent<SwimmingControllerHybrid>();

        if (promptUI != null) promptUI.SetActive(true);

        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║  🎣 เบ็ดยืด — กด □ เพื่อเก็บ           ║");
        Debug.Log("╚══════════════════════════════════════╝");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (promptUI != null) promptUI.SetActive(false);
    }

    void OnInteract(InputAction.CallbackContext context)
    {
        if (!_playerInRange || _pickedUp) return;
        PickUpFishingRod();
    }

    // เรียกตรงได้จากปุ่ม Test หรือจาก SaveManager ตอน Respawn
    public void PickUpFishingRod()
    {
        _pickedUp = true;

        // ── 1) ซ่อนเบ็ดยืดบนแท่น (เก็บไปแล้ว) ──
        if (fishingRodItemObject != null)
            fishingRodItemObject.SetActive(false);

        // ── 2) โชว์โมเดลเบ็ดยืดที่ติดตัวผู้เล่น ──
        if (rodVisualOnPlayer != null)
            rodVisualOnPlayer.SetActive(true);
        else
            Debug.LogWarning("[FishingRodPickup] ⚠️ ไม่ได้ลาก rodVisualOnPlayer ใส่ — จะไม่เห็นโมเดลติดตัวผู้เล่น!");

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CurrentSave.hasFishingRod = true;
            SaveManager.Instance.ForceSaveCurrentState();
        }

        // ── 3) HP Max + Heal ──
        if (_playerHealth != null)
        {
            int oldMax = _playerHealth.maxHealth;
            int oldCurrent = _playerHealth.currentHealth;

            _playerHealth.maxHealth += hpBonus;
            _playerHealth.Heal(hpBonus);

            Debug.Log($"[FishingRodPickup] ❤️ HP STAT CHANGED | maxHP: {oldMax} → {_playerHealth.maxHealth} | current: {oldCurrent} → {_playerHealth.currentHealth}");
        }
        else Debug.LogWarning("[FishingRodPickup] ⚠️ ไม่พบ PlayerHealth!");

        // ── 4) Equip ระบบต่อสู้ + oxygen bonus ──
        if (_rodController != null)
        {
            _rodController.OnEquip();
        }
        else
        {
            Debug.LogWarning("[FishingRodPickup] ⚠️ ไม่พบ FishingRodController บนตัวผู้เล่น!");
        }

        if (promptUI != null) promptUI.SetActive(false);

        if (SaveManager.Instance != null)
            SaveManager.Instance.ForceSaveCurrentState();

        // ── 5) แสดง Item Detail Popup ──
        if (itemData != null)
            TutorialManager.Instance?.ShowItemDetail(itemData);
        else
            Debug.LogWarning("[FishingRodPickup] ⚠️ ไม่พบ ItemData! ลืมลาก reference ใน Inspector หรือเปล่า?");

        Debug.Log("[FishingRodPickup] 🎣 เก็บเบ็ดยืดสำเร็จ! พร้อมใช้งาน");
    }
}