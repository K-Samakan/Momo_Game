using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

namespace MOMO.AI.Pathfinding
{
    /// <summary>
    /// Dynamic NavMesh Sampling สำหรับพื้นที่ไม่เรียบและ Platform
    /// ไม่ใช้ Grid แบบเก่า แต่ใช้ NavMesh.SamplePosition แบบ Dynamic
    /// </summary>
    public class NavMeshGrid : MonoBehaviour
    {
        [Header("Sampling Settings")]
        [Tooltip("ระยะระหว่าง sample points")]
        public float sampleDistance = 2f;
        
        [Tooltip("ระยะค้นหา NavMesh")]
        public float maxNavMeshDistance = 2f;
        
        [Tooltip("ความสูงในการตรวจสอบ")]
        public float sampleHeight = 10f;
        
        [Header("Neighbor Search")]
        [Tooltip("ระยะค้นหา neighbor สูงสุด")]
        public float neighborSearchRadius = 5f;
        
        [Tooltip("จำนวน neighbor สูงสุด")]
        public int maxNeighbors = 8;
        
        [Header("Debug")]
        public bool showDebug = false;
        public Color validColor = Color.green;
        public Color invalidColor = Color.red;
        
        // Cache
        Dictionary<Vector3, NavMeshNode> nodeCache = new Dictionary<Vector3, NavMeshNode>();
        
        /// <summary>
        /// สร้าง NavMeshNode จากตำแหน่งใด ๆ (Dynamic)
        /// </summary>
        public NavMeshNode GetOrCreateNode(Vector3 worldPosition)
        {
            // ปัดเศษเพื่อ cache
            Vector3 roundedPos = RoundPosition(worldPosition);
            
            if (nodeCache.ContainsKey(roundedPos))
            {
                return nodeCache[roundedPos];
            }
            
            // สุ่ม NavMesh ที่ตำแหน่งนี้
            NavMeshHit hit;
            bool isOnNavMesh = NavMesh.SamplePosition(
                worldPosition,
                out hit,
                maxNavMeshDistance,
                NavMesh.AllAreas
            );
            
            NavMeshNode node = new NavMeshNode(
                isOnNavMesh ? hit.position : worldPosition,
                0, 0, // ไม่ใช้ grid X,Y
                isOnNavMesh
            );
            
            if (isOnNavMesh)
            {
                node.navMeshHit = hit;
            }
            
            nodeCache[roundedPos] = node;
            return node;
        }
        
        /// <summary>
        /// หา Node ใกล้เคียงแบบ Dynamic (ไม่ใช้ Grid)
        /// </summary>
        public List<NavMeshNode> GetNeighbors(NavMeshNode node)
        {
            List<NavMeshNode> neighbors = new List<NavMeshNode>();
            
            // สุ่มจุดรอบ ๆ node เป็นวงกลม
            int directions = 8;
            for (int i = 0; i < directions; i++)
            {
                float angle = (360f / directions) * i * Mathf.Deg2Rad;
                
                // ลอง 2 ระยะ (ใกล้และไกล)
                for (int radius = 1; radius <= 2; radius++)
                {
                    float distance = sampleDistance * radius;
                    
                    Vector3 samplePoint = node.worldPosition + new Vector3(
                        Mathf.Cos(angle) * distance,
                        0,
                        Mathf.Sin(angle) * distance
                    );
                    
                    // ลอง sample ทั้งในระดับเดียวกันและสูงกว่า/ต่ำกว่า
                    for (int heightOffset = -1; heightOffset <= 1; heightOffset++)
                    {
                        Vector3 testPoint = samplePoint + Vector3.up * (heightOffset * sampleHeight);
                        
                        NavMeshHit hit;
                        if (NavMesh.SamplePosition(testPoint, out hit, maxNavMeshDistance, NavMesh.AllAreas))
                        {
                            // ตรวจสอบว่าไม่ซ้ำกับ node ปัจจุบัน
                            if (Vector3.Distance(hit.position, node.worldPosition) > 0.5f)
                            {
                                NavMeshNode neighbor = GetOrCreateNode(hit.position);
                                
                                if (neighbor.isOnNavMesh && !neighbors.Contains(neighbor))
                                {
                                    // ตรวจสอบว่าเดินไปถึงได้
                                    NavMeshPath testPath = new NavMeshPath();
                                    if (NavMesh.CalculatePath(
                                        node.worldPosition,
                                        neighbor.worldPosition,
                                        NavMesh.AllAreas,
                                        testPath
                                    ))
                                    {
                                        if (testPath.status == NavMeshPathStatus.PathComplete)
                                        {
                                            neighbors.Add(neighbor);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // จำกัดจำนวน neighbors
            if (neighbors.Count > maxNeighbors)
            {
                neighbors = neighbors.GetRange(0, maxNeighbors);
            }
            
            return neighbors;
        }
        
        /// <summary>
        /// หา Node ที่เดินได้ใกล้เคียงที่สุด
        /// </summary>
        public NavMeshNode GetNearestWalkableNode(Vector3 worldPosition)
        {
            NavMeshHit hit;
            
            // ลองหาจากตำแหน่งปัจจุบันก่อน
            if (NavMesh.SamplePosition(worldPosition, out hit, maxNavMeshDistance, NavMesh.AllAreas))
            {
                return GetOrCreateNode(hit.position);
            }
            
            // ถ้าหาไม่เจอ ลองขยายรัศมีค้นหา
            for (float radius = 1f; radius <= 10f; radius += 1f)
            {
                if (NavMesh.SamplePosition(worldPosition, out hit, radius, NavMesh.AllAreas))
                {
                    Debug.LogWarning($"<color=yellow>[NavMeshGrid]</color> Found NavMesh at distance {radius}m");
                    return GetOrCreateNode(hit.position);
                }
            }
            
            Debug.LogError($"<color=red>[NavMeshGrid]</color> No walkable position found near {worldPosition}");
            return GetOrCreateNode(worldPosition); // Return invalid node
        }
        
        public void ResetAllNodes()
        {
            if(nodeCache == null || nodeCache.Count == 0)
                return;
            
            foreach(var kvp in nodeCache)
            {
                kvp.Value.Reset();
            }
        }

        /// <summary>
        /// ปัดเศษตำแหน่งเพื่อ cache
        /// </summary>
        Vector3 RoundPosition(Vector3 pos)
        {
            float precision = 0.5f;
            return new Vector3(
                Mathf.Round(pos.x / precision) * precision,
                Mathf.Round(pos.y / precision) * precision,
                Mathf.Round(pos.z / precision) * precision
            );
        }
        
        /// <summary>
        /// Reset cache (เรียกเมื่อ NavMesh เปลี่ยน)
        /// </summary>
        public void ClearCache()
        {
            nodeCache.Clear();
            Debug.Log("<color=cyan>[NavMeshGrid]</color> Cache cleared");
        }
        
        // Debug
        void OnDrawGizmos()
        {
            if (!showDebug || nodeCache == null) return;
            
            foreach (var kvp in nodeCache)
            {
                NavMeshNode node = kvp.Value;
                Gizmos.color = node.isOnNavMesh ? validColor : invalidColor;
                Gizmos.DrawSphere(node.worldPosition, 0.3f);
            }
        }
    }
}