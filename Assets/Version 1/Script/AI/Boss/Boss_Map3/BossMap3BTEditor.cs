#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

[CustomEditor(typeof(BossMap3BT))]
public class BossMap3BTEditor : Editor
{
    public override bool RequiresConstantRepaint() => Application.isPlaying;

    public override void OnInspectorGUI()
    {
        var boss = (BossMap3BT)target;
        if (Application.isPlaying)
        {
            var s = new GUIStyle(EditorStyles.helpBox) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField($"⛄ STATE : {boss.CurrentState}", s, GUILayout.Height(30));
            EditorGUILayout.LabelField($"📏 Distance : {boss.DistanceToPlayer:F1}    |    🏃 AgentSpeed : {boss.AgentSpeed:F2}", EditorStyles.centeredGreyMiniLabel);

            // ✅ เพิ่ม: เช็ค NavMesh ตรงตำแหน่งบอสแบบ real-time
            bool onMesh = NavMesh.SamplePosition(boss.transform.position, out var hit, 0.5f, NavMesh.AllAreas);
            var navStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 12,
                normal = { textColor = onMesh ? Color.green : Color.red }
            };
            EditorGUILayout.LabelField(
                onMesh ? $"✅ ยืนบน NavMesh (ห่าง {Vector3.Distance(boss.transform.position, hit.position):F2}m)"
                       : "❌ ไม่ได้ยืนบน NavMesh ในระยะ 0.5m!",
                navStyle);

            EditorGUILayout.Space(4);
        }
        DrawDefaultInspector();
    }

    // ✅ วาด Gizmo แสดง NavMesh ตรงตำแหน่งบอสใน Scene view
    void OnSceneGUI()
    {
        var boss = (BossMap3BT)target;
        Vector3 pos = boss.transform.position;

        bool onMesh = NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas);

        Handles.color = onMesh ? Color.green : Color.red;
        Handles.DrawWireDisc(onMesh ? hit.position : pos, Vector3.up, 1.5f);

        if (onMesh)
        {
            Handles.Label(hit.position + Vector3.up * 2f,
                $"NavMesh: {Vector3.Distance(pos, hit.position):F2}m away");
        }
        else
        {
            Handles.Label(pos + Vector3.up * 2f, "❌ NO NAVMESH HERE (2m radius)");
        }
    }
}
#endif