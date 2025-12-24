using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Dialogs;
using EFT.UI;
using EFT.UI.Screens;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tarkin.tradermod.bep;
using tarkin.tradermod.bep.Patches;
using tarkin.tradermod.bep.UI;
using tarkin.tradermod.bep.UI.Quests;
using tarkin.tradermod.bep.UI.Trading;
using tarkin.tradermod.eft.Bep;
using tarkin.tradermod.eft.Bep.Patches;
using tarkin.tradermod.shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.eft
{
    [BepInPlugin("com.tarkin.tradermod", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        internal static ConfigEntry<KeyboardShortcut> KeyCapture;
        internal static ConfigEntry<int> ResolutionWidth;

        private static TraderScenesManager _scenesManager;
        private static DialogDataWrapper dialogData;

        private static SubtitlesManager _subtitlesManager;

        private static TraderUIManager _traderUIManager;

        private void Start()
        {
            Log = base.Logger;
            var prewarm = typeof(TraderScene);

            // main menu controller, respawned every raid end
            new Patch_MenuUI_Awake().Enable();

            // menu/pause screen, persistent through the entire game
            new Patch_MenuScreen_Show().Enable();

            new Patch_MainMenuControllerClass_ShowScreen().Enable();
            new Patch_WeaponModdingScreen_Show().Enable();
            new Patch_EditBuildScreen_Show().Enable();

            new Patch_TraderScreensGroup_Awake().Enable();

            new Patch_TraderDealScreen_Awake().Enable();
            new Patch_BarterSchemePanel_Awake().Enable();
            new Patch_TradingTable_Awake().Enable();
            new Patch_BarterSchemePanel_method_5().Enable();

            new Patch_QuestsScreen_Awake().Enable();

            new Patch_QuestObjectiveView_QuestHandover().Enable();

            new Patch_TraderDealScreen_Show().Enable();
            new Patch_QuestsScreen_Show().Enable();

            new Patch_QuestsListView_Show().Enable();

            new Patch_NarrateController_Unload().Enable();

            dialogData = new DialogDataWrapper(SafeDeserializer<TraderDialogsDTO>.Deserialize(File.ReadAllText(Path.Combine(TraderBundleManager.BundleDirectory, "dialogue.json"))));

            Patch_TraderDealScreen_Show.OnTraderTradingOpen += (trader) =>
                GetOrCreateScenesManager().TraderOpenHandler(trader.Id, EFT.UI.TraderScreensGroup.ETraderMode.Trade);

            Patch_MenuUI_Awake.OnPostfix += () =>
            {
                if (_subtitlesManager == null)
                    _subtitlesManager = new SubtitlesManager(dialogData);

                TraderBundleManager.EnsureDependencyBundlesAreLoaded();
            };

            Patch_TraderScreensGroup_Awake.OnPostfix += (parent) =>
            {
                if (_traderUIManager == null)
                    _traderUIManager = new TraderUIManager(parent, dialogData);

                GetOrCreateScenesManager().SetTraderUIManager(_traderUIManager);
            };

            Patch_MenuScreen_Show.OnPostfix += () => _scenesManager?.Close();

            // called when preparing raid
            Patch_NarrateController_Unload.OnPostfix += () =>
            {
                if (_scenesManager != null)
                {
                    _scenesManager.Dispose();
                    _scenesManager = null;
                }

                if (_subtitlesManager != null)
                {
                    _subtitlesManager.Dispose();
                    _subtitlesManager = null;
                }

                if (_traderUIManager != null)
                {
                    _traderUIManager.Dispose();
                    _traderUIManager = null;
                }
            };

            Patch_QuestsListView_Show.OnPostfix += (questController, trader) =>
            {
                int counterAvailableToStart = 0;
                int counterStarted = 0;
                int counterFailed = 0;

                foreach (var quest in questController.Quests)
                {
                    if (!quest.IsVisible)
                        continue;

                    if (quest.Template == null)
                        continue;

                    if (quest.Template.TraderId != trader.Id)
                        continue;

                    switch (quest.QuestStatus)
                    {
                        case EFT.Quests.EQuestStatus.AvailableForStart:
                            counterAvailableToStart++;
                            break;
                        case EFT.Quests.EQuestStatus.Started:
                            counterStarted++;
                            break;
                        case EFT.Quests.EQuestStatus.Fail:
                        case EFT.Quests.EQuestStatus.Expired:
                        case EFT.Quests.EQuestStatus.MarkedAsFailed:
                            counterFailed++;
                            break;
                    }
                }

                if (counterFailed > 0)
                {
                    GetOrCreateScenesManager().Interact(trader.Id, ETraderDialogType.QuestFailed);
                    return;
                }

                if (counterAvailableToStart > 0)
                {
                    GetOrCreateScenesManager().Interact(trader.Id, ETraderDialogType.QuestAvailable);
                    return;
                }

                if (counterStarted > 0)
                {
                    return;
                }

                GetOrCreateScenesManager().Interact(trader.Id, ETraderDialogType.NoJob);
            };

            Patch_QuestObjectiveView_QuestHandover.OnPostfix += (quest) =>
            {
                GetOrCreateScenesManager().Interact(quest.QuestDataClass.Template.TraderId, ETraderDialogType.Handover);
            };

            Patch_MainMenuControllerClass_ShowScreen.OnPostfix += (screenType, on) =>
            {
#if DEBUG
                Plugin.Log.LogWarning(screenType + " = " + on);
#endif
                if (on)
                {
                    if (screenType == EMenuType.Trade)
                    {
                        if (CurrentScreenSingletonClass.Instance.RootScreenType == EEftScreenType.Hideout)
                        {
                            if ((bool)Singleton<SharedGameSettingsClass>.Instance.Game.Settings.TradingIntermediateScreen)
                            {
                                Patch_MainMenuControllerClass_ShowScreen.Instance.ShowScreen(EMenuType.MainMenu, true); // hides hideout
                                Patch_MainMenuControllerClass_ShowScreen.Instance.ShowScreen(EMenuType.Trade, true);
                            }
                        }
                    }
                    else if (screenType == EMenuType.EditBuild)
                        _scenesManager?.Close();
                    else if (screenType == EMenuType.Hideout)
                    {
                        _scenesManager?.Close();

                        Scene hideoutScene = SceneManager.GetSceneByName("bunker_2");
                        if (hideoutScene != null && hideoutScene.IsValid() && hideoutScene.isLoaded)
                        {
                            SceneManager.SetActiveScene(hideoutScene);
                        }
                    }
                }
            };

            Patch_WeaponModdingScreen_Show.OnPostfix += () =>
            {
                _scenesManager?.Close();
            };

            Patch_EditBuildScreen_Show.OnPostfix += () =>
            {
                _scenesManager?.Close();
            };

            InitConfiguration();
        }

        private static TraderScenesManager GetOrCreateScenesManager()
        {
            if (_scenesManager == null)
                _scenesManager = new TraderScenesManager(dialogData);
            return _scenesManager;
        }

        private void InitConfiguration()
        {
            KeyCapture = Config.Bind("", "KeyCapture", new KeyboardShortcut(KeyCode.F11));
            ResolutionWidth = Config.Bind("", "ResolutionWidth", 4096);
        }
    }
}