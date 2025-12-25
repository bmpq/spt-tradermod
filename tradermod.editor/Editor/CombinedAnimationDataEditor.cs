using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using EFT.AnimationSequencePlayer;

public class CombinedAnimationEditorWindow : EditorWindow
{
    private TextAsset _selectedAsset;
    private CombinedAnimationData _currentData;
    private Vector2 _scrollPosition;

    private bool _isValidJson = true;
    private string _errorMessage = "";

    private bool _needsReimport = false;

    private TextAsset _localeAsset;
    private LipSyncDictionary _lipSyncDictionary;
    private AnimationDictionary _animationDictionary;
    private SecondaryAnimationDictionary _secondaryAnimationDictionary;

    private List<string> _cachedAnimKeys = new List<string>();
    private List<string> _cachedSecondaryKeys = new List<string>();
    private List<string> _cachedLipSyncKeys = new List<string>();
    private Dictionary<string, string> _cachedSubtitles = new Dictionary<string, string>();

    private bool _showAnimations = true;
    private bool _showSecondary = false;
    private bool _showLipSyncs = false;
    private bool _showSubtitles = false;

    [MenuItem("Tools/TraderMod/Combined Animation Editor")]
    public static void ShowWindow()
    {
        GetWindow<CombinedAnimationEditorWindow>("Anim Data Editor");
    }

    private void OnEnable()
    {
        if (Selection.activeObject is TextAsset asset)
        {
            _selectedAsset = asset;
            ParseData();
        }
        RefreshReferenceCaches();
    }

    private void OnDisable()
    {
        CheckAndImportPrevious();
    }

    private void OnSelectionChange()
    {
        CheckAndImportPrevious();

        if (Selection.activeObject is TextAsset asset)
        {
            _selectedAsset = asset;
            ParseData();
            Repaint();
        }
    }

    private void CheckAndImportPrevious()
    {
        if (_needsReimport && _selectedAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(_selectedAsset);
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.ImportAsset(path);
            }
        }
        _needsReimport = false;
    }

    private void ParseData()
    {
        _currentData = null;
        _isValidJson = true;
        _errorMessage = "";
        _needsReimport = false;

        if (_selectedAsset == null) return;

        try
        {
            _currentData = JsonConvert.DeserializeObject<CombinedAnimationData>(_selectedAsset.text);

            if (_currentData == null) _currentData = CombinedAnimationData.Default;

            if (_currentData.animKeysWithParams == null) _currentData.animKeysWithParams = new List<AnimationParams>();
            if (_currentData.secondaryAnimKeysWithParams == null) _currentData.secondaryAnimKeysWithParams = new List<AnimationParams>();
            if (_currentData.lipSyncKeysWithParams == null) _currentData.lipSyncKeysWithParams = new List<LipSyncParams>();
            if (_currentData.subtitleKeysWithParams == null) _currentData.subtitleKeysWithParams = new List<SubtitleParams>();
            if (_currentData.mediaData == null) _currentData.mediaData = new MediaData();
        }
        catch (System.Exception ex)
        {
            _isValidJson = false;
            _errorMessage = ex.Message;
            _currentData = null;
        }
    }

    private void RefreshReferenceCaches()
    {
        if (_lipSyncDictionary == null) _lipSyncDictionary = FindFirstObjectByType<LipSyncDictionary>();
        if (_animationDictionary == null) _animationDictionary = FindFirstObjectByType<AnimationDictionary>();
        if (_secondaryAnimationDictionary == null) _secondaryAnimationDictionary = FindFirstObjectByType<SecondaryAnimationDictionary>();

        _cachedAnimKeys.Clear();
        if (_animationDictionary != null) _cachedAnimKeys = _animationDictionary.GetAllKeys();

        _cachedSecondaryKeys.Clear();
        if (_secondaryAnimationDictionary != null) _cachedSecondaryKeys = _secondaryAnimationDictionary.GetAllKeys();

        _cachedLipSyncKeys.Clear();
        if (_lipSyncDictionary != null) _cachedLipSyncKeys = _lipSyncDictionary.GetAllKeys();

        ParseLocale();
    }

    private void ParseLocale()
    {
        _cachedSubtitles.Clear();
        if (_localeAsset != null)
        {
            try
            {
                _cachedSubtitles = JsonConvert.DeserializeObject<Dictionary<string, string>>(_localeAsset.text);
                if (_cachedSubtitles == null) _cachedSubtitles = new Dictionary<string, string>();
            }
            catch
            {
                Debug.LogWarning("Could not parse Locale dictionary.");
            }
        }
    }

    private void SaveToDisk()
    {
        if (_selectedAsset == null || _currentData == null) return;

        string path = AssetDatabase.GetAssetPath(_selectedAsset);
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            string json = JsonConvert.SerializeObject(_currentData, Formatting.Indented);
            File.WriteAllText(path, json);

            EditorUtility.SetDirty(_selectedAsset);
            _needsReimport = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to save JSON: {ex.Message}");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUI.BeginChangeCheck();
        TextAsset newAsset = (TextAsset)EditorGUILayout.ObjectField(_selectedAsset, typeof(TextAsset), false, GUILayout.ExpandWidth(true));
        if (EditorGUI.EndChangeCheck())
        {
            CheckAndImportPrevious();
            _selectedAsset = newAsset;
            ParseData();
        }

        if (_needsReimport)
        {
            EditorGUILayout.LabelField(new GUIContent("*"), GUILayout.Width(20));
        }
        if (_selectedAsset != null && _isValidJson && GUILayout.Button(new GUIContent($"", EditorGUIUtility.IconContent("d_SaveAs").image), EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            SaveToDisk();
            CheckAndImportPrevious();
        }
        EditorGUILayout.EndHorizontal();

        if (_selectedAsset == null)
        {
            EditorGUILayout.HelpBox("Select a TextAsset.", MessageType.Info);
            return;
        }

        if (!_isValidJson)
        {
            EditorGUILayout.HelpBox($"JSON Error: {_errorMessage}", MessageType.Error);
            if (GUILayout.Button("Retry Parse")) ParseData();
            return;
        }

        if (_currentData == null) return;

        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        EditorGUILayout.BeginVertical();

        EditorGUI.BeginChangeCheck();

        _localeAsset = (TextAsset)EditorGUILayout.ObjectField("Locale dict", _localeAsset, typeof(TextAsset), false);
        _lipSyncDictionary = (LipSyncDictionary)EditorGUILayout.ObjectField("LipSync Dict", _lipSyncDictionary, typeof(LipSyncDictionary), true);
        _animationDictionary = (AnimationDictionary)EditorGUILayout.ObjectField("Anim Dict", _animationDictionary, typeof(AnimationDictionary), true);
        _secondaryAnimationDictionary = (SecondaryAnimationDictionary)EditorGUILayout.ObjectField("Sec Anim Dict", _secondaryAnimationDictionary, typeof(SecondaryAnimationDictionary), true);

        if (EditorGUI.EndChangeCheck())
        {
            RefreshReferenceCaches();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(GUILayout.Width(30));
        if (GUILayout.Button(new GUIContent($"", EditorGUIUtility.IconContent("d_Refresh").image), GUILayout.ExpandHeight(true)))
        {
            RefreshReferenceCaches();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUI.BeginChangeCheck();

        DrawAnimationList("Main Animations", ref _showAnimations, _currentData.animKeysWithParams, _cachedAnimKeys);
        DrawAnimationList("Secondary Animations", ref _showSecondary, _currentData.secondaryAnimKeysWithParams, _cachedSecondaryKeys);
        DrawLipSyncList("Lip Syncs", ref _showLipSyncs, _currentData.lipSyncKeysWithParams, _cachedLipSyncKeys);
        DrawSubtitleList("Subtitles", ref _showSubtitles, _currentData.subtitleKeysWithParams);

        if (EditorGUI.EndChangeCheck())
        {
            SaveToDisk();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawSearchableRow(string label, string currentVal, System.Action<string> onSet, List<string> validKeys, Dictionary<string, string> subDict = null)
    {
        EditorGUILayout.BeginHorizontal();

        string newVal = EditorGUILayout.TextField(label, currentVal);
        if (newVal != currentVal) onSet(newVal);

        bool isValid = (subDict != null) ? subDict.ContainsKey(currentVal) : (validKeys != null && validKeys.Contains(currentVal));

        if (!string.IsNullOrEmpty(currentVal) && !isValid)
        {
            var icon = EditorGUIUtility.IconContent("console.warnicon.sml");
            icon.tooltip = "Key not found in dictionary";
            GUILayout.Label(icon, GUILayout.Width(20));
        }
        else
        {
            GUILayout.Space(24);
        }

        Rect btnRect = GUILayoutUtility.GetRect(new GUIContent("▼"), EditorStyles.miniButton, GUILayout.Width(25));
        if (GUI.Button(btnRect, "▼", EditorStyles.miniButton))
        {
            var dropdown = new KeySelectorDropdown(new AdvancedDropdownState(), validKeys, subDict);
            dropdown.OnKeySelected = onSet;
            dropdown.Show(btnRect);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawAnimationList(string label, ref bool foldout, List<AnimationParams> list, List<string> validKeys)
    {
        foldout = EditorGUILayout.Foldout(foldout, $"{label} ({list.Count})", true, EditorStyles.foldoutHeader);
        if (foldout)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Element {i}", EditorStyles.boldLabel, GUILayout.Width(80));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawSearchableRow("Anim ID", item.Key, (val) => { item.Key = val; Repaint(); }, validKeys);

                EditorGUILayout.BeginHorizontal();
                item.Start = EditorGUILayout.FloatField("Start", item.Start);
                item.End = EditorGUILayout.FloatField("End", item.End);
                EditorGUILayout.EndHorizontal();

                item.AnimSpeed = EditorGUILayout.Slider("Speed", item.AnimSpeed, 0.0f, 2.0f);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Animation"))
            {
                list.Add(new AnimationParams { Key = "New_Anim", AnimSpeed = 1.0f });
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5);
    }

    private void DrawLipSyncList(string label, ref bool foldout, List<LipSyncParams> list, List<string> validKeys)
    {
        foldout = EditorGUILayout.Foldout(foldout, $"{label} ({list.Count})", true, EditorStyles.foldoutHeader);
        if (foldout)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Element {i}", EditorStyles.boldLabel, GUILayout.Width(80));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawSearchableRow("LipSync ID", item.Key, (val) => { item.Key = val; Repaint(); }, validKeys);

                EditorGUILayout.BeginHorizontal();
                item.Start = EditorGUILayout.FloatField("Start", item.Start);
                item.End = EditorGUILayout.FloatField("End", item.End);
                EditorGUILayout.EndHorizontal();

                item.Volume = EditorGUILayout.Slider("Volume", item.Volume, 0.0f, 2.0f);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add LipSync"))
            {
                list.Add(new LipSyncParams { Key = "New_Lipsync", Volume = 1.0f });
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5);
    }

    private void DrawSubtitleList(string label, ref bool foldout, List<SubtitleParams> list)
    {
        foldout = EditorGUILayout.Foldout(foldout, $"{label} ({list.Count})", true, EditorStyles.foldoutHeader);
        if (foldout)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Element {i}", EditorStyles.boldLabel, GUILayout.Width(80));
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    list.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawSearchableRow("Subtitle ID", item.Key, (val) => { item.Key = val; Repaint(); }, null, _cachedSubtitles);

                if (_cachedSubtitles.TryGetValue(item.Key, out string subText))
                {
                    GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
                    style.wordWrap = true;
                    style.normal.textColor = Color.gray;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("", GUILayout.Width(EditorGUIUtility.labelWidth));
                    EditorGUILayout.LabelField($"\"{subText}\"", style);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.BeginHorizontal();
                item.Start = EditorGUILayout.FloatField("Start", item.Start);
                item.End = EditorGUILayout.FloatField("End", item.End);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            if (GUILayout.Button("+ Add Subtitle"))
            {
                list.Add(new SubtitleParams { Key = "New_Subtitle" });
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(5);
    }
}

public class KeySelectorDropdown : AdvancedDropdown
{
    private List<string> _keys;
    private Dictionary<string, string> _subtitles;
    public System.Action<string> OnKeySelected;

    public KeySelectorDropdown(AdvancedDropdownState state, List<string> keys, Dictionary<string, string> subtitles = null) : base(state)
    {
        _keys = keys;
        _subtitles = subtitles;

        var size = minimumSize;
        size.y = 300;
        if (_subtitles != null) size.x = 400;
        minimumSize = size;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Keys");

        if (_subtitles != null)
        {
            foreach (var kvp in _subtitles)
            {
                string display = $"{kvp.Key} | {Truncate(kvp.Value, 50)}";

                var item = new AdvancedDropdownItem(display);

                root.AddChild(item);
            }
        }
        else if (_keys != null)
        {
            foreach (var key in _keys)
            {
                root.AddChild(new AdvancedDropdownItem(key));
            }
        }
        else
        {
            root.AddChild(new AdvancedDropdownItem("(No Keys Found)"));
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        base.ItemSelected(item);
        if (OnKeySelected != null)
        {
            string result = item.name;

            if (_subtitles != null)
            {
                int separatorIndex = result.IndexOf(" | ");
                if (separatorIndex > -1)
                {
                    result = result.Substring(0, separatorIndex);
                }
            }

            OnKeySelected.Invoke(result);
        }
    }

    private string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
    }
}