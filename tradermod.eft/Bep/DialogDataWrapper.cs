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
        Dictionary<string, DialogLineTemplate> mapLines;
        Dictionary<string, string> subtitles;

        public DialogDataWrapper(TraderDialogsDTO dto)
        {
            mapLines = new Dictionary<string, DialogLineTemplate>();
            subtitles = new Dictionary<string, string>();

            string locale = LocaleManagerClass.LocaleManagerClass.String_0;

            foreach (var tdt in dto.Elements)
            {
                foreach (var line in tdt.Lines)
                {
                    mapLines[line.Id] = line;
                }

                if (tdt.LocalizationDictionary.TryGetValue(locale, out var subtitleDictionary))
                {
                    foreach (var kvp in subtitleDictionary)
                    {
                        subtitles[kvp.Key] = kvp.Value;
                    }
                }
            }
        }

        public DialogLineTemplate GetLine(string id)
        {
            mapLines.TryGetValue(id, out var line);
            return line;
        }

        public string GetLocalizedSubtitle(string id)
        {
            if (subtitles.TryGetValue(id, out var subtitle))
                return subtitle;

            return string.Empty;
        }
    }
}
