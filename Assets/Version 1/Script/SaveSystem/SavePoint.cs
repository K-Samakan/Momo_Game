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

    [Header("=== Respawn Position ===")]
    [Tooltip("ถ้าไม่ได้กำหนด จะใช้ตำแหน่งของ GameObject นี้")]
    public Transform respawnPoint;

    [Header("=== Visual ===")]
    public GameObject idleVFX;        // VFX ตอนยังไม่ได้ Activate
    public GameObject activeVFX;      // VFX ตอน Activate แล้ว
    public GameObject promptUI;       // UI "กด △ เพื่อ Save"

    [Header("=== Notification ===")]
    [Tooltip("Canvas แสดง 'บันทึกแล้ว!' (ลาก TextMeshPro มาใส่)")]
    public TextMeshProUGUI saveNotificationText;
    public float notificationDuration = 2f;

    [Header("=== Haptic ===")]
    public bool enableHaptic = true;

    #endregion

    // ─────────────────────────────────────────────────────────
    #region State

    private bool _hasBeenVisited = false;   // ผู้เล่นเคยเดินผ่านแล้วหรือยัง
    private bool _playerNearby = false;   // Player อยู่ใน Trigger ไหม
    private bool _isSaving = false;   // กำลัง Save อยู่ไหม (กัน Spam)
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

        // ซ่อน Prompt และ Notification เริ่มต้น
        if (promptUI != null) promptUI.SetActive(false);
        if (saveNotificationText != null) saveNotificationText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!_playerNearby || _isSaving) return;

        // ถ้า Save Point นี้เคย Save แล้ว → รอกด △
        if (_hasBeenVisited)
        {
            bool trianglePressed = false;

            // DualSense / Gamepad
            if (Gamepad.current != null)
                trianglePressed = Gamepad.current.buttonNorth.wasPressedThisFrame; // △ = North

            // Keyboard fallback
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
        {
            // ครั้งแรก → Auto Save ทันที
            StartCoroutine(DoSave());
        }
        else
        {
            // เคย Save แล้ว → แสดง Prompt
            if (promptUI != null) promptUI.SetActive(true);
        }
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

        // Haptic feedback
        if (enableHaptic && Gamepad.current != null)
        {
            Gamepad.current.SetMotorSpeeds(0.2f, 0.5f);
            yield return new WaitForSeconds(0.3f);
            Gamepad.current.SetMotorSpeeds(0f, 0f);
        }

        // อัปเดต State
        _hasBeenVisited = true;
        SetVisualState(true);

        // ซ่อน Prompt
        if (promptUI != null) promptUI.SetActive(false);

        // แสดง Notification
        ShowSaveNotification();

        yield return new WaitForSeconds(0.5f);  // กัน Spam save
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

        // Fade in
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

        // Fade out
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
        Gizmos.color = _hasBeenVisited ? Color.green : Color.yellow;
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