using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace tarkin.tradermod.bep.UI.Trading
{
    internal class Patch_BarterSchemePanel_Awake : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.Awake));
        }

        [PatchPostfix]
        private static void PatchPostfix(BarterSchemePanel __instance,
            Transform ____requisitesContainer,
            CustomTextMeshProUGUI ____buyRestrictionLabel,
            GameObject ____validSchemeWarning
            )
        {
            RectTransform rectTransform = __instance.transform as RectTransform;
            rectTransform.offsetMin = new Vector2(rectTransform.offsetMin.x, 118f);
            rectTransform.offsetMax = new Vector2(rectTransform.offsetMax.x, -205f);

            FlowLayoutGroup flowLayoutGroup = ____requisitesContainer.GetComponent<FlowLayoutGroup>();
            flowLayoutGroup.SpacingX = 50;
            flowLayoutGroup.SpacingY = 10;
            var padding = flowLayoutGroup.padding;
            padding.left = 0;
            padding.right = 50;
            flowLayoutGroup.padding = padding;
            flowLayoutGroup.ChildForceExpandWidth = false;
            ____requisitesContainer.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            ____buyRestrictionLabel.fontSize = 14; // default is 18
            ____validSchemeWarning.GetComponentInChildren<CustomTextMeshProUGUI>().fontSize = 14; // default is 18

            Transform barterPanel = __instance.transform.Find("Scroll View/Viewport/Content/BarterPanel");
            barterPanel.Find("Space")?.gameObject.SetActive(false);
            barterPanel.Find("Items Needed From Stash")?.gameObject.SetActive(false);

            Transform myDivider = barterPanel.Find("Divider");
            if (myDivider == null)
            {
                GameObject newDivider = new GameObject("Divider", typeof(RectTransform));
                newDivider.transform.SetParent(barterPanel, false);
                newDivider.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
                RectTransform newDividerRect = newDivider.transform as RectTransform;
                newDividerRect.anchorMin = new Vector2(0, 0);
                newDividerRect.anchorMax = new Vector2(1, 0);
                newDividerRect.offsetMin = new Vector2(-1000, -15.5f);
                newDividerRect.offsetMax = new Vector2(1000, -14.5f);
                newDivider.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
            }

            RectTransform border = __instance.transform.Find("Scroll View/Border") as RectTransform;
            border.offsetMax = Vector2.zero;

            RectTransform content = __instance.transform.Find("Scroll View/Viewport/Content") as RectTransform;
            content.pivot = new Vector2(0.5f, 0);
            content.anchorMin = new Vector2(0, 0.5f);
            content.anchorMax = new Vector2(1, 0.5f);

            RectTransform background = __instance.transform.Find("Background") as RectTransform;
            background.SetParent(content);
            background.SetAsFirstSibling();
            background.anchorMin = new Vector2(0, 0);
            background.anchorMax = new Vector2(1, 1);
            background.offsetMin = new Vector2(-1000, 0);
            background.offsetMax = new Vector2(1000, 0);
            background.GetComponent<Graphic>().color = new Color(0, 0, 0, 0.78f); 
        }
    }
}
