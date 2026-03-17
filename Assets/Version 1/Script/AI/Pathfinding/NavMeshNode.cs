using UnityEngine;
using UnityEngine.AI;

namespace MOMO.AI.Pathfinding
{
    /// <summary>
    /// Node สำหรับ Theta* Pathfinding บน NavMesh
    /// </summary>
    public class NavMeshNode
    {
        // Position & Grid
        public Vector3 worldPosition;
        public int gridX;
        public int gridY;

        // Walkability
        public bool walkable;
        public bool isOnNavMesh;

        // Theta* Algorithm
        public float gCost; // ระยะจากจุดเริ่มต้น
        public float hCost; // ระยะโดยประมาณถึงเป้าหมาย
        public NavMeshNode parent;

        public float fCost => gCost + hCost;

        // NavMesh Data
        public NavMeshHit navMeshHit;

        /// <summary>
        /// Constructor
        /// </summary>
        public NavMeshNode(Vector3 _worldPos, int _gridX, int _gridY, bool _isOnNavMesh)
        {
            worldPosition = _worldPos;
            gridX = _gridX;
            gridY = _gridY;
            isOnNavMesh = _isOnNavMesh;
            walkable = _isOnNavMesh;
            gCost = float.MaxValue;
            hCost = 0;
            parent = null;
        }

        /// <summary>
        /// Reset node สำหรับ pathfinding ใหม่
        /// </summary>
        public void Reset()
        {
            gCost = float.MaxValue;
            hCost = 0;
            parent = null;
        }

        // เพิ่มในคลาส NavMeshNode:

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is NavMeshNode))
                return false;

            NavMeshNode other = (NavMeshNode)obj;
            return Vector3.Distance(worldPosition, other.worldPosition) < 0.1f;
        }

        public override int GetHashCode()
        {
            int x = Mathf.RoundToInt(worldPosition.x * 2f);
            int y = Mathf.RoundToInt(worldPosition.y * 2f);
            int z = Mathf.RoundToInt(worldPosition.z * 2f);
            return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
        }
    }
}