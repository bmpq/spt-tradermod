using EFT.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if SPT_4_0
using DialogLineTemplate = GClass3666;
using TraderDialogTemplate = GClass3665;
#endif

namespace tarkin.tradermod.eft
{
    internal class DialogDataWrapper
    {
        TraderDialogsDTO dto;

        Dictionary<string, DialogLineTemplate> mapLines;

        public DialogDataWrapper(TraderDialogsDTO dto)
        {
            this.dto = dto;
            mapLines = new Dictionary<string, DialogLineTemplate>();

            foreach (var tdt in dto.Elements)
            {
                foreach (var line in tdt.Lines)
                {
                    mapLines[line.Id] = line;
                }
                
            }
        }

        public DialogLineTemplate GetLine(string id)
        {
            if (!mapLines.ContainsKey(id))
                return null;
            return mapLines[id];
        }
    }
}
