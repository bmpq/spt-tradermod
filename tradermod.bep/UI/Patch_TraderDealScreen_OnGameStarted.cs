using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace tarkin.tradermod.bep.UI
{
    internal class Patch_TraderDealScreen_Awake : ModulePatch
    {
        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TraderDealScreen), nameof(TraderDealScreen.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(TraderDealScreen __instance, 
            DefaultUIButton ____dealButton,
            TradingTable ____tradingTable)
        {
            ____dealButton.RectTransform.anchoredPosition = new Vector2(0, 3f);
            ____tradingTable.RectTransform.anchoredPosition = new Vector2(0, -161f); // default is (0, -205)
            ____tradingTable.transform.Find("Trading Table/Border").GetComponent<RectTransform>().offsetMin = new Vector2(-253f, -348.5f);

            RectTransform dealButtonBorder = __instance.transform.Find("TradeControll/Border").GetComponent<RectTransform>();
            dealButtonBorder.anchorMin = new Vector2(0.5f, 0f);
            dealButtonBorder.anchorMax = new Vector2(0.5f, 0f);
            dealButtonBorder.pivot = new Vector2(0.5f, 0f);
            dealButtonBorder.anchoredPosition = new Vector2(0, 3f);
        }
    }
}
