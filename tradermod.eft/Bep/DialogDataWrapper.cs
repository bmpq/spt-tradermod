using Comfort.Common;
using EFT.Dialogs;
using System.Collections.Generic;

#if SPT_4_0
using DialogLineTemplate = GClass3666;
using TraderDialogTemplate = GClass3665;
#endif

namespace tarkin.tradermod.eft
{
    public class DialogDataWrapper
    {
        private readonly Dictionary<string, DialogLineTemplate> mapLines;
        private readonly Dictionary<string, Dictionary<string, string>> localizedSubtitles;

        public DialogDataWrapper(TraderDialogsDTO dto)
        {
            mapLines = new Dictionary<string, DialogLineTemplate>();
            localizedSubtitles = new Dictionary<string, Dictionary<string, string>>();

            foreach (var tdt in dto.Elements)
            {
                foreach (var line in tdt.Lines)
                {
                    mapLines[line.Id] = line;
                }

                foreach (var localePair in tdt.LocalizationDictionary)
                {
                    string localeKey = localePair.Key;
                    var textMap = localePair.Value;

                    if (!localizedSubtitles.ContainsKey(localeKey))
                    {
                        localizedSubtitles[localeKey] = new Dictionary<string, string>();
                    }

                    foreach (var kvp in textMap)
                    {
                        localizedSubtitles[localeKey][kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        public DialogLineTemplate GetLine(string id)
        {
            mapLines.TryGetValue(id, out var line);
            return line;
        }

        public void AddExtraLocalizationData(Dictionary<string, Dictionary<string, string>> data)
        {
            foreach (var kvp in data)
            {
                AddExtraLocalizationData(kvp.Key, kvp.Value);
            }
        }

        public void AddExtraLocalizationData(string locale, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(locale) || data == null || data.Count == 0)
                return;

            if (!localizedSubtitles.TryGetValue(locale, out var localeDict))
            {
                localeDict = new Dictionary<string, string>();
                localizedSubtitles[locale] = localeDict;
            }

            foreach (var kvp in data)
            {
                // will overwrite
                localeDict[kvp.Key] = kvp.Value;
            }
        }

        public string GetLocalizedSubtitle(string id)
        {
            string currentLocale = "en";
            if (Singleton<SharedGameSettingsClass>.Instantiated)
                currentLocale = Singleton<SharedGameSettingsClass>.Instance.Game.Settings.Language.Value;

            if (TryGetText(currentLocale, id, out string result))
                return result;

            if (currentLocale != "en" && TryGetText("en", id, out string fallbackResult))
                return fallbackResult;

            return string.Empty;
        }

        private bool TryGetText(string locale, string id, out string text)
        {
            text = null;

            if (localizedSubtitles.TryGetValue(locale, out var dict))
            {
                if (dict.TryGetValue(id, out var foundText))
                {
                    text = foundText;
                    return true;
                }
            }
            return false;
        }
    }
}
