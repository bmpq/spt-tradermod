using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Playables;
using Newtonsoft.Json;

namespace tarkin.tradermod.shared
{
    public enum ETraderDialogType
    {
        Greetings,
        Goodbyes,
        Chatter,
        QuestAvailable,
        QuestFailed,
        GreetingsWhileWork,
        NoJob,
        TradeStart,
        Handover,
        Dunno,
        QuestStart
    }

    public class TraderScene : SerializedMonoBehaviour
    {
        [SerializeField] private Transform cameraPoint;
        public Transform CameraPoint => cameraPoint;

        [SerializeField] private Renderer[] allRenderers;
        public Renderer[] AllRenderers => allRenderers;

        [SerializeField] private Animator traderGameObject;
        public Animator TraderGameObject => traderGameObject;

        [SerializeField] private string traderId;
        public string TraderId => traderId;

        [SerializeField]
        [DictionaryDrawerSettings(KeyLabel = "Type", ValueLabel = "Dialog IDs")]
        private Dictionary<ETraderDialogType, List<string>> _dialogs = new Dictionary<ETraderDialogType, List<string>>();

        public Dictionary<ETraderDialogType, List<string>> Dialogs => _dialogs;

        [SerializeField] private PlayableDirector director;
        public PlayableDirector Director => director;

        [SerializeField] private Dictionary<ETraderDialogType, List<PlayableAsset>> _timelineDialogs;
        public Dictionary<ETraderDialogType, List<PlayableAsset>> TimelineDialogs => _timelineDialogs;

        [SerializeField] private List<TextAsset> _extraLocales;
        private Dictionary<string, Dictionary<string, string>> _parsedExtraLocales;
        public Dictionary<string, Dictionary<string, string>> GetExtraLocales()
        {
            if (_parsedExtraLocales == null)
            {
                _parsedExtraLocales = new Dictionary<string, Dictionary<string, string>>();

                if (_extraLocales != null)
                {
                    foreach (var asset in _extraLocales)
                    {
                        if (asset == null) continue;

                        try
                        {
                            var content = JsonConvert.DeserializeObject<Dictionary<string, string>>(asset.text);
                            if (content != null && !_parsedExtraLocales.ContainsKey(asset.name))
                            {
                                _parsedExtraLocales.Add(asset.name, content);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"Failed to parse locale '{asset.name}': {ex.Message}");
                        }
                    }
                }
            }

            return _parsedExtraLocales;
        }

        public List<string> GetDialogs(ETraderDialogType type)
        {
            if (_dialogs.TryGetValue(type, out var list))
            {
                return list;
            }
            return null;
        }

        [SerializeField] private string chatterPrompt;
        public string ChatterPrompt => chatterPrompt;

#if UNITY_EDITOR
        private void OnValidate()
        {
            _parsedExtraLocales = null;
            if (GetExtraLocales().Count > 0)
            {
                foreach (var locale in GetExtraLocales())
                {
                    Debug.Log($"{locale.Key} - {locale.Value.Count} keys");
                }
            }
            // make sure not in prefab mode
            if (!UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this))
            {
                if (transform.parent != null)
                    Debug.LogError($"[TraderScene] '{name}' is not at the root of the scene! It must not have a parent.", this);

                if (gameObject.scene.rootCount > 1)
                    Debug.LogError($"[TraderScene] '{name}' has siblings! It must be the only object at its hierarchy level.", this);
            }

            var freshList = GetComponentsInChildren<Renderer>(true);
            if (HasListChanged(allRenderers, freshList))
            {
                allRenderers = freshList;
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"[TraderScene] Updated renderer list. Count: {allRenderers.Length}");
            }
        }

        private bool HasListChanged(Renderer[] current, Renderer[] fresh)
        {
            if (current == null) return true;
            if (current.Length != fresh.Length) return true;
            for (int i = 0; i < current.Length; i++)
            {
                if (current[i] != fresh[i])
                {
                    return true;
                }
            }

            return false;
        }

        private void OnDrawGizmosSelected()
        {
            OnValidate();
        }
#endif
    }
}