using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

namespace MOMO.Cutscene
{
    /// <summary>
    /// หน้า End Credits นิ่งหน้าเดียว
    ///  - Logo เกมอยู่กลางจอ (เป็น Image ในซีน ไม่ต้องอ้างใน sคริปต์)
    ///  - ชื่อผู้จัดทำพิมพ์ทีละตัวแบบ subtitle
    ///  - กด ⭘ (buttonEast) เพื่อกลับ Main
    /// </summary>
    public class EndCreditsScreen : MonoBehaviour
    {
        [Header("=== Credit Text (พิมพ์ทีละตัว) ===")]
        [Tooltip("TMP สำหรับชื่อผู้จัดทำ")]
        [SerializeField] private TextMeshProUGUI creditText;

        [TextArea(2, 6)]
        [Tooltip("ข้อความ credit เช่น ชื่อผู้จัดทำ")]
        [SerializeField] private string creditMessage = "Created by Your Name";

        [Tooltip("ความเร็วพิมพ์ (วินาทีต่อ 1 ตัวอักษร)")]
        [SerializeField] private float typeSpeed = 0.06f;

        [Tooltip("หน่วงก่อนเริ่มพิมพ์ (วินาที) เผื่อให้ logo ขึ้นก่อน")]
        [SerializeField] private float startDelay = 0.5f;

        [Header("=== Prompt (กด ⭘ เพื่อกลับ) ===")]
        [Tooltip("GameObject ปุ่ม prompt — จะโชว์หลังพิมพ์ชื่อครบ")]
        [SerializeField] private GameObject continuePrompt;

        [Header("=== Scene ===")]
        [Tooltip("ชื่อ Scene ที่จะกลับไป")]
        [SerializeField] private string nextSceneName = "Main";

        private bool canContinue = false;
        private bool isLeaving = false;

        private Gamepad Pad => Gamepad.current;

        void Start()
        {
            if (creditText != null) creditText.text = "";
            if (continuePrompt != null) continuePrompt.SetActive(false);
            StartCoroutine(PlayCredits());
        }

        void Update()
        {
            if (!canContinue || isLeaving || Pad == null) return;

            // กด ⭘ เพื่อกลับ Main
            if (Pad.buttonEast.wasPressedThisFrame)
            {
                isLeaving = true;
                Debug.Log("⭘ → กลับ Main");
                SceneManager.LoadScene(nextSceneName);
            }
        }

        IEnumerator PlayCredits()
        {
            yield return new WaitForSecondsRealtime(startDelay);

            // typewriter ด้วย maxVisibleCharacters
            creditText.text = creditMessage;
            creditText.maxVisibleCharacters = 0;
            creditText.ForceMeshUpdate();
            int total = creditText.textInfo.characterCount;

            for (int c = 0; c <= total; c++)
            {
                creditText.maxVisibleCharacters = c;
                yield return new WaitForSecondsRealtime(typeSpeed);
            }

            // พิมพ์ครบ → โชว์ prompt และเปิดให้กด ⭘ ได้
            if (continuePrompt != null) continuePrompt.SetActive(true);
            canContinue = true;
        }
    }
}