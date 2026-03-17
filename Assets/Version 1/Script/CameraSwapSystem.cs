using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CameraSwapSystem : MonoBehaviour
{
    public static CameraSwapSystem Instance { get; private set; }

    [Header("Camera References")]
    public Camera mainCamera;
    public Camera topDownCamera;

    [Header("Cinemachine Components")]
    public MonoBehaviour cinemachineVirtualCamera;
    public MonoBehaviour minimapVirtualCamera;
    public MonoBehaviour cinemachineBrain;

    [Header("UI References")]
    public RawImage minimapDisplay;
    public RenderTexture minimapRenderTexture;

    [Header("UI Control")]
    public GameObject mirrorInteractionPanel; // Panel ที่ต้องการแสดงเฉพาะใน MainCamera
    private CanvasGroup mirrorPanelCanvasGroup; // Canvas Group สำหรับควบคุม visibility

    [Header("Input Settings")]
    public InputActionAsset inputActions;
    private InputAction toggleCameraAction;

    [Header("Camera Settings")]
    public float swapTransitionTime = 0.5f;
    public AnimationCurve swapCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isSwapped = false;
    private bool isTransitioning = false;

    // เก็บสถานะเดิมของ MirrorInteractionPanel
    private bool mirrorPanelWasActive = false;

    private Vector3 originalTopDownPos;
    private Quaternion originalTopDownRot;
    private float originalTopDownFOV;
    private bool originalTopDownOrthographic;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeCameras();
            SetupInput();
            FindAndSetupMirrorInteractionPanel();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void FindAndSetupMirrorInteractionPanel()
    {
        if (mirrorInteractionPanel == null)
        {
            // ค้นหา MirrorInteractionPanel อัตโนมัติ
            GameObject found = GameObject.Find("MirrorInteractionPanel");
            if (found == null)
            {
                found = GameObject.Find("MirrorInteractionPannel"); // ในกรณีที่สะกดผิด
            }

            if (found != null)
            {
                mirrorInteractionPanel = found;
                Debug.Log($"MirrorInteractionPanel found automatically: {found.name}");
            }
            else
            {
                Debug.LogWarning("MirrorInteractionPanel not found! Please assign it manually.");
                return;
            }
        }

        // เพิ่ม CanvasGroup ถ้าไม่มี
        mirrorPanelCanvasGroup = mirrorInteractionPanel.GetComponent<CanvasGroup>();
        if (mirrorPanelCanvasGroup == null)
        {
            mirrorPanelCanvasGroup = mirrorInteractionPanel.AddComponent<CanvasGroup>();
            Debug.Log("CanvasGroup added to MirrorInteractionPanel");
        }

        // ตั้งค่าเริ่มต้น
        mirrorPanelCanvasGroup.alpha = 1f;
        mirrorPanelCanvasGroup.interactable = true;
        mirrorPanelCanvasGroup.blocksRaycasts = true;
    }

    private void InitializeCameras()
    {
        if (minimapRenderTexture == null)
        {
            minimapRenderTexture = new RenderTexture(256, 256, 16);
            minimapRenderTexture.name = "MinimapRT";
        }

        SetupDefaultState();
        StoreOriginalSettings();
        StoreMirrorPanelState();
    }

    private void StoreMirrorPanelState()
    {
        // เก็บสถานะเดิมของ MirrorInteractionPanel
        if (mirrorInteractionPanel != null)
        {
            mirrorPanelWasActive = mirrorInteractionPanel.activeInHierarchy;
        }
    }

    private void SetupDefaultState()
    {
        if (mainCamera != null)
        {
            mainCamera.enabled = true;
            mainCamera.targetTexture = null;
        }

        if (topDownCamera != null)
        {
            topDownCamera.enabled = true;
            topDownCamera.targetTexture = minimapRenderTexture;
        }

        if (minimapDisplay != null)
        {
            minimapDisplay.texture = minimapRenderTexture;
        }

        SetupCinemachineInitial();
    }

    private void SetupCinemachineInitial()
    {
        if (cinemachineVirtualCamera != null)
        {
            cinemachineVirtualCamera.enabled = true;
            SetVirtualCameraPriority(cinemachineVirtualCamera, 10);
        }

        if (minimapVirtualCamera != null)
        {
            minimapVirtualCamera.enabled = true;
            SetVirtualCameraPriority(minimapVirtualCamera, 5);
        }

        if (cinemachineBrain != null)
        {
            cinemachineBrain.enabled = true;
        }
    }

    private void SetVirtualCameraPriority(MonoBehaviour virtualCamera, int priority)
    {
        var priorityField = virtualCamera.GetType().GetField("m_Priority",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (priorityField != null)
        {
            priorityField.SetValue(virtualCamera, priority);
        }
        else
        {
            var priorityProperty = virtualCamera.GetType().GetProperty("Priority");
            if (priorityProperty != null)
            {
                priorityProperty.SetValue(virtualCamera, priority);
            }
        }
    }

    private void StoreOriginalSettings()
    {
        if (topDownCamera != null)
        {
            originalTopDownPos = topDownCamera.transform.position;
            originalTopDownRot = topDownCamera.transform.rotation;
            originalTopDownFOV = topDownCamera.fieldOfView;
            originalTopDownOrthographic = topDownCamera.orthographic;
        }
    }

    private void SetupInput()
    {
        if (inputActions != null)
        {
            var actionMap = inputActions.FindActionMap("Puzzle2");
            if (actionMap != null)
            {
                toggleCameraAction = actionMap.FindAction("ToggleCamera");
                if (toggleCameraAction != null)
                {
                    toggleCameraAction.performed += OnToggleCamera;
                }
            }
        }

        if (toggleCameraAction == null)
        {
            toggleCameraAction = new InputAction("ToggleCamera", InputActionType.Button);
            toggleCameraAction.AddBinding("<Gamepad>/rightShoulder");
            toggleCameraAction.AddBinding("<Keyboard>/tab");
            toggleCameraAction.performed += OnToggleCamera;
        }
    }

    private void OnEnable()
    {
        if (toggleCameraAction != null) toggleCameraAction.Enable();
        if (inputActions != null) inputActions.Enable();
    }

    private void OnDisable()
    {
        if (toggleCameraAction != null)
        {
            toggleCameraAction.performed -= OnToggleCamera;
            toggleCameraAction.Disable();
        }
        if (inputActions != null) inputActions.Disable();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) && !isTransitioning) SwapCameras();
        if (Input.GetKeyDown(KeyCode.R) && !isTransitioning) SwapCameras();
        if (Input.GetKeyDown(KeyCode.JoystickButton5) && !isTransitioning) SwapCameras();
    }

    private void OnToggleCamera(InputAction.CallbackContext context)
    {
        if (!isTransitioning) SwapCameras();
    }

    public void SwapCameras()
    {
        if (isTransitioning) return;

        Debug.Log($"Starting camera swap. Currently swapped: {isSwapped}");
        StartCoroutine(SwapCamerasCoroutine());
    }

    private System.Collections.IEnumerator SwapCamerasCoroutine()
    {
        isTransitioning = true;

        if (!isSwapped)
        {
            SwapCameraTargets(true);
        }
        else
        {
            SwapCameraTargets(false);
        }

        yield return new WaitForSeconds(0.1f);

        isSwapped = !isSwapped;
        isTransitioning = false;

        Debug.Log($"Camera swap complete: {(isSwapped ? "Top-down main" : "TPS main")}");
        DebugCameraStates();
    }

    private void SwapCameraTargets(bool swapToTopDown)
    {
        if (swapToTopDown)
        {
            Debug.Log("Swapping TO Top-Down view...");

            // ซ่อน MirrorInteractionPanel โดยใช้ CanvasGroup
            HideMirrorInteractionPanelCompletely();

            SetVirtualCameraPriority(cinemachineVirtualCamera, 5);
            SetVirtualCameraPriority(minimapVirtualCamera, 10);

            mainCamera.targetTexture = minimapRenderTexture;
            topDownCamera.targetTexture = null;

            SetAudioListener(false);

            Debug.Log("SWAPPED: Main screen = Top-Down Camera (MirrorInteractionPanel HIDDEN), Minimap = Main Camera");
        }
        else
        {
            Debug.Log("Swapping BACK to TPS view...");

            SetVirtualCameraPriority(cinemachineVirtualCamera, 10);
            SetVirtualCameraPriority(minimapVirtualCamera, 5);

            mainCamera.targetTexture = null;
            topDownCamera.targetTexture = minimapRenderTexture;

            // แสดง MirrorInteractionPanel กลับมา
            ShowMirrorInteractionPanelCompletely();

            SetAudioListener(true);

            Debug.Log("NORMAL: Main screen = Main Camera (MirrorInteractionPanel VISIBLE), Minimap = Top-Down Camera");
        }

        RefreshCameras();
    }

    private void HideMirrorInteractionPanelCompletely()
    {
        if (mirrorPanelCanvasGroup != null)
        {
            // เก็บสถานะปัจจุบัน
            mirrorPanelWasActive = mirrorPanelCanvasGroup.alpha > 0;

            // ซ่อนแบบสมบูรณ์
            mirrorPanelCanvasGroup.alpha = 0f;
            mirrorPanelCanvasGroup.interactable = false;
            mirrorPanelCanvasGroup.blocksRaycasts = false;

            Debug.Log("MirrorInteractionPanel COMPLETELY HIDDEN using CanvasGroup");
        }
        else if (mirrorInteractionPanel != null)
        {
            // วิธีสำรอง: ปิด GameObject
            mirrorPanelWasActive = mirrorInteractionPanel.activeInHierarchy;
            mirrorInteractionPanel.SetActive(false);
            Debug.Log("MirrorInteractionPanel hidden using GameObject.SetActive(false)");
        }
    }

    private void ShowMirrorInteractionPanelCompletely()
    {
        if (mirrorPanelCanvasGroup != null && mirrorPanelWasActive)
        {
            // แสดงแบบสมบูรณ์
            mirrorPanelCanvasGroup.alpha = 1f;
            mirrorPanelCanvasGroup.interactable = true;
            mirrorPanelCanvasGroup.blocksRaycasts = true;

            Debug.Log("MirrorInteractionPanel COMPLETELY VISIBLE using CanvasGroup");
        }
        else if (mirrorInteractionPanel != null && mirrorPanelWasActive)
        {
            // วิธีสำรอง: เปิด GameObject
            mirrorInteractionPanel.SetActive(true);
            Debug.Log("MirrorInteractionPanel shown using GameObject.SetActive(true)");
        }
    }

    private void SetAudioListener(bool mainCameraActive)
    {
        AudioListener mainListener = mainCamera.GetComponent<AudioListener>();
        AudioListener topDownListener = topDownCamera.GetComponent<AudioListener>();

        if (mainCameraActive)
        {
            if (mainListener != null) mainListener.enabled = true;
            if (topDownListener != null) topDownListener.enabled = false;
        }
        else
        {
            if (mainListener != null) mainListener.enabled = false;
            if (topDownListener != null) topDownListener.enabled = true;
        }
    }

    private void RefreshCameras()
    {
        mainCamera.enabled = false;
        topDownCamera.enabled = false;
        StartCoroutine(EnableCamerasNextFrame());
    }

    private System.Collections.IEnumerator EnableCamerasNextFrame()
    {
        yield return null;
        mainCamera.enabled = true;
        topDownCamera.enabled = true;
    }

    // Public methods สำหรับควบคุม MirrorInteractionPanel จากภายนอก
    public void ShowMirrorInteractionPanel()
    {
        if (!isSwapped) // แสดงเฉพาะเมื่อไม่ได้ swap
        {
            ShowMirrorInteractionPanelCompletely();
        }
    }

    public void HideMirrorInteractionPanel()
    {
        HideMirrorInteractionPanelCompletely();
    }

    public bool IsMirrorInteractionPanelVisible()
    {
        if (mirrorPanelCanvasGroup != null)
        {
            return mirrorPanelCanvasGroup.alpha > 0 && !isSwapped;
        }
        else if (mirrorInteractionPanel != null)
        {
            return mirrorInteractionPanel.activeInHierarchy && !isSwapped;
        }
        return false;
    }

    // Public methods อื่นๆ
    public bool IsSwapped() => isSwapped;

    public void ForceSwapToTopDown()
    {
        if (!isSwapped && !isTransitioning) SwapCameras();
    }

    public void ForceSwapToNormal()
    {
        if (isSwapped && !isTransitioning) SwapCameras();
    }

    public void OnPuzzleComplete()
    {
        if (isSwapped)
        {
            ForceSwapToNormal();
            Debug.Log("Puzzle complete - switching back to TPS view");
        }
    }

    public void SetMinimapVisible(bool visible)
    {
        if (minimapDisplay != null)
        {
            minimapDisplay.gameObject.SetActive(visible);
        }
    }

    private void OnDestroy()
    {
        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
        }
    }

    public void DebugCameraStates()
    {
        Debug.Log($"=== Camera Swap System Debug ===");
        Debug.Log($"Is Swapped: {isSwapped}");
        Debug.Log($"Main Camera Target: {(mainCamera.targetTexture == null ? "Main Screen" : "Minimap")}");
        Debug.Log($"Top-down Camera Target: {(topDownCamera.targetTexture == null ? "Main Screen" : "Minimap")}");
        Debug.Log($"MirrorInteractionPanel: {(mirrorInteractionPanel != null ? mirrorInteractionPanel.name : "NULL")}");
        Debug.Log($"CanvasGroup Alpha: {(mirrorPanelCanvasGroup != null ? mirrorPanelCanvasGroup.alpha : -1)}");
        Debug.Log($"MirrorInteractionPanel Active: {(mirrorInteractionPanel != null ? mirrorInteractionPanel.activeInHierarchy : false)}");
    }
}