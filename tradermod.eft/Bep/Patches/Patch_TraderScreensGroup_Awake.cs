using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_TraderScreensGroup_Awake : ModulePatch
    {
        public static event Action<TraderScreensGroup> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderScreensGroup), nameof(TraderScreensGroup.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(TraderScreensGroup __instance, ServicesScreen ____servicesScreen)
        {
            try
            {
                // fix bsg bug, the services screen is active in the background when the trading screen is first opened
                ____servicesScreen.gameObject.SetActive(false);

                OnPostfix?.Invoke(__instance);
            }
            catch (Exception e) { Logger.LogError(e); }
        }
    }
}
