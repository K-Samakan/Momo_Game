using UnityEngine;
using TMPro;

public class CageQuestManager : MonoBehaviour
{
    public static CageQuestManager Instance;

    [Header("Quest Settings")]
    public int totalCages = 5;
    private int freedCages = 0;

    [Header("UI")]
    public TextMeshProUGUI questCounterText;
    public GameObject questCompleteUI;

    [Header("Boss Buff Reference")]
    public TigerAllyEffect tigerAllyEffect;

    public bool IsQuestComplete { get; private set; } = false;

    void Awake()
    {
        Instance = this;
        UpdateUI();
    }

    public void OnCageFreed()
    {
        freedCages++;
        freedCages = Mathf.Clamp(freedCages, 0, totalCages);

        Debug.Log($"[Quest] ช่วยเสือแล้ว {freedCages}/{totalCages}");

        UpdateUI();

        if (freedCages >= totalCages)
            CompleteQuest();
    }

    void CompleteQuest()
    {
        IsQuestComplete = true;

        if (questCompleteUI) questCompleteUI.SetActive(true);

        if (tigerAllyEffect) tigerAllyEffect.EnableTigerAlly();

        // ✅ แก้: ใช้ PlayPattern(Success) แทน PlayQuestComplete()
        // สั่นเฉลิมฉลอง = ภารกิจรองสำเร็จ
        HapticManager.Instance?.PlayPattern(HapticPatternType.Success);

        Debug.Log("[Quest] ภารกิจรองสำเร็จ! เสือพร้อมช่วยสู้กับบอส");
    }

    void UpdateUI()
    {
        if (questCounterText)
            questCounterText.text = $"🐅 เสือที่ช่วย: {freedCages}/{totalCages}";
    }
}