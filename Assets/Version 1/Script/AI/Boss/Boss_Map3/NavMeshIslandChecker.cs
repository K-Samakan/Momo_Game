#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;

/// <summary>
/// NavMesh Island Checker — เครื่องมือเช็คว่าจุด 2 จุดอยู่บน NavMesh
/// ผืนเดียวกันจริงหรือไม่ (ไม่ใช่แค่ "อยู่บน NavMesh" แต่ "เชื่อมถึงกันได้")
/// 
/// วิธีใช้:
///   Window → AI → NavMesh Island Checker
///   ลาก Transform ของ Boss กับ Player มาใส่ → กด Check
///   ถ้าขึ้นสีแดง = เป็นคนละเกาะ ต้อง Bake ใหม่ให้ครอบคลุมทั้งสองจุด
/// </summary>
public class NavMeshIslandChecker : EditorWindow
{
    private Transform pointA;
    private Transform pointB;
    private string result = "";
    private Color resultColor = Color.white;

    [MenuItem("Window/AI/NavMesh Island Checker")]
    public static void ShowWindow()
    {
        GetWindow<NavMeshIslandChecker>("NavMesh Island Checker");
    }

    void OnGUI()
    {
        GUILayout.Label("เช็คว่า NavMesh ระหว่าง 2 จุดเชื่อมถึงกันไหม", EditorStyles.boldLabel);
        GUILayout.Space(8);

        pointA = (Transform)EditorGUILayout.ObjectField("จุด A (เช่น Boss)", pointA, typeof(Transform), true);
        pointB = (Transform)EditorGUILayout.ObjectField("จุด B (เช่น Player)", pointB, typeof(Transform), true);

        GUILayout.Space(8);

        if (GUILayout.Button("🔍 Check Connection", GUILayout.Height(32)))
        {
            CheckConnection();
        }

        GUILayout.Space(12);

        if (!string.IsNullOrEmpty(result))
        {
            var style = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize = 13,
                normal = { textColor = resultColor }
            };
            EditorGUILayout.LabelField(result, style, GUILayout.Height(80));
        }
    }

    void CheckConnection()
    {
        if (pointA == null || pointB == null)
        {
            result = "❌ กรุณาลาก Transform ทั้งสองจุดก่อน";
            resultColor = Color.red;
            return;
        }

        // เช็คว่าแต่ละจุดอยู่บน NavMesh ไหม
        bool aOnMesh = NavMesh.SamplePosition(pointA.position, out var hitA, 2f, NavMesh.AllAreas);
        bool bOnMesh = NavMesh.SamplePosition(pointB.position, out var hitB, 2f, NavMesh.AllAreas);

        if (!aOnMesh || !bOnMesh)
        {
            result = $"❌ จุดใดจุดหนึ่งไม่อยู่บน NavMesh เลย!\n" +
                      $"A บน NavMesh: {aOnMesh}\nB บน NavMesh: {bOnMesh}";
            resultColor = Color.red;
            return;
        }

        // ✅ หัวใจของการเช็ค: CalculatePath แล้วดู pathStatus
        NavMeshPath path = new NavMeshPath();
        bool pathFound = NavMesh.CalculatePath(hitA.position, hitB.position, NavMesh.AllAreas, path);

        if (path.status == NavMeshPathStatus.PathComplete)
        {
            result = $"✅ เชื่อมถึงกันสมบูรณ์! (PathComplete)\n" +
                      $"ระยะทาง A→B: {Vector3.Distance(hitA.position, hitB.position):F1}m\n" +
                      $"จำนวน corner ของเส้นทาง: {path.corners.Length}";
            resultColor = Color.green;
        }
        else if (path.status == NavMeshPathStatus.PathPartial)
        {
            result = $"⚠️ เชื่อมถึงกันบางส่วน (PathPartial)\n" +
                      "อาจมีสิ่งกีดขวางบางส่วน แต่ไม่ใช่คนละเกาะทั้งหมด";
            resultColor = new Color(1f, 0.6f, 0f);
        }
        else
        {
            result = $"❌ คนละเกาะ! (PathInvalid)\n" +
                      "จุด A และ B อยู่บน NavMesh แยกกัน ไม่เชื่อมต่อกันเลย\n" +
                      "→ วิธีแก้: เช็ค mesh ตรงกลางทางว่ามีช่องว่างหรือ gap เล็กๆ ไหม\n" +
                      "→ ลอง Bake NavMesh ใหม่ทั้งซีนจาก GameObject เดียวที่ครอบคลุมทุกอย่าง";
            resultColor = Color.red;
        }
    }

    // วาดเส้นแสดงผลใน Scene view ด้วย
    void OnFocus() => SceneView.duringSceneGui += OnSceneGUI;
    void OnDestroy() => SceneView.duringSceneGui -= OnSceneGUI;

    void OnSceneGUI(SceneView sceneView)
    {
        if (pointA == null || pointB == null) return;

        NavMeshPath path = new NavMeshPath();
        if (NavMesh.CalculatePath(pointA.position, pointB.position, NavMesh.AllAreas, path))
        {
            Handles.color = path.status == NavMeshPathStatus.PathComplete ? Color.green : Color.red;
            for (int i = 0; i < path.corners.Length - 1; i++)
                Handles.DrawLine(path.corners[i], path.corners[i + 1]);
        }
    }
}
#endif