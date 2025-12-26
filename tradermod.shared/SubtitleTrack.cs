using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace tarkin.tradermod.shared
{
    [TrackColor(0.19f, 0.65f, 0.76f)]
    [TrackClipType(typeof(SubtitleClip))]
    public class SubtitleTrack : TrackAsset
    {
    }

    public class SubtitleBehaviour : PlayableBehaviour
    {
        public static event Action<string> OnSubtitleChange;

        public string subtitleKey;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (Application.isPlaying && !string.IsNullOrEmpty(subtitleKey))
            {
                OnSubtitleChange?.Invoke(subtitleKey);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (Application.isPlaying)
            {
                OnSubtitleChange?.Invoke(string.Empty);
            }
        }
    }
}
