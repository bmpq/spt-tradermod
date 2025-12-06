using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.Dialogs;
using EFT.UI.Screens;
using System.Collections.Generic;
using System.IO;
using tarkin.tradermod.bep;
using tarkin.tradermod.bep.Patches;
using tarkin.tradermod.bep.UI;
using tarkin.tradermod.bep.UI.Quests;
using tarkin.tradermod.bep.UI.Trading;
using tarkin.tradermod.eft.Bep.Patches;
using tarkin.tradermod.eft.Bep.UILayoutTweaks;
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
        private static TraderDialogsDTO traderDialogsDTO;

        private void Start()
        {
            Log = base.Logger;
            var prewarm = typeof(TraderScene);

            new Patch_MenuScreen_Awake().Enable();

            new Patch_TraderScreensGroup_Awake().Enable();

            new Patch_TraderDealScreen_Awake().Enable();
            new Patch_BarterSchemePanel_Awake().Enable();
            new Patch_TradingTable_Awake().Enable();
            new Patch_BarterSchemePanel_method_5().Enable();

            new Patch_QuestsScreen_Awake().Enable();

            new Patch_TraderDealScreen_Show().Enable();
            new Patch_QuestsScreen_Show().Enable();

            new Patch_NarrateController_Unload().Enable();

            traderDialogsDTO = SafeDeserializer<TraderDialogsDTO>.Deserialize(File.ReadAllText(Path.Combine(TraderBundleManager.BundleDirectory, "dialogue.json")));

            Patch_TraderDealScreen_Show.OnTraderTradingOpen += (trader) =>
                GetOrCreateScenesManager().TraderOpenHandler(trader, EFT.UI.TraderScreensGroup.ETraderMode.Trade);

            Patch_QuestsScreen_Show.OnPostfix += (trader) =>
                GetOrCreateScenesManager().TraderOpenHandler(trader, EFT.UI.TraderScreensGroup.ETraderMode.Tasks);

            Patch_NarrateController_Unload.OnPostfix += () =>
            {
                if (_scenesManager != null)
                {
                    _scenesManager.Dispose();
                    _scenesManager = null;
                }
            };

            InitConfiguration();
        }

        private static TraderScenesManager GetOrCreateScenesManager()
        {
            if (_scenesManager == null)
                _scenesManager = new TraderScenesManager(traderDialogsDTO);
            return _scenesManager;
        }

        private void InitConfiguration()
        {
            KeyCapture = Config.Bind("", "KeyCapture", new KeyboardShortcut(KeyCode.F11));
            ResolutionWidth = Config.Bind("", "ResolutionWidth", 4096);
        }
    }
}