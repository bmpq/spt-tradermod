using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace tarkin.tradermod.bep.UI.Quests
{
    internal class Patch_QuestsScreen_Awake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestsListView), nameof(QuestsListView.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(QuestsListView __instance)
        {
            QuestsScreen questsScreen = __instance.GetComponentInParent<QuestsScreen>();
            Transform mainArea = questsScreen.transform.GetChild(0);
            Image bgDark = mainArea.GetComponent<Image>();
            bgDark.enabled = false;

            Transform questView = mainArea.GetChild(0).Find("QuestView");
            Transform questList = mainArea.GetChild(0).Find("QuestList");

            Image newBg = questList.gameObject.AddComponent<Image>();
            newBg.sprite = bgDark.sprite;
            newBg.color = bgDark.color;

            RectTransform questViewScrollview = questView.Find("Center/Scrollview") as RectTransform;
            questViewScrollview.offsetMax = new Vector2(0, -60);

            Transform centerBlock = questView.Find("Center/Scrollview/Content/CenterBlock");
            centerBlock.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 15, 15);

            Transform descriptionBlock = questView.Find("Center/Scrollview/Content/CenterBlock/DescriptionBlock");
            descriptionBlock.GetComponent<LayoutElement>().preferredHeight = 500;

            Transform questImage = descriptionBlock.Find("Image");
            questImage.gameObject.SetActive(false);

            RectTransform textBlock = descriptionBlock.Find("TextBlock") as RectTransform;
            textBlock.anchorMin = new Vector2(0, 0);
            textBlock.anchorMax = new Vector2(1, 1);
            textBlock.offsetMin = new Vector2(700, 0);

            ScrollRectNoDrag scrollRectDescription = textBlock.Find("Scroll").GetComponent<ScrollRectNoDrag>();
            scrollRectDescription.movementType = ScrollRect.MovementType.Clamped;
        }
    }
}
