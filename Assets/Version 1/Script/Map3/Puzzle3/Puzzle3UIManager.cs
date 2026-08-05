using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;

public class Puzzle3UIManager : MonoBehaviour
{
    public static Puzzle3UIManager Instance { get; private set; }

    [Header("Health UI")]
    public GameObject healthUIPanel;
    public Transform heartContainer;
    public GameObject heartPrefab;

    [Header("Minimap Camera Render")]
    public GameObject minimapPanel;
    public RawImage minimapDisplay;
    public Camera minimapCamera; // CameraMinimap3

    [Header("Minimap Render Settings")]
    public int renderTextureWidth = 512;
    public int renderTextureHeight = 512;
    public float minimapSize = 200f;
    public Vector2 minimapPosition = new Vector2(110, -110); // From top-left corner

    [Header("Camera Follow Settings")]
    public Transform playerTransform;
    public float cameraHeight = 20f;
    public float cameraOrthographicSize = 12f;
    public bool smoothFollow = true;
    public float followSpeed = 5f;

    [Header("Visual Settings")]
    public bool showBorder = true;
    public Color borderColor = Color.white;
    public float borderThickness = 4f;
    public bool circularMask = true;

    // Private variables
    private PlayerInput playerInput;
    private RenderTexture minimapRenderTexture;
    private int currentHealth = 3;
    private int maxHealth = 3;
    private Image[] heartImages;
    private bool isMinimapActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SetupHealthUI();
        SetupMinimapRender();
        
        // Subscribe to events
        IceBlock.OnHPChanged += OnHealthChanged;
        IceBlock.OnGameRestart += OnGameRestart;
        
        // Monitor action map changes
        StartCoroutine(MonitorActionMapChanges());
        
        Debug.Log("Puzzle3UIManager initialized with camera rendering");
    }

    private void InitializeComponents()
    {
        // Find player
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log($"Found Player: {player.name}");
            }
        }

        // Find PlayerInput
        if (playerInput == null)
        {
            playerInput = FindFirstObjectByType<PlayerInput>();
        }

        // Find minimap camera
        if (minimapCamera == null)
        {
            GameObject cameraObj = GameObject.Find("CameraMinimap3");
            if (cameraObj != null)
            {
                minimapCamera = cameraObj.GetComponent<Camera>();
                Debug.Log("Found CameraMinimap3");
            }
            else
            {
                Debug.LogWarning("CameraMinimap3 not found! Create a camera named 'CameraMinimap3'");
            }
        }
    }

    private void SetupHealthUI()
    {
        if (healthUIPanel == null) return;

        // Position health UI at top-right
        RectTransform healthRect = healthUIPanel.GetComponent<RectTransform>();
        if (healthRect != null)
        {
            healthRect.anchorMin = new Vector2(1, 1);
            healthRect.anchorMax = new Vector2(1, 1);
            healthRect.anchoredPosition = new Vector2(-120, -50);
        }

        CreateHearts();
        healthUIPanel.SetActive(true);
        
        Debug.Log("Health UI setup complete at top-right");
    }

    private void CreateHearts()
    {
        if (heartContainer == null || heartPrefab == null) return;

        // Clear existing hearts
        foreach (Transform child in heartContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // Create heart instances
        heartImages = new Image[maxHealth];
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartContainer);
            heartImages[i] = heart.GetComponent<Image>() ?? heart.GetComponentInChildren<Image>();
            
            if (heartImages[i] != null)
            {
                RectTransform rectTransform = heartImages[i].GetComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(40, 40);
            }
        }

        UpdateHealthDisplay();
        Debug.Log($"Created {maxHealth} heart icons");
    }

    private void SetupMinimapRender()
    {
        if (minimapPanel == null || minimapDisplay == null) return;

        // Position minimap at top-left
        RectTransform minimapRect = minimapPanel.GetComponent<RectTransform>();
        if (minimapRect != null)
        {
            minimapRect.anchorMin = new Vector2(0, 1); // Top-left anchor
            minimapRect.anchorMax = new Vector2(0, 1);
            minimapRect.anchoredPosition = minimapPosition;
            minimapRect.sizeDelta = new Vector2(minimapSize, minimapSize);
        }

        // Setup border
        if (showBorder)
        {
            Image borderImage = minimapPanel.GetComponent<Image>();
            if (borderImage == null)
                borderImage = minimapPanel.AddComponent<Image>();
            
            borderImage.color = borderColor;
        }

        // Create RenderTexture for camera rendering
        CreateRenderTexture();

        // Setup RawImage display
        if (minimapRenderTexture != null)
        {
            minimapDisplay.texture = minimapRenderTexture;
            
            RectTransform displayRect = minimapDisplay.GetComponent<RectTransform>();
            displayRect.anchorMin = Vector2.zero;
            displayRect.anchorMax = Vector2.one;
            displayRect.sizeDelta = showBorder ? 
                new Vector2(-borderThickness * 2, -borderThickness * 2) : Vector2.zero;
            displayRect.anchoredPosition = Vector2.zero;
        }

        // Add circular mask if needed
        if (circularMask)
        {
            SetupCircularMask();
        }

        // Initially hide minimap
        minimapPanel.SetActive(false);
        
        Debug.Log("Minimap render setup complete");
    }

    private void CreateRenderTexture()
    {
        // Create high-quality render texture
        minimapRenderTexture = new RenderTexture(
            renderTextureWidth, 
            renderTextureHeight, 
            16, 
            RenderTextureFormat.ARGB32
        );
        
        minimapRenderTexture.name = "MinimapRenderTexture";
        minimapRenderTexture.antiAliasing = 4; // 4x MSAA for smooth edges
        minimapRenderTexture.filterMode = FilterMode.Bilinear;
        minimapRenderTexture.wrapMode = TextureWrapMode.Clamp;
        
        Debug.Log($"Created RenderTexture: {renderTextureWidth}x{renderTextureHeight}");
    }

    private void SetupCircularMask()
    {
        // Add mask component
        Mask mask = minimapPanel.GetComponent<Mask>();
        if (mask == null)
        {
            mask = minimapPanel.AddComponent<Mask>();
        }
        mask.showMaskGraphic = false;

        // Create circular mask image
        GameObject maskObj = new GameObject("CircularMask");
        maskObj.transform.SetParent(minimapPanel.transform);
        
        Image maskImage = maskObj.AddComponent<Image>();
        maskImage.sprite = CreateCircleSprite();
        maskImage.color = Color.white;

        RectTransform maskRect = maskObj.GetComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.sizeDelta = Vector2.zero;
        maskRect.anchoredPosition = Vector2.zero;
    }

    private void SetupMinimapCamera()
    {
        if (minimapCamera == null) return;

        // Configure camera for top-down rendering
        minimapCamera.orthographic = true;
        minimapCamera.orthographicSize = cameraOrthographicSize;
        minimapCamera.clearFlags = CameraClearFlags.SolidColor;
        minimapCamera.backgroundColor = new Color(0.1f, 0.1f, 0.2f, 1f); // Dark background
        minimapCamera.depth = -20; // Render before main camera
        
        // Assign our render texture
        minimapCamera.targetTexture = minimapRenderTexture;
        
        // Position camera above player looking down
        if (playerTransform != null)
        {
            Vector3 cameraPos = new Vector3(
                playerTransform.position.x,
                playerTransform.position.y + cameraHeight,
                playerTransform.position.z
            );
            minimapCamera.transform.position = cameraPos;
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
        
        Debug.Log("Minimap camera configured for rendering");
    }

    private IEnumerator MonitorActionMapChanges()
    {
        string currentActionMap = "";
        
        while (true)
        {
            if (playerInput != null && playerInput.currentActionMap != null)
            {
                string newActionMap = playerInput.currentActionMap.name;
                
                if (newActionMap != currentActionMap)
                {
                    currentActionMap = newActionMap;
                    HandleActionMapChange(currentActionMap);
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void HandleActionMapChange(string actionMapName)
    {
        Debug.Log($"Action Map changed to: {actionMapName}");
        
        if (actionMapName == "Puzzle3")
        {
            ActivateMinimap();
        }
        else
        {
            DeactivateMinimap();
        }
    }

    private void ActivateMinimap()
    {
        if (minimapCamera != null && minimapPanel != null)
        {
            // Enable camera and UI
            minimapCamera.gameObject.SetActive(true);
            minimapPanel.SetActive(true);
            
            // Setup camera rendering
            SetupMinimapCamera();
            
            // Start updating
            isMinimapActive = true;
            
            // Fade in effect
            StartCoroutine(FadeInMinimap());
            
            Debug.Log("Minimap activated - Camera rendering started");
        }
    }

    private void DeactivateMinimap()
    {
        if (minimapCamera != null && minimapPanel != null)
        {
            minimapCamera.gameObject.SetActive(false);
            minimapPanel.SetActive(false);
            isMinimapActive = false;
            
            Debug.Log("Minimap deactivated");
        }
    }

    private void Update()
    {
        if (isMinimapActive && minimapCamera != null && playerTransform != null)
        {
            UpdateCameraPosition();
        }
    }

    private void UpdateCameraPosition()
    {
        Vector3 targetPosition = new Vector3(
            playerTransform.position.x,
            playerTransform.position.y + cameraHeight,
            playerTransform.position.z
        );

        if (smoothFollow && followSpeed > 0)
        {
            minimapCamera.transform.position = Vector3.Lerp(
                minimapCamera.transform.position,
                targetPosition,
                followSpeed * Time.deltaTime
            );
        }
        else
        {
            minimapCamera.transform.position = targetPosition;
        }

        // Keep camera looking straight down
        minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    // Health System
    private void UpdateHealthDisplay()
    {
        if (heartImages == null) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                bool isAlive = i < currentHealth;
                heartImages[i].color = isAlive ? 
                    new Color(1f, 0f, 0f, 1f) : // Red
                    new Color(1f, 0f, 0f, 0f);  // Transparent

                if (!isAlive && i == currentHealth)
                {
                    StartCoroutine(HeartDisappearAnimation(heartImages[i]));
                }
            }
        }
    }

    private IEnumerator HeartDisappearAnimation(Image heartImage)
    {
        Vector3 originalScale = heartImage.transform.localScale;
        
        // Pulse effect
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            float scale = Mathf.Lerp(1f, 1.4f, elapsed / 0.3f);
            heartImage.transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        Color startColor = new Color(1f, 0f, 0f, 1f);
        
        while (elapsed < 0.4f)
        {
            float progress = elapsed / 0.4f;
            float scale = Mathf.Lerp(1.4f, 0f, progress);
            float alpha = Mathf.Lerp(1f, 0f, progress);
            
            heartImage.transform.localScale = originalScale * scale;
            heartImage.color = new Color(1f, 0f, 0f, alpha);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Final state
        heartImage.transform.localScale = originalScale;
        heartImage.color = new Color(1f, 0f, 0f, 0f);
    }

    private IEnumerator FadeInMinimap()
    {
        CanvasGroup canvasGroup = minimapPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = minimapPanel.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        
        float elapsed = 0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        canvasGroup.alpha = 1f;
    }

    // Utility Methods
    private Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = (size / 2f) - 2f;

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // Event Handlers
    private void OnHealthChanged(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        UpdateHealthDisplay();
        Debug.Log($"Health changed to: {currentHealth}/{maxHealth}");
    }

    private void OnGameRestart()
    {
        currentHealth = maxHealth;
        UpdateHealthDisplay();
        Debug.Log("Game restarted - Health reset");
    }

    // Public Configuration Methods
    public void SetCameraHeight(float height)
    {
        cameraHeight = height;
        if (isMinimapActive)
        {
            UpdateCameraPosition();
        }
    }

    public void SetCameraSize(float size)
    {
        cameraOrthographicSize = size;
        if (minimapCamera != null)
        {
            minimapCamera.orthographicSize = size;
        }
    }

    public void SetRenderQuality(int width, int height)
    {
        renderTextureWidth = width;
        renderTextureHeight = height;
        
        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
            Destroy(minimapRenderTexture);
            CreateRenderTexture();
            
            if (minimapCamera != null)
            {
                minimapCamera.targetTexture = minimapRenderTexture;
            }
            
            if (minimapDisplay != null)
            {
                minimapDisplay.texture = minimapRenderTexture;
            }
        }
    }

    // Debug Methods
    [ContextMenu("Test Health Decrease")]
    private void TestHealthDecrease()
    {
        if (currentHealth > 0)
        {
            OnHealthChanged(currentHealth - 1);
        }
    }

    [ContextMenu("Force Activate Minimap")]
    private void ForceActivateMinimap()
    {
        ActivateMinimap();
    }

    [ContextMenu("Force Deactivate Minimap")]
    private void ForceDeactivateMinimap()
    {
        DeactivateMinimap();
    }

    private void OnDestroy()
    {
        // Cleanup
        IceBlock.OnHPChanged -= OnHealthChanged;
        IceBlock.OnGameRestart -= OnGameRestart;
        
        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
            Destroy(minimapRenderTexture);
        }
    }

    private void OnApplicationQuit()
    {
        // Clean up render texture on quit
        if (minimapRenderTexture != null)
        {
            minimapRenderTexture.Release();
        }
    }
}