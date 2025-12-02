using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace tarkin.tradermod.bep.UI.Trading
{
    internal class Patch_TraderDealScreen_Awake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(TraderDealScreen __instance)
        {
            RectTransform tradeControll = __instance.transform.Find("TradeControll") as RectTransform;
            RectTransform tradeControllTabs = tradeControll.Find("Tabs") as RectTransform;
            RectTransform tradeControllDealButton = tradeControll.Find("Deal Button") as RectTransform;
            RectTransform tradeControllBorder = tradeControll.Find("Border") as RectTransform;

            tradeControllTabs.anchorMin = new Vector2(0, 1);
            tradeControllTabs.anchorMax = new Vector2(0, 1);
            tradeControllTabs.anchoredPosition = Vector2.zero;

            tradeControll.anchorMin = new Vector2(0.5f, 0f);
            tradeControll.anchorMax = new Vector2(0.5f, 1f);
            tradeControll.offsetMin = new Vector2(tradeControll.offsetMin.x, 75f);

            tradeControllDealButton.anchoredPosition = new Vector2(0, 0f);

            tradeControllBorder.anchorMin = new Vector2(0.5f, 0f);
            tradeControllBorder.anchorMax = new Vector2(0.5f, 0f);
            tradeControllBorder.pivot = new Vector2(0.5f, 0f);
            tradeControllBorder.anchoredPosition = new Vector2(0, 0f);
        }
    }
}
