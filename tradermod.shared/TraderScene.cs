using System.Collections.Generic;
using UnityEngine;

namespace tarkin.tradermod.shared
{
    public class TraderScene : MonoBehaviour
    {
        [SerializeField] private Transform cameraPoint;
        public Transform CameraPoint => cameraPoint;

        [SerializeField] private Renderer[] allRenderers;
        public Renderer[] AllRenderers => allRenderers;
        [SerializeField] private Animator trader;
        public Animator Trader => trader;

        [SerializeField] private string TraderId;

        [SerializeField] private List<string> DialogCombinedAnimGreetings;
        [SerializeField] private List<string> DialogCombinedAnimGoodbye;
        [SerializeField] private List<string> DialogCombinedAnimChatter;
        [SerializeField] private List<string> DialogCombinedAnimQuestAvailable;
        [SerializeField] private List<string> DialogCombinedAnimGreetingsWhileWork;
        [SerializeField] private List<string> DialogCombinedAnimNoJob;
        [SerializeField] private List<string> DialogCombinedAnimTradeStart;
        [SerializeField] private List<string> DialogCombinedAnimHandover;
        [SerializeField] private List<string> DialogCombinedAnimDunno;

        public List<string> GetDialogsGreetings() => DialogCombinedAnimGreetings;

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