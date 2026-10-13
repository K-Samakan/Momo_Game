using UnityEngine;
using Momo.UITutorial.Popups;
using Momo.UITutorial.Data;


namespace Momo.UITutorial.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("Popup References (ลาก Prefab ที่วางใน Canvas มาใส่)")]
        [SerializeField] private TutorialPopupUI tutorialPopup;
        [SerializeField] private ItemDetailPopupUI itemDetailPopup;

        [Header("Tutorial Data")]
        [SerializeField] private TutorialPageData vseTutorialData;
        [SerializeField] private TutorialPageData puzzleTutorialData;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private const string PREF_PREFIX = "TutorialSeen_";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ---------- A) Visual Sound Effect Tutorial ----------
        public void TryShowVSETutorial()
        {
            if (vseTutorialData == null)
            {
                Debug.LogError("[Tutorial] ❌ Vse Tutorial Data ว่างใน Inspector");
                return;
            }
            if (tutorialPopup == null)
            {
                Debug.LogError("[Tutorial] ❌ Tutorial Popup ว่างใน Inspector");
                return;
            }

            string key = PREF_PREFIX + vseTutorialData.tutorialId;
            bool seen = HasSeenTutorial(vseTutorialData.tutorialId);

            if (showDebugLogs)
                Debug.Log($"[Tutorial] TryShowVSE called | key={key} | seen={seen}");

            if (seen) return;

            if (showDebugLogs) Debug.Log("[Tutorial] ▶ เปิด VSE Tutorial");
            tutorialPopup.ShowTutorial(vseTutorialData);
        }

        // ---------- C) Puzzle Tutorial ----------
        public void TryShowPuzzleTutorial()
        {
            if (HasSeenTutorial(puzzleTutorialData.tutorialId)) return;
            tutorialPopup.ShowTutorial(puzzleTutorialData);
        }

        // ---------- B) Item Detail ----------
        public void ShowItemDetail(ItemData item)
        {
            itemDetailPopup.ShowItem(item);
        }

        // ---------- Utility ----------
        public bool HasSeenTutorial(string id)
        {
            return PlayerPrefs.GetInt(PREF_PREFIX + id, 0) == 1;
        }

        public void MarkTutorialAsSeen(string id)
        {
            PlayerPrefs.SetInt(PREF_PREFIX + id, 1);
            PlayerPrefs.Save();
            if (showDebugLogs) Debug.Log($"[Tutorial] ✔ บันทึกว่าดูแล้ว: {PREF_PREFIX + id}");
        }

        // ---------- Debug Menu (คลิกขวาที่ component) ----------
        [ContextMenu("Reset VSE Tutorial Flag")]
        public void ResetVSEFlag()
        {
            if (vseTutorialData == null)
            {
                Debug.LogError("[Tutorial] ❌ Vse Tutorial Data ว่าง รีเซ็ตไม่ได้");
                return;
            }
            string key = PREF_PREFIX + vseTutorialData.tutorialId;
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
            Debug.Log($"[Tutorial] 🔄 รีเซ็ตแล้ว: {key}");
        }

        [ContextMenu("Show VSE Tutorial Now (ข้ามการเช็ค)")]
        public void ForceShowVSETutorial()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[Tutorial] ต้องกดตอนอยู่ใน Play Mode");
                return;
            }
            tutorialPopup.ShowTutorial(vseTutorialData);
        }
    }
}