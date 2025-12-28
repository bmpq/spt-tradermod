using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using tarkin.tradermod.shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.eft.Bep
{
    internal class TraderBundleManager
    {
        private static readonly ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(TraderBundleManager));

        public static readonly string BundleDirectory = Path.Combine(BepInEx.Paths.PluginPath, "tarkin", "bundles", "vendors");
        private static readonly string[] DependencyBundles = new string[] { "vendors_shared" };

        private static readonly Dictionary<string, string> _resolvedBundleCache = new Dictionary<string, string>();
        private static readonly Dictionary<string, AssetBundle> _loadedAssetBundles = new Dictionary<string, AssetBundle>();

        private static readonly SemaphoreSlim _bundleLock = new SemaphoreSlim(1, 1);

        private static string GetBundleNameFromId(string traderId)
        {
            if (_resolvedBundleCache.TryGetValue(traderId, out string cachedName))
                return cachedName;

            if (!Directory.Exists(BundleDirectory)) 
                return null;

            string matchingFile = Directory.GetFiles(BundleDirectory, $"{traderId}*")
                                        .Select(path => Path.GetFileName(path))
                                        .FirstOrDefault();

            if (!string.IsNullOrEmpty(matchingFile))
                _resolvedBundleCache[traderId] = matchingFile;

            return matchingFile;
        }

        public static async Task<Scene?> LoadTraderScene(string traderId)
        {
            string traderSceneBundleName = GetBundleNameFromId(traderId);

            if (string.IsNullOrEmpty(traderSceneBundleName))
            {
                Logger.LogWarning($"No bundle found for trader ID: {traderId}");
                return null;
            }

            await _bundleLock.WaitAsync();
            try
            {
                await EnsureDependencyBundlesAreLoaded();

                AssetBundle bundle = await LoadAssetBundleAsync(traderSceneBundleName);
                if (bundle == null) return null;

                if (!bundle.isStreamedSceneAssetBundle || bundle.GetAllScenePaths().Length == 0)
                {
                    Logger.LogError($"Bundle {traderSceneBundleName} contains no scenes.");
                    return null;
                }

                string sceneName = Path.GetFileNameWithoutExtension(bundle.GetAllScenePaths()[0]);

                Scene existingScene = SceneManager.GetSceneByName(sceneName);
                if (existingScene.IsValid() && existingScene.isLoaded)
                {
                    return existingScene;
                }

                await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                Scene loadedScene = SceneManager.GetSceneByName(sceneName);

                if (loadedScene.IsValid())
                {
                    ReplaceShadersToNative(loadedScene);
                    return loadedScene;
                }

                Logger.LogError($"Scene loaded but handle is invalid: {sceneName}");
                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Exception loading trader scene: {ex}");
                return null;
            }
            finally
            {
                _bundleLock.Release();
            }
        }

        public static async Task EnsureDependencyBundlesAreLoaded()
        {
            foreach (var depName in DependencyBundles)
            {
                await LoadAssetBundleAsync(depName);
            }
        }

        private static async Task<AssetBundle> LoadAssetBundleAsync(string bundleName)
        {
            if (_loadedAssetBundles.TryGetValue(bundleName, out AssetBundle cachedBundle) && cachedBundle != null)
                return cachedBundle;

            string fullPath = Path.Combine(BundleDirectory, bundleName);
            if (!File.Exists(fullPath))
            {
                Logger.LogError($"Bundle file missing: {fullPath}");
                return null;
            }

            var op = AssetBundle.LoadFromFileAsync(fullPath);
            await op;

            if (op.assetBundle == null)
            {
                Logger.LogError($"Failed to load asset bundle: {bundleName}");
                return null;
            }

            _loadedAssetBundles[bundleName] = op.assetBundle;
            return op.assetBundle;
        }

        public static async Task<T> LoadAssetFromBundleAsync<T>(string bundleName, string assetName, bool fuzzyAssetName = false) where T : UnityEngine.Object
        {
            AssetBundle bundle = await LoadAssetBundleAsync(bundleName);

            if (bundle == null)
            {
                Logger.LogError($"Cannot load asset '{assetName}': Bundle '{bundleName}' failed to load.");
                return null;
            }

            if (fuzzyAssetName)
            {
                string foundPath = bundle.GetAllAssetNames()
                    .FirstOrDefault(path => path.Contains(assetName));

                if (!string.IsNullOrEmpty(foundPath))
                {
                    assetName = foundPath;
                }
                else
                {
                    Logger.LogWarning($"Fuzzy search failed to find asset containing '{assetName}' in bundle '{bundleName}'");
                }
            }

            AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);

            await request;

            if (request.asset == null)
            {
                Logger.LogError($"Asset '{assetName}' of type {typeof(T).Name} not found in bundle '{bundleName}'.");
                return null;
            }

            return request.asset as T;
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

        public static void UnloadAllBundles()
        {
            foreach (var kvp in _loadedAssetBundles)
            {
                if (kvp.Value != null) kvp.Value.Unload(true);
            }
            _loadedAssetBundles.Clear();
            _resolvedBundleCache.Clear();
        }
    }
}