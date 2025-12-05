using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tarkin.tradermod.bep.Patches;
using UnityEngine;
using UnityEngine.SceneManagement;

using CombinedAnimationData = GClass4067;
using AnimationParams = GClass4071;
using LipSyncParams = GClass4072;
using SubtitleParams = GClass4073;
using tarkin.tradermod.shared;
using EFT.AnimationSequencePlayer;

namespace tarkin.tradermod.bep
{
    internal class TraderScenesManager
    {
        private readonly ManualLogSource _logger;
        private Camera _cam;

        Coroutine fadeCoroutine;

        private string _requestedTraderId = null;

        private readonly Dictionary<string, Scene> _openedScenes = new Dictionary<string, Scene>();

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

        public TraderScenesManager()
        {
            _logger = BepInEx.Logging.Logger.CreateLogSource(nameof(TraderScenesManager));
            Patch_TraderDealScreen_Show.OnTraderTradingOpen += OnTraderTradingOpenHandler;
        }

        private async void OnTraderTradingOpenHandler(TraderClass trader)
        {
            _requestedTraderId = trader.Id;

            try
            {
                await HandleTraderOpen(trader);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error switching trader scene: {ex}");
                SetMainMenuBGVisible(true);

                FadeToBlack(false);
            }
        }

        Task FadeToBlack(bool blackScreen)
        {
            // bridge Coroutine and Async
            var tcs = new TaskCompletionSource<bool>();

            if (fadeCoroutine != null)
                CoroutineRunner.Instance.StopCoroutine(fadeCoroutine);

            fadeCoroutine = CoroutineRunner.Instance.StartCoroutine(FadeCoroutine(blackScreen, tcs));

            return tcs.Task;

            IEnumerator FadeCoroutine(bool black, TaskCompletionSource<bool> completionSource)
            {
                if (_cam == null)
                {
                    completionSource.TrySetResult(false);
                    yield break;
                }

                float t = 0;

                var effectControl = _cam.GetComponent<CC_BrightnessContrastGamma>();
                effectControl.enabled = true;

                float targetBrightness = black ? -100f : 0;
                float startBrightness = effectControl.brightness;

                Quaternion startWorldRot = black 
                    ? _cam.transform.rotation
                    : Quaternion.LookRotation(-_cam.transform.right + _cam.transform.forward * 2f, _cam.transform.up);
                Quaternion targetWorldRot = black
                    ? Quaternion.LookRotation(_cam.transform.right + _cam.transform.forward * 2f, _cam.transform.up)
                    : _cam.transform.rotation;

                while (t < 1f)
                {
                    t += Time.deltaTime * 3f;
                    t = Mathf.Clamp01(t);
                    float easedT = black ? EaseInCubic(0, 1f, t) : EaseOutCubic(0, 1f, t);

                    float brightness = Mathf.Lerp(startBrightness, targetBrightness, easedT);
                    effectControl.brightness = brightness;

                    _cam.transform.rotation = Quaternion.Slerp(startWorldRot, targetWorldRot, easedT);

                    yield return null;
                }

                completionSource.TrySetResult(true);
            }

            static float EaseInCubic(float start, float end, float value)
            {
                end -= start;
                return end * value * value * value + start;
            }

            static float EaseOutCubic(float start, float end, float value)
            {
                value--;
                end -= start;
                return end * (value * value * value + 1) + start;
            }
        }

        private async Task HandleTraderOpen(TraderClass trader)
        {
            if (!_traderIdToBundleMap.TryGetValue(trader.Id, out string vendorBundle))
            {
                SetMainMenuBGVisible(true);
                return;
            }

            Task fadeTask = FadeToBlack(true);
            var loadHandle = await BundleManager.LoadTraderSceneWithHandle(vendorBundle);

            if (loadHandle == null)
            {
                await fadeTask;
                SetMainMenuBGVisible(true);
                return;
            }

            // wait for BOTH the Fade animation AND the Loading to finish
            await Task.WhenAll(fadeTask, loadHandle.WaitUntilReady());

            if (_requestedTraderId != trader.Id)
            {
                _logger.LogInfo($"User switched from {trader.Id} before load finished. Aborting switch.");
                return;
            }

            Scene scene = await loadHandle.Activate();

            TraderScene traderScene = scene.GetRootGameObjects()[0].GetComponent<TraderScene>();
            if (!scene.IsValid() || traderScene == null)
            {
                SceneManager.UnloadSceneAsync(scene);
                SetMainMenuBGVisible(true);
                return;
            }

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
            ReplaceShadersToNative(traderScene.AllRenderers);

            SceneManager.SetActiveScene(scene);
            ManageSceneVisibility(scene);
            _openedScenes[trader.Id] = scene;

            SetupCamera(traderScene.CameraPoint);
            SetMainMenuBGVisible(false);

            var npc = traderScene.Trader.GetComponent<SequenceReader>();


            npc.Play(clip);

            FadeToBlack(false);
        }

        private void SetMainMenuBGVisible(bool value)
        {
            if (Singleton<EnvironmentUI>.Instance != null)
            {
                Singleton<EnvironmentUI>.Instance.ShowCameraContainer(value);
                Singleton<EnvironmentUI>.Instance.EnableOverlay(value);
            }
        }

        private void ManageSceneVisibility(Scene targetSceneVisible)
        {
            foreach (var rootGo in targetSceneVisible.GetRootGameObjects())
            {
                rootGo.SetActive(true);
            }

            foreach (var kvp in _openedScenes)
            {
                if (kvp.Value == targetSceneVisible) continue;

                if (kvp.Value.IsValid() && kvp.Value.isLoaded)
                {
                    foreach (var rootGo in kvp.Value.GetRootGameObjects())
                    {
                        rootGo.SetActive(false);
                    }
                }
            }
        }

        private void SetupCamera(Transform camPoint)
        {
            if (_cam == null)
            {
                _cam = GameObject.Instantiate(Resources.Load<GameObject>("Cam2_fps_hideout")).GetComponent<Camera>();
                _cam.GetComponent<PrismEffects>().useExposure = true;
                _cam.fieldOfView = 60;
                GameObject.DontDestroyOnLoad(_cam.gameObject);
            }

            _cam.gameObject.SetActive(true);
            _cam.transform.SetPositionAndRotation(camPoint.position, camPoint.rotation);
        }
    }
}
