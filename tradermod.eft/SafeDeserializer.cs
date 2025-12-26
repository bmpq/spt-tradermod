using EFT;
using EFT.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if SPT_4_0
using BepInEx.Logging;
using DialogCondition = GClass3651;
using Elements = GClass3665;
using EFTJsonConverters = JsonSerializerSettingsClass;
#endif


namespace tarkin.tradermod.eft
{
    public class DictionaryInADictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(IReadOnlyDictionary<string, Dictionary<string, string>>);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var concreteDictionary = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(reader);

            return concreteDictionary;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

    public class SafeDeserializer<T>
    {
#if SPT_4_0
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(SafeDeserializer<T>));
#endif

        public static T Deserialize(string jsonContent)
        {
            var converters = new List<JsonConverter>(); //EFTJsonConverters.Converters.ToList();
            converters.Add(new DictionaryInADictionaryConverter());

            var settings = new JsonSerializerSettings
            {
                Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
                {
                    // will error out because eft 16.9 lacks certain enums from 1.0, we just ignore those errors lol
#if SPT_4_0
                    Logger.LogWarning($"{args.ErrorContext.Error.Message}");
#else
                    Debug.LogError($"{args.ErrorContext.Error.Message} {args.ErrorContext.Path}");
#endif
                    args.ErrorContext.Handled = true;
                },
                Converters = converters
            };

            return JsonConvert.DeserializeObject<T>(jsonContent, settings);
        }
    }
}
