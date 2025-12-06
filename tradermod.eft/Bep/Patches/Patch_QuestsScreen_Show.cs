using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_QuestsScreen_Show : ModulePatch
    {
        public static event Action<TraderClass> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestsScreen), nameof(QuestsScreen.Show));
        }

        [PatchPostfix]
        private static void PatchPostfix(QuestsScreen __instance, TraderClass trader)
        {
            try
            {
                OnPostfix?.Invoke(trader);
            }
            catch { }
        }
    }
}
