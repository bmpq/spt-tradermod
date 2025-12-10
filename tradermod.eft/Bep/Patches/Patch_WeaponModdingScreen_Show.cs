using EFT;
using EFT.UI.WeaponModding;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using static EFT.UI.WeaponModding.WeaponModdingScreen;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_WeaponModdingScreen_Show : ModulePatch
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(Patch_WeaponModdingScreen_Show));

        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WeaponModdingScreen), nameof(WeaponModdingScreen.Show), new Type[] { typeof(GClass3922) });
        }

        [PatchPostfix]
        private static void PatchPostfix(WeaponModdingScreen __instance)
        {
            try
            {
                OnPostfix?.Invoke();
            }
            catch (Exception e) { Logger.LogError(e); }
        }
    }
}