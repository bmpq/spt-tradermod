using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.bep.Patches
{
    internal class Patch_MenuScreen_Awake : ModulePatch
    {
        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuScreen), nameof(MenuScreen.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(MenuScreen __instance)
        {
            try
            {
                OnPostfix?.Invoke();
            }
            catch { }
        }
    }
}
