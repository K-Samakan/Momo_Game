using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SavePoint : MonoBehaviour, Interactable
{
    #region Inspector

    [Header("=== Identity ===")]
    [Tooltip("ID เฉพาะของ Save Point นี้ (ห้ามซ้ำกันใน Scene)")]
    public string savePointID = "SP_001";

    [Header("=== Interaction ===")]
    [Tooltip("ข้อความที่แสดงบนป้าย prompt ตอนเข้าใกล้")]
    [SerializeField] private string interactionPrompt = "Save Game";

    [Header("=== Start Point ===")]
    [Tooltip("ติ๊กถ้าเป็นจุดเริ่มต้นของด่าน → Auto Save ตอนโหลดโดยไม่ต้องรอ Trigger")]
    public bool isStartPoint = false;

    [Header("=== Respawn Position ===")]
    [Tooltip("ถ้าไม่ได้กำหนด จะใช้ตำแหน่งของ GameObject นี้")]
    public Transform respawnPoint;

    [Header("=== Visual ===")]
    public GameObject idleVFX;
    public GameObject activeVFX;

    [Header("=== Notification ===")]
    [Tooltip("Canvas แสดง 'บันทึกแล้ว!'")]
    public GameObject saveNotificationPanel;
    public TextMeshProUGUI saveNotificationText;
    public float notificationDuration = 2f;

    [Tooltip("ชื่อจุดพักที่แสดงบรรทัดบน")]
    public string notificationTitle = "Forest Sanctuary";
    [Tooltip("ข้อความบรรทัดล่าง")]
    public string notificationSubtitle = "Health Restored";

    [Header("=== Sound ===")]
    [Tooltip("เสียงที่เล่นเมื่อ Save สำเร็จ")]
    public AudioClip saveSound;
    [Range(0f, 1f)]
    public float saveSoundVolume = 0.8f;

    [Header("=== Haptic ===")]
    public bool enableHaptic = true;

    #endregion

    #region State

    private bool _hasBeenVisited = false;
    private bool _isSaving = false;
    private Coroutine _notifCoroutine;

    #endregion

    #region Interactable Implementation

    /// <summary>ข้อความที่ InteractionPromptUI จะเอาไปแสดงบนป้าย</summary>
    public string InteractionPrompt => interactionPrompt;

    /// <summary>ถูกเรียกจาก Interactor เมื่อผู้เล่นกด ⭕ ขณะอยู่ในระยะ</summary>
    public bool Interact(Interactor interactor)
    {
        if (_isSaving) return false;

        StartCoroutine(DoSave());
        return true;
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // ตรวจว่า Save Point นี้เคยถูก Save ไว้แล้วหรือยัง
        if (SaveManager.Instance != null &&
            SaveManager.Instance.CurrentSave?.savePointID == savePointID)
        {
            _hasBeenVisited = true;
            SetVisualState(true);
            Debug.Log($"🔵 [SavePoint:{savePointID}] Already visited (from save data)");
        }
        else
        {
            SetVisualState(false);
        }

        if (saveNotificationText != null) saveNotificationText.gameObject.SetActive(false);

        // ── Start Point: Auto Save ทันทีถ้ายังไม่มี Save Data ──
        if (isStartPoint && SaveManager.Instance != null)
        {
            bool hasSave = !string.IsNullOrEmpty(SaveManager.Instance.CurrentSave?.savePointID);
            if (!hasSave)
            {
                StartCoroutine(DoSave());
                Debug.Log($"🟢 [SavePoint:{savePointID}] Start Point → Auto saved on load");
            }
        }
    }

    #endregion

    #region Trigger (Auto Save ครั้งแรก)

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"📍 [SavePoint:{savePointID}] Player entered");

        // ครั้งแรกที่มาถึง → save อัตโนมัติ ไม่ต้องกดปุ่ม
        // ครั้งต่อไป → ป้าย prompt จะขึ้นเอง (จาก Interactor) รอผู้เล่นกด ⭕
        if (!_hasBeenVisited)
            StartCoroutine(DoSave());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log($"👋 [SavePoint:{savePointID}] Player left");
    }

    #endregion

    #region Save Logic

    private IEnumerator DoSave()
    {
        _isSaving = true;

        // บันทึกผ่าน SaveManager
        SaveManager.Instance?.SaveAtPoint(this);

        _hasBeenVisited = true;
        SetVisualState(true);

        // แสดง notification ทันที (ไม่ต้องรอ haptic)
        ShowSaveNotification();

        // ── เสียง Save (2D ดังคงที่ ไม่ขึ้นกับระยะกล้อง) ──
        if (saveSound != null)
        {
            GameObject sfxObj = new GameObject("SaveSFX");
            AudioSource src = sfxObj.AddComponent<AudioSource>();
            src.clip = saveSound;
            src.volume = saveSoundVolume;
            src.spatialBlend = 0f;
            src.Play();
            Destroy(sfxObj, saveSound.length + 0.1f);
        }

        // ── Haptic ──
        if (enableHaptic && Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0.2f, 0.5f);
            yield return new WaitForSeconds(0.3f);
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }

        yield return new WaitForSeconds(0.5f);
        _isSaving = false;
    }

    #endregion

    #region Visual & UI

    private void SetVisualState(bool activated)
    {
        if (idleVFX != null) idleVFX.SetActive(!activated);
        if (activeVFX != null) activeVFX.SetActive(activated);
    }

    private void ShowSaveNotification()
    {
        if (saveNotificationText == null) return;
        if (_notifCoroutine != null) StopCoroutine(_notifCoroutine);
        _notifCoroutine = StartCoroutine(NotificationRoutine());
    }

    private IEnumerator NotificationRoutine()
    {
        if (saveNotificationPanel != null) saveNotificationPanel.SetActive(true);
        saveNotificationText.gameObject.SetActive(true);
        saveNotificationText.text = $"{notificationTitle}\n{notificationSubtitle}";

        // Fade In
        var color = saveNotificationText.color;
        color.a = 0f;
        saveNotificationText.color = color;

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, t / 0.3f);
            saveNotificationText.color = color;
            yield return null;
        }

        yield return new WaitForSeconds(notificationDuration);

        // Fade Out
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, t / 0.5f);
            saveNotificationText.color = color;
            yield return null;
        }

        saveNotificationText.gameObject.SetActive(false);
        if (saveNotificationPanel != null) saveNotificationPanel.SetActive(false);
    }

    #endregion

    #region Public API

    public Vector3 GetRespawnPosition()
        => respawnPoint != null ? respawnPoint.position : transform.position;

    #endregion

    #region Gizmos

    private void OnDrawGizmos()
    {
        Gizmos.color = isStartPoint ? Color.blue
                     : (_hasBeenVisited ? Color.green : Color.yellow);
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (respawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(respawnPoint.position, 0.3f);
            Gizmos.DrawLine(transform.position, respawnPoint.position);
        }
    }

    #endregion
}