using System.Linq;
using LocalizationPackageExtensions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace LocalizationPackageExtensionsEditor
{
    [CustomEditor(typeof(AudioPairTable))]
    public class AudioPairTableEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.Add(new PropertyField(serializedObject.FindProperty(nameof(AudioPairTable.tableCollectionName))));
            root.Add(new PropertyField(serializedObject.FindProperty(nameof(AudioPairTable.pairMap))));

            root.Add(new VisualElement(){style={height = 50}});

            var openTableWindowButton = new Button(OpenTable);
            openTableWindowButton.text = "Open AudioPairTable Window";
            root.Add(openTableWindowButton);
            
            root.Add(new VisualElement(){style={height = 20}});
            
            var removeUnusedIdPairButton = new Button(RemovePairsWithUnusedKeyId);
            removeUnusedIdPairButton.text = "Remove Pairs with Unused keyId";
            root.Add(removeUnusedIdPairButton);
            
            var updatePairsKeyIdByKeyButton = new Button(UpdatePairsKeyIdByKey);
            updatePairsKeyIdByKeyButton.text = "Update keyId values based on the key value";
            root.Add(updatePairsKeyIdByKeyButton);
            
            var updatePairsKeyByKeyIdButton = new Button(UpdatePairsKeyByKeyID);
            updatePairsKeyByKeyIdButton.text = "Update key values based on the keyId value";
            root.Add(updatePairsKeyByKeyIdButton);

            return root;
        }

        void OpenTable()
        {
            var window = EditorWindow.GetWindow<AudioPairTableWindow>();
            window.Show();
        }

        /// <summary>
        /// Removes pairs with keyId values that are not used.
        /// </summary>
        public void RemovePairsWithUnusedKeyId()
        {
            var apTable = (AudioPairTable)target;
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(apTable.tableCollectionName);

            var removeCount = 0;
            foreach (var map in apTable.pairMap.ToList())
            {
                var entry = tableCollection.SharedData.GetEntry(map.keyId);
                if (entry != null) continue;
                apTable.pairMap.Remove(map);
                removeCount++;
                EditorUtility.SetDirty(apTable);
            }
            
            AssetDatabase.SaveAssetIfDirty(apTable);
            
            if(removeCount > 0) Debug.Log($"[{apTable.name}] Unused pairs are removed");
            else Debug.Log($"[{apTable.name}] No pairs to remove");
        }

        /// <summary>
        /// Updates the string key value of each pair by the long keyId value.
        /// </summary>
        public void UpdatePairsKeyByKeyID()
        {
            var apTable = (AudioPairTable)target;
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(apTable.tableCollectionName);

            var updateCound = 0;
            foreach (var map in apTable.pairMap)
            {
                var entry = tableCollection.SharedData.GetEntry(map.keyId);
                if(entry == null) continue;
                if(map.key == entry.Key) continue;
                
                EditorUtility.SetDirty(apTable);
                map.key = entry.Key;
                updateCound++;
            }
            
            AssetDatabase.SaveAssetIfDirty(apTable);
            if(updateCound > 0) Debug.Log($"[{apTable.name}] key values are updated");
            else Debug.Log($"[{apTable.name}] No key values to update");
        }
        
        /// <summary>
        /// Updates the long keyId value of each pair by the string key value.
        /// </summary>
        public void UpdatePairsKeyIdByKey()
        {
            var apTable = (AudioPairTable)target;
            var tableCollection = LocalizationEditorSettings.GetStringTableCollection(apTable.tableCollectionName);

            var updateCount = 0;
            foreach (var map in apTable.pairMap)
            {
                var entry = tableCollection.SharedData.GetEntry(map.key);
                if(entry == null) continue;
                if(map.keyId == entry.Id) continue;
                
                EditorUtility.SetDirty(apTable);
                map.keyId = entry.Id;
                updateCount++;
            }
            
            AssetDatabase.SaveAssetIfDirty(apTable);
            
            if(updateCount > 0) Debug.Log($"[{apTable.name}] keyId values are updated");
            else Debug.Log($"[{apTable.name}] No keyId values to update");
        }

        /// <summary>
        /// Do <see cref="RemovePairsWithUnusedKeyId"/> and <see cref="UpdatePairsKeyByKeyID"/> at once.
        /// </summary>
        public void Sync()
        {
            RemovePairsWithUnusedKeyId();
            UpdatePairsKeyByKeyID();
        }

        /// <summary>
        /// Synchronize all AudioPairTables with each StringTable based on their long KeyId values.
        /// Specifically, the changed Key value is updated, and the missing Entry is also removed from the PairMap.
        /// </summary>
        [MenuItem("Window/Asset Management/Localization Package Extensions/Sync All AudioPairTables")]
        public static void SyncAll()
        {
            Debug.Log($"Start Sync...");
            var guids = AssetDatabase.FindAssets("t:AudioPairTable");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var table = AssetDatabase.LoadAssetAtPath<AudioPairTable>(path);

                var editor = CreateEditor(table) as AudioPairTableEditor;
                editor.Sync();
                
                DestroyImmediate(editor);
            }

            Debug.Log($"Sync Finish");
        }
    }
}