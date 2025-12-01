using BepInEx.Logging;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using tarkin.tradermod.bep.Patches;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.bep
{
    internal class TraderScenesManager
    {
        private readonly ManualLogSource _logger;
        private Camera _cam;

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
            try
            {
                await HandleTraderOpen(trader);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error switching trader scene: {ex}");
            }
        }

        private async Task HandleTraderOpen(TraderClass trader)
        {
            _cam?.gameObject.SetActive(false);

            _logger.LogInfo($"Opened trader id {trader.Id}");

            if (!_traderIdToBundleMap.TryGetValue(trader.Id, out string vendorBundle))
            {
                _logger.LogInfo($"Trader id {trader.Id} has no bundle mapped.");
                return;
            }

            await BundleManager.LoadTraderScene(vendorBundle);

            if (Patch_EnvironmentUIRoot_SetCameraActive.CurrentEnvironmentUIRoot != null)
            {
                Patch_EnvironmentUIRoot_SetCameraActive.CurrentEnvironmentUIRoot.SetCameraActive(false);
                Patch_EnvironmentUIRoot_SetCameraActive.CurrentEnvironmentUIRoot.Shading?.gameObject.SetActive(false);
            }

            Scene scene = SceneManager.GetSceneByName(vendorBundle);

            if (!scene.IsValid())
            {
                _logger.LogError($"Scene {vendorBundle} was loaded but is invalid.");
                return;
            }

            _openedScenes[trader.Id] = scene;

            ManageSceneVisibility(scene);

            var camPoint = scene.GetRootGameObjects()
                .Select(go => go.GetComponentInChildren<StaticCameraObservationPoint>())
                .FirstOrDefault(campoint => campoint != null);

            if (camPoint == null)
            {
                _logger.LogError($"Trader scene {vendorBundle} has no 'StaticCameraObservationPoint'.");
                return;
            }

            SetupCamera(camPoint);
        }

        private void ManageSceneVisibility(Scene activeScene)
        {
            foreach (var rootGo in activeScene.GetRootGameObjects())
            {
                rootGo.SetActive(true);
            }

            foreach (var kvp in _openedScenes)
            {
                if (kvp.Value == activeScene) continue;

                if (kvp.Value.IsValid() && kvp.Value.isLoaded)
                {
                    foreach (var rootGo in kvp.Value.GetRootGameObjects())
                    {
                        rootGo.SetActive(false);
                    }
                }
            }
        }

        private void SetupCamera(StaticCameraObservationPoint camPoint)
        {
            if (_cam == null)
            {
                _cam = GameObject.Instantiate(Resources.Load<GameObject>("Cam2_fps_hideout")).GetComponent<Camera>();
            }

            _cam.gameObject.SetActive(true);
            _cam.transform.SetPositionAndRotation(camPoint.transform.position, camPoint.transform.rotation);
        }
    }
}
