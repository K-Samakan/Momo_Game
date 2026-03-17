using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ✅ จัดการ Input สำหรับ Ice Block (L2, R2, Move)
/// </summary>
public class IceBlockInputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private bool isL2Pressed = false;
    private bool isR2Pressed = false;
    private Vector2 moveInput = Vector2.zero;

    public void Initialize(PlayerInput input)
    {
        playerInput = input;
        SubscribeToInputs();
    }

    void SubscribeToInputs()
    {
        if (playerInput == null) 
        {
            Debug.LogError("❌ InputManager: PlayerInput is null!");
            return;
        }

        var puzzle3Map = playerInput.actions.FindActionMap("Puzzle3");
        if (puzzle3Map != null)
        {
            var l2Action = puzzle3Map.FindAction("L2");
            var r2Action = puzzle3Map.FindAction("R2");
            var moveAction = puzzle3Map.FindAction("Move");

            if (l2Action != null)
            {
                l2Action.performed += OnL2;
                l2Action.canceled += OnL2;
                Debug.Log("✅ L2 Action bound");
            }
            else
            {
                Debug.LogError("❌ L2 Action not found in Puzzle3 map!");
            }

            if (r2Action != null)
            {
                r2Action.performed += OnR2;
                r2Action.canceled += OnR2;
                Debug.Log("✅ R2 Action bound");
            }
            else
            {
                Debug.LogError("❌ R2 Action not found in Puzzle3 map!");
            }

            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
                Debug.Log("✅ Move Action bound");
            }
            else
            {
                Debug.LogError("❌ Move Action not found in Puzzle3 map!");
            }

            Debug.Log("✅ Input Manager: All actions bound successfully");
        }
        else
        {
            Debug.LogError("❌ InputManager: Puzzle3 action map not found!");
        }
    }

    void OnL2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isL2Pressed = true;
            Debug.Log("🔴 InputManager: L2 กดค้าง");
        }
        else if (context.canceled)
        {
            isL2Pressed = false;
            Debug.Log("⚫ InputManager: L2 ปล่อย");
        }
    }

    void OnR2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isR2Pressed = true;
            Debug.Log("🔴 InputManager: R2 กดค้าง");
        }
        else if (context.canceled)
        {
            isR2Pressed = false;
            Debug.Log("⚫ InputManager: R2 ปล่อย");
        }
    }

    void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (moveInput.magnitude > 0.1f)
        {
            Debug.Log($"🕹️ InputManager: Move Input: {moveInput}");
        }
    }

    public bool IsBothTriggersPressed()
    {
        bool result = isL2Pressed && isR2Pressed;
        if (result)
        {
            Debug.Log($"🎮 Both triggers pressed: L2={isL2Pressed}, R2={isR2Pressed}");
        }
        return result;
    }

    public bool HasMoveInput()
    {
        return moveInput.magnitude > 0.5f;
    }

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    void OnDestroy()
    {
        if (playerInput != null)
        {
            var puzzle3Map = playerInput.actions.FindActionMap("Puzzle3");
            if (puzzle3Map != null)
            {
                var l2Action = puzzle3Map.FindAction("L2");
                var r2Action = puzzle3Map.FindAction("R2");
                var moveAction = puzzle3Map.FindAction("Move");

                if (l2Action != null)
                {
                    l2Action.performed -= OnL2;
                    l2Action.canceled -= OnL2;
                }

                if (r2Action != null)
                {
                    r2Action.performed -= OnR2;
                    r2Action.canceled -= OnR2;
                }

                if (moveAction != null)
                {
                    moveAction.performed -= OnMove;
                    moveAction.canceled -= OnMove;
                }
            }
        }

        Debug.Log("🗑️ InputManager destroyed and unsubscribed");
    }
}