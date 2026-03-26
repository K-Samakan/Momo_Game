using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FS_ThirdPerson
{
    public class FootTrigger : MonoBehaviour
    {
        Collider currCollider;
        FloorStepData currFloorData;

        FootStepEffects footStepEffects;
        private void Awake()
        {
            footStepEffects = GetComponentInParent<FootStepEffects>();

            // ==============================================================
            // [MODIFIED] Layer "FootTrigger" ไม่มีในโปรเจกต์ (Layer เต็ม 32/32)
            // เปลี่ยนไปใช้ Layer "SPECIAL" (Index 31) แทน
            // ต้องตรงกับที่แก้ใน FootStepEffects.cs, FSSystemsSetupEditorWindow.cs
            // และ ClimbPointAndCreateCharacterEditorWindow.cs ด้วย
            // ==============================================================
            this.gameObject.layer = LayerMask.NameToLayer("SPECIAL");
        }


        private void OnTriggerEnter(Collider other)
        {
            if (footStepEffects == null)
                return;

            if (footStepEffects.OverrideStepEffects != null && footStepEffects.OverrideStepEffects.Count > 0)
            {
                if (currFloorData == null || currCollider != other || currFloorData.terrain != null)
                {
                    currCollider = other;
                    currFloorData = new FloorStepData(transform, footStepEffects, other.transform, other, footStepEffects.OverrideType == StepEffectsOverrideType.Tag);
                }
            }

            footStepEffects.OnFootLand(transform, currFloorData);
        }
    }

    public class FloorStepData
    {
        public string materialName;
        public string textureName;
        public string tag;
        public Terrain terrain;

        Material material;
        Renderer renderer;

        FootStepEffects stepEffects;

        public FloorStepData(Transform footTransform, FootStepEffects stepEffects, Transform groundTransform, Collider floorCollider, bool onlyTakeTag = false)
        {
            tag = floorCollider.tag;
            this.stepEffects = stepEffects;
            if (onlyTakeTag) return;

            renderer = groundTransform.GetComponent<Renderer>();

            if (renderer != null && renderer.material != null)
            {
                material = renderer.materials[0];
                terrain = null;
                materialName = material.name.Substring(0, material.name.Length - 11).ToLower();
                textureName = material.mainTexture?.name;
            }
            else
            {
                if (terrain == null || terrain.transform != groundTransform)
                    terrain = groundTransform.GetComponent<Terrain>();

                if (terrain != null)
                {
                    string terrainLayer = GetTerrainLayerAtPosition(footTransform);
                    materialName = terrainLayer.ToLower();
                    textureName = terrainLayer.ToLower();
                }
            }
        }

        string GetTerrainLayerAtPosition(Transform footTransform)
        {
            var footPos = footTransform.position;

            if (Physics.Raycast(footPos + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 0.5f, stepEffects.GroundLayer))
            {
                var terrain = hit.collider.GetComponent<Terrain>();
                if (terrain != null)
                {
                    TerrainData terrainData = terrain.terrainData;
                    Vector3 terrainPosition = footPos - terrain.transform.position;
                    float relativeX = terrainPosition.x / terrainData.size.x;
                    float relativeZ = terrainPosition.z / terrainData.size.z;

                    int mapX = Mathf.FloorToInt(relativeX * terrainData.alphamapWidth);
                    int mapZ = Mathf.FloorToInt(relativeZ * terrainData.alphamapHeight);

                    float[,,] alphaMap = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

                    int terrainLayerIdx = 0;
                    float maxWeight = 0;
                    for (int i = 0; i < alphaMap.GetLength(2); i++)
                    {
                        if (alphaMap[0, 0, i] > maxWeight)
                        {
                            maxWeight = alphaMap[0, 0, i];
                            terrainLayerIdx = i;
                        }
                    }

                    string textureName = terrainData.terrainLayers[terrainLayerIdx].diffuseTexture.name;
                    return textureName;
                }
            }

            return "";
        }
    }
}