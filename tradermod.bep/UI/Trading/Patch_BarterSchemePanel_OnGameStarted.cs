using EFT;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace tarkin.tradermod.bep.UI.Trading
{
    internal class Patch_BarterSchemePanel_OnGameStarted : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.method_5));
        }

        [PatchPostfix]
        private static void PatchPostfix(BarterSchemePanel __instance,
            GameObject ____buyRestrictionWarning,
            TraderAssortmentControllerClass ___traderAssortmentControllerClass)
        {
            if (____buyRestrictionWarning.activeSelf)
            {
                bool showLabel = __instance.Item_0.IsEmptyStack ||
                    __instance.Item_0.BuyRestrictionCurrent >= ___traderAssortmentControllerClass.SelectedItemBuyRestrictionMax;

                if (!showLabel)
                    ____buyRestrictionWarning.SetActive(false);
            }
        }
    }
}
