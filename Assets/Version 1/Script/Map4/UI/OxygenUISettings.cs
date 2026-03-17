using UnityEngine;

/// <summary>
/// ScriptableObject สำหรับตั้งค่า Oxygen UI
/// ใช้สำหรับสลับ theme หรือ preset ต่างๆ
/// </summary>
[CreateAssetMenu(fileName = "OxygenUISettings", menuName = "MOMO/UI/Oxygen UI Settings")]
public class OxygenUISettings : ScriptableObject
{
    [Header("Visual Settings")]
    public Color normalColor = Color.white;
    public Color criticalColor = new Color(1f, 0.27f, 0.27f, 1f);
    public float colorTransitionSpeed = 5f;

    [Header("Blink Settings")]
    public float blinkSpeed = 0.5f;
    public float blinkMinAlpha = 0.3f;
    public float blinkMaxAlpha = 1f;

    [Header("Pulse Settings")]
    public bool enablePulseEffect = true;
    public float pulseThreshold = 30f;
    public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 1.2f);
}