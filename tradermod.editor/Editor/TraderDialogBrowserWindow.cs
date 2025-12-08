using System;
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

        private ETraderDialogType _selectedTabType = ETraderDialogType.Greetings;

        [MenuItem("Tools/TraderMod/Trader Dialog Browser")]
        public static void ShowWindow()
        {
            GetWindow<TraderDialogWindow>("Trader Dialogs");
        }

        private void OnGUI()
        {
            DrawTargetSelection();

            if (_targetScript == null) return;

            if (_serializedObject == null || _serializedObject.targetObject == null)
                _serializedObject = new SerializedObject(_targetScript);

            _serializedObject.Update();

            SerializedProperty traderIdProp = _serializedObject.FindProperty("traderId");
            if (traderIdProp.stringValue != _cachedTraderId)
            {
                ReloadData(traderIdProp.stringValue);
            }

            GUILayout.Space(10);
            DrawEnumToolbar();

            if (!_targetScript.Dialogs.ContainsKey(_selectedTabType))
            {
                _targetScript.Dialogs[_selectedTabType] = new List<string>();
            }
            List<string> activeList = _targetScript.Dialogs[_selectedTabType];

            GUILayout.Space(10);
            DrawContentArea(activeList);

            _serializedObject.ApplyModifiedProperties();
        }

        private void DrawEnumToolbar()
        {
            string[] names = Enum.GetNames(typeof(ETraderDialogType));
            int currentIndex = (int)_selectedTabType;
            int newIndex = GUILayout.SelectionGrid(currentIndex, names, 5);
            _selectedTabType = (ETraderDialogType)newIndex;
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
                SerializedProperty traderIdProp = _serializedObject.FindProperty("traderId");
                ReloadData(traderIdProp.stringValue);
            }

            if (GUILayout.Button("Refresh Data", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                _targetScript = FindFirstObjectByType<TraderScene>();
                if (_targetScript != null)
                {
                    _serializedObject = new SerializedObject(_targetScript);
                    _cachedTraderId = null;
                    ReloadData(_serializedObject.FindProperty("traderId").stringValue);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void DrawContentArea(List<string> activeList)
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
                GUILayout.Label("No dialogs found.");
            }
            else
            {
                foreach (var entry in _allDialogs)
                {
                    if (!string.IsNullOrEmpty(_searchFilter))
                    {
                        if (!entry.Text.ToLower().Contains(_searchFilter.ToLower()) &&
                            !entry.Id.Contains(_searchFilter) &&
                            !entry.LipsyncKey.ToLower().Contains(_searchFilter.ToLower()))
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

                    GUILayout.Label(entry.Text, EditorStyles.wordWrappedLabel);
                    GUILayout.Label(entry.LipsyncKey, EditorStyles.miniLabel);
                    EditorGUILayout.EndVertical();
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            {
                GUILayout.Label($"Current List: {_selectedTabType} ({activeList.Count})", EditorStyles.boldLabel);

                _scrollPosSelected = GUILayout.BeginScrollView(_scrollPosSelected);

                for (int i = 0; i < activeList.Count; i++)
                {
                    string currentId = activeList[i];
                    var cached = _allDialogs.FirstOrDefault(x => x.Id == currentId);
                    string displayText = cached != null ? cached.Text : "Unknown ID";

                    GUILayout.BeginHorizontal(EditorStyles.helpBox);

                    GUILayout.BeginVertical();
                    GUILayout.Label(displayText, EditorStyles.wordWrappedLabel);
                    GUILayout.Label(currentId, EditorStyles.wordWrappedMiniLabel);
                    GUILayout.EndVertical();

                    if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(35)))
                    {
                        RemoveIdFromList(activeList, i);
                        break;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void AddIdToList(List<string> list, string id)
        {
            if (list.Contains(id)) return;

            Undo.RecordObject(_targetScript, "Add Dialog ID");

            list.Add(id);

            EditorUtility.SetDirty(_targetScript);
        }

        private void RemoveIdFromList(List<string> list, int index)
        {
            Undo.RecordObject(_targetScript, "Remove Dialog ID");
            list.RemoveAt(index);
            EditorUtility.SetDirty(_targetScript);
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