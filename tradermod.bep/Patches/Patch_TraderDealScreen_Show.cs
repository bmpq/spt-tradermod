using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.bep.Patches
{
    internal class Patch_TraderDealScreen_Show : ModulePatch
    {
        public static event Action<TraderClass> OnTraderTradingOpen;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.Show));
        }

        [PatchPostfix]
        private static void PatchPostfix(TraderDealScreen __instance, TraderClass trader)
        {
            try
            {
                OnTraderTradingOpen?.Invoke(trader);
            }
            catch (Exception ex) { Plugin.Log.LogError(ex.StackTrace); }
        }
    }
}
