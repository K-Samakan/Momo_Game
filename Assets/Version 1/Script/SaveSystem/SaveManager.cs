// ============================================================
// FILE: SaveManager.cs
// PATH: Assets/Version 1/Script/SaveSystem/SaveManager.cs
// ============================================================
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "MOMO_SaveData";

    private SaveData _currentSave;
    public SaveData CurrentSave => _currentSave;

    public static event System.Action<SaveData> OnSaved;
    public static event System.Action<SaveData> OnLoaded;

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
            // ตรวจว่า HP ไม่เต็มก่อน → ถ้าเต็มอยู่แล้วไม่ต้องเล่น VFX
            bool needHeal = ph.currentHealth < ph.maxHealth;

            ph.currentHealth = ph.maxHealth;
            ph.UpdateHealthUI();
            ph.NotifyHPChanged();

            // ✅ เล่น healEffect เหมือนตอนเก็บ Soul (เฉพาะตอนที่ HP ไม่เต็ม)
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

        WriteToDisk(_currentSave);

        Debug.Log($"💾 [SaveManager] Saved at [{savePoint.savePointID}] | " +
                  $"HP={_currentSave.playerCurrentHP}/{_currentSave.playerMaxHP}");

        OnSaved?.Invoke(_currentSave);
    }

    public void RegisterMonsterKill(string monsterID)
    {
        if (!_currentSave.killedMonsterIDs.Contains(monsterID))
        {
            _currentSave.killedMonsterIDs.Add(monsterID);
            WriteToDisk(_currentSave);
            Debug.Log($"☠️ [SaveManager] Monster killed & saved: {monsterID}");
        }
    }

    public void RegisterQuestComplete(string questID)
    {
        if (!_currentSave.completedSideQuests.Contains(questID))
        {
            _currentSave.completedSideQuests.Add(questID);
            WriteToDisk(_currentSave);
            Debug.Log($"✅ [SaveManager] Quest completed & saved: {questID}");
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Load / Respawn

    public void RespawnAtLastSavePoint()
    {
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

            // ── เปิด ThirdPersonController กลับมา ────────────
            var tpc = ph.GetComponent<StarterAssets.ThirdPersonController>();
            if (tpc != null) tpc.enabled = true;

            // ── Reset Animation ───────────────────────────────
            var anim = ph.GetComponent<Animator>();
            if (anim != null)
            {
                anim.ResetTrigger("Die");
                anim.ResetTrigger("DieFall");
                anim.ResetTrigger("GetHit");
                anim.Play("Idle Walk Run Blend", 0, 0f);
            }

            Debug.Log($"❤️ [SaveManager] Player revived | HP={ph.currentHealth}/{ph.maxHealth}");
        }

        RestoreMonsterStates();
        RestoreQuestStates();
        OnLoaded?.Invoke(_currentSave);
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
        => _currentSave != null && _currentSave.killedMonsterIDs.Contains(monsterID);

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