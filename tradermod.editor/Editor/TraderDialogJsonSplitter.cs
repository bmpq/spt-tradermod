using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EFT.Dialogs;

namespace EFT.Tools
{
    public class TraderDialogJsonSplitter : EditorWindow
    {
        private TextAsset _sourceJson;
        private string _outputPath = "Assets/_Mods/TraderDialogueOriginalDumpSplit";

        [MenuItem("Tools/TraderMod/dialogue.json splitter")]
        public static void ShowWindow()
        {
            GetWindow<TraderDialogJsonSplitter>("dialogue.json splitter");
        }

        private void OnGUI()
        {
            _sourceJson = (TextAsset)EditorGUILayout.ObjectField("Source JSON File", _sourceJson, typeof(TextAsset), false);

            EditorGUILayout.BeginHorizontal();
            _outputPath = EditorGUILayout.TextField("Output Path", _outputPath);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        _outputPath = "Assets" + path.Substring(Application.dataPath.Length);
                    else
                        _outputPath = path;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("1. AnimationData -> Output/TraderID/LineID.json\n2. Localization -> Output/locales/en.json (consolidated)", MessageType.Info);

            if (GUILayout.Button("Split and Export", GUILayout.Height(40)))
            {
                if (_sourceJson == null)
                {
                    EditorUtility.DisplayDialog("Error", "JSON file not assigned.", "OK");
                    return;
                }
                ProcessJson();
            }
        }

        private void ProcessJson()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Processing", "Parsing JSON...", 0.1f);

                var readSettings = new JsonSerializerSettings
                {
                    Converters = new List<JsonConverter> { new LocalizationConverter() },
                    Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                    {
                        args.ErrorContext.Handled = true;
                    },
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                var rootData = JsonConvert.DeserializeObject<TraderDialogsDTO>(_sourceJson.text, readSettings);

                if (rootData == null || rootData.Elements == null)
                    throw new Exception("Failed to parse JSON or 'elements' array is empty.");

                int total = rootData.Elements.Length;
                int current = 0;

                string absoluteOutputPath = _outputPath.StartsWith("Assets")
                    ? Path.Combine(Directory.GetParent(Application.dataPath).FullName, _outputPath)
                    : _outputPath;

                string localesFolder = Path.Combine(absoluteOutputPath, "locales");

                if (!Directory.Exists(absoluteOutputPath)) Directory.CreateDirectory(absoluteOutputPath);
                if (!Directory.Exists(localesFolder)) Directory.CreateDirectory(localesFolder);

                var writeSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore
                };

                var consolidatedLocales = new Dictionary<string, Dictionary<string, string>>();

                foreach (var dialog in rootData.Elements)
                {
                    current++;
                    EditorUtility.DisplayProgressBar("Processing", $"Exporting {current}/{total}", (float)current / total);

                    if (dialog == null) continue;

                    string traderId = dialog.MainTrader;

                    if (dialog.LocalizationDictionary != null)
                    {
                        foreach (var langPair in dialog.LocalizationDictionary)
                        {
                            string langCode = langPair.Key; // "en"
                            var entries = langPair.Value; // dictionary of id:text

                            if (entries == null) continue;

                            if (!consolidatedLocales.ContainsKey(langCode))
                            {
                                consolidatedLocales[langCode] = new Dictionary<string, string>();
                            }

                            foreach (var entry in entries)
                            {
                                consolidatedLocales[langCode][entry.Key] = entry.Value;
                            }
                        }
                    }

                    string traderFolder = Path.Combine(absoluteOutputPath, traderId);
                    if (!Directory.Exists(traderFolder)) Directory.CreateDirectory(traderFolder);

                    if (dialog.Lines != null)
                    {
                        foreach (var line in dialog.Lines)
                        {
                            if (line == null || line.AnimationData == null) continue;

                            string lineId = line.Id;

                            string animJson = JsonConvert.SerializeObject(line.AnimationData, writeSettings);
                            string animPath = Path.Combine(traderFolder, $"{lineId}.json");
                            File.WriteAllText(animPath, animJson);
                        }
                    }
                }

                EditorUtility.DisplayProgressBar("Processing", "Writing Locales...", 1.0f);
                foreach (var kvp in consolidatedLocales)
                {
                    string langCode = kvp.Key;
                    var dictionary = kvp.Value;

                    string locJson = JsonConvert.SerializeObject(dictionary, writeSettings);
                    string locPath = Path.Combine(localesFolder, $"{langCode}.json");
                    File.WriteAllText(locPath, locJson);
                }

                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("Success", $"Extraction complete.\nProcessed {total} dialog blocks.\nLocales extracted: {consolidatedLocales.Count}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"Splitter Error: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Error", $"An error occurred: {ex.Message}\nSee Console for details.", "OK");
            }
        }

        // handles cases where localization is an empty array [] instead of object {}.
        public class LocalizationConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return typeof(IReadOnlyDictionary<string, Dictionary<string, string>>).IsAssignableFrom(objectType);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();
                    if (reader.TokenType == JsonToken.EndArray)
                        return new Dictionary<string, Dictionary<string, string>>();
                    throw new JsonSerializationException("Unexpected non-empty array for Localization.");
                }

                if (reader.TokenType == JsonToken.Null) return null;

                JToken token = JToken.Load(reader);
                return token.ToObject<Dictionary<string, Dictionary<string, string>>>();
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}