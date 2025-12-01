using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;

namespace tarkin.tradermod.bep.UI.Quests
{
    internal class Patch_QuestsScreen_OnGameStarted : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestView), nameof(QuestView.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(QuestView __instance)
        {
        }
    }
}
