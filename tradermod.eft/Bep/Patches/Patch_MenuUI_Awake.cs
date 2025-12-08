using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using tarkin.tradermod.eft;

namespace tarkin.tradermod.bep.Patches
{
    internal class Patch_MenuUI_Awake : ModulePatch
    {
        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(MenuUI), nameof(MenuUI.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(MenuUI __instance)
        {
            try
            {
                OnPostfix?.Invoke();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex);
            }
        }
    }
}
