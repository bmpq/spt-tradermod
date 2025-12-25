using UnityEngine;
using UnityEditor;
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

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        EditorGUI.BeginChangeCheck();

        DrawAnimationList("Main Animations", ref _showAnimations, _currentData.animKeysWithParams);
        DrawAnimationList("Secondary Animations", ref _showSecondary, _currentData.secondaryAnimKeysWithParams);
        DrawLipSyncList("Lip Syncs", ref _showLipSyncs, _currentData.lipSyncKeysWithParams);
        DrawSubtitleList("Subtitles", ref _showSubtitles, _currentData.subtitleKeysWithParams);

        if (EditorGUI.EndChangeCheck())
        {
            SaveToDisk();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAnimationList(string label, ref bool foldout, List<AnimationParams> list)
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

                item.Key = EditorGUILayout.TextField("Anim ID", item.Key);

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

    private void DrawLipSyncList(string label, ref bool foldout, List<LipSyncParams> list)
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

                item.Key = EditorGUILayout.TextField("LipSync ID", item.Key);

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

                item.Key = EditorGUILayout.TextField("Subtitle ID", item.Key);

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