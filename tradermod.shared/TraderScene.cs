using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Playables;

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