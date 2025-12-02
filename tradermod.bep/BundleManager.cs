using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.bep
{
    internal class BundleManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(BundleManager));

        private static readonly string BundleDirectory = Path.Combine(BepInEx.Paths.PluginPath, "tarkin", "bundles", "vendors");
        private static readonly string[] DependencyBundles = new string[] { "vendors_shared" };

        private static readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private static readonly Dictionary<string, Task<AssetBundle>> _pendingBundleLoads = new Dictionary<string, Task<AssetBundle>>();
        private static readonly Dictionary<string, Task<Scene>> _pendingSceneLoads = new Dictionary<string, Task<Scene>>();

        public static async Task LoadTraderScene(string traderSceneBundleName)
        {
            await EnsureDependencyBundlesAreLoaded();

            AssetBundle bundle = await LoadAssetBundleAsync(traderSceneBundleName);
            if (bundle == null) return;

            if (!bundle.isStreamedSceneAssetBundle || bundle.GetAllScenePaths().Length == 0)
            {
                Logger.LogError($"Bundle {traderSceneBundleName} is not a scene bundle!");
                return;
            }

            string sceneName = Path.GetFileNameWithoutExtension(bundle.GetAllScenePaths()[0]);

            await LoadSceneAsync(sceneName);
        }

        private static async Task EnsureDependencyBundlesAreLoaded()
        {
            foreach (var depName in DependencyBundles)
            {
                await LoadAssetBundleAsync(depName);
            }
        }

        public static Task<AssetBundle> LoadAssetBundleAsync(string bundleName)
        {
            if (_loadedAssetBundles.TryGetValue(bundleName, out AssetBundle cachedBundle) && cachedBundle != null)
                return Task.FromResult(cachedBundle);

            if (_pendingBundleLoads.TryGetValue(bundleName, out Task<AssetBundle> pendingTask))
                return pendingTask;

            var tcs = new TaskCompletionSource<AssetBundle>();
            _pendingBundleLoads[bundleName] = tcs.Task;

            CoroutineRunner.Instance.StartCoroutine(LoadAssetBundleCoroutine(bundleName, tcs));

            return tcs.Task;
        }

        private static IEnumerator LoadAssetBundleCoroutine(string bundleName, TaskCompletionSource<AssetBundle> tcs)
        {
            try
            {
                string fullPath = Path.Combine(BundleDirectory, bundleName);
                if (!File.Exists(fullPath))
                {
                    Logger.LogError($"Bundle missing: {fullPath}");
                    tcs.SetResult(null);
                    yield break;
                }

                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(fullPath);
                yield return request;

                if (request.assetBundle == null)
                {
                    Logger.LogError($"Failed to load bundle: {bundleName}");
                    tcs.SetResult(null);
                }
                else
                {
                    _loadedAssetBundles[bundleName] = request.assetBundle;
                    tcs.SetResult(request.assetBundle);
                }
            }
            finally
            {
                _pendingBundleLoads.Remove(bundleName);
            }
        }

        public static Task<Scene> LoadSceneAsync(string sceneName)
        {
            Scene existingScene = SceneManager.GetSceneByName(sceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                return Task.FromResult(existingScene);
            }

            if (_pendingSceneLoads.TryGetValue(sceneName, out Task<Scene> pendingTask))
            {
                Logger.LogInfo($"Scene {sceneName} is already loading. Waiting...");
                return pendingTask;
            }

            var tcs = new TaskCompletionSource<Scene>();
            _pendingSceneLoads[sceneName] = tcs.Task;

            CoroutineRunner.Instance.StartCoroutine(LoadSceneCoroutine(sceneName, tcs));

            return tcs.Task;
        }

        private static IEnumerator LoadSceneCoroutine(string sceneName, TaskCompletionSource<Scene> tcs)
        {
            try
            {
                Scene existing = SceneManager.GetSceneByName(sceneName);
                if (existing.IsValid() && existing.isLoaded)
                {
                    tcs.SetResult(existing);
                    yield break;
                }

                AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                while (!asyncOp.isDone)
                {
                    yield return null;
                }

                Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                if (loadedScene.IsValid())
                {
                    tcs.SetResult(loadedScene);
                }
                else
                {
                    Logger.LogError($"Scene {sceneName} loaded but handle is invalid.");
                    tcs.SetResult(default);
                }
            }
            finally
            {
                _pendingSceneLoads.Remove(sceneName);
            }
        }

        public static void UnloadAllBundles()
        {
            foreach (var bundle in _loadedAssetBundles.Values)
            {
                if (bundle != null) bundle.Unload(true);
            }
            _loadedAssetBundles.Clear();
            _pendingBundleLoads.Clear();
            _pendingSceneLoads.Clear();
        }

        private class CoroutineRunner : MonoBehaviour
        {
            private static CoroutineRunner _instance;
            public static CoroutineRunner Instance
            {
                get
                {
                    if (_instance == null)
                    {
                        var go = new GameObject("TarkinBundleRunner");
                        _instance = go.AddComponent<CoroutineRunner>();
                        DontDestroyOnLoad(go);
                    }
                    return _instance;
                }
            }
        }
    }
}