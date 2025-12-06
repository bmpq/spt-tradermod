using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_QuestsListView_Show : ModulePatch
    {
        public static event Action<AbstractQuestControllerClass, TraderClass> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestsListView), nameof(QuestsListView.Show));
        }

        [PatchPostfix]
        private static void PatchPostfix(QuestsListView __instance, AbstractQuestControllerClass questController, TraderClass trader)
        {
            try
            {
                OnPostfix?.Invoke(questController, trader);
            }
            catch { }
        }
    }
}
