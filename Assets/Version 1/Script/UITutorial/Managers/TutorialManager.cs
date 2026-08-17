using UnityEngine;
using Momo.UITutorial.Popups;
using Momo.UITutorial.Data;

namespace Momo.UITutorial.Managers
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("Popup References (ลาก Prefab ที่วางใน Canvas มาใส่)")]
        [SerializeField] private TutorialPopupUI tutorialPopup;      // ใช้ร่วมกันทั้ง VSE และ Puzzle
        [SerializeField] private ItemDetailPopupUI itemDetailPopup;

        [Header("Tutorial Data")]
        [SerializeField] private TutorialPageData vseTutorialData;
        [SerializeField] private TutorialPageData puzzleTutorialData;

        private const string PREF_PREFIX = "TutorialSeen_";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ---------- A) Visual Sound Effect Tutorial ----------
        public void TryShowVSETutorial()
        {
            if (HasSeenTutorial(vseTutorialData.tutorialId)) return;
            tutorialPopup.ShowTutorial(vseTutorialData);
        }

        // ---------- C) Puzzle Tutorial ----------
        public void TryShowPuzzleTutorial()
        {
            if (HasSeenTutorial(puzzleTutorialData.tutorialId)) return;
            tutorialPopup.ShowTutorial(puzzleTutorialData);
        }

        // ---------- B) Item Detail (แสดงทุกครั้ง ไม่เช็คสถานะ) ----------
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
        }

        // เผื่อ Debug/QA อยากรีเซ็ต tutorial เพื่อทดสอบซ้ำ
        [ContextMenu("Reset All Tutorial Flags")]
        public void ResetAllFlags()
        {
            PlayerPrefs.DeleteKey(PREF_PREFIX + vseTutorialData.tutorialId);
            PlayerPrefs.DeleteKey(PREF_PREFIX + puzzleTutorialData.tutorialId);
        }
    }
}