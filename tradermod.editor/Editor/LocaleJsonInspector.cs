using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

[CustomEditor(typeof(TextAsset))]
public class LocaleJsonInspector : Editor
{
    private const string LOCALE_FILENAME_PATTERN = @"^[a-z]{2,3}(-[a-zA-Z0-9]+)?$";

    private bool _isLocaleFile = false;
    private string _assetPath;

    private Dictionary<string, string> _currentData;
    private bool _isDirty = false;
    private Vector2 _scrollPos;

    private string _searchFilter = "";
    private string _newKeyInput = "";
    private bool _foldoutAdd = false;

    private void OnEnable()
    {
        ValidateFile();
        if (_isLocaleFile)
        {
            ParseJson();
        }
    }

    private void ValidateFile()
    {
        if (!target) return;
        _assetPath = AssetDatabase.GetAssetPath(target);

        if (!Path.GetExtension(_assetPath).ToLower().Equals(".json"))
        {
            _isLocaleFile = false;
            return;
        }

        string fileName = Path.GetFileNameWithoutExtension(_assetPath);
        _isLocaleFile = Regex.IsMatch(fileName, LOCALE_FILENAME_PATTERN, RegexOptions.IgnoreCase);
    }

    private void ParseJson()
    {
        TextAsset asset = (TextAsset)target;
        if (string.IsNullOrWhiteSpace(asset.text))
        {
            _currentData = new Dictionary<string, string>();
            return;
        }

        try
        {
            var root = JObject.Parse(asset.text);
            _currentData = root.ToObject<Dictionary<string, string>>();
        }
        catch
        {
            _currentData = null;
        }
    }

    public override void OnInspectorGUI()
    {
        GUI.enabled = true;

        if (!_isLocaleFile)
        {
            DrawDefaultInspector();
            return;
        }

        if (_currentData == null)
        {
            EditorGUILayout.HelpBox("Could not parse JSON as a simple string dictionary. \nEnsure format is {\"key\": \"value\"}.", MessageType.Warning);
            if (GUILayout.Button("Reload")) ParseJson();
        }
        else
        {
            DrawLocaleTable();
        }
    }

    private void DrawLocaleTable()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(20));

        if (_isDirty)
        {
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button(new GUIContent(" Unsaved Changes", EditorGUIUtility.IconContent("SaveAs").image), EditorStyles.toolbarButton, GUILayout.Width(130)))
            {
                SaveJson();
            }
            GUI.backgroundColor = Color.white;
        }

        GUILayout.Label(EditorGUIUtility.IconContent("Search Icon"), GUILayout.Width(20), GUILayout.Height(20));
        _searchFilter = EditorGUILayout.TextField(_searchFilter);
        if (GUILayout.Button(new GUIContent("", EditorGUIUtility.IconContent("winbtn_win_close").image, "Clear filter"), EditorStyles.iconButton, GUILayout.Width(20)))
        {
            _searchFilter = "";
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        DrawAddEntrySection();

        EditorGUILayout.Space(5);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

        var keys = _currentData.Keys.ToList();
        if (!string.IsNullOrEmpty(_searchFilter))
        {
            string lowerFilter = _searchFilter.ToLower();
            keys = keys.Where(k => k.ToLower().Contains(lowerFilter) || _currentData[k].ToLower().Contains(lowerFilter)).ToList();
        }

        keys.Sort();

        if (keys.Count == 0)
        {
            EditorGUILayout.HelpBox("No keys found matching filter.", MessageType.Info);
        }

        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            string value = _currentData[key];

            DrawKeyValueRow(key, value, i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAddEntrySection()
    {
        _foldoutAdd = EditorGUILayout.Foldout(_foldoutAdd, new GUIContent("Add new key", EditorGUIUtility.IconContent("Toolbar Plus").image), true);
        if (_foldoutAdd)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label("Key:", GUILayout.Width(40));
            _newKeyInput = EditorGUILayout.TextField(_newKeyInput);

            bool keyExists = _currentData.ContainsKey(_newKeyInput);
            bool isValid = !string.IsNullOrEmpty(_newKeyInput) && !keyExists;

            GUI.enabled = isValid;
            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                _currentData.Add(_newKeyInput, "");
                _newKeyInput = "";
                _isDirty = true;
                GUI.FocusControl(null);
            }
            GUI.enabled = true;

            EditorGUILayout.EndHorizontal();

            if (keyExists)
            {
                EditorGUILayout.HelpBox($"Key '{_newKeyInput}' already exists.", MessageType.Error);
            }
            EditorGUILayout.EndVertical();
        }
    }

    private void DrawKeyValueRow(string key, string value, int index)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        float width = EditorStyles.label.CalcSize(new GUIContent(key)).x;
        EditorGUILayout.SelectableLabel(key, GUILayout.Height(18), GUILayout.Width(width));

        if (GUILayout.Button(new GUIContent("", EditorGUIUtility.IconContent("d_TreeEditor.Duplicate").image, "Copy Key"), EditorStyles.iconButton, GUILayout.Width(25), GUILayout.Height(18)))
        {
            EditorGUIUtility.systemCopyBuffer = key;
        }


        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("", EditorGUIUtility.IconContent("TreeEditor.Trash").image, "Delete Key"), EditorStyles.miniButton, GUILayout.Width(25), GUILayout.Height(18)))
        {
            if (EditorUtility.DisplayDialog("Delete Key", $"Are you sure you want to delete '{key}'?", "Yes", "No"))
            {
                _currentData.Remove(key);
                _isDirty = true;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUIUtility.ExitGUI();
                return;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.BeginChangeCheck();
        GUIStyle areaStyle = new GUIStyle(EditorStyles.textArea);
        areaStyle.wordWrap = true;

        string newValue = EditorGUILayout.TextArea(value, areaStyle);

        if (EditorGUI.EndChangeCheck())
        {
            _currentData[key] = newValue;
            _isDirty = true;
        }

        EditorGUILayout.EndVertical();
    }

    private void SaveJson()
    {
        try
        {
            var sortedData = _currentData.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);

            string jsonOutput = JsonConvert.SerializeObject(sortedData, Formatting.Indented);
            File.WriteAllText(_assetPath, jsonOutput);

            _isDirty = false;

            AssetDatabase.ImportAsset(_assetPath);
            Debug.Log($"[LocaleEditor] Saved {sortedData.Count} keys to {_assetPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LocaleEditor] Failed to save: {e.Message}");
        }
    }
}