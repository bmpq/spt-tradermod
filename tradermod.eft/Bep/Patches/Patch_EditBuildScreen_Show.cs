using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_EditBuildScreen_Show : ModulePatch
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(Patch_EditBuildScreen_Show));

        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.Show), new Type[] { typeof(EditBuildScreen.GClass3881) });
        }

        [PatchPostfix]
        private static void PatchPostfix(EditBuildScreen __instance)
        {
            try
            {
                OnPostfix?.Invoke();
            }
            catch (Exception e) { Logger.LogError(e); }
        }
    }
}