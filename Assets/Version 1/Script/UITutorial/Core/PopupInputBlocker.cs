using UnityEngine;
using UnityEngine.InputSystem;

namespace Momo.UITutorial.Core
{
    public static class PopupInputBlocker
    {
        // ตั้งชื่อ Action Map ให้ตรงกับที่จะสร้างใน Input Actions Asset (ดู step setup ข้อ 3)
        private const string POPUP_ACTION_MAP = "PopupUI";
        private const string PLAYER_ACTION_MAP = "Player";

        public static void SetPlayerInputEnabled(bool enabled)
        {
            var playerObj = PlayerReference.Instance != null
                ? PlayerReference.Instance.gameObject
                : GameObject.FindGameObjectWithTag("Player");

            if (playerObj == null) return;

            // 1) สลับ Action Map (กันปุ่ม Attack/Jump/Dodge หลุดออกมาระหว่าง Popup เปิด)
            var playerInput = playerObj.GetComponent<PlayerInput>();
            if (playerInput != null)
                playerInput.SwitchCurrentActionMap(enabled ? PLAYER_ACTION_MAP : POPUP_ACTION_MAP);

            // 2) ตั้ง flag กันระบบ Parkour/Combat ที่เช็คตรงๆ (มีอยู่แล้วใน MOMOCharacterBridge)
            var bridge = playerObj.GetComponent<MOMOCharacterBridge>();
            if (bridge != null)
                bridge.PreventAllSystems = !enabled;

            // 3) Cursor (เผื่อเทสบน PC ด้วยเมาส์)
            Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !enabled;
        }
    }
}