#if UNITY_EDITOR
using FS_ThirdPerson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;

namespace FS_Core
{
    public partial class FSSystemsSetup : MonoBehaviour
    {
        public static string welcomeWindowOpenKey = "FS_WelcomeWindow_Opened";

        public static new List<string> layers = new List<string> { "Ledge", "Player", "Enemy", "NPC", "Vehicle", "VisionSensor", "FootTrigger", "Cover", "Hotspot", "PlayerAlly", "HitBone" };
        public static new List<string> tags = new List<string>() { "Hitbox", "NarrowBeam", "SwingableLedge", "Slide", "NavmeshLink", "NavmeshSurface" };

        public static FSSystemInfo ThirdPersonControllerSystemSetup = new FSSystemInfo
        (
            characterType: CharacterType.Player,
            selected: true,
            systemName: "Locomotion System",
            displayName: "Locomotion",

            prefabName: "Locomotion Controller",
            welcomeEditorShowKey: "LocomotionSystem_WelcomeWindow_Opened_3"
        );

        static string LocomotionSystemWelcomeEditorKey => ThirdPersonControllerSystemSetup.welcomeEditorShowKey;

        [InitializeOnLoadMethod]
        public static void LoadLocomotionSystem()
        {
            if (!string.IsNullOrEmpty(LocomotionSystemWelcomeEditorKey) && !PlayerPrefs.HasKey(LocomotionSystemWelcomeEditorKey))
            {
                SessionState.SetBool(welcomeWindowOpenKey, false);
                PlayerPrefs.SetString(LocomotionSystemWelcomeEditorKey, "");
                FSSystemsSetupEditorWindow.OnProjectLoad();
            }
        }

        private void Awake()
        {
            this.enabled = false;
        }

        // Filtered systems by current selected charcater type from window
        public Dictionary<string, FSSystemInfo> CurrentFSSystemsForSetup = new Dictionary<string, FSSystemInfo>();
        // All installed FS Systems
        public Dictionary<string, FSSystemInfo> AllInstalledSystems = new Dictionary<string, FSSystemInfo>();


        public void FindSystem()
        {
            AllInstalledSystems.Clear();
            CurrentFSSystemsForSetup.Clear();
            FieldInfo[] fields = GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static);

            foreach (var field in fields)
            {
                // Check if the field's type is FSSystem
                if (field.FieldType == typeof(FSSystemInfo))
                {
                    // Get the value of the field and cast it to FSSystem
                    FSSystemInfo system = field.GetValue(this) as FSSystemInfo;

                    if (system != null)
                    {
                        if(system.characterType == FSSystemsSetupEditorWindow.characterType)
                            CurrentFSSystemsForSetup.TryAdd(field.Name, system);
                        AllInstalledSystems.TryAdd(field.Name, system);
                    }
                }
            }
        }
        public void EnableSystems(params string[] systemNames)
        {
            foreach (var s in CurrentFSSystemsForSetup)
            {
                if(systemNames.Contains(s.Value.systemName))
                    s.Value.selected = true;
                else
                    s.Value.selected = false;
            }
        }

        /// <summary>
        /// Loads the prefab from Resources and copies all its components to this GameObject.
        /// </summary>
        public GameObject CopyComponentsAndAnimControllerFromPrefab(string prefabName, AnimatorMergerUtility animatorMergerUtility, GameObject characterObject)
        {
            if (!string.IsNullOrEmpty(prefabName))
            {
                // Load the prefab from Resources
                GameObject prefab = Resources.Load<GameObject>(prefabName);
                bool isPlayer = FSSystemsSetupEditorWindow.characterType == CharacterType.Player;
                if (prefab != null)
                {
                    var characterPrefab = isPlayer ? prefab.GetComponentInChildren<PlayerController>().gameObject: prefab;

                    var animatorController = characterPrefab.GetComponent<Animator>().runtimeAnimatorController as AnimatorController;

                    animatorMergerUtility.MergeAnimatorControllers(animatorController);

                    FSSystemsSetupEditorWindow.CopyComponents(characterPrefab, characterObject);

                    //if (isPlayer)
                    //{
                    //    var managedScript = characterObject.GetComponents<SystemBase>().ToList();
                    //    managedScript.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                    //    characterObject.GetComponent<PlayerController>().managedScripts = managedScript;
                    //}
                }
                return prefab;
            }
            else
            {
                //Debug.LogWarning("Prefab name is not specified.");
            } 
            return null;
        }

        public void ImportProjectSettings()
        {
            FindSystem();
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");


            for (int i = 0; i < FSSystemsSetup.layers.Count; i++)
            {
                string layerName = FSSystemsSetup.layers[i];

                if (!string.IsNullOrEmpty(layerName))
                {
                    bool layerExists = false;
                    for (int j = 0; j < layersProp.arraySize; j++)
                    {
                        SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(j);
                        if (layerProp.stringValue == layerName)
                        {
                            layerExists = true;
                            break;
                        }
                    }

                    if (!layerExists)
                    {
                        for (int j = 7; j < layersProp.arraySize; j++)
                        {
                            SerializedProperty layerProp = layersProp.GetArrayElementAtIndex(j);
                            if (string.IsNullOrEmpty(layerProp.stringValue))
                            {
                                layerProp.stringValue = layerName;
                                break;
                            }
                        }
                    }
                }
            }
            tagManager.ApplyModifiedProperties();


            foreach (var tag in FSSystemsSetup.tags)
            {
                if (!InternalEditorUtility.tags.ToList().Contains(tag))
                    InternalEditorUtility.AddTag(tag);
            }


            foreach (var systemProjectSettingsData in AllInstalledSystems.Values)
            {
                if (systemProjectSettingsData.OnInstallation == null) 
                    continue;

                systemProjectSettingsData.OnInstallation?.Invoke();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
            var serializedObject = new SerializedObject(asset);
            var property = serializedObject.FindProperty("activeInputHandler");

            if(property.intValue == 1)
            {
                ScriptingDefineSymbolController.ToggleScriptingDefineSymbol(FSSystemsSetupEditorWindow.inputsystem, true);
            }
        }
    }
    public enum CharacterType
    {
        Player,
        AI,
        None
    }
    public class FSSystemInfo
    {
        public CharacterType characterType;
        public bool selected;
        public string systemName;
        public string displayName;
        public string prefabName;
        public string mobileControllerPrefabName;
        public Action<GameObject, GameObject, GameObject> extraSetupActionPlayer;
        public Action<GameObject, GameObject> extraSetupActionAI;
        public Action OnInstallation;
        public string welcomeEditorShowKey;
        public FSSystemInfo(CharacterType characterType, string systemName, string displayName = "", string prefabName = "", bool selected = false, string welcomeEditorShowKey = "", Action OnInstallation = null, string mobileControllerPrefabName = "", Action<GameObject, GameObject, GameObject> extraSetupActionPlayer = null, Action<GameObject, GameObject> extraSetupActionAI = null)
        {
            this.characterType = characterType;
            this.selected = selected;
            this.systemName = systemName;
            this.displayName = displayName;
            this.prefabName = prefabName;
            this.OnInstallation = OnInstallation;
            this.mobileControllerPrefabName = mobileControllerPrefabName;

            if (extraSetupActionPlayer != null)
                this.extraSetupActionPlayer = extraSetupActionPlayer;
            if (extraSetupActionAI != null)
                this.extraSetupActionAI = extraSetupActionAI;

            if (!string.IsNullOrEmpty(welcomeEditorShowKey))
                this.welcomeEditorShowKey = welcomeEditorShowKey;
            else
                this.welcomeEditorShowKey = FSSystemsSetup.welcomeWindowOpenKey;
        }

    }
}
#endif