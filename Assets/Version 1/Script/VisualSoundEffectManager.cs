using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VisualSoundEffectManager : MonoBehaviour
{
    public static VisualSoundEffectManager Instance;

    public Transform player;
    public Transform cameraTransform;
    //public Image iconPrefab;
    public GameObject visualPrefab;  // Prefab ที่มี icon+line+effect เป็นลูก

    public float displayTime = 1f;
    public float updateSpeed = 5f;
    public RadialLayoutManual radialLayout;


    /*
    private List<Image> iconPool = new();
    private Dictionary<Image, Vector3> iconSources = new();
    private Dictionary<Image, float> iconTimers = new();
    */
    private List<GameObject> visualPool = new();
    private Dictionary<GameObject, Vector3> visualSources = new();
    private Dictionary<GameObject, float> visualTimers = new();


    private void Awake()
    {
        Instance = this;
        //CreateIconPool();
        CreateVisualPool();
    }

    void CreateVisualPool()
    {
        for (int i = 0; i < 10; i++)
        {
            //Image icon = Instantiate(iconPrefab, transform);
            GameObject visual = Instantiate(visualPrefab, transform);
            //icon.enabled = false;
            visual.SetActive(false);
            //iconPool.Add(icon);
            visualPool.Add(visual);
        }
    }

    [System.Serializable]
    public class VisualType
    {
        public string tagName;
        public Sprite iconSprite;   // ใส่ Sprite หรือ GameObject แทนก็ได้
    }
    public List<VisualType> visualTypes;  // ใส่ใน Inspector ว่าจะให้ Tag ไหนใช้ Icon อะไร

    public void ShowIcon(Vector3 sourceWorldPos, float intensity, string tagName = "")
    {
        if (visualPrefab == null || player == null || /*cameraTransform == null ||*/ radialLayout == null) return;

        GameObject visual = GetAvailableVisual();
        if (visual == null) return;

        visual.SetActive(true);
        visualSources[visual] = sourceWorldPos;
        visualTimers[visual] = displayTime;

        //var images = visual.GetComponentsInChildren<Image>();
        //foreach (var img in images)
        //{
        //    img.color = new Color(1, 1, 1, Mathf.Clamp01(intensity));
        //}
        Image iconImage = visual.GetComponentInChildren<Image>();  // สมมติว่ามี Image ตัวเดียว

        // หาสัญลักษณ์ที่ตรงกับ tagName
        Sprite iconSprite = null;
        foreach (var vt in visualTypes)
        {
            if (vt.tagName == tagName)
            {
                iconSprite = vt.iconSprite;
                break;
            }
        }

        if (iconSprite != null && iconImage != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.color = new Color(1, 1, 1, Mathf.Clamp01(intensity));
        }

        // ปรับขนาด prefab ตาม intensity
        visual.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.5f, intensity);
    }

    GameObject GetAvailableVisual()
    {
        foreach (var visual in visualPool)
        {
            if (!visual.activeSelf) return visual;
        }
        return null;
    }

    void Update()
    {
        foreach (var visual in visualPool)
        {
            if (!visual.activeSelf) continue;

            if (visualSources.TryGetValue(visual, out Vector3 source) && visualTimers.TryGetValue(visual, out float timeLeft))
            {
                Vector3 direction = source - player.position;
                Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;

                Vector3 localDir = cameraTransform.InverseTransformDirection(flatDirection);
                float angle = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
                //float angle = Vector3.SignedAngle(player.forward, flatDirection, Vector3.up);
                angle = (angle + 360f) % 360f;

                // แก้ไขมุมที่อยู่ด้านหลัง (90-270 องศา)
                if (angle > 180f)
                {
                    angle = angle - 360f; // ทำให้เป็นค่าลบสำหรับมุมด้านหลัง
                }

                // ปรับมุมลบให้เป็นค่าบวกที่ตรงข้าม
                if (angle < 0)
                {
                    angle = 180f + (180f + angle);
                    // หรือ angle = 360f + angle; แล้วแต่กรณี
                }

                float radius = radialLayout.radius; // เช่น 150
                float angleRad = angle * Mathf.Deg2Rad;

                Vector2 targetPos = new Vector2(
                Mathf.Sin(angleRad) * radius,
                Mathf.Cos(angleRad) * radius);

                //int index = radialLayout.GetIndexFromAngle(angle);
                //Vector2 targetPos = radialLayout.GetPositionAtIndex(index);

                RectTransform rt = visual.GetComponent<RectTransform>();
                //icon.rectTransform.anchoredPosition = Vector2.Lerp(icon.rectTransform.anchoredPosition, targetPos, Time.deltaTime * updateSpeed);
                //icon.rectTransform.rotation = Quaternion.Euler(0, 0, -angle);
                rt.anchoredPosition = Vector2.Lerp(rt.anchoredPosition, targetPos, Time.deltaTime * updateSpeed);
                rt.rotation = Quaternion.Euler(0, 0, -angle);
                
                Vector3 scale = visual.transform.localScale;
            if (angle > 90f && angle < 270f)
            {
                scale.x = -Mathf.Abs(scale.x);
            }
            else
            {
                scale.x = Mathf.Abs(scale.x);
            }
            visual.transform.localScale = scale;


                visualTimers[visual] = timeLeft - Time.deltaTime;

                if (visualTimers[visual] <= 0)
                {
                    visual.SetActive(false);
                    visualSources.Remove(visual);
                    visualTimers.Remove(visual);
                }


            }
        }
    }
}
