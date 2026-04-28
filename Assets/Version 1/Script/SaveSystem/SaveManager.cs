using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "MOMO_SaveData";

    // ─── Current Save Data (RAM) ─────────────────────────────
    private SaveData _currentSave;
    public SaveData CurrentSave => _currentSave;

    // ─── Events ─────────────────────────────────────────────
    public static event System.Action<SaveData> OnSaved;
    public static event System.Action<SaveData> OnLoaded;

    // ─────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // โหลด Save ที่มีอยู่เข้า RAM ทันที
        _currentSave = LoadFromDisk() ?? new SaveData();
        Debug.Log($"✅ [SaveManager] Initialized | Last Save Point: {_currentSave.savePointID}");
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Save

    /// <summary>
    /// Save สถานะทั้งหมด ณ Save Point นั้น
    /// </summary>
    public void SaveAtPoint(SavePoint savePoint)
    {
        if (savePoint == null) return;

        // หา PlayerHealth
        var ph = FindObjectOfType<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            _currentSave.playerCurrentHP = ph.currentHealth;
            _currentSave.playerMaxHP = ph.maxHealth;
        }

        // ตำแหน่ง respawn = ตำแหน่ง Save Point นั้น
        _currentSave.savePointID = savePoint.savePointID;
        _currentSave.sceneName = SceneManager.GetActiveScene().name;
        _currentSave.SetRespawnPosition(savePoint.GetRespawnPosition());

        // เวลา
        _currentSave.savedAt = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        // เขียนลง Disk
        WriteToDisk(_currentSave);

        Debug.Log($"💾 [SaveManager] Saved at [{savePoint.savePointID}] | " +
                  $"HP={_currentSave.playerCurrentHP}/{_currentSave.playerMaxHP} | " +
                  $"Pos={_currentSave.GetRespawnPosition()}");

        OnSaved?.Invoke(_currentSave);
    }

    /// <summary>
    /// บันทึก Monster Kill ลง Save ปัจจุบัน
    /// </summary>
    public void RegisterMonsterKill(string monsterID)
    {
        if (!_currentSave.killedMonsterIDs.Contains(monsterID))
        {
            _currentSave.killedMonsterIDs.Add(monsterID);
            WriteToDisk(_currentSave);
            Debug.Log($"☠️ [SaveManager] Monster killed & saved: {monsterID}");
        }
    }

    /// <summary>
    /// บันทึก Quest สำเร็จ
    /// </summary>
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

    /// <summary>
    /// Respawn ผู้เล่นไปยัง Save Point ล่าสุด
    /// (เรียกจาก DeathScreenManager เมื่อกด ○)
    /// </summary>
    public void RespawnAtLastSavePoint()
    {
        if (_currentSave == null || string.IsNullOrEmpty(_currentSave.savePointID))
        {
            Debug.LogWarning("⚠️ [SaveManager] No save data found! Reloading scene from start.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        Debug.Log($"🔄 [SaveManager] Respawning at [{_currentSave.savePointID}] in scene [{_currentSave.sceneName}]");

        // ถ้า Scene เดียวกัน → Teleport ทันที
        if (SceneManager.GetActiveScene().name == _currentSave.sceneName)
        {
            ApplySaveToPlayer();
        }
        else
        {
            // ต้อง Load Scene ก่อน
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
        {
            player.transform.position = _currentSave.GetRespawnPosition();
            Debug.Log($"📍 [SaveManager] Player moved to {_currentSave.GetRespawnPosition()}");
        }

        // ── คืน HP ──────────────────────────────────────────
        var ph = FindObjectOfType<MOMO.Player.PlayerHealth>();
        if (ph != null)
        {
            ph.currentHealth = _currentSave.playerCurrentHP;
            ph.maxHealth = _currentSave.playerMaxHP;
            ph.UpdateHealthUI();
            Debug.Log($"❤️ [SaveManager] HP restored: {ph.currentHealth}/{ph.maxHealth}");
        }

        // ── คืนสถานะ Monster ─────────────────────────────────
        RestoreMonsterStates();

        // ── คืนสถานะ Quest ───────────────────────────────────
        RestoreQuestStates();

        OnLoaded?.Invoke(_currentSave);
    }

    private void RestoreMonsterStates()
    {
        // หา MonsterHealth ทั้งหมดใน Scene
        var allMonsters = FindObjectsOfType<MonoBehaviour>();
        foreach (var mb in allMonsters)
        {
            // ตรวจว่า Monster นี้ตายแล้วหรือยัง
            if (_currentSave.killedMonsterIDs.Contains(mb.gameObject.name))
            {
                mb.gameObject.SetActive(false);
                Debug.Log($"☠️ [SaveManager] Monster still dead: {mb.gameObject.name}");
            }
        }
    }

    private void RestoreQuestStates()
    {
        // ส่ง Event ให้ QuestManager จัดการ (ถ้ามี)
        // QuestManager.Instance?.LoadCompletedQuests(_currentSave.completedSideQuests);
        Debug.Log($"📋 [SaveManager] Quests restored: {_currentSave.completedSideQuests.Count} completed");
    }

    /// <summary>
    /// ตรวจสอบว่า Monster นี้ตายแล้วหรือยัง (ใช้ตอน Scene โหลด)
    /// </summary>
    public bool IsMonsterDead(string monsterID)
        => _currentSave != null && _currentSave.killedMonsterIDs.Contains(monsterID);

    /// <summary>
    /// ตรวจสอบว่า Quest นี้สำเร็จแล้วหรือยัง
    /// </summary>
    public bool IsQuestCompleted(string questID)
        => _currentSave != null && _currentSave.completedSideQuests.Contains(questID);

    /// <summary>
    /// มี Save Data หรือยัง (สำหรับ Main Menu: เปิด/ปิดปุ่ม Continue)
    /// </summary>
    public bool HasSaveData()
        => PlayerPrefs.HasKey(SAVE_KEY) && !string.IsNullOrEmpty(_currentSave?.savePointID);

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Disk IO

    private void WriteToDisk(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private SaveData LoadFromDisk()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return null;
        string json = PlayerPrefs.GetString(SAVE_KEY);
        return JsonUtility.FromJson<SaveData>(json);
    }

    /// <summary>
    /// ลบ Save ทั้งหมด (New Game)
    /// </summary>
    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        _currentSave = new SaveData();
        Debug.Log("🗑️ [SaveManager] Save data deleted.");
    }

    #endregion
}