using UnityEngine;

namespace Momo.UITutorial.Data
{
    [CreateAssetMenu(fileName = "NewTutorialSet", menuName = "Momo/Tutorial/Tutorial Set")]
    public class TutorialPageData : ScriptableObject
    {
        [System.Serializable]
        public class Page
        {
            public Sprite image;
            [TextArea(2, 4)] public string description;
        }

        [Tooltip("ID ไว้เช็คว่าเคยแสดง Tutorial ชุดนี้ไปแล้วหรือยัง เช่น 'vse_tutorial_stage1'")]
        public string tutorialId;

        public Page[] pages;
    }
}