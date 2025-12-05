using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;
using tarkin.tradermod.bep;

using NarrateController = EFT.TarkovApplication.GClass2302;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_NarrateController_Unload : ModulePatch
    {
        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NarrateController), nameof(NarrateController.Unload));
        }

        [PatchPostfix]
        private static void Postfix(ref Task __result)
        {
            __result = UnloadWrapper(__result);
        }

        private static async Task UnloadWrapper(Task originalTask)
        {
            await originalTask;

            await TraderBundleManager.UnloadAllBundles();

            OnPostfix?.Invoke();
        }
    }
}
