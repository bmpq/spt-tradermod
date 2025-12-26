using EFT.UI;
using System;
using tarkin.tradermod.bep;
using tarkin.tradermod.shared;
using UnityEngine;

#if SPT_4_0
using SequencePlayer = GClass4065;
#endif

namespace tarkin.tradermod.eft.Bep
{
    internal class SubtitlesManager : IDisposable
    {
        DialogDataWrapper dialogData;
        SubtitlesUI view;

        internal SubtitlesManager(DialogDataWrapper dialogData)
        {
            this.dialogData = dialogData;
            SpawnSubtitlesController();

            SequencePlayer.OnSubtitleChanged += SequencePlayer_OnSubtitleChanged;
        }

        private void SequencePlayer_OnSubtitleChanged(string subtitleId)
        {
            if (view != null)
            {
                view.SetText(dialogData.GetLocalizedSubtitle(subtitleId));
            }
        }

        public async void SpawnSubtitlesController()
        {
            GameObject prefabSubtitlesController = await TraderBundleManager.LoadAssetFromBundleAsync<GameObject>("vendors_ui", "SubtitlesUI", true);

            GameObject go = GameObject.Instantiate(prefabSubtitlesController, ItemUiContext.Instance.transform);
            view = go.GetComponent<SubtitlesUI>();
            view.SetText(string.Empty);
        }

        public void Dispose()
        {
            SequencePlayer.OnSubtitleChanged -= SequencePlayer_OnSubtitleChanged;

            if (view != null)
                GameObject.Destroy(view.gameObject);
        }
    }
}
