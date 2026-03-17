using UnityEngine;
using UnityEngine.UI;

public class GamePuzzleManager : MonoBehaviour
{
    [Header("Game Settings")]
    public int maxHearts = 3;
    public Transform[] iceSymbols; // ตำแหน่งสัญลักษณ์เกล็ดน้ำแข็ง
    public GameObject doorToOpen;
    
    [Header("UI")]
    public Image[] heartUI;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public Button restartButton;
    
    private int currentHearts;
    private int blocksOnSymbols = 0;
    
    void Start()
    {
        currentHearts = maxHearts;
        UpdateHeartsUI();
        restartButton.onClick.AddListener(RestartPuzzle);
    }
    
    public void OnBlockBroken()
    {
        currentHearts--;
        UpdateHeartsUI();
        
        if (currentHearts <= 0)
        {
            GameOver();
        }
    }
    
    void UpdateHeartsUI()
    {
        for (int i = 0; i < heartUI.Length; i++)
        {
            heartUI[i].gameObject.SetActive(i < currentHearts);
        }
    }
    
    public void CheckVictoryCondition()
    {
        // ตรวจสอบว่ามีบล็อกครบทุกสัญลักษณ์หรือไม่
        int blocksOnSymbols = 0;
        foreach (Transform symbol in iceSymbols)
        {
            Collider[] blocks = Physics.OverlapSphere(symbol.position, 0.4f);
            foreach (Collider block in blocks)
            {
                if (block.GetComponent<IceBlock>() != null)
                {
                    blocksOnSymbols++;
                    break;
                }
            }
        }
        
        if (blocksOnSymbols >= iceSymbols.Length)
        {
            Victory();
        }
    }
    
    void GameOver()
    {
        // Haptic สำหรับ Game Over
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPattern(HapticPatternType.Fail);
        }
        
        gameOverPanel.SetActive(true);
        Time.timeScale = 0;
    }
    
    void Victory()
    {
        // Haptic สำหรับชนะ
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.PlayPattern(HapticPatternType.Success);
        }
        
        if (doorToOpen != null)
            doorToOpen.SetActive(false); // เปิดประตู
        
        victoryPanel.SetActive(true);
    }
    
    public void RestartPuzzle()
    {
        // รีเซ็ต Adaptive Triggers
        if (HapticManager.Instance != null)
        {
            HapticManager.Instance.ResetAdaptiveTriggers();
        }
        
        Time.timeScale = 1;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}