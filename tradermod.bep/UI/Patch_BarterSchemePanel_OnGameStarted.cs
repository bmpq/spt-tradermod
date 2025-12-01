using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace tarkin.tradermod.bep.UI
{
    internal class Patch_BarterSchemePanel_Show : ModulePatch
    {
        public static event Action OnPostfix;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BarterSchemePanel), nameof(BarterSchemePanel.Show));
        }

        [PatchPostfix]
        private static void PatchPostfix(BarterSchemePanel __instance, 
            Transform ____requisitesContainer,
            CustomTextMeshProUGUI ____buyRestrictionLabel,
            GameObject ____validSchemeWarning
            )
        {
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
                RectTransform rectTransform = newDivider.transform as RectTransform;
                rectTransform.anchorMin = new Vector2(0, 0);
                rectTransform.anchorMax = new Vector2(1, 0);
                rectTransform.offsetMin = new Vector2(-1000, -15.5f);
                rectTransform.offsetMax = new Vector2(1000, -14.5f);
                newDivider.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
            }
        }
    }
}
