// ============================================================
// FILE: SavePoint.cs
// PATH: Assets/Version 1/Script/SaveSystem/SavePoint.cs
// ============================================================
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SavePoint : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    #region Inspector

    [Header("=== Identity ===")]
    [Tooltip("ID เฉพาะของ Save Point นี้ (ห้ามซ้ำกันใน Scene)")]
    public string savePointID = "SP_001";

    [Header("=== Start Point ===")]
    [Tooltip("ติ๊กถ้าเป็นจุดเริ่มต้นของด่าน → Auto Save ตอนโหลดโดยไม่ต้องรอ Trigger")]
    public bool isStartPoint = false;

    [Header("=== Respawn Position ===")]
    [Tooltip("ถ้าไม่ได้กำหนด จะใช้ตำแหน่งของ GameObject นี้")]
    public Transform respawnPoint;

    [Header("=== Visual ===")]
    public GameObject idleVFX;
    public GameObject activeVFX;
    public GameObject promptUI;

    [Header("=== Notification ===")]
    [Tooltip("Canvas แสดง 'บันทึกแล้ว!'")]
    public TextMeshProUGUI saveNotificationText;
    public float notificationDuration = 2f;

    [Header("=== Sound ===")]
    [Tooltip("เสียงที่เล่นเมื่อ Save สำเร็จ ทั้งครั้งแรกและกด △ เอง")]
    public AudioClip saveSound;
    [Range(0f, 1f)]
    public float saveSoundVolume = 0.8f;

    [Header("=== Haptic ===")]
    public bool enableHaptic = true;

    #endregion

    // ─────────────────────────────────────────────────────────
    #region State

    private bool _hasBeenVisited = false;
    private bool _playerNearby = false;
    private bool _isSaving = false;
    private Coroutine _notifCoroutine;

    #endregion

    // ─────────────────────────────────────────────────────────
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

        if (promptUI != null) promptUI.SetActive(false);
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

    private void Update()
    {
        if (!_playerNearby || _isSaving) return;

        // ถ้า Save Point นี้เคย Save แล้ว → รอกด △
        if (_hasBeenVisited)
        {
            bool trianglePressed = false;

            if (Gamepad.current != null)
                trianglePressed = Gamepad.current.buttonNorth.wasPressedThisFrame;
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
                trianglePressed = true;

            if (trianglePressed)
                StartCoroutine(DoSave());
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Trigger

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNearby = true;
        Debug.Log($"📍 [SavePoint:{savePointID}] Player entered");

        if (!_hasBeenVisited)
            StartCoroutine(DoSave());
        else
            if (promptUI != null) promptUI.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        _playerNearby = false;
        if (promptUI != null) promptUI.SetActive(false);
        Debug.Log($"👋 [SavePoint:{savePointID}] Player left");
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Save Logic

    private IEnumerator DoSave()
    {
        _isSaving = true;

        // บันทึกผ่าน SaveManager
        SaveManager.Instance?.SaveAtPoint(this);

        // ── เสียง Save ────────────────────────────────────────
        if (saveSound != null)
            AudioSource.PlayClipAtPoint(saveSound, transform.position, saveSoundVolume);

        // ── Haptic ────────────────────────────────────────────
        if (enableHaptic && Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0.2f, 0.5f);
            yield return new WaitForSeconds(0.3f);
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }

        _hasBeenVisited = true;
        SetVisualState(true);
        if (promptUI != null) promptUI.SetActive(false);
        ShowSaveNotification();

        yield return new WaitForSeconds(0.5f);
        _isSaving = false;
    }

    #endregion

    // ─────────────────────────────────────────────────────────
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
        saveNotificationText.gameObject.SetActive(true);
        saveNotificationText.text = "💾 บันทึกแล้ว!";

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

        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, t / 0.5f);
            saveNotificationText.color = color;
            yield return null;
        }

        saveNotificationText.gameObject.SetActive(false);
    }

    #endregion

    // ─────────────────────────────────────────────────────────
    #region Public API

    public Vector3 GetRespawnPosition()
        => respawnPoint != null ? respawnPoint.position : transform.position;

    #endregion

    // ─────────────────────────────────────────────────────────
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