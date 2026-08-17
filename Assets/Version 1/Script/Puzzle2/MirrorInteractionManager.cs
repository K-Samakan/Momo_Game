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
    public GameObject cameraMiniMap;

    private InputAction l2Action;
    private InputAction r2Action;

    private List<InteractionMirror> allMirrors = new List<InteractionMirror>();
    private InteractionMirror activeMirror = null;

    [Header("Player Health")]
    public int playerMaxHealth = 5;
    private int playerCurrentHealth;

    [Header("Puzzle Entry / Warp")]
    public PlayerInput playerInput;
    public Transform puzzleSpawnPoint;
    public Transform player;
    public Puzzle2WarpTrigger warpTrigger;

    [Header("Doors")]
    public GameObject entranceDoor;          // ประตูทางเข้า
    public GameObject exitDoor;              // ประตูทางออก (⚠️ ตอนนี้เปิดที่ JetpackPickup.EquipJetpack() แทน)

    [Header("Main HUD")]
    public GameObject mainHUD;

    [Header("Jetpack Reward")]
    public JetpackPickup jetpackPickup;

    private bool isSolved = false;

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

        if (MirrorUIManager.Instance != null)
            MirrorUIManager.Instance.OnPlayerHealthChanged(playerCurrentHealth, playerMaxHealth);

        SetPuzzleVisualsActive(false);
    }

    private void InitializeInput()
    {
        if (playerInput == null) playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput == null) { Debug.LogError("PlayerInput not found!"); return; }

        puzzleActionMap = playerInput.actions.FindActionMap("Puzzle2");
        playerActionMap = playerInput.actions.FindActionMap("Player");

        l2Action = puzzleActionMap.FindAction("L2");
        r2Action = puzzleActionMap.FindAction("R2");

        if (l2Action == null) Debug.LogError("L2 Action not found!");
        if (r2Action == null) Debug.LogError("R2 Action not found!");
    }

    private void OnEnable()
    {
        if (l2Action == null || r2Action == null) return;

        l2Action.started += OnL2Started;
        l2Action.canceled += OnL2Canceled;
        r2Action.started += OnR2Started;
        r2Action.canceled += OnR2Canceled;

        Debug.Log("🎮 Mirror input subscribed (puzzle NOT active yet)");
    }

    private void SetPuzzleVisualsActive(bool on)
    {
        if (cameraMiniMap != null)
            cameraMiniMap.SetActive(on);

        if (MirrorUIManager.Instance != null)
            MirrorUIManager.Instance.SetPuzzleUIActive(on);

        if (mainHUD != null)
            mainHUD.SetActive(!on);

        Debug.Log($"HUD = {!on}");
    }

    public void EnterPuzzle()
    {
        if (isSolved) return;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        if (player != null && puzzleSpawnPoint != null)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = puzzleSpawnPoint.position;
            player.rotation = puzzleSpawnPoint.rotation;
            if (cc != null) cc.enabled = true;
        }

        playerInput.SwitchCurrentActionMap("Puzzle2");

        Debug.Log("=== ENTER PUZZLE ===");
        SetPuzzleVisualsActive(true);
        Debug.Log("Puzzle UI Opened");
        Debug.Log("🪞 เข้าโหมด Puzzle2 แล้ว");
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
                ClearActiveMirror(mirror);

            Debug.Log($"📋 Unregistered mirror: {mirror.gameObject.name}");
        }
    }

    public void SetActiveMirror(InteractionMirror mirror)
    {
        if (activeMirror != null)
            activeMirror.SetInteractionState(false);

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
            activeMirror.StartRotation(false);
        else
            Debug.Log("❌ No active mirror to rotate");
    }

    private void OnL2Canceled(InputAction.CallbackContext context)
    {
        Debug.Log("🎮 L2 Canceled");
        if (activeMirror != null)
            activeMirror.StopRotation();
    }

    private void OnR2Started(InputAction.CallbackContext context)
    {
        Debug.Log("🎮 R2 Started");

        if (activeMirror != null)
            activeMirror.StartRotation(true);
        else
            Debug.Log("❌ No active mirror to rotate");
    }

    private void OnR2Canceled(InputAction.CallbackContext context)
    {
        Debug.Log("🎮 R2 Canceled");
        if (activeMirror != null)
            activeMirror.StopRotation();
    }

    public void OnPlayerTakeDamage(int damageAmount, string sourceName)
    {
        playerCurrentHealth -= damageAmount;

        Debug.Log($"💔 PLAYER TAKES {damageAmount} DAMAGE from {sourceName}!");
        Debug.Log($"❤️ Player Health: {playerCurrentHealth}/{playerMaxHealth}");

        if (MirrorUIManager.Instance != null)
            MirrorUIManager.Instance.OnPlayerHealthChanged(playerCurrentHealth, playerMaxHealth);

        if (HapticManager.Instance != null)
            HapticManager.Instance.PlayPattern(HapticPatternType.Boom);

        if (playerCurrentHealth <= 0)
        {
            Debug.Log($"💀💀💀 PLAYER IS DEAD! 💀💀💀");
            OnPlayerDeath();
        }
        else if (playerCurrentHealth == 1)
        {
            Debug.Log($"🚨 CRITICAL! Only 1 HP remaining!");
            if (HapticManager.Instance != null)
                HapticManager.Instance.PlayPattern(HapticPatternType.Warning);
        }
        else if (playerCurrentHealth == 2)
        {
            Debug.Log($"⚠️ WARNING! Only 2 HP remaining!");
            if (HapticManager.Instance != null)
                HapticManager.Instance.PlayPattern(HapticPatternType.Short);
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
            HapticManager.Instance.PlayPattern(HapticPatternType.Fail);

        puzzleActionMap.Disable();

        foreach (var mirror in allMirrors)
        {
            if (mirror != null)
                mirror.SetInteractionState(false);
        }
    }

    public void HealPlayer(int healAmount)
    {
        playerCurrentHealth = Mathf.Min(playerCurrentHealth + healAmount, playerMaxHealth);
        Debug.Log($"💚 Player healed {healAmount} HP! Current: {playerCurrentHealth}/{playerMaxHealth}");

        if (MirrorUIManager.Instance != null)
            MirrorUIManager.Instance.OnPlayerHealthChanged(playerCurrentHealth, playerMaxHealth);

        if (HapticManager.Instance != null)
            HapticManager.Instance.PlayPattern(HapticPatternType.Success);
    }

    public void SwitchActionMapToPlayer()
    {
        if (puzzleActionMap != null) puzzleActionMap.Disable();
        if (playerActionMap != null) playerActionMap.Enable();
        Debug.Log("Switched Input to Player ActionMap");
    }

    public void DisableAllMirrors()
    {
        foreach (var mirror in allMirrors)
        {
            if (mirror != null)
            {
                mirror.SetInteractionState(false);
                mirror.gameObject.SetActive(false);
            }
        }
        Debug.Log("All mirrors have been disabled/hidden after puzzle complete.");
    }

    public void OnPuzzleComplete()
    {
        if (isSolved) return;
        isSolved = true;
        Debug.Log("🎉 PUZZLE2 COMPLETED! 🎉");

        if (HapticManager.Instance != null)
            HapticManager.Instance.PlayPuzzleCompleteFeedback();

        // 1) สลับกลับ Player ก่อน — ต้องทำก่อนเสมอ เพื่อให้กด Attack เก็บ jetpack ได้
        playerInput.SwitchCurrentActionMap("Player");

        if (jetpackPickup != null)
            jetpackPickup.ShowJetpackAfterPuzzle();

        // 2) ปิดแค่ entranceDoor (กันคนเข้าซ้ำ) — 
        //    ❌ ไม่แตะ exitDoor ตรงนี้แล้ว! exitDoor จะเปิดก็ต่อเมื่อ
        //    JetpackPickup.EquipJetpack() เก็บ jetpack สำเร็จเท่านั้น
        if (entranceDoor != null) entranceDoor.SetActive(false);
        Debug.Log("🚪 ปิดทางเข้าแล้ว รอผู้เล่นเก็บ Jetpack เพื่อเปิดทางออก");

        // 3) ล็อกแท่นกด
        if (warpTrigger != null) warpTrigger.LockPedestal();

        // 4) ปิด UI + MiniMap
        SetPuzzleVisualsActive(false);

        // 5) ปิดกระจกทั้งหมด
        DisableAllMirrors();

        // 6) ปิดตัวจัดการ puzzle
        this.enabled = false;
        Debug.Log("🛑 ปิด MirrorInteractionManager แล้ว");
    }
}