using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MirrorInteractionManager : MonoBehaviour
{
    public static MirrorInteractionManager Instance { get; private set; }

    [Header("Input Settings")]
    public InputActionAsset inputActions;

    private InputActionMap puzzleActionMap;
    private InputActionMap playerActionMap;

    [Header("MiniMap Camera")]
    public GameObject cameraMiniMap; // << Drag กล้อง MiniMap ที่ Inspector

    private InputAction l2Action;
    private InputAction r2Action;

    private List<InteractionMirror> allMirrors = new List<InteractionMirror>();
    private InteractionMirror activeMirror = null;

    [Header("Player Health")]
    public int playerMaxHealth = 5;
    private int playerCurrentHealth;

    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeInput();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        playerCurrentHealth = playerMaxHealth;
        Debug.Log($"❤️ Player Health initialized: {playerCurrentHealth}/{playerMaxHealth}");

        // แจ้ง UI Manager เรื่อง Health
        if (MirrorUIManager.Instance != null)
        {
            MirrorUIManager.Instance.OnPlayerHealthChanged(playerCurrentHealth, playerMaxHealth);
        }
    }

    private void InitializeInput()
    {
        if (inputActions == null)
        {
            Debug.LogError("Input Actions Asset not assigned!");
            return;
        }

        puzzleActionMap = inputActions.FindActionMap("Puzzle2");
        l2Action = puzzleActionMap.FindAction("L2");
        r2Action = puzzleActionMap.FindAction("R2");

        playerActionMap = inputActions.FindActionMap("Player");
        if (playerActionMap == null)
            Debug.LogWarning("Player Action Map not found!");

        if (l2Action == null) Debug.LogError("L2 Action not found!");
        if (r2Action == null) Debug.LogError("R2 Action not found!");
    }

    private void OnEnable()
    {
        if (puzzleActionMap == null) return;

        puzzleActionMap.Enable();

        l2Action.started += OnL2Started;
        l2Action.canceled += OnL2Canceled;

        r2Action.started += OnR2Started;
        r2Action.canceled += OnR2Canceled;

        Debug.Log("🎮 Mirror Interaction Manager input enabled");
    }

    private void OnDisable()
    {
        if (l2Action != null)
        {
            l2Action.started -= OnL2Started;
            l2Action.canceled -= OnL2Canceled;
        }

        if (r2Action != null)
        {
            r2Action.started -= OnR2Started;
            r2Action.canceled -= OnR2Canceled;
        }

        if (puzzleActionMap != null)
            puzzleActionMap.Disable();
    }

    public void RegisterMirror(InteractionMirror mirror)
    {
        if (!allMirrors.Contains(mirror))
        {
            allMirrors.Add(mirror);
            Debug.Log($"📋 Registered mirror: {mirror.gameObject.name}");
        }
    }

    public void UnregisterMirror(InteractionMirror mirror)
    {
        if (allMirrors.Contains(mirror))
        {
            allMirrors.Remove(mirror);

            if (activeMirror == mirror)
            {
                ClearActiveMirror(mirror);
            }

            Debug.Log($"📋 Unregistered mirror: {mirror.gameObject.name}");
        }
    }

    public void SetActiveMirror(InteractionMirror mirror)
    {
        if (activeMirror != null)
        {
            activeMirror.SetInteractionState(false);
        }

        activeMirror = mirror;
        activeMirror.SetInteractionState(true);

        Debug.Log($"🎯 Active mirror set to: {mirror.gameObject.name}");
    }

    public void ClearActiveMirror(InteractionMirror mirror)
    {
        if (activeMirror == mirror)
        {
            activeMirror.SetInteractionState(false);
            activeMirror.StopRotation();
            activeMirror = null;

            Debug.Log("🎯 Active mirror cleared");
        }
    }

    private void OnL2Started(InputAction.CallbackContext context)
    {
        Debug.Log("🎮 L2 Started");

        if (activeMirror != null)
        {
            activeMirror.StartRotation(false);
        }
        else
        {
            Debug.Log("❌ No active mirror to rotate");
        }
    }

    private void OnL2Canceled(InputAction.CallbackContext context)
    {
        Debug.Log("🎮 L2 Canceled");

        if (activeMirror != null)
        {
            activeMirror.StopRotation();
        }
    }

    private void OnR2Started(InputAction.CallbackContext context)
    {
        Debug.Log("🎮 R2 Started");

        if (activeMirror != null)
        {
            activeMirror.StartRotation(true);
        }
        else
        {
            Debug.Log("❌ No active mirror to rotate");
        }
    }

    private void OnR2Canceled(InputAction.CallbackContext context)
    {
        Debug.Log("🎮 R2 Canceled");

        if (activeMirror != null)
        {
            activeMirror.StopRotation();
        }
    }

    public void OnPlayerTakeDamage(int damageAmount, string sourceName)
    {
        playerCurrentHealth -= damageAmount;

        Debug.Log($"💔 PLAYER TAKES {damageAmount} DAMAGE from {sourceName}!");
        Debug.Log($"❤️ Player Health: {playerCurrentHealth}/{playerMaxHealth}");

        // อัพเดท UI
        if (MirrorUIManager.Instance != null)
        {
            MirrorUIManager.Instance.OnPlayerHealthChanged(playerCurrentHealth, playerMaxHealth);
        }

        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPattern(HapticPatternType.Boom);
        }

        if (playerCurrentHealth <= 0)
        {
            Debug.Log($"💀💀💀 PLAYER IS DEAD! 💀💀💀");
            OnPlayerDeath();
        }
        else if (playerCurrentHealth == 1)
        {
            Debug.Log($"🚨 CRITICAL! Only 1 HP remaining!");

            if (HapticManager.Instance != null)
            {
                HapticManager.Instance.PlayPattern(HapticPatternType.Warning);
            }
        }
        else if (playerCurrentHealth == 2)
        {
            Debug.Log($"⚠️ WARNING! Only 2 HP remaining!");

            if (HapticManager.Instance != null)
            {
                HapticManager.Instance.PlayPattern(HapticPatternType.Short);
            }
        }
        else
        {
            Debug.Log($"✅ Player health is {playerCurrentHealth}/{playerMaxHealth}");
        }
    }

    private void OnPlayerDeath()
    {
        Debug.Log($"🎮 GAME OVER! Player died with 0/{playerMaxHealth} HP");

        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPattern(HapticPatternType.Fail);
        }

        puzzleActionMap.Disable();

        foreach (var mirror in allMirrors)
        {
            if (mirror != null)
            {
                mirror.SetInteractionState(false);
            }
        }
    }

    public void HealPlayer(int healAmount)
    {
        playerCurrentHealth = Mathf.Min(playerCurrentHealth + healAmount, playerMaxHealth);
        Debug.Log($"💚 Player healed {healAmount} HP! Current: {playerCurrentHealth}/{playerMaxHealth}");

        // อัพเดท UI
        if (MirrorUIManager.Instance != null)
        {
            MirrorUIManager.Instance.OnPlayerHealthChanged(playerCurrentHealth, playerMaxHealth);
        }

        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPattern(HapticPatternType.Success);
        }
    }

    // 2. เพิ่มฟังก์ชันนี้ใน class เดียวกัน
    public void SwitchActionMapToPlayer()
    {
        if (puzzleActionMap != null) puzzleActionMap.Disable();
        if (playerActionMap != null) playerActionMap.Enable();
        Debug.Log("Switched Input to Player ActionMap");
    }

    // 3. เพิ่มฟังก์ชันนี้ใน class เดียวกัน
    public void DisableAllMirrors()
    {
        foreach (var mirror in allMirrors)
        {
            if (mirror != null)
            {
                mirror.SetInteractionState(false);
                mirror.gameObject.SetActive(false); // หรือแค่ SetActive ก็ได้
            }
        }
        Debug.Log("All mirrors have been disabled/hidden after puzzle complete.");
    }

    public void OnPuzzleComplete()
    {
        Debug.Log($"🎉 PUZZLE COMPLETED! 🎉");

        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPuzzleCompleteFeedback();
        }

        SwitchActionMapToPlayer(); // เปลี่ยน input map เป็น player

        //DisableAllMirrors();  // ปิดระบบกระจก

        // ปิด UI ของ Mirror Puzzle
        if (MirrorUIManager.Instance != null)
        {
            MirrorUIManager.Instance.HideMirrorInteractionUI(); // <<<<<<----- ปิด UI ของ Mirror
            MirrorUIManager.Instance.healthUIPanel.SetActive(false);// MirrorUIManager.Instance.healthUIPanel.SetActive(false);
        }

        if (cameraMiniMap != null)
        {
            cameraMiniMap.SetActive(false);
            Debug.Log("MiniMap Camera ถูกปิดเรียบร้อย");
        }
    }
}