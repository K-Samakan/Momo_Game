// ============================================================
// FILE: JetpackUIManager.cs
// PATH: Assets/Version 1/Script/Map1/UI/JetpackUIManager.cs
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JetpackUIManager : MonoBehaviour
{
    public static JetpackUIManager Instance { get; private set; }

    [Header("Energy Bar")]
    public GameObject energyPanel; // Panel ที่มุมขวาล่าง
    public Image energyFillImage;
    public TextMeshProUGUI energyText;

    [Header("Colors")]
    public Color fullColor = Color.cyan;
    public Color mediumColor = Color.yellow;
    public Color lowColor = Color.red;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if (energyPanel != null)
            energyPanel.SetActive(false);
    }

    public void ShowEnergyBar()
    {
        if (energyPanel != null)
            energyPanel.SetActive(true);
    }

    public void HideEnergyBar()
    {
        if (energyPanel != null)
            energyPanel.SetActive(false);
    }

    public void UpdateEnergy(float current, float max)
    {
        if (energyFillImage == null) return;

        float ratio = current / max;
        energyFillImage.fillAmount = ratio;

        // เปลี่ยนสีตามพลังงาน
        if (ratio > 0.5f)
            energyFillImage.color = fullColor;
        else if (ratio > 0.2f)
            energyFillImage.color = mediumColor;
        else
            energyFillImage.color = lowColor;

        // แสดงเปอร์เซ็นต์
        if (energyText != null)
            energyText.text = $"{Mathf.RoundToInt(ratio * 100)}%";
    }
}