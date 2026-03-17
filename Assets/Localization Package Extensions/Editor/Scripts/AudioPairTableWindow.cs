using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using LocalizationPackageExtensions;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UIElements;

namespace LocalizationPackageExtensionsEditor
{
    public class AudioPairTableWindow : EditorWindow
    {
        DropdownField stringTableDropdown;
        Toggle highlightNullToggle;
        Button syncButton;
        Button readButton;
        VisualElement tableGUI;
        MultiColumnListView view;

        StringTableCollection table;
        AudioPairTable audioPairTable;
        ReadOnlyCollection<Locale> allLocale => LocalizationEditorSettings.GetLocales();
        ReadOnlyCollection<StringTableCollection> allTables => LocalizationEditorSettings.GetStringTableCollections();
        Action<bool> onHighlight;

        [MenuItem("Window/Asset Management/Localization Package Extensions/Audio Pair Table Window")]
        static void ShowWindow()
        {
            var window = GetWindow<AudioPairTableWindow>();
            window.Show();
        }

        void CreateGUI()
        {
            stringTableDropdown = new DropdownField(allTables.Select(x => x.TableCollectionName).ToList(), 0);
            stringTableDropdown.label = "String Table Collection";
            stringTableDropdown.RegisterValueChangedCallback(SetTable);
            rootVisualElement.Add(stringTableDropdown);
            
            
            highlightNullToggle = new Toggle("Highlight Null Field");
            highlightNullToggle.RegisterValueChangedCallback(evt => onHighlight?.Invoke(evt.newValue));
            rootVisualElement.Add(highlightNullToggle);
            
            var horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
            rootVisualElement.Add(horizontal);

            syncButton = new Button(Sync);
            syncButton.text = "Sync";
            syncButton.tooltip = "Updates the entries and text data in this AudioPairTable to match the connected StringTable";
            syncButton.style.width = 100;
            horizontal.Add(syncButton);

            readButton = new Button(() => AudioClipReaderWindow.Open(audioPairTable));
            readButton.text = "Read and Apply AudioClips";
            readButton.tooltip = "Reads AudioClips under specific directory with given conditions. This is useful when you have to set lots of clips at once.";
            readButton.style.width = 250;
            horizontal.Add(readButton);
            
            rootVisualElement.Add(new VisualElement() { style = { height = 20 } });

            tableGUI = new VisualElement();
            tableGUI.style.flexGrow = 1;
            rootVisualElement.Add(tableGUI);
            
            table = allTables.FirstOrDefault();
            
            FindConnectedAudioTable();
            RefreshTableGUI();
        }

        void Sync()
        {
            var editor = Editor.CreateEditor(audioPairTable) as AudioPairTableEditor;
            editor.Sync();
            DestroyImmediate(editor);
            RefreshTableGUI();
        }

        void SetTable(ChangeEvent<string> evt)
        {
            table = allTables.FirstOrDefault(x => x.TableCollectionName == evt.newValue);
            FindConnectedAudioTable();
            RefreshTableGUI();
        }

        void FindConnectedAudioTable()
        {
            var guids = AssetDatabase.FindAssets("t:AudioPairTable");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var apTable = AssetDatabase.LoadAssetAtPath<AudioPairTable>(path);
                if (apTable.tableCollectionName == table.TableCollectionName)
                {
                    audioPairTable = apTable;
                    return;
                }
            }

            audioPairTable = null;
        }

        public void RefreshTableGUI()
        {
            onHighlight = null;
            if (tableGUI.childCount > 0) tableGUI.Clear();

            if (table == null) return;

            if (audioPairTable == null)
            {
                highlightNullToggle.SetEnabled(false);
                syncButton.SetEnabled(false);
                readButton.SetEnabled(false);
                
                var label = new Label($"There is no AudioPairTable paired with this StringTableCollection [{table.TableCollectionName}]\n" +
                                      $"Create a new AudioPairTable asset by clicking the button below.");
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                tableGUI.Add(label);

                var createAssetButton = new Button(CreateAudioPairTableAsset);
                createAssetButton.style.height = 30;
                createAssetButton.style.width = 200;
                createAssetButton.style.marginTop = 40;
                createAssetButton.style.alignSelf = Align.Center;
                createAssetButton.text = "Create New AudioPairTable Asset";
                tableGUI.Add(createAssetButton);
                return;
            }
            highlightNullToggle.SetEnabled(true);
            syncButton.SetEnabled(true);
            readButton.SetEnabled(true);

            var columns = new Columns();
            var keyColumn = new Column()
            {
                makeHeader = key_column_make_header,
                makeCell = key_coumn_make_cell,
                bindCell = key_column_bind_cell,
                width = 200,
                resizable = false
            };

            columns.Add(keyColumn);

            foreach (var stringTable in table.StringTables.OrderBy(x => allLocale.IndexOf(LocalizationEditorSettings.GetLocale(x.LocaleIdentifier))))
            {
                var column = new Column()
                {
                    makeHeader = column_make_header,
                    makeCell = column_make_cell,
                    bindCell = column_bind_cell,
                    width = 200,
                    resizable = false,
                };
                

                columns.Add(column);


                continue;
                VisualElement column_make_header()
                {
                    return new Label($"{stringTable.LocaleIdentifier.CultureInfo.DisplayName} ({stringTable.LocaleIdentifier.Code})") { style = { unityTextAlign = TextAnchor.LowerCenter } };
                }
                
                VisualElement column_make_cell()
                {
                    return new VisualElement()
                    {
                        style = { flexGrow = 1 }
                    };
                    
                }

                void column_bind_cell(VisualElement ve, int i)
                {
                    ve.Clear();
                    
                    if(i > stringTable.SharedData.Entries.Count - 1) return;
                    
                    var textField = new TextField()
                    {
                        isReadOnly = true,
                        multiline = true,
                        style = { flexGrow = 1, whiteSpace = WhiteSpace.Normal}
                    };
                    ve.Add(textField);
                    
                    var id = stringTable.SharedData.Entries[i].Id;
                    if (stringTable.TryGetValue(id, out var value))
                    {
                        textField.value = value.Value;
                    }


                    var horizontal = new VisualElement() { style = { flexDirection = FlexDirection.Row } };
                    ve.Add(horizontal);
                    var clipField = new ObjectField()
                    {
                        objectType = typeof(AudioClip),
                        style = { flexGrow = 1}
                    };
                    
                    clipField.value = audioPairTable.GetAudioClip(value.KeyId, stringTable.LocaleIdentifier);
                    if (highlightNullToggle.value && clipField.value == null) clipField.Q<Label>().style.color = Color.red;
                    onHighlight += _highlight;


                    horizontal.Add(clipField);

                    var playButton = new Button(_play);
                    playButton.text = "▶";
                    playButton.style.width = 25;
                    horizontal.Add(playButton);
                    
                    var stopButton = new Button(_stop);
                    stopButton.text = "■";
                    stopButton.style.width = 25;
                    horizontal.Add(stopButton);
                    
                    clipField.RegisterValueChangedCallback(evt => _on_clip_changed(evt.newValue as AudioClip));
                    playButton.SetEnabled(clipField.value != null);
                    stopButton.SetEnabled(clipField.value != null);
                    return;
                    
                    
                    void _on_clip_changed(AudioClip clip)
                    {
                        if (clip == null)
                        {
                            playButton.SetEnabled(false);
                            stopButton.SetEnabled(false);
                            audioPairTable.RemovePair(value.KeyId);
                        }
                        else
                        {
                            playButton.SetEnabled(true);
                            stopButton.SetEnabled(true);
                            audioPairTable.AddPair(new AudioStringPair(value.Key, value.KeyId, stringTable.LocaleIdentifier, clip));
                        }
                        
                        _highlight(highlightNullToggle.value);
                        EditorUtility.SetDirty(audioPairTable);
                        AssetDatabase.SaveAssetIfDirty(audioPairTable);
                    }

                    void _highlight(bool highlight)
                    {
                        if (highlight && clipField.value == null)
                        {
                            clipField.Q<Label>().style.color = Color.red;
                        }
                        else
                        {
                            clipField.Q<Label>().style.color = Color.white;
                        }
                    }
                    
                    void _play()
                    {
                        if(clipField.value == null) return;
                        _stop();
                        var unityEditorAssembly = typeof(AudioImporter).Assembly;
 
                        var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                        var method = audioUtilClass.GetMethod(
                            "PlayPreviewClip",
                            BindingFlags.Static | BindingFlags.Public,
                            null,
                            new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                            null
                        );
 
                        method.Invoke(
                            null,
                            new object[] { clipField.value as AudioClip, 0, false }
                        );
                    }

                    void _stop()
                    {
                        var unityEditorAssembly = typeof(AudioImporter).Assembly;
 
                        var audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                        var method = audioUtilClass.GetMethod(
                            "StopAllPreviewClips",
                            BindingFlags.Static | BindingFlags.Public,
                            null,
                            new Type[] { },
                            null
                        );
                        
                        method.Invoke(
                            null,
                            new object[] { }
                        );
                    }
                }
            }

            view = new MultiColumnListView(columns);
            view.itemsSource = table.SharedData.Entries;
            view.fixedItemHeight = 100;
            view.StretchToParentSize();
            tableGUI.Add(view);


            return;

            
            VisualElement key_column_make_header()
            {
                return new Label("Key"){style = { unityTextAlign = TextAnchor.LowerCenter}};
            }
            
            VisualElement key_coumn_make_cell()
            {
                return new TextField()
                {
                    isReadOnly = true,
                    multiline = true,
                    style = { flexGrow = 1, whiteSpace = WhiteSpace.Normal}
                };
            }

            void key_column_bind_cell(VisualElement ve, int i)
            {
                if(i > table.SharedData.Entries.Count - 1) return;
                
                var textField = ve as TextField;
                textField.value = table.SharedData.Entries[i].Key;
            }
        }

        void CreateAudioPairTableAsset()
        {
            var path = EditorUtility.SaveFilePanel("Select Save Path...", "Assets", $"{table.TableCollectionName} AudioPairTable", "asset");
            if(string.IsNullOrWhiteSpace(path)) return;

            var asset = CreateInstance<AudioPairTable>();
            asset.tableCollectionName = table.TableCollectionName;


            var relativePath = path.Substring(Application.dataPath.Length - "Assets".Length);
            AssetDatabase.CreateAsset(asset, relativePath);
            audioPairTable = asset;
            RefreshTableGUI();
        }
    }
}