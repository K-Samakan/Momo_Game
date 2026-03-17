using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

namespace MOMO.AI.Pathfinding
{
    /// <summary>
    /// Theta* Pathfinding บน NavMesh
    /// </summary>
    public class ThetaStarNavMesh : MonoBehaviour
    {
        public NavMeshGrid navMeshGrid;

        [Header("Pathfinding Settings")]
        [Tooltip("ใช้ Path Smoothing")]
        public bool usePathSmoothing = true;

        [Tooltip("จำนวนครั้งสูงสุดในการหา path")]
        public int maxIterations = 1000;

        [Header("Debug")]
        public bool showPath = true;
        public bool showSearchedNodes = false;
        public Color pathColor = Color.cyan;
        public Color searchedNodeColor = new Color(1f, 1f, 0f, 0.3f);

        List<Vector3> currentPath = new List<Vector3>();
        HashSet<NavMeshNode> searchedNodes = new HashSet<NavMeshNode>();

        void Awake()
        {
            if (navMeshGrid == null)
                navMeshGrid = GetComponent<NavMeshGrid>();
        }

        /// <summary>
        /// หาเส้นทางด้วย Theta* บน NavMesh
        /// </summary>
        public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
        {
            float startTime = Time.realtimeSinceStartup;

            // หา Node ใกล้เคียงบน NavMesh
            NavMeshNode startNode = navMeshGrid.GetNearestWalkableNode(startPos);
            NavMeshNode targetNode = navMeshGrid.GetNearestWalkableNode(targetPos);

            if (!startNode.isOnNavMesh || !targetNode.isOnNavMesh)
            {
                Debug.LogWarning("<color=yellow>[ThetaStar]</color> Start or Target is not on NavMesh!");
                return new List<Vector3>();
            }

            // Reset ทุก node
            navMeshGrid.ResetAllNodes();
            searchedNodes.Clear();

            List<NavMeshNode> openSet = new List<NavMeshNode>();
            HashSet<NavMeshNode> closedSet = new HashSet<NavMeshNode>();

            startNode.gCost = 0;
            startNode.hCost = GetDistance(startNode.worldPosition, targetNode.worldPosition);
            openSet.Add(startNode);

            int iterations = 0;

            while (openSet.Count > 0 && iterations < maxIterations)
            {
                iterations++;

                // หา node ที่มี fCost ต่ำสุด
                NavMeshNode currentNode = openSet
                    .OrderBy(n => n.fCost)
                    .ThenBy(n => n.hCost)
                    .First();

                openSet.Remove(currentNode);
                closedSet.Add(currentNode);
                searchedNodes.Add(currentNode);

                // ถ้าถึงเป้าหมายแล้ว
                if (currentNode == targetNode)
                {
                    currentPath = RetracePath(startNode, targetNode);

                    float endTime = Time.realtimeSinceStartup;
                    Debug.Log($"<color=green>[ThetaStar]</color> Path found in {(endTime - startTime) * 1000:F2}ms " +
                             $"({iterations} iterations, {currentPath.Count} waypoints)");

                    return currentPath;
                }

                // ตรวจสอบ neighbours
                foreach (NavMeshNode neighbour in navMeshGrid.GetNeighbors(currentNode))
                {
                    if (!neighbour.isOnNavMesh || closedSet.Contains(neighbour))
                        continue;

                    // *** Theta* Algorithm ***
                    // ลองตัดเส้นตรงจาก parent ของ current ไปหา neighbour
                    if (currentNode.parent != null && LineOfSightNavMesh(currentNode.parent, neighbour))
                    {
                        // Path 2: ใช้ parent โดยตรง (Any-angle path)
                        float newCost = currentNode.parent.gCost +
                                       GetDistance(currentNode.parent.worldPosition, neighbour.worldPosition);

                        if (newCost < neighbour.gCost)
                        {
                            neighbour.gCost = newCost;
                            neighbour.hCost = GetDistance(neighbour.worldPosition, targetNode.worldPosition);
                            neighbour.parent = currentNode.parent;

                            if (!openSet.Contains(neighbour))
                                openSet.Add(neighbour);
                        }
                    }
                    else
                    {
                        // Path 1: ใช้ current เป็น parent (ปกติ)
                        float newCost = currentNode.gCost +
                                       GetDistance(currentNode.worldPosition, neighbour.worldPosition);

                        if (newCost < neighbour.gCost)
                        {
                            neighbour.gCost = newCost;
                            neighbour.hCost = GetDistance(neighbour.worldPosition, targetNode.worldPosition);
                            neighbour.parent = currentNode;

                            if (!openSet.Contains(neighbour))
                                openSet.Add(neighbour);
                        }
                    }
                }
            }

            if (iterations >= maxIterations)
            {
                Debug.LogWarning($"<color=yellow>[ThetaStar]</color> Max iterations reached ({maxIterations})");
            }
            else
            {
                //Debug.LogWarning("<color=yellow>[ThetaStar]</color> No path found!");
            }

            return new List<Vector3>();
        }

        /// <summary>
        /// ตรวจสอบว่ามองเห็นกันได้ไหมบน NavMesh (Line of Sight)
        /// </summary>
        // เปลี่ยนฟังก์ชัน LineOfSightNavMesh เป็น:

        /// <summary>
        /// ตรวจสอบ Line of Sight บน NavMesh (รองรับ Platform)
        /// </summary>
        bool LineOfSightNavMesh(NavMeshNode nodeA, NavMeshNode nodeB)
        {
            // วิธี 1: NavMesh Raycast
            NavMeshHit hit;
            bool hasObstacle = NavMesh.Raycast(
                nodeA.worldPosition,
                nodeB.worldPosition,
                out hit,
                NavMesh.AllAreas
            );

            if (!hasObstacle)
                return true;

            // ตรวจสอบว่า hit point ใกล้เป้าหมายมากไหม
            float distanceToTarget = Vector3.Distance(hit.position, nodeB.worldPosition);
            if (distanceToTarget < 0.5f)
                return true;

            // วิธี 2: ตรวจสอบด้วย NavMesh Path (สำหรับ Platform)
            NavMeshPath testPath = new NavMeshPath();
            if (NavMesh.CalculatePath(
                nodeA.worldPosition,
                nodeB.worldPosition,
                NavMesh.AllAreas,
                testPath
            ))
            {
                // ถ้า path เป็นเส้นตรง (1-2 waypoints) = มองเห็นกัน
                if (testPath.corners.Length <= 2)
                {
                    float pathLength = 0f;
                    for (int i = 0; i < testPath.corners.Length - 1; i++)
                    {
                        pathLength += Vector3.Distance(testPath.corners[i], testPath.corners[i + 1]);
                    }

                    float directDistance = Vector3.Distance(nodeA.worldPosition, nodeB.worldPosition);

                    // ถ้า path ยาวกว่าระยะตรงไม่เกิน 20% = ใกล้เคียงเส้นตรง
                    return pathLength <= directDistance * 1.2f;
                }
            }

            return false;
        }

        /// <summary>
        /// คำนวณระยะทาง (Euclidean Distance)
        /// </summary>
        float GetDistance(Vector3 posA, Vector3 posB)
        {
            return Vector3.Distance(posA, posB);
        }

        /// <summary>
        /// ติดตามเส้นทางย้อนกลับจาก target ไป start
        /// </summary>
        List<Vector3> RetracePath(NavMeshNode startNode, NavMeshNode endNode)
        {
            List<NavMeshNode> path = new List<NavMeshNode>();
            NavMeshNode currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }

            path.Reverse();

            // Convert to Vector3
            List<Vector3> waypoints = path.Select(n => n.worldPosition).ToList();

            // Smooth path (ถ้าเปิดใช้)
            if (usePathSmoothing)
            {
                waypoints = SmoothPath(path);
            }

            return waypoints;
        }

        /// <summary>
        /// Smooth path โดยลบ waypoint ที่ไม่จำเป็น
        /// </summary>
        List<Vector3> SmoothPath(List<NavMeshNode> path)
        {
            if (path.Count <= 2)
                return path.Select(n => n.worldPosition).ToList();

            List<Vector3> smoothPath = new List<Vector3>();
            smoothPath.Add(path[0].worldPosition);

            int currentIndex = 0;

            while (currentIndex < path.Count - 1)
            {
                // หาจุดที่ไกลที่สุดที่มองเห็นได้
                int furthestIndex = currentIndex + 1;

                for (int i = currentIndex + 2; i < path.Count; i++)
                {
                    if (LineOfSightNavMesh(path[currentIndex], path[i]))
                    {
                        furthestIndex = i;
                    }
                    else
                    {
                        break;
                    }
                }

                smoothPath.Add(path[furthestIndex].worldPosition);
                currentIndex = furthestIndex;
            }

            return smoothPath;
        }

        // Debug: แสดงเส้นทาง
        void OnDrawGizmos()
        {
            // Show searched nodes
            if (showSearchedNodes && searchedNodes != null && searchedNodes.Count > 0)
            {
                Gizmos.color = searchedNodeColor;
                foreach (var node in searchedNodes)
                {
                    Gizmos.DrawCube(node.worldPosition + Vector3.up * 0.2f, Vector3.one * 0.4f);
                }
            }

            // Show path
            if (showPath && currentPath != null && currentPath.Count > 0)
            {
                Gizmos.color = pathColor;

                for (int i = 0; i < currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(
                        currentPath[i] + Vector3.up * 0.5f,
                        currentPath[i + 1] + Vector3.up * 0.5f
                    );
                    Gizmos.DrawSphere(currentPath[i] + Vector3.up * 0.5f, 0.3f);
                }

                if (currentPath.Count > 0)
                {
                    Gizmos.DrawSphere(
                        currentPath[currentPath.Count - 1] + Vector3.up * 0.5f,
                        0.3f
                    );
                }
            }
        }
    }
}