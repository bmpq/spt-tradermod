using BepInEx.Logging;
using Comfort.Common;
using EFT.UI;
using EFT.UI.Screens;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

        private readonly DialogDataWrapper _dialogData;
        private readonly TraderCameraController _cameraController;
        private readonly TraderInteractionService _interactionService;

        private TraderUIManager _tradingUIManager;
        private string currentlyActiveTraderId = null;

        private readonly SemaphoreSlim _switchLock = new SemaphoreSlim(1, 1);

        private readonly Dictionary<string, TraderScene> _openedScenes = new Dictionary<string, TraderScene>();

        public TraderScenesManager(DialogDataWrapper dialogData)
        {
            _dialogData = dialogData;
            _interactionService = new TraderInteractionService(dialogData);
            _cameraController = new TraderCameraController();
        }

        public void SetTraderUIManager(TraderUIManager tradingUIManager)
        {
            this._tradingUIManager = tradingUIManager;
            _tradingUIManager.OnTraderFaceClick -= OnTraderFaceClicked;
            _tradingUIManager.OnTraderFaceClick += OnTraderFaceClicked;
        }

        private void OnTraderFaceClicked()
        {
            if (!string.IsNullOrEmpty(currentlyActiveTraderId) && _openedScenes.TryGetValue(currentlyActiveTraderId, out var scene))
            {
                if (_switchLock.CurrentCount == 0) 
                    return;

                PlayAnimationSequenceWhileHidingFaceButton(scene, currentlyActiveTraderId, ETraderDialogType.Chatter);
            }
        }

        private void RefreshUIState(TraderScene scene)
        {
            if (_tradingUIManager == null) 
                return;
            bool hasLines = _interactionService.HasUnplayedChatter(scene);
            _tradingUIManager.SetTraderState(scene, hasLines);
        }

        public async void Interact(string traderId, ETraderDialogType dialogType)
        {
            try
            {
                if (currentlyActiveTraderId == traderId)
                {
                    if (_openedScenes.TryGetValue(traderId, out var scene))
                    {
                        await PlayAnimationSequenceWhileHidingFaceButton(scene, traderId, dialogType);
                    }
                }
                else
                {
                    TraderOpenHandler(traderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in Interact: {ex}");
            }
        }

        private async Task PlayAnimationSequenceWhileHidingFaceButton(TraderScene scene, string traderId, ETraderDialogType type)
        {
            if (_tradingUIManager != null && currentlyActiveTraderId == traderId)
            {
                _tradingUIManager.SetTraderState(scene, false);
            }

            try
            {
                await _interactionService.PlayAnimation(scene, traderId, type);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error playing animation sequence: {ex}");
            }
            finally
            {
                if (currentlyActiveTraderId == traderId)
                {
                    RefreshUIState(scene);
                }
            }
        }

        public async void TraderOpenHandler(string traderId)
        {
            if (currentlyActiveTraderId == traderId)
            {
                SetMainMenuBGVisible(false);
                return;
            }

            await _switchLock.WaitAsync();

            try
            {
                bool success = await SwitchToTraderInternal(traderId);

                if (success)
                    Interact(traderId, ETraderDialogType.Greetings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error switching trader scene: {ex}");
                Close();
            }
            finally
            {
                _switchLock.Release();
            }
        }

        public void Close()
        {
            _interactionService.StopNativeFormatAnimation();

            ManageSceneVisibility(null);
            currentlyActiveTraderId = string.Empty;

            _cameraController.FadeToBlack(false);
            _interactionService.SavePlayedLines();

            SetMainMenuBGVisible(true);
        }

        private async Task<bool> SwitchToTraderInternal(string traderId)
        {
            if (CurrentScreenSingletonClass.Instance.RootScreenType == EEftScreenType.Hideout)
            {
                Patch_MainMenuControllerClass_ShowScreen.Instance.ShowScreen(EMenuType.MainMenu, true);
                Patch_MainMenuControllerClass_ShowScreen.Instance.ShowScreen(EMenuType.Trade, true);
            }

            Task fadeTask = _cameraController.FadeToBlack(true);

            Task<Scene?> loadTask = TraderBundleManager.LoadTraderScene(traderId);

            // wait for BOTH the Fade animation AND the Loading to finish
            await Task.WhenAll(fadeTask, loadTask);

            Scene? loadedScene = loadTask.Result;

            if (loadedScene == null || !loadedScene.Value.IsValid())
            {
                _logger.LogError($"Failed to load scene for {traderId}");
                Close();
                return false;
            }

            Scene scene = loadedScene.Value;
            TraderScene traderScene = scene.GetRootGameObjects()[0].GetComponent<TraderScene>();

            if (traderScene == null)
            {
                _logger.LogError("TraderScene component missing on root object.");
                Close();
                return false;
            }

            await _interactionService.StopNativeFormatAnimation();

            if (!string.IsNullOrEmpty(currentlyActiveTraderId))
                _interactionService.MarkTraderSeen(currentlyActiveTraderId);

            _dialogData.AddExtraLocalizationData(traderScene.GetExtraLocales());

            currentlyActiveTraderId = traderId;
            _openedScenes[traderId] = traderScene;

            SceneManager.SetActiveScene(scene);

            // to avoid interfering with hideout, if it exists in the world
            traderScene.transform.position = new Vector3(0, 300, 0);

            ManageSceneVisibility(traderScene);
            RefreshUIState(traderScene);
            SetMainMenuBGVisible(false);

            traderScene.TraderGameObject.gameObject.GetOrAddComponent<NPCPropSoundPlayer>();

            _cameraController.Setup(traderScene.CameraPoint);
            _cameraController.FadeToBlack(false);

            return true;
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
                    if (kvp.Value.Director != null)
                        kvp.Value.Director.Stop();
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
            _interactionService.Dispose();
            _tradingUIManager?.Dispose();
            _switchLock?.Dispose();
        }
    }
}
