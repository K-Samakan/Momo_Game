using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UIElements;

namespace LocalizationPackageExtensionsEditor
{
    public enum TargetCell
    {
        Empty,
        Filled,
        All
    }
    public class GoogleTranslationWindow : EditorWindow
    {
        DropdownField baseLocaleDropdown;
        DropdownField stringTableDropdown;
        MaskField targetLocaleMaskField;
        DropdownField targetCellField;
        Toggle overrideSmartStateToggle;
        Button localizationButton;


        protected Locale baseLocale;
        protected StringTable baseTable;
        protected ReadOnlyCollection<Locale> allLocale => LocalizationEditorSettings.GetLocales();
        protected List<Locale> selectedLocales;
        protected StringTableCollection selectedTableCollection;
        protected List<StringTableCollection> allTables;

        public TargetCell targetCell;

        protected string path_icon => "Assets/Localization Package Extensions/Editor/Icons/Google Translator Icon.png";
        protected string path_icon_package => "Package/Localization Package Extensions/Editor/Icons/Google Translator Icon.png";


        Locale GetLocale(LocaleIdentifier identifier) => allLocale.FirstOrDefault(x => x.Identifier == identifier);

        [MenuItem("Window/Asset Management/Localization Package Extensions/Google Translation")]
        static void OpenWindow()
        {
            var wnd = GetWindow<GoogleTranslationWindow>();
            wnd.Show();
        }


        protected virtual void OnEnable()
        {
            if (allLocale.Count == 0) return;
            baseLocale = allLocale.FirstOrDefault();
            allTables = LocalizationEditorSettings.GetStringTableCollections().ToList();
            if (allTables.Count == 0) return;
            selectedTableCollection = allTables[0];
            baseTable = LocalizationEditorSettings.GetStringTableCollection(selectedTableCollection.TableCollectionNameReference).StringTables
                .FirstOrDefault(x => x.LocaleIdentifier == baseLocale.Identifier);
            selectedLocales = allLocale.Where(x => x != baseLocale).ToList();
        }

        void CreateGUI()
        {
            rootVisualElement.style.paddingBottom = 10;
            rootVisualElement.style.paddingLeft = 10;
            rootVisualElement.style.paddingRight = 10;
            rootVisualElement.style.paddingTop = 10;
            if (allLocale.Count == 0)
            {
                var label = new Label("No Available Locales in the LocalizationSettings.\n" +
                                      "Please open this window after creating a Locale and a StringTable.\n\n" +
                                      "If this message appears even though there is no problem, please click the button below.");
                label.style.whiteSpace = WhiteSpace.Normal;
                rootVisualElement.Add(label);

                rootVisualElement.Add(new Button(Refresh) { text = "Refresh" });
                return;
            }

            if (allTables.Count == 0)
            {
                var label = new Label("No Available StringTables in the LocalizationSettings.\n" +
                                      "Please open this window after creating a StringTable.\n\n" +
                                      "If this message appears even though there is no problem, please click the button below.");
                label.style.whiteSpace = WhiteSpace.Normal;
                rootVisualElement.Add(label);

                rootVisualElement.Add(new Button(Refresh) { text = "Refresh" });
                return;
            }

            stringTableDropdown = new DropdownField("String Table Collection");
            stringTableDropdown.choices = allTables.Select(x => x.TableCollectionName).ToList();
            stringTableDropdown.SetValueWithoutNotify(selectedTableCollection.TableCollectionName);
            stringTableDropdown.RegisterValueChangedCallback(evt => { selectedTableCollection = allTables.Find(x => x.TableCollectionName == evt.newValue); });
            rootVisualElement.Add(stringTableDropdown);

            rootVisualElement.Add(new VisualElement() { style = { height = 20 } });

            baseLocaleDropdown = new DropdownField("Base Locale", allLocale.Select(x => x.LocaleName)
                .ToList(), allLocale.IndexOf(baseLocale), s => s);
            baseLocaleDropdown.RegisterValueChangedCallback(evt =>
            {
                baseLocale = GetLocale(evt.newValue);
                baseTable = LocalizationEditorSettings.GetStringTableCollection(selectedTableCollection.TableCollectionNameReference).StringTables
                    .FirstOrDefault(x => x.LocaleIdentifier == baseLocale.Identifier);
                selectedLocales.Remove(baseLocale);

                if (targetLocaleMaskField.value is not (~0 or -1 or 0))
                {
                    var choices = allLocale.Where(x => x != baseLocale).Select(x => x.LocaleName).ToList();
                    targetLocaleMaskField.choices = choices;

                    targetLocaleMaskField.value = Mask.GetMaskValue(choices, selectedLocales.Select(x => x.LocaleName).ToList());
                }
            });
            rootVisualElement.Add(baseLocaleDropdown);


            targetLocaleMaskField =
                new MaskField("Target Locales", allLocale.Where(x => x != baseLocale)
                    .Select(x => x.LocaleName).ToList(), -1, s => s);
            targetLocaleMaskField.RegisterValueChangedCallback(evt =>
            {
                Mask.GetSelectedElements(allLocale.Where(x => x != baseLocale).ToList(), selectedLocales, evt.newValue);
            });
            rootVisualElement.Add(targetLocaleMaskField);

            targetCellField = new DropdownField("Target Cells",
                Enum.GetNames(typeof(TargetCell)).ToList(), 0);
            targetCellField.RegisterValueChangedCallback(evt => { targetCell = Enum.Parse<TargetCell>(evt.newValue); });
            rootVisualElement.Add(targetCellField);

            overrideSmartStateToggle = new Toggle("Override Smart State");
            overrideSmartStateToggle.value = true;
            rootVisualElement.Add(overrideSmartStateToggle);

            rootVisualElement.Add(new VisualElement() { style = { height = 20 } });

            rootVisualElement.Add(new VisualElement() { style = { height = 50 } });

            localizationButton = new Button(Translate);

            var image = AssetDatabase.LoadAssetAtPath<Texture2D>(path_icon);
            if(image == null) image = AssetDatabase.LoadAssetAtPath<Texture2D>(path_icon_package);
            var icon = new Image
            {
                image = image,
                style =
                {
                    height = 20,
                    width = 20
                }
            };
            localizationButton.Add(icon);
            localizationButton.Add(new Label("Translate"));
            localizationButton.style.flexDirection = FlexDirection.Row;
            localizationButton.style.justifyContent = Justify.Center;
            localizationButton.style.alignItems = Align.Center;
            localizationButton.style.height = 30;
            rootVisualElement.Add(localizationButton);
        }


        protected void Translate()
        {
            if (string.IsNullOrWhiteSpace(LocalizationPackageExtensionsProjectSettings.instance.googleAPIKey))
            {
                Debug.LogError("Google API Key is not assigned.");
                EditorUtility.DisplayDialog("Failed to Translate",
                    "Google API Key is not assigned.\nYou can see the details of how to get an API Key in the Document.pdf.", "OK");
                return;
            }

            if (selectedTableCollection.StringTables.Count == 0)
            {
                Debug.LogWarning($"Any StringTables are not selected.");
                return;
            }

            if (selectedLocales.Count == 0)
            {
                Debug.LogWarning($"Any Locales are not selected.");
                return;
            }

            try
            {
                Debug.Log($"Translation Start | from [{baseLocale.name}] to [{string.Join(", ", selectedLocales.Select(x => x.LocaleName))}]");
                foreach (var stringTable in selectedTableCollection.StringTables)
                {
                    EditorUtility.SetDirty(stringTable);
                }

                EditorUtility.SetDirty(selectedTableCollection);


                var idx = 0f;
                var requestCount = 0;
                var completeCount = 0;
                var entryCount = baseTable.Count;
                var totalCount = entryCount * (selectedTableCollection.StringTables.Count - 1);
                foreach (var stringTable in selectedTableCollection.StringTables)
                {
                    if (stringTable.LocaleIdentifier == baseLocale.Identifier) continue;
                    var locale = GetLocale(stringTable.LocaleIdentifier);
                    if (!selectedLocales.Contains(locale)) continue;

                    foreach (var baseEntry in baseTable.Values)
                    {
                        var key = baseEntry.Key;
                        var entry = stringTable.GetEntry(key);

                        if (string.IsNullOrWhiteSpace(baseEntry.Value)) continue;
                        switch (targetCell)
                        {
                            case TargetCell.Empty:
                                if (entry != null && !string.IsNullOrWhiteSpace(entry.Value)) continue;
                                break;
                            case TargetCell.Filled:
                                if (entry != null && string.IsNullOrWhiteSpace(entry.Value)) continue;
                                break;
                            case TargetCell.All:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        requestCount++;
                        EditorUtility.DisplayProgressBar("Translating...", $"{locale.LocaleName} : {key}",
                            idx / totalCount);


                        RequestTranslation(locale, baseEntry.Value,
                            (isSuccess, text) =>
                            {
                                if (isSuccess)
                                {
                                    if (entry == null) entry = stringTable.AddEntry(key, text);
                                    else entry.Value = text;

                                    if (overrideSmartStateToggle.value) entry.IsSmart = baseEntry.IsSmart;

                                    completeCount++;
                                    if (completeCount == requestCount)
                                    {
                                        Debug.Log($"Translation Complete for the StringTable of [{stringTable.name}] | Key: {key} | Id: {entry.KeyId}");
                                        EditorUtility.ClearProgressBar();


                                        if (HasOpenInstances<LocalizationTablesWindow>())
                                        {
                                            var wnd = GetWindow<LocalizationTablesWindow>();
                                            wnd.Close();
                                            LocalizationTablesWindow.ShowWindow();
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.LogError($"Fail to Translate the StringTable of [{stringTable.name}]");
                                }
                            });
                        idx++;
                    }
                }
                if(requestCount == 0) Debug.Log($"Translation End. Nothing to translate.");
                AssetDatabase.SaveAssets();
            }
            catch (Exception)
            {
                if (HasOpenInstances<LocalizationTablesWindow>())
                {
                    var wnd = GetWindow<LocalizationTablesWindow>();
                    wnd.Close();
                    LocalizationTablesWindow.ShowWindow();
                }

                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        protected void RequestTranslation(Locale targetLocale, string text, Action<bool, string> result)
        {
            GoogleTranslatorAPI.Request(baseLocale.Identifier, targetLocale.Identifier, text, result);
        }

        void Refresh()
        {
            Close();
            OpenWindow();
        }
    }
}