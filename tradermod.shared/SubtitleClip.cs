using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace tarkin.tradermod.shared
{
    public class SubtitleClip : PlayableAsset, ITimelineClipAsset
    {
        public string subtitleKey;
        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SubtitleBehaviour>.Create(graph);

            var behaviour = playable.GetBehaviour();
            behaviour.subtitleKey = subtitleKey;

            return playable;
        }
    }
}
