using BepInEx.Logging;
using EFT.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using DialogCondition = GClass3651;
using Elements = GClass3665;


namespace tarkin.tradermod.bep
{
    internal class DialogueDeserialization
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(DialogueDeserialization));

        public DialogueDeserialization() 
        {
            TraderDialogsDTO traderDialogsDTO;

            var converters = JsonSerializerSettingsClass.Converters.ToList();
            converters.Add(new DictionaryOrEmptyArrayConverter());

            var settings = new JsonSerializerSettings
            {
                Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                {
                    // will error out because eft 16.9 lacks certain enums from 1.0, we just ignore those errors lol
                    Logger.LogWarning($"{args.ErrorContext.Error.Message}");
                    args.ErrorContext.Handled = true;
                },
                Converters = converters
            };

            traderDialogsDTO = JsonConvert.DeserializeObject<TraderDialogsDTO>(File.ReadAllText(Path.Combine(BundleManager.BundleDirectory, "dialogue.json")), settings);
            Logger.LogWarning($"dialog elements size: {traderDialogsDTO.Elements.Length}");

        }
    }

    public class DictionaryOrEmptyArrayConverter
    : JsonConverter<IReadOnlyDictionary<string, Dictionary<string, string>>>
    {
        public override IReadOnlyDictionary<string, Dictionary<string, string>> ReadJson(
            JsonReader reader, Type objectType,
            IReadOnlyDictionary<string, Dictionary<string, string>> existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartArray)
            {
                reader.Read();
                if (reader.TokenType == JsonToken.EndArray)
                    return new Dictionary<string, Dictionary<string, string>>();

                throw new JsonSerializationException("Expected an empty array for fallback.");
            }

            if (reader.TokenType == JsonToken.StartObject)
            {
                return serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(reader);
            }

            throw new JsonSerializationException("Invalid token for dictionary.");
        }

        public override void WriteJson(JsonWriter writer,
            IReadOnlyDictionary<string, Dictionary<string, string>> value,
            JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

}
