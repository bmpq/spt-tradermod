using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using tarkin.tradermod.eft;
using tarkin.tradermod.shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.bep
{
    internal class TraderBundleManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(TraderBundleManager));

        public static readonly string BundleDirectory = Path.Combine(BepInEx.Paths.PluginPath, "tarkin", "bundles", "vendors");
        private static readonly string[] DependencyBundles = new string[] { "vendors_shared" };
        private static readonly Dictionary<string, string> _traderIdToBundleMap = new Dictionary<string, string>
        {
            { "579dc571d53a0658a154fbec", "vendors_fence" },
            { "5c0647fdd443bc2504c2d371", "vendors_jaeger" },
            { "5a7c2eca46aef81a7ca2145d", "vendors_mechanic" },
            { "54cb50c76803fa8b248b4571", "vendors_prapor" },
            { "5ac3b934156ae10c4430e83c", "vendors_ragman" },
            { "58330581ace78e27b8b10cee", "vendors_skier" },
            { "54cb57776803fa99248b456e", "vendors_therapist" },
        };

        private static readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private static readonly Dictionary<string, Task<AssetBundle>> _pendingBundleLoads = new Dictionary<string, Task<AssetBundle>>();

        private static readonly Dictionary<string, SceneLoadHandle> _pendingSceneLoads = new Dictionary<string, SceneLoadHandle>();

        public static async Task<SceneLoadHandle> LoadTraderSceneWithHandle(string traderId)
        {
            if (!_traderIdToBundleMap.TryGetValue(traderId, out string traderSceneBundleName))
            {
                Logger.LogWarning($"No bundle associated with traderid:{traderId}");
                return null;
            }

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

                static void ReplaceShadersToNative(Renderer[] rends)
                {
                    int counter = 0;
                    foreach (Renderer rend in rends)
                    {
                        if (rend == null) continue;
                        foreach (Material mat in rend.sharedMaterials)
                        {
                            if (mat == null || mat.shader == null) continue;
                            Shader nativeShader = Shader.Find(mat.shader.name);
                            if (nativeShader != null && mat.shader != nativeShader)
                            {
                                mat.shader = nativeShader;
                                counter++;
                            }
                        }
                    }
                    Plugin.Log.LogInfo($"Replaced {counter} shaders to native");
                }
                ReplaceShadersToNative(loadedScene.GetRootGameObjects()[0].GetComponent<TraderScene>().AllRenderers);

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