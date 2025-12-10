using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_MainMenuControllerClass_ShowScreen : ModulePatch
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(Patch_MainMenuControllerClass_ShowScreen));

        public static event Action<EMenuType, bool> OnPostfix;

        public static MainMenuControllerClass Instance { get; private set; }    

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MainMenuControllerClass), nameof(MainMenuControllerClass.ShowScreen));
        }

        [PatchPostfix]
        private static void PatchPostfix(MainMenuControllerClass __instance, EMenuType screen, bool turnOn)
        {
            try
            {
                Instance = __instance;
                OnPostfix?.Invoke(screen, turnOn);
            }
            catch (Exception e) { Logger.LogError(e); }
        }
    }
}