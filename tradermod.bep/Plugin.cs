using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.UI.Screens;
using System.Collections.Generic;
using tarkin.tradermod.bep.Patches;
using tarkin.tradermod.bep.UI;
using tarkin.tradermod.bep.UI.Trading;
using tarkin.tradermod.shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.bep
{
    [BepInPlugin("com.tarkin.tradermod", MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        internal static ConfigEntry<KeyboardShortcut> KeyCapture;
        internal static ConfigEntry<int> ResolutionWidth;

        private void Start()
        {
            Log = base.Logger;
            var prewarm = typeof(TraderScene);

            new DialogueDeserialization();

            new Patch_MenuScreen_Awake().Enable();

            new Patch_TraderScreensGroup_Awake().Enable();

            new Patch_TraderDealScreen_Awake().Enable();
            new Patch_BarterSchemePanel_Awake().Enable();
            new Patch_TradingTable_Awake().Enable();
            new Patch_BarterSchemePanel_method_5().Enable();

            new Patch_TraderDealScreen_Show().Enable();

            new TraderScenesManager();

            InitConfiguration();
        }

        private void InitConfiguration()
        {
            KeyCapture = Config.Bind("", "KeyCapture", new KeyboardShortcut(KeyCode.F11));
            ResolutionWidth = Config.Bind("", "ResolutionWidth", 4096);
        }
    }
}