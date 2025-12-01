using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;

namespace tarkin.tradermod.bep.UI
{
    internal class Patch_TradingTable_Awake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(TradingTable), nameof(TradingTable.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(TradingTable __instance)
        {
            RectTransform rectTransform = __instance.transform as RectTransform;
            RectTransform tradingTable = __instance.transform.Find("Trading Table") as RectTransform;
            RectTransform tradingTableBorder = tradingTable.Find("Border") as RectTransform;
            RectTransform tradingTableScrollArea = tradingTable.Find("Scroll Area") as RectTransform;

            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 1);

            rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 118f);
            rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -205);

            tradingTable.offsetMin = Vector2.zero;
            tradingTable.offsetMax = Vector2.zero;

            tradingTableBorder.anchorMin = new Vector2(0, 0);
            tradingTableBorder.anchorMax = new Vector2(1, 1);
            tradingTableBorder.offsetMin = Vector2.zero;
            tradingTableBorder.offsetMax = Vector2.zero;

            tradingTableScrollArea.offsetMin = new Vector2(1, 1f);
            tradingTableScrollArea.offsetMax = new Vector2(0, -1f);
        }
    }
}
