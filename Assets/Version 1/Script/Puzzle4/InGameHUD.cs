using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InGameHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private TextMeshProUGUI heartCountText;
    [SerializeField] private Image[] heartImages;
    [SerializeField] private TextMeshProUGUI objectiveText;

    [Header("Settings")]
    [SerializeField] private Color heartActiveColor = Color.red;
    [SerializeField] private Color heartInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    private int currentHearts;
    private int maxHearts;

    void Awake()
    {
        // ซ่อน HUD ตอนเริ่ม
        HideImmediate();
    }

    #region Show/Hide

    /// <summary>
    /// แสดง HUD
    /// </summary>
    public void Show(int hearts, int maxHearts)
    {
        this.currentHearts = hearts;
        this.maxHearts = maxHearts;

        if (hudPanel != null)
        {
            hudPanel.SetActive(true);
        }

        UpdateHearts(hearts);

        Debug.Log("[InGameHUD] ✅ HUD displayed");
    }

    /// <summary>
    /// ซ่อน HUD ทันที
    /// </summary>
    public void HideImmediate()
    {
        if (hudPanel != null)
        {
            hudPanel.SetActive(false);
        }
    }

    #endregion

    #region Update Hearts

    /// <summary>
    /// อัปเดตจำนวนหัวใจ
    /// </summary>
    public void UpdateHearts(int hearts)
    {
        currentHearts = hearts;

        // อัปเดต Text
        if (heartCountText != null)
        {
            heartCountText.text = $"{hearts}/{maxHearts}";
        }

        // อัปเดตรูปหัวใจ
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
            {
                if (i < hearts)
                {
                    // แสดงหัวใจ (มีชีวิต)
                    heartImages[i].color = heartActiveColor;
                    heartImages[i].enabled = true;
                }
                else
                {
                    // ซ่อนหัวใจ (หมดชีวิต)
                    heartImages[i].color = heartInactiveColor;
                    heartImages[i].enabled = true; // แสดงแต่สีเทา
                }
            }
        }

        Debug.Log($"[InGameHUD] 💗 Hearts updated: {hearts}/{maxHearts}");
    }

    #endregion

    #region Update Objective

    /// <summary>
    /// อัปเดตข้อความเป้าหมาย
    /// </summary>
    public void UpdateObjective(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = text;
        }
    }

    #endregion
}