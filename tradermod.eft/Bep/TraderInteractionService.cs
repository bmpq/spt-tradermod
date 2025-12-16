using System;
using System.Collections.Generic;
using EFT.AnimationSequencePlayer;
using tarkin.tradermod.shared;

#if SPT_4_0
using CombinedAnimationData = GClass4067;
#endif

namespace tarkin.tradermod.eft
{
    internal class TraderInteractionService
    {
        private readonly DialogDataWrapper _dialogData;
        private readonly Dictionary<string, DateTime> _lastSeenTimestamps = new Dictionary<string, DateTime>();
        private const float GREETING_COOLDOWN_SEC = 60;

        public TraderInteractionService(DialogDataWrapper dialogData)
        {
            _dialogData = dialogData;
        }

        public void MarkTraderSeen(string traderId)
        {
            _lastSeenTimestamps[traderId] = DateTime.Now;
        }

        public void PlayAnimation(TraderScene scene, string traderId, ETraderDialogType interactionType)
        {
            if (scene == null) return;

            var cad = GetAnimationData(scene, traderId, interactionType);

            SequenceReader npc = scene.TraderGameObject.GetComponent<SequenceReader>();
            if (npc != null && cad != null)
            {
                npc.Play(cad);
            }
        }

        private CombinedAnimationData GetAnimationData(TraderScene scene, string traderId, ETraderDialogType type)
        {
            if (type == ETraderDialogType.Greetings && !ShouldPlayGreeting(traderId))
                return null;

            List<string> dialogs = scene.GetDialogs(type);

            if (dialogs != null && dialogs.Count > 0)
            {
                string randomId = dialogs[UnityEngine.Random.Range(0, dialogs.Count)];
                return _dialogData.GetLine(randomId)?.AnimationData;
            }
            return null;
        }

        private bool ShouldPlayGreeting(string traderId)
        {
            if (string.IsNullOrEmpty(traderId)) return false;

            if (!_lastSeenTimestamps.TryGetValue(traderId, out DateTime lastTime)) 
                return true; // first visit

            return (DateTime.Now - lastTime).TotalSeconds > GREETING_COOLDOWN_SEC;
        }
    }
}