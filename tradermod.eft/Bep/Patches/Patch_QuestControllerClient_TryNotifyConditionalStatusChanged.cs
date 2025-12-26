using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

using QuestControllerClient = GClass4005;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_QuestControllerClient_TryNotifyConditionalStatusChanged : ModulePatch
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(Patch_QuestControllerClient_TryNotifyConditionalStatusChanged));

        public static event Action<QuestClass> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestControllerClient), nameof(QuestControllerClient.TryNotifyConditionalStatusChanged));
        }

        [PatchPostfix]
        private static void PatchPostfix(QuestClass quest)
        {
            try
            {
                OnPostfix?.Invoke(quest);
            }
            catch (Exception e) { Logger.LogError(e); }
        }
    }
}