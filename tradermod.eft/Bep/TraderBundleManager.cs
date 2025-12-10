using BepInEx.Logging;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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

        private static CancellationTokenSource _cts = new CancellationTokenSource();

        private static readonly Dictionary<string, string> _resolvedBundleCache = new Dictionary<string, string>();

        private static readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();
        private static readonly Dictionary<string, Task<AssetBundle>> _pendingBundleLoads = new Dictionary<string, Task<AssetBundle>>();
        private static readonly Dictionary<string, SceneLoadHandle> _pendingSceneLoads = new Dictionary<string, SceneLoadHandle>();

        private static string GetBundleNameFromId(string traderId)
        {
            if (_resolvedBundleCache.TryGetValue(traderId, out string cachedName))
                return cachedName;

            string matchingFile = Directory.GetFiles(BundleDirectory, $"{traderId}*")
                                        .Select(path => Path.GetFileName(path))
                                        .FirstOrDefault();

            if (string.IsNullOrEmpty(matchingFile))
                return null;

            _resolvedBundleCache[traderId] = matchingFile;
            return matchingFile;
        }

        public static async Task<SceneLoadHandle> LoadTraderSceneWithHandle(string traderId)
        {
            // if currently unloading, do not accept new work (shouldnt happen tho)
            if (_cts.IsCancellationRequested) return null;

            string traderSceneBundleName = GetBundleNameFromId(traderId);

            if (string.IsNullOrEmpty(traderSceneBundleName))
            {
                Logger.LogWarning($"No bundle found in {BundleDirectory} matching trader ID: {traderId}");
                return null;
            }

            try
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
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        public static async Task EnsureDependencyBundlesAreLoaded()
        {
            foreach (var depName in DependencyBundles)
            {
                await LoadAssetBundleAsync(depName);
            }
        }

        public static Task<AssetBundle> LoadAssetBundleAsync(string bundleName)
        {
#if DEBUG
            Logger.LogWarning($"Requesting bundle load: {bundleName}");
#endif

            if (_loadedAssetBundles.TryGetValue(bundleName, out AssetBundle cachedBundle) && cachedBundle != null)
                return Task.FromResult(cachedBundle);

            if (_pendingBundleLoads.TryGetValue(bundleName, out Task<AssetBundle> pendingTask))
                return pendingTask;

            var tcs = new TaskCompletionSource<AssetBundle>();
            _pendingBundleLoads[bundleName] = tcs.Task;

            // Pass the current token to the coroutine
            CoroutineRunner.Instance.StartCoroutine(LoadAssetBundleCoroutine(bundleName, tcs, _cts.Token));
            return tcs.Task;
        }

        private static IEnumerator LoadAssetBundleCoroutine(string bundleName, TaskCompletionSource<AssetBundle> tcs, CancellationToken token)
        {
            try
            {
                string fullPath = Path.Combine(BundleDirectory, bundleName);
                if (!File.Exists(fullPath))
                {
                    Logger.LogError($"Bundle missing: {fullPath}");
                    tcs.TrySetResult(null);
                    yield break;
                }

                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    yield break;
                }

#if DEBUG
                System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
#endif

                AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(fullPath);

                // Wait for load, checking cancellation
                while (!request.isDone)
                {
                    if (token.IsCancellationRequested)
                    {
                        tcs.TrySetCanceled();
                        yield break;
                    }
                    yield return null;
                }

                if (request.assetBundle == null)
                {
                    Logger.LogError($"Failed to load bundle: {bundleName}");
                    tcs.TrySetResult(null);
                }
                else
                {
                    if (token.IsCancellationRequested)
                    {
                        request.assetBundle.Unload(true);
                        tcs.TrySetCanceled();
                    }
                    else
                    {
                        _loadedAssetBundles[bundleName] = request.assetBundle;

#if DEBUG
                        sw.Stop();
                        Logger.LogWarning($"[Load Time] '{bundleName}' loaded in {sw.ElapsedMilliseconds} ms");
#endif

                        tcs.TrySetResult(request.assetBundle);
                    }
                }
            }
            finally
            {
                if (_pendingBundleLoads.ContainsKey(bundleName) && _pendingBundleLoads[bundleName] == tcs.Task)
                {
                    _pendingBundleLoads.Remove(bundleName);
                }
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

            CoroutineRunner.Instance.StartCoroutine(LoadSceneCoroutine(sceneName, newHandle, _cts.Token));

            return newHandle;
        }

        private static IEnumerator LoadSceneCoroutine(string sceneName, SceneLoadHandle handle, CancellationToken token)
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    handle.SetCanceled();
                    yield break;
                }

                AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                asyncOp.allowSceneActivation = false;

                while (asyncOp.progress < 0.9f)
                {
                    if (token.IsCancellationRequested)
                    {
                        handle.SetCanceled();
                        yield break;
                    }
                    yield return null;
                }

                handle.SetReady();

                while (!handle.ShouldActivate)
                {
                    if (token.IsCancellationRequested)
                    {
                        handle.SetCanceled();
                        yield break;
                    }
                    yield return null;
                }

                asyncOp.allowSceneActivation = true;

                while (!asyncOp.isDone)
                {
                    yield return null;
                }

                // if cancelled during final activation
                if (token.IsCancellationRequested)
                {
                    Scene badScene = SceneManager.GetSceneByName(sceneName);
                    if (badScene.IsValid()) SceneManager.UnloadSceneAsync(badScene);
                    handle.SetCanceled();
                    yield break;
                }

                Scene loadedScene = SceneManager.GetSceneByName(sceneName);
                ReplaceShadersToNative(loadedScene);
                handle.SetResult(loadedScene);
            }
            finally
            {
                if (_pendingSceneLoads.ContainsKey(sceneName) && _pendingSceneLoads[sceneName] == handle)
                {
                    _pendingSceneLoads.Remove(sceneName);
                }
            }
        }

        private static void ReplaceShadersToNative(Scene loadedScene)
        {
            if (!loadedScene.IsValid()) return;
            var roots = loadedScene.GetRootGameObjects();
            if (roots.Length == 0) return;

            var traderScene = roots[0].GetComponent<TraderScene>();
            if (traderScene == null) return;

            Renderer[] rends = traderScene.AllRenderers;
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

        public static async Task<T> LoadAssetFromBundleAsync<T>(string bundleName, string assetName, bool fuzzyAssetName = false) where T : UnityEngine.Object
        {
            AssetBundle bundle = await LoadAssetBundleAsync(bundleName);
            if (bundle == null)
            {
                Logger.LogError($"Cannot load asset '{assetName}': Bundle '{bundleName}' failed to load.");
                return null;
            }

            if (_cts.IsCancellationRequested) return null;
            var tcs = new TaskCompletionSource<T>();
            CoroutineRunner.Instance.StartCoroutine(LoadAssetCoroutine(bundle, assetName, fuzzyAssetName, tcs, _cts.Token));

            return await tcs.Task;
        }

        private static IEnumerator LoadAssetCoroutine<T>(AssetBundle bundle, string assetName, bool fuzzyAssetName, TaskCompletionSource<T> tcs, CancellationToken token) where T : UnityEngine.Object
        {
            if (token.IsCancellationRequested)
            {
                tcs.TrySetCanceled();
                yield break;
            }

            if (fuzzyAssetName)
            {
                foreach (var fullPath in bundle.GetAllAssetNames())
                {
                    if (fullPath.Contains(assetName))
                    {
                        assetName = fullPath;
                        break;
                    }
                }
            }

            AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);
            while (!request.isDone)
            {
                if (token.IsCancellationRequested)
                {
                    tcs.TrySetCanceled();
                    yield break;
                }
                yield return null;
            }

            if (request.asset == null)
            {
                Logger.LogError($"Asset '{assetName}' of type {typeof(T).Name} not found in bundle '{bundle.name}'.");
                tcs.TrySetResult(null);
            }
            else
            {
                tcs.TrySetResult(request.asset as T);
            }
        }

        public static async Task UnloadAllBundles()
        {
            Logger.LogInfo("UnloadAllBundles...");

            _cts.Cancel();

            var pendingBundleTasks = _pendingBundleLoads.Values.ToList();
            var pendingSceneTasks = _pendingSceneLoads.Values.Select(h => h.WaitUntilReady()).ToList();

            try { await Task.WhenAll(pendingBundleTasks); }
            catch {}
            try { await Task.WhenAll(pendingSceneTasks); }
            catch {}

            foreach (var kvp in _loadedAssetBundles)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Unload(true);
                }
            }

            _loadedAssetBundles.Clear();
            _pendingBundleLoads.Clear();
            _pendingSceneLoads.Clear();

            _cts.Dispose();
            _cts = new CancellationTokenSource();

            Logger.LogInfo("UnloadAllBundles complete.");
        }
    }
}