#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// แสดง State ปัจจุบันของบอส Map3 (Snowman) แบบ real-time บนสุดของ Inspector
/// ⚠️ ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น
/// โครงสร้างเหมือน BossMap2BTEditor ทุกประการ
/// </summary>
[CustomEditor(typeof(BossMap3BT))]
public class BossMap3BTEditor : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        var boss = (BossMap3BT)target;

        if (Application.isPlaying)
        {
            var stateStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"☃️ STATE : {boss.CurrentState}", stateStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField(
                $"📏 Distance : {boss.DistanceToPlayer:F1}    |    🏃 AgentSpeed : {boss.AgentSpeed:F2}",
                EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(4);
        }

        DrawDefaultInspector();
    }
}
#endif