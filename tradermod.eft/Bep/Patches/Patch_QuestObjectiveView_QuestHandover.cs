using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace tarkin.tradermod.eft.Bep.Patches
{
    internal class Patch_QuestObjectiveView_QuestHandover : ModulePatch
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(Patch_QuestObjectiveView_QuestHandover));

        public static event Action<QuestClass> OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestObjectiveView), nameof(QuestObjectiveView.method_2));
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Task __result, QuestClass selectedQuest)
        {
            __result = Wrapper(__result, selectedQuest);
        }

        private static async Task Wrapper(Task originalTask, QuestClass selectedQuest)
        {
            await originalTask;

            try
            {
                OnPostfix?.Invoke(selectedQuest);
            }
            catch (Exception e) { Logger.LogError(e); }
        }
    }
}
