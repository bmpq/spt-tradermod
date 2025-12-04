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

        public static readonly string BundleDirectory = Path.Combine(BepInEx.Paths.PluginPath, "tarkin", "bundles", "vendors");
        private static readonly string[] DependencyBundles = new string[] { "vendors_shared" };

        private static readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private static readonly Dictionary<string, Task<AssetBundle>> _pendingBundleLoads = new Dictionary<string, Task<AssetBundle>>();

        private static readonly Dictionary<string, SceneLoadHandle> _pendingSceneLoads = new Dictionary<string, SceneLoadHandle>();

        public static async Task<SceneLoadHandle> LoadTraderSceneWithHandle(string traderSceneBundleName)
        {
            await EnsureDependencyBundlesAreLoaded();

            AssetBundle bundle = await LoadAssetBundleAsync(traderSceneBundleName);
            if (bundle == null) return null;

            if (!bundle.isStreamedSceneAssetBundle || bundle.GetAllScenePaths().Length == 0)
            {
                Logger.LogError($"Bundle {traderSceneBundleName} is not a scene bundle!");
                return null;
            }

            string sceneName = Path.GetFileNameWithoutExtension(bundle.GetAllScenePaths()[0]);

            return PrepareScene(sceneName);
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

        public static SceneLoadHandle PrepareScene(string sceneName)
        {
            Scene existingScene = SceneManager.GetSceneByName(sceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                var handle = new SceneLoadHandle();
                handle.SetReady();
                handle.SetResult(existingScene);
                return handle;
            }

            if (_pendingSceneLoads.TryGetValue(sceneName, out SceneLoadHandle existingHandle))
            {
                return existingHandle;
            }

            var newHandle = new SceneLoadHandle();
            _pendingSceneLoads[sceneName] = newHandle;

            CoroutineRunner.Instance.StartCoroutine(LoadSceneCoroutine(sceneName, newHandle));

            return newHandle;
        }

        private static IEnumerator LoadSceneCoroutine(string sceneName, SceneLoadHandle handle)
        {
            try
            {
                AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                asyncOp.allowSceneActivation = false;

                while (asyncOp.progress < 0.9f)
                {
                    yield return null;
                }

                handle.SetReady();

                while (!handle.ShouldActivate)
                {
                    yield return null;
                }

                asyncOp.allowSceneActivation = true;

                while (!asyncOp.isDone)
                {
                    yield return null;
                }

                Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                handle.SetResult(loadedScene);
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
    }
}