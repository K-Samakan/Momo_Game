using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VisualSoundEffectManager1 : MonoBehaviour
{
    public static VisualSoundEffectManager Instance;

    public Transform player; // ตัวละครผู้เล่น
    public Transform cameraTransform; // กล้องที่ติดตามผู้เล่น
    public Image iconPrefab;
    public float iconDistance = 150f;
    public float displayTime = 1f;
    public float updateSpeed = 5f;
    public RadialLayoutManual radialLayout;

    private List<Image> iconPool = new List<Image>(); // พูลของไอคอน
    private Dictionary<Image, Vector3> iconSources = new Dictionary<Image, Vector3>(); // เก็บตำแหน่งแหล่งของแต่ละไอคอน
    private Dictionary<Image, float> iconTimers = new Dictionary<Image, float>(); // ตัวจับเวลาให้แต่ละไอคอน

    private void Awake()
    {
        //Instance = this;
        CreateIconPool();
    }

    void CreateIconPool()
    {
        for (int i = 0; i < 10; i++) // สร้างไอคอนล่วงหน้า 10 ตัว
        {
            Image icon = Instantiate(iconPrefab, transform);
            icon.enabled = false;
            iconPool.Add(icon);
        }
    }

    public void ShowIcon(Vector3 hapticSourcePosition, float intensity)
    {
        if (iconPrefab == null || player == null || cameraTransform == null || radialLayout == null) return;

        // หาไอคอนที่ว่างจากพูล
        Image icon = GetAvailableIcon();
        if (icon == null) return;

        icon.enabled = true;
        iconSources[icon] = hapticSourcePosition; // เก็บตำแหน่งแหล่งของศัตรู
        iconTimers[icon] = displayTime;

        // ปรับความโปร่งใสและขนาดตาม intensity
        icon.color = new Color(1, 1, 1, Mathf.Clamp01(intensity));
        icon.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.5f, intensity);
    }

    Image GetAvailableIcon()
    {
        foreach (var icon in iconPool)
        {
            if (!icon.enabled) return icon;
        }
        return null;
    }

    void Update()
    {
        foreach (var icon in iconPool)
        {
            if (!icon.enabled) continue;

            if (iconSources.ContainsKey(icon) && iconTimers.ContainsKey(icon))
            {
                // คำนวณทิศทางจากตัวละครไปยังแหล่งของไอคอน
                Vector3 directionToSource = (iconSources[icon] - player.position).normalized;
                float angle = Vector3.SignedAngle(cameraTransform.forward, directionToSource, Vector3.up);

                // Debug มุมและทิศทาง
                Debug.Log($"Angle to source: {angle}, Camera Forward: {cameraTransform.forward}, Direction to Source: {directionToSource}");

                // ปรับมุมให้สอดคล้องกับ Radial UI (ถ้าจำเป็น)
                // จาก Debug ล่าสุด มุม -153.0472 ควรอยู่ในด้านขวา (90-135 องศา) แต่ไปโผล่ด้านซ้ายบน
                // ลองปรับมุมโดยหมุน 180 องศาเพื่อแก้ไขทิศทาง
                if (angle < 0) angle += 360;

                // หาดัชนีจากมุมโดยตรง
                int index = radialLayout.GetIndexFromAngle(angle);
                Vector2 targetPosition = radialLayout.GetPositionAtIndex(index);

                // Debug ดัชนีและตำแหน่ง
                Debug.Log($"Adjusted Angle: {angle}, Icon Index: {index}, Target Position: {targetPosition}");

                // อัปเดตตำแหน่งของไอคอน
                icon.rectTransform.anchoredPosition = Vector2.Lerp(icon.rectTransform.anchoredPosition, targetPosition, Time.deltaTime * updateSpeed);

                // ลดเวลาและปิดไอคอนเมื่อหมดเวลา
                iconTimers[icon] -= Time.deltaTime;
                if (iconTimers[icon] <= 0)
                {
                    icon.enabled = false;
                    iconSources.Remove(icon);
                    iconTimers.Remove(icon);
                }
            }
        }
    }
}