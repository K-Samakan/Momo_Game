// ============================================================
// FILE: SaveManager.cs
// PATH: Assets/Version 1/Script/SaveSystem/SaveManager.cs
// ============================================================
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "MOMO_SaveData";

    private SaveData _currentSave;
    public SaveData CurrentSave => _currentSave;

    public static event System.Action<SaveData> OnSaved;
    public static event System.Action<SaveData> OnLoaded;

    private List<string> _tempKilledMonsters = new List<string>();
    private List<string> _tempCompletedQuests = new List<string>();

    // ─────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _currentSave = LoadFromDisk() ?? new SaveData();
        Debug.Log($"✅ [SaveManager] Initialized | Last Save Point: {_currentSave.savePointID}");
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Save

    public void SaveAtPoint(SavePoint savePoint)
    {
        if (savePoint == null) return;

        var ph = FindFirstObjectByType<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            bool needHeal = ph.currentHealth < ph.maxHealth;

            ph.currentHealth = ph.maxHealth;
            ph.UpdateHealthUI();
            ph.NotifyHPChanged();

            if (needHeal && ph.healEffect != null)
            {
                GameObject fx = Object.Instantiate(ph.healEffect, ph.transform.position, Quaternion.identity);
                fx.transform.SetParent(ph.transform);
                Object.Destroy(fx, 2f);
            }

            _currentSave.playerCurrentHP = ph.currentHealth;
            _currentSave.playerMaxHP = ph.maxHealth;
        }

        _currentSave.savePointID = savePoint.savePointID;
        _currentSave.sceneName = SceneManager.GetActiveScene().name;
        _currentSave.SetRespawnPosition(savePoint.GetRespawnPosition());
        _currentSave.savedAt = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        foreach (var m in _tempKilledMonsters)
            if (!_currentSave.killedMonsterIDs.Contains(m))
                _currentSave.killedMonsterIDs.Add(m);

        foreach (var q in _tempCompletedQuests)
            if (!_currentSave.completedSideQuests.Contains(q))
                _currentSave.completedSideQuests.Add(q);

        _tempKilledMonsters.Clear();
        _tempCompletedQuests.Clear();
        WriteToDisk(_currentSave);

        Debug.Log($"💾 [SaveManager] Saved at [{savePoint.savePointID}] | " +
                  $"HP={_currentSave.playerCurrentHP}/{_currentSave.playerMaxHP} | " +
                  $"hasGlove={_currentSave.hasMonkeyGlove}");

        OnSaved?.Invoke(_currentSave);
    }

    // ✅ Save ค่าปัจจุบันทันทีโดยไม่ต้องแตะ SavePoint
    public void ForceSaveCurrentState()
    {
        var ph = FindFirstObjectByType<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            _currentSave.playerCurrentHP = ph.currentHealth;
            _currentSave.playerMaxHP = ph.maxHealth;
        }
        WriteToDisk(_currentSave);
        Debug.Log($"💾 [SaveManager] Force saved | hasGlove={_currentSave.hasMonkeyGlove} | hasRod={_currentSave.hasFishingRod} | maxHP={_currentSave.playerMaxHP}");
    }

    public void RegisterMonsterKill(string monsterID)
    {
        if (!_tempKilledMonsters.Contains(monsterID))
        {
            _tempKilledMonsters.Add(monsterID);
            Debug.Log($"☠️ [SaveManager] Monster killed (temp): {monsterID}");
        }
    }

    public void RegisterQuestComplete(string questID)
    {
        if (!_tempCompletedQuests.Contains(questID))
        {
            _tempCompletedQuests.Add(questID);
            Debug.Log($"📋 [SaveManager] Quest completed (temp): {questID}");
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Load / Respawn

    public void RespawnAtLastSavePoint()
    {
        _tempKilledMonsters.Clear();
        _tempCompletedQuests.Clear();

        if (_currentSave == null || string.IsNullOrEmpty(_currentSave.savePointID))
        {
            Debug.LogWarning("⚠️ [SaveManager] No save data! Reloading scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        Debug.Log($"🔄 [SaveManager] Respawning at [{_currentSave.savePointID}]");

        if (SceneManager.GetActiveScene().name == _currentSave.sceneName)
            ApplySaveToPlayer();
        else
        {
            SceneManager.sceneLoaded += OnSceneLoadedForRespawn;
            SceneManager.LoadScene(_currentSave.sceneName);
        }
    }

    private void OnSceneLoadedForRespawn(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedForRespawn;
        ApplySaveToPlayer();
    }

    private void ApplySaveToPlayer()
    {
        // ── ย้าย Player ──────────────────────────────────────
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = _currentSave.GetRespawnPosition();

        // ── คืน HP + Reset Death State ───────────────────────
        var ph = FindFirstObjectByType<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            ph.currentHealth = _currentSave.playerCurrentHP;
            ph.maxHealth = _currentSave.playerMaxHP;
            ph._isDead = false;
            ph.regenTimer = 0f;
            ph.isRegenActive = false;

            ph.UpdateHealthUI();
            ph.NotifyHPChanged();

            var tpc = ph.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.enabled = true;

            var anim = ph.GetComponent<Animator>();
            if (anim != null)
            {
                anim.ResetTrigger("Die");
                anim.ResetTrigger("DieFall");
                anim.ResetTrigger("GetHit");
                anim.Play("Idle Walk Run Blend", 0, 0f);
            }

            Debug.Log("╔══════════════════════════════════════════╗");
            Debug.Log("║        🔄 RESPAWN STAT SUMMARY           ║");
            Debug.Log($"║  ❤️  HP  : {ph.currentHealth} / {ph.maxHealth}");
            Debug.Log($"║  🥊 Glove: {(_currentSave.hasMonkeyGlove ? "Equipped ✅" : "Not equipped ❌")}");
            Debug.Log($"║  🎣 Rod  : {(_currentSave.hasFishingRod ? "Equipped ✅" : "Not equipped ❌")}");
            Debug.Log("╚══════════════════════════════════════════╝");
        }

        // ✅ Restore Glove ถ้าเคยรับไปแล้ว
        if (_currentSave.hasMonkeyGlove)
            RestoreGlove();

        // ✅ Restore เบ็ดยืด ถ้าเคยรับไปแล้ว
        if (_currentSave.hasFishingRod)
            RestoreFishingRod();

        RestoreMonsterStates();
        RestoreQuestStates();
        OnLoaded?.Invoke(_currentSave);
    }

    private void RestoreGlove()
    {
        var glovePickup = FindFirstObjectByType<GlovePickup>();
        if (glovePickup != null)
        {
            glovePickup.ForceEquipOnRespawn();
            Debug.Log("[SaveManager] 🥊 Glove restored after respawn!");
        }
        else
        {
            Debug.LogWarning("[SaveManager] ⚠️ ไม่พบ GlovePickup ใน scene (อาจ SetActive=false แล้ว)");
        }
    }

    private void RestoreFishingRod()
    {
        var rodPickup = FindFirstObjectByType<FishingRodPickup>();
        if (rodPickup != null)
        {
            rodPickup.PickUpFishingRod();
            Debug.Log("[SaveManager] 🎣 Fishing Rod restored after respawn!");
        }
        else
        {
            Debug.LogWarning("[SaveManager] ⚠️ ไม่พบ FishingRodPickup ใน scene (อาจ SetActive=false แล้ว)");
        }
    }

    private void RestoreMonsterStates()
    {
        var allMonsters = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in allMonsters)
        {
            if (_currentSave.killedMonsterIDs.Contains(mb.gameObject.name))
                mb.gameObject.SetActive(false);
        }
    }

    private void RestoreQuestStates()
    {
        Debug.Log($"📋 [SaveManager] Quests restored: {_currentSave.completedSideQuests.Count}");
    }

    public bool IsMonsterDead(string monsterID)
        => (_currentSave != null && _currentSave.killedMonsterIDs.Contains(monsterID))
        || _tempKilledMonsters.Contains(monsterID);

    public bool IsQuestCompleted(string questID)
        => _currentSave != null && _currentSave.completedSideQuests.Contains(questID);

    public bool HasSaveData()
        => PlayerPrefs.HasKey(SAVE_KEY) && !string.IsNullOrEmpty(_currentSave?.savePointID);

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Disk IO

    private void WriteToDisk(SaveData data)
    {
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(data, true));
        PlayerPrefs.Save();
    }

    private SaveData LoadFromDisk()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return null;
        return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SAVE_KEY));
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        _currentSave = new SaveData();
        Debug.Log("🗑️ [SaveManager] Save deleted.");
    }

    #endregion
}