using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.AnimationSequencePlayer;
using Newtonsoft.Json;
using tarkin.tradermod.shared;
using UnityEngine;
using UnityEngine.Playables;

#if SPT_4_0
using CombinedAnimationData = GClass4067;
using SequencePlayer = GClass4065;
#endif

namespace tarkin.tradermod.eft
{
    internal class TraderInteractionService : IDisposable
    {
        private readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource(nameof(TraderInteractionService));

        private readonly DialogDataWrapper _dialogData;

        private readonly Dictionary<string, DateTime> _lastSeenTimestamps = new Dictionary<string, DateTime>();
        private HashSet<string> _playedChatterLines = new HashSet<string>();

        private readonly string _savePath;
        private bool _isDirty;

        private bool _isAnimatingTask;

        private const float GREETING_COOLDOWN_SEC =
#if DEBUG
            1;
#else
            60;
#endif

        public TraderInteractionService(DialogDataWrapper dialogData)
        {
            _dialogData = dialogData;

            string currentProfileId = Singleton<ClientApplication<ISession>>.Instance?.GetClientBackEndSession()?.Profile?.Id;
            if (string.IsNullOrEmpty(currentProfileId))
                currentProfileId = "unknownprofile";

            string dir = Path.Combine(BepInEx.Paths.PluginPath, "tarkin", "tradermod_saves");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _savePath = Path.Combine(dir, $"{currentProfileId}.json");

            LoadPlayedLines();
        }

        private void LoadPlayedLines()
        {
            if (!File.Exists(_savePath)) return;

            try
            {
                var loaded = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(_savePath));
                if (loaded != null)
                {
                    _playedChatterLines = new HashSet<string>(loaded);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load trader interaction history: {ex.Message}");
                _playedChatterLines = new HashSet<string>();
            }
        }

        public void SavePlayedLines()
        {
            if (!_isDirty || string.IsNullOrEmpty(_savePath)) return;

            try
            {
                File.WriteAllText(_savePath, JsonConvert.SerializeObject(_playedChatterLines));
                _isDirty = false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save trader interaction history: {ex.Message}");
            }
        }

        public void MarkTraderSeen(string traderId)
        {
            _lastSeenTimestamps[traderId] = DateTime.Now;
        }

        public bool IsBusy(TraderScene scene)
        {
            if (_isAnimatingTask) return true;

            if (scene != null && scene.Director != null && scene.Director.state == PlayState.Playing)
                return true;

            if (SequencePlayer.IsPlaying)
                return true;

            return false;
        }

        public async Task PlayAnimation(TraderScene scene, string traderId, ETraderDialogType interactionType)
        {
            if (scene == null || scene.TraderGameObject == null) 
                return;

            PlayableAsset selectedTimeline = null;
            CombinedAnimationData selectedNativeData = null;
            bool directorAvailable = scene.Director != null;

            if (directorAvailable && scene.TimelineDialogs.TryGetValue(interactionType, out var dialogList) && dialogList.Count > 0)
            {
                selectedTimeline = dialogList.Random();
            }

            if (selectedTimeline == null)
            {
                selectedNativeData = GetAnimationData(scene, traderId, interactionType);
            }

            if (selectedTimeline == null && selectedNativeData == null)
            {
                return;
            }

            if (directorAvailable) scene.Director.Stop();
            await StopNativeFormatAnimation();

            _isAnimatingTask = true;
            try
            {
                if (selectedTimeline != null)
                {
                    scene.Director.playableAsset = selectedTimeline;
                    var tcs = new TaskCompletionSource<bool>();
                    Action<PlayableDirector> onStopped = null;
                    onStopped = (director) =>
                    {
                        scene.Director.stopped -= onStopped;
                        tcs.TrySetResult(true);
                    };
                    scene.Director.stopped += onStopped;
                    scene.Director.Play();
                    await tcs.Task;
                }
                else if (selectedNativeData != null)
                {
                    SequenceReader npc = scene.TraderGameObject.GetComponent<SequenceReader>();
                    if (npc != null)
                    {
                        await npc.Play(selectedNativeData);
                    }
                    else
                    {
                        _logger.LogWarning($"Trader {traderId} missing SequenceReader component.");
                    }
                }
            }
            finally
            {
                _isAnimatingTask = false;
            }
        }

        private CombinedAnimationData GetAnimationData(TraderScene scene, string traderId, ETraderDialogType type)
        {
            if (type == ETraderDialogType.Greetings && !ShouldPlayGreeting(traderId))
                return null;

            List<string> dialogs = scene.GetDialogs(type);
            if (dialogs == null || dialogs.Count == 0) return null;

            string selectedId;

            if (type == ETraderDialogType.Chatter)
            {
                // filter out lines already played
                var available = dialogs.Where(id => !_playedChatterLines.Contains(id)).ToList();

                if (available.Count > 0)
                {
                    selectedId = available[UnityEngine.Random.Range(0, available.Count)];
                    RecordPlayedLine(selectedId);
                }
                else
                {
                    // should not reach here
                    return null;
                }
            }
            else
            {
                selectedId = dialogs[UnityEngine.Random.Range(0, dialogs.Count)];
            }

            var line = _dialogData.GetLine(selectedId);
            if (line == null)
            {
                _logger.LogWarning($"Dialog ID {selectedId} found in scene config but missing in DialogData.");
                return null;
            }

            return line.AnimationData;
        }

        private bool ShouldPlayGreeting(string traderId)
        {
            if (string.IsNullOrEmpty(traderId)) return false;

            if (!_lastSeenTimestamps.TryGetValue(traderId, out DateTime lastTime))
                return true; // first visit

            return (DateTime.Now - lastTime).TotalSeconds > GREETING_COOLDOWN_SEC;
        }

        public bool HasUnplayedChatter(TraderScene scene)
        {
            if (scene == null) 
                return false;

            List<string> dialogs = scene.GetDialogs(ETraderDialogType.Chatter);
            if (dialogs == null || dialogs.Count == 0) 
                return false;

            return dialogs.Any(id => !_playedChatterLines.Contains(id));
        }

        private void RecordPlayedLine(string id)
        {
            if (_playedChatterLines.Add(id))
            {
                _isDirty = true;
            }
        }

        // needed because eft animation logic is static and keeps running even when the caller game object is no longer active
        public async Task StopNativeFormatAnimation()
        {
            SequencePlayer.StopSequence();
            _isAnimatingTask = false;
            await Task.Yield();
        }

        public void Dispose()
        {
            SavePlayedLines();
        }
    }
}