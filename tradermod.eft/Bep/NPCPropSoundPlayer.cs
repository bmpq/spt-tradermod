using Audio.ConfiguredAudioPlayer;
using Audio.NPC;
using Comfort.Common;
using EFT.NPC;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace tarkin.tradermod.eft.Bep
{
    internal class NPCPropSoundPlayer : MonoBehaviour
    {
        NPCAnimationsEventReceiver eventReceiver;
        AudioClipDataConfigurator audioClipData;

        AudioSource audioSource;

        void Start()
        {
            audioClipData = GetComponent<AudioClipDataConfigurator>();
            audioSource = (AudioSource)AccessTools.Field(typeof(NPCAudioSourceSpatializeController), "_speechSource").GetValue(GetComponent<NPCAudioSourceSpatializeController>());
        }

        void OnEnable()
        {
            eventReceiver = GetComponent<NPCAnimationsEventReceiver>();

            // stops working on reenable without force reinit
            (AccessTools.Field(typeof(NPCAnimationsEventReceiver), "list_0").GetValue(eventReceiver) as System.Collections.IList)?.Clear();
            eventReceiver.Initialize(GetComponent<Animator>());

            eventReceiver.OnNeedToPlaySomeSound += EventReceiver_OnNeedToPlaySomeSound;
        }

        void OnDisable()
        {
            eventReceiver.OnNeedToPlaySomeSound -= EventReceiver_OnNeedToPlaySomeSound;
        }

        private void EventReceiver_OnNeedToPlaySomeSound(string name)
        {
            if (audioClipData.TryGetValue(name, out var clipConfig))
            {
                audioSource.PlayOneShot(clipConfig.clip, 1f);
            }
        }
    }
}
