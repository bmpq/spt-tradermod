using BepInEx.Logging;
using Comfort.Common;
using EFT.UI;
using EFT.UI.Screens;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using tarkin.tradermod.bep;
using tarkin.tradermod.eft.Bep;
using tarkin.tradermod.eft.Bep.Patches;
using tarkin.tradermod.shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.eft
{
    internal class TraderScenesManager : IDisposable
    {
        private static readonly ManualLogSource _logger = BepInEx.Logging.Logger.CreateLogSource(nameof(TraderScenesManager));

        private readonly TraderCameraController _cameraController;
        private readonly TraderInteractionService _interactionService;

        private TraderUIManager _tradingUIManager;
        private TraderClass currentlyActiveTrader = null;
        private string _requestedTraderId = null;
        private Task _activeSwitchTask;

        private readonly Dictionary<string, TraderScene> _openedScenes = new Dictionary<string, TraderScene>();

        public TraderScenesManager(DialogDataWrapper dialogData)
        {
            _interactionService = new TraderInteractionService(dialogData);
            _cameraController = new TraderCameraController();
        }

        public void SetTraderUIManager(TraderUIManager tradingUIManager)
        {
            this._tradingUIManager = tradingUIManager;
            _tradingUIManager.OnTraderFaceClick += () =>
            {
                if (currentlyActiveTrader != null && _openedScenes.TryGetValue(currentlyActiveTrader.Id, out var scene))
                {
                    _interactionService.PlayAnimation(scene, currentlyActiveTrader.Id, ETraderDialogType.Chatter);
                }
            };
        }

        public async void Interact(TraderClass trader, ETraderDialogType dialogType)
        {
            if (_requestedTraderId != trader.Id)
            {
                TraderOpenHandler(trader, TraderScreensGroup.ETraderMode.Tasks);
            }
            if (_activeSwitchTask != null && !_activeSwitchTask.IsCompleted)
            {
                await _activeSwitchTask;
            }
            if (_openedScenes.TryGetValue(trader.Id, out var scene))
                _interactionService.PlayAnimation(scene, trader.Id, dialogType);
        }

        public async void TraderOpenHandler(TraderClass trader, TraderScreensGroup.ETraderMode mode)
        {
            if (currentlyActiveTrader == trader)
            {
                SetMainMenuBGVisible(false);
                return;
            }

            _requestedTraderId = trader.Id;

            try
            {
                await SwitchToTrader(trader);

                if (mode == TraderScreensGroup.ETraderMode.Trade)
                    Interact(trader, ETraderDialogType.Greetings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error switching trader scene: {ex}");
                Close();
            }
        }

        public void Close()
        {
            ManageSceneVisibility(null);
            currentlyActiveTrader = null;

            _cameraController.FadeToBlack(false);

            SetMainMenuBGVisible(true);
        }

        private async Task SwitchToTrader(TraderClass trader)
        {
            if (_activeSwitchTask != null && !_activeSwitchTask.IsCompleted && _requestedTraderId == trader.Id)
            {
                await _activeSwitchTask;
                return;
            }

            _activeSwitchTask = SwitchToTraderInternal(trader);
            await _activeSwitchTask;
        }

        private async Task SwitchToTraderInternal(TraderClass trader)
        {
            if (CurrentScreenSingletonClass.Instance.RootScreenType == EEftScreenType.Hideout)
            {
                Patch_MainMenuControllerClass_ShowScreen.Instance.ShowScreen(EMenuType.MainMenu, true); // hides hideout
                Patch_MainMenuControllerClass_ShowScreen.Instance.ShowScreen(EMenuType.Trade, true);
            }

            Task fadeTask = _cameraController.FadeToBlack(true);

            var loadHandle = await TraderBundleManager.LoadTraderSceneWithHandle(trader.Id);

            if (loadHandle == null)
            {
                await fadeTask;
                Close();
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
                Close();
                return;
            }

            if (currentlyActiveTrader != null)
                _interactionService.MarkTraderSeen(currentlyActiveTrader.Id);

            currentlyActiveTrader = trader;

            _tradingUIManager?.SetCurrentTrader(traderScene);

            SceneManager.SetActiveScene(scene);
            _openedScenes[trader.Id] = traderScene;
            ManageSceneVisibility(traderScene);

            // to avoid interfering with hideout, if it exists in the world
            traderScene.transform.position = new Vector3(0, 300, 0);

            SetMainMenuBGVisible(false);

            _cameraController.Setup(traderScene.CameraPoint);
            _cameraController.FadeToBlack(false);
        }

        private void ManageSceneVisibility(TraderScene targetSceneVisible)
        {
            if (targetSceneVisible != null)
            {
                targetSceneVisible.gameObject.SetActive(true);
            }
            else
            {
                _cameraController.SetActive(false);
            }

            foreach (var kvp in _openedScenes)
            {
                if (kvp.Value == targetSceneVisible) continue;

                if (kvp.Value != null)
                {
                    kvp.Value.gameObject.SetActive(false);
                }
            }
        }

        private void SetMainMenuBGVisible(bool value)
        {
            if (Singleton<EnvironmentUI>.Instance != null)
            {
                Singleton<EnvironmentUI>.Instance.ShowCameraContainer(value);
                Singleton<EnvironmentUI>.Instance.EnableOverlay(false);
            }
        }

        public void Dispose()
        {
            _logger.LogInfo("Disposing self");
            _cameraController.Dispose();
        }
    }
}
