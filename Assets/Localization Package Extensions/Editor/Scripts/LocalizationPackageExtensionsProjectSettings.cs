using System.Collections.Generic;
using LocalizationPackageExtensions;
using UnityEditor;
using UnityEngine.UIElements;

namespace LocalizationPackageExtensionsEditor
{
    [FilePath("ProjectSettings/LocalizationPackageExtensinosSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class LocalizationPackageExtensionsProjectSettings : ScriptableSingleton<LocalizationPackageExtensionsProjectSettings>
    {
        public string googleAPIKey;
        public string audioClipReaderTargetFolder = "Assets";

        public void Save()
        {
            Save(true);
        }
    }
    
    class LocalizationPackageExtensinosSettingsProvider : SettingsProvider
    {
        SerializedObject so;
        SerializedProperty googleAPIKey;
        LocalizationPackageExtensionsProjectSettings settings => LocalizationPackageExtensionsProjectSettings.instance;
        LocalizationPackageExtensinosSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords) { }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new LocalizationPackageExtensinosSettingsProvider("Project/Localization/Extensions", SettingsScope.Project)
            {
                label = "Localization Package Extensions"
            };
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);
            so = new SerializedObject(settings);
            googleAPIKey = so.FindProperty(nameof(settings.googleAPIKey));
        }

        public override void OnGUI(string searchContext)
        {
            so.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(googleAPIKey);
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
                settings.Save();
            }
        }
    }
}