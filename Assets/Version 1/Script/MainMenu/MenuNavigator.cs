using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MomoGame.MainMenu
{
    /// <summary>
    /// จัดการ Navigation ด้วย DualSense Controller
    /// Asian Layout: Circle=Confirm, Cross=Cancel
    /// D-Pad + Left Stick = เลื่อนขึ้น-ลง
    ///
    /// FIX: ข้ามปุ่มที่ถูกซ่อน (inactive) หรือ non-interactable อัตโนมัติ
    /// เช่น เมื่อซ่อนปุ่ม Options การเลื่อน NewGame <-> Quit จะทำงานถูกต้อง
    /// โดยไม่ต้องไปแก้ช่อง Navigation ใน Inspector ของแต่ละปุ่ม
    /// </summary>
    public class MenuNavigator : MonoBehaviour
    {
        [Header("Navigation Settings")]
        [SerializeField] private float navigationDelay = 0.2f;
        [SerializeField] private float analogDeadzone = 0.5f;
        [SerializeField] private bool enableWrapping = true;

        [Header("Haptic Feedback")]
        [SerializeField] private bool hapticOnNavigate = true;
        [SerializeField][Range(0f, 1f)] private float navigateHapticIntensity = 0.2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = false;

        private EventSystem eventSystem;
        private MainMenuController menuController;
        private DualSenseController hapticController;

        private float lastNavigationTime;
        private Vector2 lastAxisValue;

        // ป้องกันการวน loop ไม่รู้จบเวลาเดินตาม navigation chain
        private const int MaxNavigationSteps = 32;

        private void Awake()
        {
            eventSystem = EventSystem.current;

            if (eventSystem == null)
            {
                CreateEventSystem();
            }

            menuController = GetComponent<MainMenuController>();
            hapticController = FindObjectOfType<DualSenseController>();
        }

        private void Start()
        {
            if (showDebugLogs && Gamepad.current != null)
            {
                Debug.Log($"[MenuNavigator] Gamepad detected: {Gamepad.current.name}");
                Debug.Log("[MenuNavigator] Controls: D-Pad/Left Stick=Navigate, Circle=Confirm, Cross=Cancel");
            }
        }

        private void CreateEventSystem()
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystem = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();

            if (showDebugLogs)
                Debug.Log("[MenuNavigator] EventSystem created");
        }

        private void Update()
        {
            if (Gamepad.current == null) return;

            HandleNavigation();
            HandleConfirmButton();
            HandleCancelButton();
            EnsureSomethingSelected();
        }

        #region Navigation

        private void HandleNavigation()
        {
            if (Time.unscaledTime - lastNavigationTime < navigationDelay)
                return;

            Vector2 input = GetNavigationInput();

            if (Mathf.Abs(input.y) < analogDeadzone)
            {
                lastAxisValue = Vector2.zero;
                return;
            }

            if (lastAxisValue.y != 0f)
                return;

            lastAxisValue = input;

            if (input.y > analogDeadzone)
                NavigateUp();
            else if (input.y < -analogDeadzone)
                NavigateDown();
        }

        private Vector2 GetNavigationInput()
        {
            Vector2 input = Vector2.zero;

            // D-Pad (มีความสำคัญมากกว่า)
            if (Gamepad.current.dpad.up.isPressed)
                input.y = 1f;
            else if (Gamepad.current.dpad.down.isPressed)
                input.y = -1f;

            // Left Stick (ถ้า D-Pad ไม่กด)
            if (input.y == 0f)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                input.y = stick.y;
            }

            return input;
        }

        private void NavigateUp()
        {
            if (eventSystem.currentSelectedGameObject == null)
            {
                SelectFirstButton();
                lastNavigationTime = Time.unscaledTime;
                return;
            }

            Selectable current = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
            if (current == null) return;

            Selectable next = GetAdjacentSelectable(current, moveUp: true);

            if (next != null && next != current)
            {
                SelectButton(next.gameObject);
                OnNavigationFeedback();
            }

            lastNavigationTime = Time.unscaledTime;
        }

        private void NavigateDown()
        {
            if (eventSystem.currentSelectedGameObject == null)
            {
                SelectFirstButton();
                lastNavigationTime = Time.unscaledTime;
                return;
            }

            Selectable current = eventSystem.currentSelectedGameObject.GetComponent<Selectable>();
            if (current == null) return;

            Selectable next = GetAdjacentSelectable(current, moveUp: false);

            if (next != null && next != current)
            {
                SelectButton(next.gameObject);
                OnNavigationFeedback();
            }

            lastNavigationTime = Time.unscaledTime;
        }

        private void OnNavigationFeedback()
        {
            if (hapticOnNavigate && hapticController != null)
            {
                hapticController.TriggerHaptic(navigateHapticIntensity, 0.05f);
            }
        }

        #endregion

        #region Selectable Resolution (FIX)

        /// <summary>
        /// ปุ่มนี้ "ใช้งานได้จริง" หรือไม่
        /// - ต้อง active ใน hierarchy (ไม่ใช่ปุ่มที่ถูกซ่อน เช่น Options)
        /// - ต้อง interactable
        /// - ต้องไม่ได้ตั้ง Navigation = None
        /// </summary>
        private bool IsValidTarget(Selectable s)
        {
            return s != null
                && s.gameObject.activeInHierarchy
                && s.interactable
                && s.navigation.mode != Navigation.Mode.None;
        }

        /// <summary>
        /// หาปุ่มถัดไปในทิศที่กด โดยข้ามปุ่มที่ถูกซ่อน/ปิดอยู่
        /// และ wrap ไปยังขอบฝั่งตรงข้ามเฉพาะปุ่มที่ใช้งานได้จริง
        /// </summary>
        private Selectable GetAdjacentSelectable(Selectable current, bool moveUp)
        {
            if (current == null) return null;

            // 1) เดินตามทิศที่กด ข้ามปุ่มที่ใช้ไม่ได้ จนเจอปุ่มที่ valid
            Selectable cursor = current;
            for (int i = 0; i < MaxNavigationSteps; i++)
            {
                Selectable next = moveUp
                    ? cursor.FindSelectableOnUp()
                    : cursor.FindSelectableOnDown();

                if (next == null || next == current)
                    break; // ถึงปลายทาง หรือวนกลับมาที่เดิม

                if (IsValidTarget(next))
                    return next;

                cursor = next; // เป็นปุ่มที่ซ่อนอยู่ -> ข้ามไปต่อในทิศเดิม
            }

            // 2) ไม่เจอปุ่ม valid ในทิศนั้น -> wrap ไปขอบฝั่งตรงข้าม (ถ้าเปิด wrapping)
            if (!enableWrapping)
                return null;

            Selectable farValid = null;
            cursor = current;
            for (int i = 0; i < MaxNavigationSteps; i++)
            {
                Selectable next = moveUp
                    ? cursor.FindSelectableOnDown()
                    : cursor.FindSelectableOnUp();

                if (next == null || next == current)
                    break;

                if (IsValidTarget(next))
                    farValid = next; // จำปุ่ม valid ที่ไกลสุดไว้

                cursor = next;
            }

            return farValid; // อาจเป็น null ถ้ามีปุ่มที่ใช้ได้แค่ปุ่มเดียว
        }

        #endregion

        #region Button Handling

        private void HandleConfirmButton()
        {
            // Circle button (East) = Confirm
            if (Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                if (showDebugLogs)
                    Debug.Log("[Input] Circle pressed (Confirm)");

                ExecuteCurrentButton();
            }
        }

        private void HandleCancelButton()
        {
            // Cross button (South) = Cancel
            if (Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                if (showDebugLogs)
                    Debug.Log("[Input] Cross pressed (Cancel)");

                OnCancelPressed();
            }

            // ESC key support
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                OnCancelPressed();
            }
        }

        private void ExecuteCurrentButton()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return;

            Button button = eventSystem.currentSelectedGameObject.GetComponent<Button>();

            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }

        private void OnCancelPressed()
        {
            if (menuController != null)
            {
                if (menuController.IsOptionsActive())
                {
                    menuController.OnBackToMainMenu();
                }
            }
        }

        #endregion

        #region Helper Methods

        public void SetFirstSelected(GameObject target)
        {
            if (eventSystem != null && target != null)
            {
                SelectButton(target);

                if (showDebugLogs)
                    Debug.Log($"[MenuNavigator] Selected: {target.name}");
            }
        }

        private void SelectButton(GameObject target)
        {
            eventSystem.SetSelectedGameObject(null);
            eventSystem.SetSelectedGameObject(target);
        }

        private void SelectFirstButton()
        {
            MenuButton[] allButtons = FindObjectsOfType<MenuButton>();

            foreach (var menuButton in allButtons)
            {
                if (menuButton.gameObject.activeInHierarchy &&
                    menuButton.GetComponent<Button>().interactable)
                {
                    SetFirstSelected(menuButton.gameObject);
                    return;
                }
            }
        }

        private void EnsureSomethingSelected()
        {
            if (eventSystem == null) return;

            if (eventSystem.currentSelectedGameObject == null)
            {
                SelectFirstButton();
            }
        }

        #endregion
    }
}