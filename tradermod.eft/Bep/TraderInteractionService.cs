using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.AnimationSequencePlayer;
using Newtonsoft.Json;
using tarkin.tradermod.shared;
using UnityEngine;

#if SPT_4_0
using CombinedAnimationData = GClass4067;
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

        private const float GREETING_COOLDOWN_SEC = 60;

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

        public void PlayAnimation(TraderScene scene, string traderId, ETraderDialogType interactionType)
        {
            if (scene == null || scene.TraderGameObject == null) return;

            var cad = GetAnimationData(scene, traderId, interactionType);
            if (cad == null) return;

            SequenceReader npc = scene.TraderGameObject.GetComponent<SequenceReader>();
            if (npc != null)
            {
                npc.Play(cad);
            }
            else
            {
                _logger.LogWarning($"Trader {traderId} missing SequenceReader component.");
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

        public void Dispose()
        {
            SavePlayedLines();
        }
    }
}