using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MirrorUIManager : MonoBehaviour
{
    public static MirrorUIManager Instance { get; private set; }

    [Header("Health UI")]
    public GameObject healthUIPanel;
    public Transform heartContainer;
    public GameObject heartPrefab;

    [Header("Mirror Interaction UI")]
    public GameObject mirrorInteractionPanel;
    public GameObject leftInteractionUI;
    public GameObject rightInteractionUI;

    [Header("Button Icons and Progress")]
    public Image leftButtonIcon;
    public Image rightButtonIcon;
    public Image leftProgressFill;
    public Image rightProgressFill;
    public TextMeshProUGUI leftInstructionText;
    public TextMeshProUGUI rightInstructionText;

    [Header("UI Animation Settings")]
    public float fadeInDuration = 0.3f;
    public float fadeOutDuration = 0.2f;
    public AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Hold Progress Settings")]
    public Color normalProgressColor = Color.white;
    public Color warningProgressColor = Color.yellow;
    public Color dangerProgressColor = Color.red;

    private Camera playerCamera;
    private InteractionMirror currentActiveMirror;
    private Coroutine leftHoldCoroutine;
    private Coroutine rightHoldCoroutine;

    // Health tracking
    private int currentHealth;
    private int maxHealth;
    private Image[] heartImages;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerCamera = Camera.main ?? FindObjectOfType<Camera>();
            InitializeHealthUI();
            HideMirrorInteractionUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeHealthUI()
    {
        if (healthUIPanel != null)
            healthUIPanel.SetActive(true);

        // ตั้งค่า default
        maxHealth = 5;
        currentHealth = 5;
        CreateHeartUI();

        // อัพเดทจาก Manager หากมี
        StartCoroutine(WaitForManagerInitialization());
    }

    private IEnumerator WaitForManagerInitialization()
    {
        // รอให้ MirrorInteractionManager initialize ก่อน
        while (MirrorInteractionManager.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        maxHealth = MirrorInteractionManager.Instance.playerMaxHealth;
        currentHealth = maxHealth;
        CreateHeartUI();
    }

    private void CreateHeartUI()
    {
        if (heartContainer == null || heartPrefab == null)
        {
            Debug.LogWarning("Heart Container หรือ Heart Prefab ไม่ได้กำหนด");
            return;
        }

        // ลบหัวใจเก่าออก
        foreach (Transform child in heartContainer)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // ตั้งค่า Horizontal Layout Group ให้ไม่ปรับขนาด
        HorizontalLayoutGroup layoutGroup = heartContainer.GetComponent<HorizontalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.childScaleWidth = false;
            layoutGroup.childScaleHeight = false;
        }

        // สร้างหัวใจใหม่
        heartImages = new Image[maxHealth];
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heart = Instantiate(heartPrefab, heartContainer);
            heartImages[i] = heart.GetComponent<Image>() ?? heart.GetComponentInChildren<Image>();

            if (heartImages[i] == null)
            {
                Debug.LogError($"Heart Prefab ต้องมี Image Component: {heart.name}");
            }
            else
            {
                // กำหนดขนาดคงที่สำหรับหัวใจ
                RectTransform rectTransform = heartImages[i].GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.sizeDelta = new Vector2(50, 50); // ขนาดคงที่
                }
            }
        }

        UpdateHealthDisplay();
        Debug.Log($"สร้าง Health UI: {maxHealth} หัวใจ");
    }

    public void UpdateHealthDisplay()
    {
        if (heartImages == null || heartImages.Length == 0) return;

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                // ใช้ความโปร่งใสแทน SetActive เพื่อรักษาขนาด layout
                if (i < currentHealth)
                {
                    // หัวใจที่ยังมี HP - แสดงปกติ
                    heartImages[i].color = new Color(1f, 0f, 0f, 1f); // สีแดงทึบ
                }
                else
                {
                    // หัวใจที่หมด - โปร่งใสสมบูรณ์แต่ยัง occupy space
                    heartImages[i].color = new Color(1f, 0f, 0f, 0f); // โปร่งใสแต่ยัง occupy layout
                }

                // Animation เมื่อเสียหัวใจ - หายไปจากซ้ายไปขวา
                if (i == currentHealth && currentHealth >= 0 && currentHealth < maxHealth)
                {
                    StartCoroutine(HeartDisappearAnimation(heartImages[i]));
                }
            }
        }

        Debug.Log($"อัพเดท Health UI: {currentHealth}/{maxHealth}");
    }

    private IEnumerator HeartDisappearAnimation(Image heartImage)
    {
        Vector3 originalScale = heartImage.transform.localScale;
        Color originalColor = new Color(1f, 0f, 0f, 1f); // สีแดงทึบ

        // Pulse animation ก่อนหายไป
        float pulseElapsed = 0f;
        while (pulseElapsed < 0.3f)
        {
            float scale = Mathf.Lerp(1f, 1.4f, pulseElapsed / 0.3f);
            heartImage.transform.localScale = originalScale * scale;
            pulseElapsed += Time.deltaTime;
            yield return null;
        }

        // Scale down และ fade out พร้อมกัน
        float disappearElapsed = 0f;

        while (disappearElapsed < 0.4f)
        {
            float progress = disappearElapsed / 0.4f;

            // Scale down
            float scale = Mathf.Lerp(1.4f, 0f, progress);
            heartImage.transform.localScale = originalScale * scale;

            // Fade out
            Color newColor = originalColor;
            newColor.a = Mathf.Lerp(1f, 0f, progress);
            heartImage.color = newColor;

            disappearElapsed += Time.deltaTime;
            yield return null;
        }

        // ตั้งค่าสุดท้าย - โปร่งใสสมบูรณ์แต่ยังคงขนาดเดิม
        heartImage.transform.localScale = originalScale; // คืนขนาดเดิมเพื่อรักษา layout
        heartImage.color = new Color(1f, 0f, 0f, 0f); // โปร่งใสแต่ยัง occupy space
    }

    public void OnHealthChanged(int newHealth)
    {
        int previousHealth = currentHealth;
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        if (currentHealth < previousHealth)
        {
            Debug.Log($"ผู้เล่นเสียหัวใจ: {previousHealth} -> {currentHealth}");
            // หัวใจจะหายไปจากซ้ายไปขวา (index สูงไปต่ำ)
        }
        else if (currentHealth > previousHealth)
        {
            Debug.Log($"ผู้เล่นฟื้น HP: {previousHealth} -> {currentHealth}");
            // แสดงหัวใจที่ฟื้นคืนมา (จากซ้ายไปขวา)
            for (int i = previousHealth; i < currentHealth; i++)
            {
                if (heartImages != null && i < heartImages.Length && heartImages[i] != null)
                {
                    heartImages[i].gameObject.SetActive(true);
                    StartCoroutine(HeartRecoverAnimation(heartImages[i]));
                }
            }
        }

        UpdateHealthDisplay();
    }

    private IEnumerator HeartRecoverAnimation(Image heartImage)
    {
        Vector3 originalScale = heartImage.transform.localScale;
        Color finalColor = new Color(1f, 0f, 0f, 1f); // สีแดงทึบ

        // เริ่มต้นจากขนาดเล็กและโปร่งใส
        heartImage.transform.localScale = Vector3.zero;
        heartImage.color = new Color(1f, 0f, 0f, 0f);

        // Scale up และ fade in พร้อมกัน
        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            float progress = elapsed / 0.5f;

            // Scale up with bounce effect
            float scale = Mathf.Lerp(0f, 1.2f, progress);
            heartImage.transform.localScale = originalScale * scale;

            // Fade in
            Color newColor = finalColor;
            newColor.a = Mathf.Lerp(0f, 1f, progress);
            heartImage.color = newColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Scale back to normal
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            float progress = elapsed / 0.2f;
            float scale = Mathf.Lerp(1.2f, 1f, progress);
            heartImage.transform.localScale = originalScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // รีเซ็ตค่าเป็นปกติ
        heartImage.transform.localScale = originalScale;
        heartImage.color = finalColor;
    }

    public void ShowMirrorInteractionUI(InteractionMirror mirror)
    {
        if (mirror == null)
        {
            Debug.LogWarning("ไม่สามารถแสดง UI สำหรับ mirror ที่เป็น null");
            return;
        }

        currentActiveMirror = mirror;

        if (mirrorInteractionPanel != null)
        {
            mirrorInteractionPanel.SetActive(true);
            StartCoroutine(FadeInMirrorUI());
        }

        UpdateInteractionUI();
        Debug.Log($"แสดง Mirror UI สำหรับ: {mirror.gameObject.name}");
    }

    public void HideMirrorInteractionUI()
    {
        if (mirrorInteractionPanel != null)
        {
            mirrorInteractionPanel.SetActive(false);
        }

        currentActiveMirror = null;
        StopAllHoldProgress();
        Debug.Log("ซ่อน Mirror UI");
    }

    private void UpdateInteractionUI()
    {
        if (currentActiveMirror == null) return;

        bool canRotateLeft = currentActiveMirror.CanRotateInDirection(false);
        bool canRotateRight = currentActiveMirror.CanRotateInDirection(true);

        // อัพเดทข้อความและสี
        if (leftInstructionText != null)
        {
            leftInstructionText.text = canRotateLeft ? "Hold             to Rotate Left." : "Unable to rotate left. ";
            leftInstructionText.color = canRotateLeft ? Color.white : Color.gray;
        }
        if (rightInstructionText != null)
        {
            rightInstructionText.text = canRotateRight ? "Hold             to Rotate Right." : "Unable to rotate right.";
            rightInstructionText.color = canRotateRight ? Color.white : Color.gray;
        }


        // แสดง/ซ่อน UI elements
        if (leftInteractionUI != null)
            leftInteractionUI.SetActive(canRotateLeft);
        if (rightInteractionUI != null)
            rightInteractionUI.SetActive(canRotateRight);

        // รีเซ็ต progress
        ResetProgressFill(leftProgressFill);
        ResetProgressFill(rightProgressFill);
    }

    private void PositionInteractionUI()
    {
        if (currentActiveMirror == null || playerCamera == null) return;

        // ใช้ตำแหน่งที่สูงกว่า center ของ mirror
        Vector3 mirrorPosition = currentActiveMirror.transform.position;

        // ใช้ความสูงเดียวกันแต่เพิ่มระยะห่างด้านข้าง
        Vector3 adjustedPosition = mirrorPosition + Vector3.up * 1f;
        Vector3 screenPosition = playerCamera.WorldToScreenPoint(adjustedPosition);

        // ตรวจสอบว่า mirror อยู่ในหน้าจอ
        if (screenPosition.z < 0) return;

        float offsetDistance = 500f; // เพิ่มระยะห่างให้มากพอที่จะไม่ทับกัน

        if (leftInteractionUI != null)
        {
            Vector3 leftPos = new Vector3(screenPosition.x - offsetDistance, screenPosition.y, screenPosition.z);
            leftInteractionUI.transform.position = leftPos;
        }

        if (rightInteractionUI != null)
        {
            Vector3 rightPos = new Vector3(screenPosition.x + offsetDistance, screenPosition.y, screenPosition.z);
            rightInteractionUI.transform.position = rightPos;
        }
    }

    public void StartHoldProgress(bool isRightSide, float holdDuration)
    {
        if (isRightSide)
        {
            if (rightHoldCoroutine != null)
                StopCoroutine(rightHoldCoroutine);
            rightHoldCoroutine = StartCoroutine(UpdateHoldProgress(rightProgressFill, holdDuration));
        }
        else
        {
            if (leftHoldCoroutine != null)
                StopCoroutine(leftHoldCoroutine);
            leftHoldCoroutine = StartCoroutine(UpdateHoldProgress(leftProgressFill, holdDuration));
        }
    }

    public void StopHoldProgress(bool isRightSide)
    {
        if (isRightSide)
        {
            if (rightHoldCoroutine != null)
            {
                StopCoroutine(rightHoldCoroutine);
                rightHoldCoroutine = null;
            }
            ResetProgressFill(rightProgressFill);
        }
        else
        {
            if (leftHoldCoroutine != null)
            {
                StopCoroutine(leftHoldCoroutine);
                leftHoldCoroutine = null;
            }
            ResetProgressFill(leftProgressFill);
        }
    }

    private void StopAllHoldProgress()
    {
        StopHoldProgress(false);
        StopHoldProgress(true);
    }

    private IEnumerator UpdateHoldProgress(Image progressFill, float duration)
    {
        if (progressFill == null) yield break;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            progressFill.fillAmount = progress;

            // เปลี่ยนสีตาม progress และประเภท mirror
            Color progressColor = GetProgressColor(progress);
            progressFill.color = progressColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // เสร็จแล้ว
        progressFill.fillAmount = 1f;
        progressFill.color = Color.green;

        // รอสักครู่แล้วรีเซ็ต
        yield return new WaitForSeconds(0.2f);
        ResetProgressFill(progressFill);
    }

    private Color GetProgressColor(float progress)
    {
        if (currentActiveMirror == null) return normalProgressColor;

        if (currentActiveMirror.IsFakeMirror())
        {
            int rotationCount = currentActiveMirror.GetFakeRotationCount();
            // เปลี่ยนสีตามความอันตราย
            if (rotationCount >= 3) return dangerProgressColor;
            if (rotationCount >= 1) return warningProgressColor;
        }

        return normalProgressColor;
    }

    private void ResetProgressFill(Image progressFill)
    {
        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
            progressFill.color = normalProgressColor;
        }
    }

    private IEnumerator FadeInMirrorUI()
    {
        CanvasGroup canvasGroup = mirrorInteractionPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = mirrorInteractionPanel.AddComponent<CanvasGroup>();

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            float alpha = fadeInCurve.Evaluate(elapsed / fadeInDuration);
            canvasGroup.alpha = alpha;
            elapsed += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private void Update()
    {
        // อัพเดทตำแหน่ง UI ตามกระจกทุกเฟรม
        if (currentActiveMirror != null && mirrorInteractionPanel != null && mirrorInteractionPanel.activeInHierarchy)
        {
            PositionInteractionUI();
        }
    }

    // เรียกจาก MirrorInteractionManager เมื่อมีการเปลี่ยนแปลง HP
    public void OnPlayerHealthChanged(int newHealth, int maxHealth)
    {
        this.maxHealth = maxHealth;

        // สร้าง UI ใหม่หากจำนวนหัวใจเปลี่ยน
        if (heartImages == null || heartImages.Length != maxHealth)
        {
            CreateHeartUI();
        }

        OnHealthChanged(newHealth);
    }

    // Method สำหรับ Debug
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void TestHealthSystem()
    {
        Debug.Log("ทดสอบระบบ Health UI");
        OnHealthChanged(currentHealth - 1);
    }
}