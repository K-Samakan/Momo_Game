#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

// แสดง State ปัจจุบันของบอสแบบ real-time บนสุดของ Inspector
// ⚠️ ไฟล์นี้ต้องอยู่ในโฟลเดอร์ชื่อ "Editor" เท่านั้น
[CustomEditor(typeof(BossMap1BT))]
public class BossMap1BTEditor : Editor
{
    // บังคับให้ Inspector วาดใหม่ทุกเฟรมตอน Play เพื่อให้ค่าอัปเดตสด
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        var boss = (BossMap1BT)target;

        if (Application.isPlaying)
        {
            var stateStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"🤖 STATE : {boss.CurrentState}", stateStyle, GUILayout.Height(30));
            EditorGUILayout.LabelField(
                $"📏 Distance : {boss.DistanceToPlayer:F1}    |    🏃 AgentSpeed : {boss.AgentSpeed:F2}",
                EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(4);
        }

        DrawDefaultInspector();
    }
}
#endif