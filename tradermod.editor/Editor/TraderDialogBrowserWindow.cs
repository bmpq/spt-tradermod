using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using tarkin.tradermod.shared;
using EFT.Dialogs;
using tarkin.tradermod.eft;

namespace tarkin.tradermod.Editor
{
    public class TraderDialogWindow : EditorWindow
    {
        private class DialogCacheEntry
        {
            public string Id;
            public string Text;
            public string LipsyncKey;
        }

        private TraderScene _targetScript;
        private SerializedObject _serializedObject;

        private List<DialogCacheEntry> _allDialogs = new List<DialogCacheEntry>();
        private string _cachedTraderId = "";

        private string _searchFilter = "";
        private Vector2 _scrollPosAvailable;
        private Vector2 _scrollPosSelected;
        private int _selectedTab = 0;
        private readonly string[] _tabs = { 
            "Greetings", 
            "Goodbyes", 
            "Chatter", 
            "QuestAvailable", 
            "GreetingsWhileWork", 
            "NoJob", 
            "TradeStart",
            "Handover", 
            "Dunno"
        };

        [MenuItem("Tools/TraderMod/Trader Dialog Browser")]
        public static void ShowWindow()
        {
            GetWindow<TraderDialogWindow>("Trader Dialogs");
        }

        private void OnEnable()
        {
            _targetScript = FindFirstObjectByType<TraderScene>();
            if (_targetScript != null)
            {
                _serializedObject = new SerializedObject(_targetScript);
                ReloadData(_targetScript.GetType().GetField("TraderId").GetValue(_targetScript) as string);
            }
        }

        private void OnGUI()
        {
            DrawTargetSelection();

            _serializedObject.Update();

            SerializedProperty traderIdProp = _serializedObject.FindProperty("TraderId");
            if (traderIdProp.stringValue != _cachedTraderId)
            {
                ReloadData(traderIdProp.stringValue);
            }

            GUILayout.Space(10);
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);

            SerializedProperty activeListProp = GetActiveListProperty();

            GUILayout.Space(10);
            DrawContentArea(activeListProp);

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawTargetSelection()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Target Script:", GUILayout.Width(80));

            EditorGUI.BeginChangeCheck();
            _targetScript = (TraderScene)EditorGUILayout.ObjectField(_targetScript, typeof(TraderScene), true);
            if (EditorGUI.EndChangeCheck() && _targetScript != null)
            {
                _serializedObject = new SerializedObject(_targetScript);
                SerializedProperty traderIdProp = _serializedObject.FindProperty("TraderId");
                ReloadData(traderIdProp.stringValue);
            }

            if (GUILayout.Button("Refresh Data", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _cachedTraderId = null;
                if (_serializedObject != null)
                    ReloadData(_serializedObject.FindProperty("TraderId").stringValue);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawContentArea(SerializedProperty activeList)
        {
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(position.width * 0.6f));

            GUILayout.Label($"Available Dialogs (Trader: {_cachedTraderId})", EditorStyles.boldLabel);

            GUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label("Search:", GUILayout.Width(50));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);
            if (GUILayout.Button("X", GUILayout.Width(20))) _searchFilter = "";
            GUILayout.EndHorizontal();

            _scrollPosAvailable = GUILayout.BeginScrollView(_scrollPosAvailable);

            if (_allDialogs.Count == 0)
            {
                GUILayout.Label("No dialogs found for this TraderId.");
            }
            else
            {
                foreach (var entry in _allDialogs)
                {
                    if (!string.IsNullOrEmpty(_searchFilter))
                    {
                        if (!entry.Text.ToLower().Contains(_searchFilter.ToLower()) &&
                            !entry.Id.Contains(_searchFilter))
                            continue;
                    }

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.BeginHorizontal();

                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Toolbar Plus"), GUILayout.Width(25), GUILayout.Height(20)))
                    {
                        AddIdToList(activeList, entry.Id);
                    }
                    EditorGUILayout.SelectableLabel(entry.Id, EditorStyles.textArea, GUILayout.Width(185), GUILayout.Height(20));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginVertical();
                    {
                        var style = EditorStyles.label;
                        style.wordWrap = true;
                        var content = new GUIContent(entry.Text);

                        GUILayout.Label(content, style, GUILayout.ExpandHeight(true));
                    }
                    {
                        GUILayout.Label(entry.LipsyncKey, EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();

                    EditorGUILayout.EndVertical();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label($"Current List: {_tabs[_selectedTab]} ({activeList.arraySize})", EditorStyles.boldLabel);

                _scrollPosSelected = GUILayout.BeginScrollView(_scrollPosSelected);

                for (int i = 0; i < activeList.arraySize; i++)
                {
                    SerializedProperty element = activeList.GetArrayElementAtIndex(i);
                    string currentId = element.stringValue;

                    var cached = _allDialogs.FirstOrDefault(x => x.Id == currentId);
                    string displayText = cached != null ? cached.Text : "Unknown ID";

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    GUILayout.BeginVertical();
                    GUILayout.Label(displayText, EditorStyles.wordWrappedLabel);
                    GUILayout.Label(currentId, EditorStyles.wordWrappedMiniLabel);
                    GUILayout.EndVertical();

                    if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(35)))
                    {
                        activeList.DeleteArrayElementAtIndex(i);
                        break;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private SerializedProperty GetActiveListProperty()
        {
            switch (_selectedTab)
            {
                case 0: return _serializedObject.FindProperty("DialogCombinedAnimGreetings");
                case 1: return _serializedObject.FindProperty("DialogCombinedAnimGoodbye");
                case 2: return _serializedObject.FindProperty("DialogCombinedAnimChatter");
                case 3: return _serializedObject.FindProperty("DialogCombinedAnimQuestAvailable");
                case 4: return _serializedObject.FindProperty("DialogCombinedAnimGreetingsWhileWork");
                case 5: return _serializedObject.FindProperty("DialogCombinedAnimNoJob");
                case 6: return _serializedObject.FindProperty("DialogCombinedAnimTradeStart");
                case 7: return _serializedObject.FindProperty("DialogCombinedAnimHandover");
                case 8: return _serializedObject.FindProperty("DialogCombinedAnimDunno");
                default: return null;
            }
        }

        private void AddIdToList(SerializedProperty list, string id)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).stringValue == id) return;
            }

            int index = list.arraySize;
            list.arraySize++;
            list.GetArrayElementAtIndex(index).stringValue = id;
        }

        private void ReloadData(string traderId)
        {
            _cachedTraderId = traderId;
            _allDialogs.Clear();

            TextAsset jsonAsset = Resources.Load<TextAsset>("dialogue");
            if (jsonAsset == null) return;

            TraderDialogsDTO data = null;
            try
            {
                data = SafeDeserializer<TraderDialogsDTO>.Deserialize(jsonAsset.text);
            }
            catch
            {
                Debug.LogError("Error parsing dialogue JSON");
                return;
            }

            if (data == null || data.Elements == null) return;

            foreach (var template in data.Elements)
            {
                string localeKey = "ru";

                var localization = template.LocalizationDictionary != null && template.LocalizationDictionary.ContainsKey(localeKey)
                    ? template.LocalizationDictionary[localeKey]
                    : null;

                if (template.Lines == null) continue;

                foreach (var line in template.Lines)
                {
                    string lineTraderId = line.TraderId?.ToString();
                    if (string.IsNullOrEmpty(_cachedTraderId) || lineTraderId != _cachedTraderId)
                        continue;

                    string display = "";
                    string subKey = "";
                    string lipsyncKey = "";

                    if (line.AnimationData != null)
                    {
                        foreach (var lipsync in line.AnimationData.lipSyncKeysWithParams)
                        {
                            lipsyncKey += lipsync.Key + " ";
                        }

                        foreach (var subtitle in line.AnimationData.subtitleKeysWithParams)
                        {
                            subKey = subtitle.Key;
                            if (localization == null || !localization.ContainsKey(subKey))
                                continue;

                            display += localization[subKey] + " ";
                        }
                    }
                    else
                    {
                        display = "[No Subtitle Data]";
                    }

                    if (lipsyncKey == "")
                        continue;

                    _allDialogs.Add(new DialogCacheEntry
                    {
                        Id = line.Id.ToString(),
                        Text = display,
                        LipsyncKey = lipsyncKey
                    });
                }
            }
        }
    }
}