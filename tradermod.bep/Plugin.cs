using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.UI.Screens;
using tarkin.tradermod.bep.Patches;
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
            //var prewarm = typeof();

            new Patch_EnvironmentUIRoot_SetCameraActive().Enable();
            new Patch_MenuScreen_Awake().Enable();

            CurrentScreenSingletonClass.Instance.OnScreenChanged += Instance_OnScreenChanged;

            InitConfiguration();
        }

        private async void Instance_OnScreenChanged(EEftScreenType screenType)
        {
            Log.LogWarning(screenType);

            if (screenType == EEftScreenType.Trader)
            {
                Patch_EnvironmentUIRoot_SetCameraActive.CurrentEnvironmentUIRoot?.SetCameraActive(false);

                string vendorBundle = "vendors_therapist";

                await BundleManager.LoadTraderScene(vendorBundle);

                StaticCameraObservationPoint camPoint = null;
                Scene scene = SceneManager.GetSceneByName(vendorBundle);
                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    if (camPoint == null)
                        camPoint = rootGo.GetComponentInChildren<StaticCameraObservationPoint>();
                }

                GameObject newCam = Instantiate(Resources.Load<GameObject>("Cam2_fps_hideout"));

                newCam.transform.SetParent(camPoint.transform, false);
                newCam.transform.localPosition = Vector3.zero;
            }
        }

        private void InitConfiguration()
        {
            KeyCapture = Config.Bind("", "KeyCapture", new KeyboardShortcut(KeyCode.F11));
            ResolutionWidth = Config.Bind("", "ResolutionWidth", 4096);
        }
    }
}