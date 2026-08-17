using UnityEngine;

namespace Momo.UITutorial.Core
{
    /// <summary>
    /// รวมจุดเดียวสำหรับเปิด/ปิด Input ของผู้เล่นเวลามี Popup
    /// ปรับส่วนนี้ให้ตรงกับระบบ Input ของโปรเจกต์จริง (Fantacode Third Person Controller)
    /// </summary>
    public static class PopupInputBlocker
    {
        public static void SetPlayerInputEnabled(bool enabled)
        {
            // ---- ตัวอย่างสำหรับ Fantacode Third Person Controller ----
            var playerInput = GameObject.FindGameObjectWithTag("Player");
            if (playerInput == null) return;

            // ปิด/เปิด Component ควบคุมการเคลื่อนที่และโจมตี
            var motor = playerInput.GetComponent<MonoBehaviour>(); // แทนด้วยคลาสจริง เช่น TC_Motor, ThirdPersonMotor
            // motor.enabled = enabled;

            // ถ้าใช้ New Input System:
            // var playerInputComp = playerInput.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            // if (playerInputComp != null)
            // {
            //     if (enabled) playerInputComp.ActivateInput();
            //     else playerInputComp.DeactivateInput();
            // }

            // ปิด Cursor lock ถ้าจำเป็นระหว่างเปิด Popup (กรณีเล่นด้วยเมาส์)
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }
    }
}