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
            Color colorDarkOverlay = new Color(0, 0, 0, 0.8f);

            QuestsScreen questsScreen = __instance.GetComponentInParent<QuestsScreen>();
            Transform mainArea = questsScreen.transform.GetChild(0);
            Image bgDark = mainArea.GetComponent<Image>();
            bgDark.enabled = false;

            Transform questView = mainArea.GetChild(0).Find("QuestView");
            Transform questList = mainArea.GetChild(0).Find("QuestList");

            Image newBg = questList.gameObject.AddComponent<Image>();
            newBg.color = colorDarkOverlay;

            RectTransform questViewScrollview = questView.Find("Center/Scrollview") as RectTransform;
            questViewScrollview.offsetMax = new Vector2(0, -60);

            Transform centerBlock = questView.Find("Center/Scrollview/Content/CenterBlock");
            centerBlock.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(0, 0, 15, 15);

            Transform descriptionBlock = questView.Find("Center/Scrollview/Content/CenterBlock/DescriptionBlock");
            descriptionBlock.GetComponent<LayoutElement>().enabled = false;
            SetupVLG(descriptionBlock).padding = new RectOffset(700, 0, 0, 0);

            RectTransform textBlock = descriptionBlock.Find("TextBlock") as RectTransform;
            SetupVLG(textBlock).padding = new RectOffset(15, 15, 15, 15);

            Image imgTextBlock = textBlock.gameObject.GetComponent<Image>();
            imgTextBlock.color = colorDarkOverlay;
            imgTextBlock.gameObject.GetComponent<Mask>().showMaskGraphic = true;

            ScrollRectNoDrag scrollRectDescription = textBlock.Find("Scroll").GetComponent<ScrollRectNoDrag>();
            scrollRectDescription.verticalScrollbar.gameObject.SetActive(false);
            scrollRectDescription.enabled = false;
            SetupVLG(scrollRectDescription.RectTransform()).spacing = 20f;

            Transform questImage = descriptionBlock.Find("Image");
            questImage.SetParent(scrollRectDescription.RectTransform());
            questImage.SetAsFirstSibling();

            CustomTextMeshProUGUI textDescription = scrollRectDescription.GetComponentInChildren<CustomTextMeshProUGUI>();
            textDescription.gameObject.GetComponent<ContentSizeFitter>().enabled = false;
        }

        static VerticalLayoutGroup SetupVLG(Transform tr)
        {
            VerticalLayoutGroup verticalLayoutGroup = tr.gameObject.GetOrAddComponent<VerticalLayoutGroup>();

            verticalLayoutGroup.childForceExpandWidth = false;
            verticalLayoutGroup.childForceExpandHeight = false;
            verticalLayoutGroup.childControlHeight = true;
            verticalLayoutGroup.childControlWidth = true;

            return verticalLayoutGroup;
        }
    }
}
