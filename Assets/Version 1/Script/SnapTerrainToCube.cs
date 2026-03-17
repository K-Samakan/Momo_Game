using UnityEngine;

public class SnapTerrainToCube : MonoBehaviour
{
    public Terrain terrain;
    public Transform cube;

    void Start()
    {
        Vector3 pos = terrain.transform.position;
        pos.y = cube.position.y + (cube.localScale.y / 2);
        terrain.transform.position = pos;
    }
}
