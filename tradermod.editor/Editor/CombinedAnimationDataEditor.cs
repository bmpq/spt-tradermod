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

    private TextAsset _localeAsset;
    private LipSyncDictionary _lipSyncDictionary;
    private AnimationDictionary _animationDictionary;
    private SecondaryAnimationDictionary _secondaryAnimationDictionary;
    private SequenceReader _sequenceReader;

    private List<string> _cachedAnimKeys = new List<string>();
    private List<string> _cachedSecondaryKeys = new List<string>();
    private List<string> _cachedLipSyncKeys = new List<string>();
    private Dictionary<string, string> _cachedSubtitles = new Dictionary<string, string>();

    private float _inspectorWidth = 320f;
    private float _pixelsPerSecond = 100f;
    private float _maxTimelineDuration = 20f;

    private int _dragHash = -1;
    private DragType _dragType = DragType.None;
    private float _dragStartVal;
    private float _dragEndVal;
    private float _dragMouseX;

    private enum DragType { None, Move, TrimStart, TrimEnd }

    [MenuItem("Tools/TraderMod/Combined Animation Editor")]
    public static void ShowWindow()
    {
        GetWindow<CombinedAnimationEditorWindow>("Anim Data Editor");
    }

    private void OnEnable()
    {
        if (Selection.activeObject is TextAsset asset)
        {
            AttemptChangeAsset(asset, force: true);
        }
        RefreshReferenceCaches();

        EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
    }

    private void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
    {
        RefreshReferenceCaches();
    }

    private void OnDisable() 
    { 
        CheckAndImportPrevious();
        EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is TextAsset asset && asset != _selectedAsset)
        {
            AttemptChangeAsset(asset);
            Repaint();
        }
    }

    private void AttemptChangeAsset(TextAsset newAsset, bool force = false)
    {
        if (newAsset == null) return;

        CombinedAnimationData tempData;
        bool valid = TryParse(newAsset, out tempData);

        if (valid)
        {
            CheckAndImportPrevious();

            _selectedAsset = newAsset;
            _currentData = tempData;
            _isValidJson = true;
            _errorMessage = "";
            _needsReimport = false;
        }
        else if (force)
        {
            _selectedAsset = newAsset;
            _currentData = null;
            _isValidJson = false;
            try { JsonConvert.DeserializeObject<CombinedAnimationData>(newAsset.text); }
            catch (System.Exception ex) { _errorMessage = ex.Message; }
        }
    }

    private bool TryParse(TextAsset asset, out CombinedAnimationData data)
    {
        data = null;
        if (asset == null) return false;

        if (!asset.text.StartsWith("{\r\n  \"animations\""))
            return false;

        try
        {
            data = JsonConvert.DeserializeObject<CombinedAnimationData>(asset.text);
            if (data == null) data = CombinedAnimationData.Default;

            if (data.animKeysWithParams == null) data.animKeysWithParams = new List<AnimationParams>();
            if (data.secondaryAnimKeysWithParams == null) data.secondaryAnimKeysWithParams = new List<AnimationParams>();
            if (data.lipSyncKeysWithParams == null) data.lipSyncKeysWithParams = new List<LipSyncParams>();
            if (data.subtitleKeysWithParams == null) data.subtitleKeysWithParams = new List<SubtitleParams>();
            if (data.mediaData == null) data.mediaData = new MediaData();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private void ReloadCurrentAsset()
    {
        if (_selectedAsset != null)
        {
            AttemptChangeAsset(_selectedAsset, force: true);
        }
    }

    private void CheckAndImportPrevious()
    {
        if (_needsReimport && _selectedAsset != null)
        {
            string path = AssetDatabase.GetAssetPath(_selectedAsset);
            if (!string.IsNullOrEmpty(path)) AssetDatabase.ImportAsset(path);
        }
        _needsReimport = false;
    }

    private void RefreshReferenceCaches()
    {
        if (_lipSyncDictionary == null) _lipSyncDictionary = FindFirstObjectByType<LipSyncDictionary>();
        if (_animationDictionary == null) _animationDictionary = FindFirstObjectByType<AnimationDictionary>();
        if (_secondaryAnimationDictionary == null) _secondaryAnimationDictionary = FindFirstObjectByType<SecondaryAnimationDictionary>();
        if (_sequenceReader == null) _sequenceReader = FindFirstObjectByType<SequenceReader>();

        _cachedAnimKeys = _animationDictionary != null ? _animationDictionary.GetAllKeys() : new List<string>();
        _cachedSecondaryKeys = _secondaryAnimationDictionary != null ? _secondaryAnimationDictionary.GetAllKeys() : new List<string>();
        _cachedLipSyncKeys = _lipSyncDictionary != null ? _lipSyncDictionary.GetAllKeys() : new List<string>();

        ParseLocale();
    }

    private void ParseLocale()
    {
        _cachedSubtitles.Clear();
        if (_localeAsset != null)
        {
            try
            {
                _cachedSubtitles = JsonConvert.DeserializeObject<Dictionary<string, string>>(_localeAsset.text) ?? new Dictionary<string, string>();
            }
            catch { Debug.LogWarning("Could not parse Locale dictionary."); }
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
        catch (System.Exception ex) { Debug.LogError($"Failed to save JSON: {ex.Message}"); }
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (_selectedAsset == null)
        {
            EditorGUILayout.HelpBox("Select a valid CombinedAnimationData TextAsset.", MessageType.Info);
            return;
        }

        if (!_isValidJson)
        {
            EditorGUILayout.HelpBox($"JSON Error: {_errorMessage}", MessageType.Error);
            if (GUILayout.Button("Retry Parse")) ReloadCurrentAsset();
            return;
        }

        if (_currentData == null) return;

        DrawReferences();

        EditorGUILayout.BeginHorizontal();
        DrawPreviewControls();
        DrawTimelineHeader();
        EditorGUILayout.EndHorizontal();

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
        EditorGUI.BeginChangeCheck();

        DrawSection("Main Animations", _currentData.animKeysWithParams, _cachedAnimKeys, new Color(0.4f, 0.6f, 1f));
        DrawSection("Secondary Animations", _currentData.secondaryAnimKeysWithParams, _cachedSecondaryKeys, new Color(0.4f, 0.8f, 0.8f));
        DrawSection("Lip Syncs", _currentData.lipSyncKeysWithParams, _cachedLipSyncKeys, new Color(1f, 0.5f, 0.5f));
        DrawSubtitlesSection(_currentData.subtitleKeysWithParams, new Color(1f, 0.8f, 0.4f));

        if (EditorGUI.EndChangeCheck())
        {
            SaveToDisk();
        }

        if (Event.current.type == EventType.MouseDown)
        {
            GUI.FocusControl(null);
            Repaint();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        EditorGUI.BeginChangeCheck();
        TextAsset newAsset = (TextAsset)EditorGUILayout.ObjectField(_selectedAsset, typeof(TextAsset), false, GUILayout.Width(200));
        if (EditorGUI.EndChangeCheck())
        {
            if (newAsset != _selectedAsset)
            {
                AttemptChangeAsset(newAsset);
            }
        }

        if (_needsReimport) EditorGUILayout.LabelField("*", GUILayout.Width(10));

        if (GUILayout.Button(EditorGUIUtility.IconContent("d_SaveAs"), EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            SaveToDisk();
            CheckAndImportPrevious();
        }

        GUILayout.FlexibleSpace();

        // zoom slider
        GUILayout.Label(EditorGUIUtility.IconContent("d_ViewToolZoom"), GUILayout.Width(20));
        _pixelsPerSecond = GUILayout.HorizontalSlider(_pixelsPerSecond, 10f, 300f, GUILayout.Width(100));

        EditorGUILayout.EndHorizontal();
    }

    private void DrawReferences()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUI.BeginChangeCheck();

        EditorGUILayout.BeginHorizontal();
        _localeAsset = (TextAsset)EditorGUILayout.ObjectField("Locale", _localeAsset, typeof(TextAsset), false);
        _lipSyncDictionary = (LipSyncDictionary)EditorGUILayout.ObjectField("Lips", _lipSyncDictionary, typeof(LipSyncDictionary), true);
        _animationDictionary = (AnimationDictionary)EditorGUILayout.ObjectField("Anims", _animationDictionary, typeof(AnimationDictionary), true);
        _secondaryAnimationDictionary = (SecondaryAnimationDictionary)EditorGUILayout.ObjectField("Sec Anims", _secondaryAnimationDictionary, typeof(SecondaryAnimationDictionary), true);
        _sequenceReader = (SequenceReader)EditorGUILayout.ObjectField("Seq Reader", _sequenceReader, typeof(SequenceReader), true);
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck()) RefreshReferenceCaches();
        EditorGUILayout.EndVertical();
    }

    private void DrawPreviewControls()
    {
        EditorGUI.BeginDisabledGroup(!Application.isPlaying || _sequenceReader == null);

        EditorGUILayout.BeginHorizontal(GUILayout.Width(_inspectorWidth));
        if (GUILayout.Button(EditorGUIUtility.IconContent("PlayButton On"), GUILayout.Width(20)))
        {
            _sequenceReader.json = _selectedAsset.text;
            _sequenceReader.Play();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.EndDisabledGroup();
    }

    private void DrawTimelineHeader()
    {
        Rect rect = EditorGUILayout.GetControlRect(false);
        Rect rightRect = new Rect(rect.x, rect.y, rect.width, rect.height);

        // bg
        EditorGUI.DrawRect(rightRect, new Color(0.2f, 0.2f, 0.2f));

        // cool ticks
        Handles.color = Color.gray;
        float step = _pixelsPerSecond > 150 ? 0.5f : 1.0f;

        for (float t = 0; t < _maxTimelineDuration + 50f; t += step)
        {
            float x = rightRect.x + (t * _pixelsPerSecond);
            if (x > rightRect.xMax) break;

            float height = (t % 1.0f == 0) ? 10 : 5;
            Handles.DrawLine(new Vector3(x, rightRect.y + 20 - height), new Vector3(x, rightRect.y + 20));

            if (t % 1.0f == 0)
            {
                GUI.Label(new Rect(x + 2, rightRect.y, 30, 20), t.ToString("0"), EditorStyles.miniLabel);
            }
        }
    }

    private void DrawSection<T>(string title, List<T> list, List<string> validKeys, Color color) where T : class
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"{title} ({list.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            AddItem(list);
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            DrawRow(i, item, validKeys, color, list);
        }
        EditorGUILayout.Space(5);
    }

    private void DrawSubtitlesSection(List<SubtitleParams> list, Color color)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label($"Subtitles ({list.Count})", EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.Width(25)))
        {
            list.Add(new SubtitleParams { Key = "New_Subtitle" });
        }
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            DrawRow(i, item, null, color, list, _cachedSubtitles);
        }
        EditorGUILayout.Space(5);
    }

    private void AddItem<T>(List<T> list)
    {
        if (typeof(T) == typeof(AnimationParams))
            (list as List<AnimationParams>).Add(new AnimationParams { Key = "New_Anim", AnimSpeed = 1.0f });
        else if (typeof(T) == typeof(LipSyncParams))
            (list as List<LipSyncParams>).Add(new LipSyncParams { Key = "New_Lip", Volume = 1.0f });
        else if (typeof(T) == typeof(SubtitleParams))
            (list as List<SubtitleParams>).Add(new SubtitleParams { Key = "New_Sub" });
    }

    private void DrawRow<T>(int index, T item, List<string> validKeys, Color color, List<T> list, Dictionary<string, string> subDict = null)
    {
        EditorGUILayout.BeginHorizontal();

        // INSPECTOR
        
        EditorGUILayout.BeginVertical(GUILayout.Width(_inspectorWidth), GUILayout.ExpandWidth(false));
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(_inspectorWidth), GUILayout.ExpandWidth(false));

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"#{index}", EditorStyles.miniLabel, GUILayout.Width(20));

        if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), EditorStyles.iconButton, GUILayout.Width(20)))
        {
            list.RemoveAt(index);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
            return;
        }

        dynamic dItem = item; // using dynamic to access common properties (Key, Start, End) without interface

        DrawSearchableRow((string)dItem.Key, (val) => dItem.Key = val, validKeys, subDict);
        EditorGUILayout.EndHorizontal();

        if (subDict != null && subDict.TryGetValue((string)dItem.Key, out string subText))
        {
            GUIStyle wrapStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, normal = { textColor = Color.gray } };
            GUILayout.Label($"\"{Truncate(subText, 60)}\"", wrapStyle);
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.BeginHorizontal();
        {
            dItem.Start = EditorGUILayout.FloatField((float)dItem.Start);
            EditorGUILayout.LabelField("-", GUILayout.Width(10));
            dItem.End = EditorGUILayout.FloatField((float)dItem.End);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
        {
            if (item is AnimationParams animP)
            {
                EditorGUILayout.LabelField(new GUIContent("", EditorGUIUtility.IconContent("SpeedScale").image), GUILayout.Width(20));
                animP.AnimSpeed = EditorGUILayout.Slider(animP.AnimSpeed, 0.0f, 2.0f);
            }
            else if (item is LipSyncParams lipP)
            {
                EditorGUILayout.LabelField(new GUIContent("", EditorGUIUtility.IconContent("SceneViewAudio").image), GUILayout.Width(20));
                lipP.Volume = EditorGUILayout.Slider(lipP.Volume, 0.0f, 2.0f);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndHorizontal();


        EditorGUILayout.EndVertical();
        EditorGUILayout.EndVertical();

        // TIMELINE

        Rect rowRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true));

        DrawTimelineGrid(rowRect);

        float startTime = (float)dItem.Start;
        float endTime = (float)dItem.End;

        HandleTimelineClip(rowRect, ref startTime, ref endTime, item.GetHashCode(), color);

        if (startTime < 0) startTime = 0;
        if (endTime < startTime) endTime = startTime + 0.1f;

        dItem.Start = startTime;
        dItem.End = endTime;

        EditorGUILayout.EndHorizontal();
        GUILayout.Space(2);
    }

    private void DrawTimelineGrid(Rect rect)
    {
        if (Event.current.type != EventType.Repaint) return;

        EditorGUI.DrawRect(rect, new Color(0.18f, 0.18f, 0.18f));

        Handles.color = new Color(1, 1, 1, 0.1f);
        for (float t = 0; t < _maxTimelineDuration + 50; t += 1.0f)
        {
            float x = rect.x + (t * _pixelsPerSecond);
            if (x > rect.xMax) break;
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));
        }
    }

    private void HandleTimelineClip(Rect trackRect, ref float start, ref float end, int id, Color color)
    {
        float duration = end - start;
        float startX = trackRect.x + (start * _pixelsPerSecond);
        float width = duration * _pixelsPerSecond;

        Rect clipRect = new Rect(startX, trackRect.y + 2, width, trackRect.height - 4);

        EditorGUI.DrawRect(clipRect, color);
        EditorGUI.DrawRect(new Rect(clipRect.x, clipRect.y, 4, clipRect.height), Color.white * 0.8f); // Left Handle
        EditorGUI.DrawRect(new Rect(clipRect.xMax - 4, clipRect.y, 4, clipRect.height), Color.white * 0.8f); // Right Handle

        if (width > 40)
        {
            GUI.Label(clipRect, $"{duration:F2}s", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.black } });
        }

        Event e = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        switch (e.type)
        {
            case EventType.MouseDown:
                if (clipRect.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    _dragHash = id;
                    _dragStartVal = start;
                    _dragEndVal = end;
                    _dragMouseX = e.mousePosition.x;

                    // determine Drag Type based on click position
                    if (e.mousePosition.x < clipRect.x + 8) _dragType = DragType.TrimStart;
                    else if (e.mousePosition.x > clipRect.xMax - 8) _dragType = DragType.TrimEnd;
                    else _dragType = DragType.Move;

                    e.Use();
                }
                break;

            case EventType.MouseDrag:
                if (GUIUtility.hotControl == controlID && _dragHash == id)
                {
                    float pixelDelta = e.mousePosition.x - _dragMouseX;
                    float timeDelta = pixelDelta / _pixelsPerSecond;

                    if (_dragType == DragType.Move)
                    {
                        start = _dragStartVal + timeDelta;
                        end = _dragEndVal + timeDelta;
                    }
                    else if (_dragType == DragType.TrimStart)
                    {
                        start = _dragStartVal + timeDelta;
                    }
                    else if (_dragType == DragType.TrimEnd)
                    {
                        end = _dragEndVal + timeDelta;
                    }

                    // force repaint
                    GUI.changed = true;
                    e.Use();
                }
                break;

            case EventType.MouseUp:
                if (GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    _dragType = DragType.None;
                    _dragHash = -1;
                    e.Use();
                }
                break;
        }
    }

    private void DrawSearchableRow(string currentVal, System.Action<string> onSet, List<string> validKeys, Dictionary<string, string> subDict = null)
    {
        EditorGUILayout.BeginHorizontal();

        string newVal = EditorGUILayout.TextField(currentVal);
        if (newVal != currentVal) onSet(newVal);

        bool isValid = (subDict != null) ? subDict.ContainsKey(currentVal) : (validKeys != null && validKeys.Contains(currentVal));

        if (!string.IsNullOrEmpty(currentVal) && !isValid)
        {
            var icon = EditorGUIUtility.IconContent("console.warnicon.sml");
            icon.tooltip = "Key not found in dictionary";
            GUILayout.Label(icon, GUILayout.Width(20));
        }

        Rect btnRect = GUILayoutUtility.GetRect(new GUIContent("▼"), EditorStyles.miniButton, GUILayout.Width(20));
        if (GUI.Button(btnRect, "▼", EditorStyles.miniButton))
        {
            var dropdown = new KeySelectorDropdown(new UnityEditor.IMGUI.Controls.AdvancedDropdownState(), validKeys, subDict);
            dropdown.OnKeySelected = onSet;
            dropdown.Show(btnRect);
        }

        EditorGUILayout.EndHorizontal();
    }

    private string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
    }
}