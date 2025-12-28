using EFT.UI;
using System;
using tarkin.tradermod.shared;
using UnityEngine;
using UnityEngine.UI;

namespace tarkin.tradermod.eft.Bep
{
    public class TraderUIManager : IDisposable
    {
        private readonly BepInEx.Logging.ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource(nameof(TraderUIManager));

        GameObject traderFaceButton;
        HoverTooltipArea traderFaceTooltip;

        public event Action OnTraderFaceClick;

        DialogDataWrapper dialogData;

        public TraderUIManager(TraderScreensGroup parent, DialogDataWrapper dialogData)
        {
            this.dialogData = dialogData;

            traderFaceButton = new GameObject("TraderFaceButton", typeof(RectTransform));
            RectTransform rectTransform = traderFaceButton.GetComponent<RectTransform>();

            rectTransform.SetParent(parent.transform, false);
            rectTransform.SetAsLastSibling();

            rectTransform.anchorMin = new Vector2(0.5f, 1f);
            rectTransform.anchorMax = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0, -363f);
            rectTransform.sizeDelta = new Vector2(200, 200);

            Image image = traderFaceButton.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0);

            CanvasGroup canvasGroup = traderFaceButton.AddComponent<CanvasGroup>();

            traderFaceTooltip = traderFaceButton.AddComponent<HoverTooltipArea>();
            traderFaceTooltip.SetMessageText("Trader");

            ButtonFeedback buttonFeedback = traderFaceButton.AddComponent<ButtonFeedback>();

            ClickTrigger clickTrigger = traderFaceButton.AddComponent<ClickTrigger>();
            clickTrigger.Init((_) => OnTraderFaceClick?.Invoke());
        }

        public void SetTraderCanInteractState(TraderScene traderScene, bool canInteract)
        {
            if (traderScene == null)
            {
                traderFaceButton.SetActive(false);
                return;
            }

            var subtitle = dialogData.GetLocalizedSubtitle(traderScene.ChatterPrompt);
            bool hasValidText = !string.IsNullOrEmpty(subtitle);

            bool shouldShow = hasValidText && canInteract;

            traderFaceButton.SetActive(shouldShow);

            if (shouldShow)
            {
                traderFaceTooltip.SetMessageText(subtitle);
            }
        }

        public void Dispose()
        {
            if (traderFaceButton != null)
                GameObject.Destroy(traderFaceButton);
        }
    }
}
