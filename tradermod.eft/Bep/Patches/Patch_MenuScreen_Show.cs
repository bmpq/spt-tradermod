using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

using MainMenuBaseScreenController = EFT.UI.MenuScreen.GClass3877;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_MenuScreen_Show : ModulePatch
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(Patch_MenuScreen_Show));

        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuScreen), nameof(MenuScreen.Show), new Type[] { typeof(MainMenuBaseScreenController) });
        }

        [PatchPostfix]
        private static void PatchPostfix(MenuScreen __instance)
        {
            try
            {
                OnPostfix?.Invoke();
            }
            catch (Exception e) { Logger.LogError(e); }
        }
    }
}