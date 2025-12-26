using System;
using System.Linq;
using tarkin.tradermod.shared;
using UnityEngine;

namespace tarkin.tradermod.eft.Bep.DebugTools
{
    internal class InteractionTester : MonoBehaviour
    {
        private static readonly BepInEx.Logging.ManualLogSource Logger = BepInEx.Logging.Logger.CreateLogSource(nameof(InteractionTester));

        public Func<TraderScenesManager> SceneManagerGetter;

        private readonly string[] _traderIds = new string[] {
            "54cb50c76803fa8b248b4571",
            "54cb57776803fa99248b456e",
            "58330581ace78e27b8b10cee",
            "5935c25fb3acc3127c3d8cd9",
            "5a7c2eca46aef81a7ca2145d",
            "5ac3b934156ae10c4430e83c",
            "5c0647fdd443bc2504c2d371",
            "579dc571d53a0658a154fbec",
        }; 

        private string[] GetTraderNames()
        {
            return _traderIds.Select(id =>
            {
                switch (id)
                {
                    case "54cb50c76803fa8b248b4571": return "Prapor";
                    case "54cb57776803fa99248b456e": return "Therapist";
                    case "58330581ace78e27b8b10cee": return "Skier";
                    case "5935c25fb3acc3127c3d8cd9": return "Peacekeeper";
                    case "5a7c2eca46aef81a7ca2145d": return "Mechanic";
                    case "5ac3b934156ae10c4430e83c": return "Ragman";
                    case "5c0647fdd443bc2504c2d371": return "Jaeger";
                    case "579dc571d53a0658a154fbec": return "Fence";
                    default: return id;
                }
            }).ToArray();
        }

        private Rect _windowRect = new Rect(20, 20, 450, 500);
        private Vector2 _scrollPosition;

        private int _selectedTraderIndex = 0;
        private ETraderDialogType _selectedDialogType = ETraderDialogType.Greetings;

        void OnGUI()
        {
            GUI.skin.window.fontSize = 14;

            _windowRect = GUI.Window(9999, _windowRect, DrawWindow, "Trader Interaction Tester");
        }

        void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);

            GUILayout.Label("<b>Target Trader:</b>", GetLabelStyle());
            _selectedTraderIndex = GUILayout.SelectionGrid(_selectedTraderIndex, GetTraderNames(), 2);

            GUILayout.Space(10);

            GUILayout.Label("<b>Interaction Type:</b>", GetLabelStyle());

            int selection = (int)_selectedDialogType;
            selection = GUILayout.SelectionGrid(selection, Enum.GetNames(typeof(ETraderDialogType)), 2);
            _selectedDialogType = (ETraderDialogType)selection;

            GUILayout.Space(20);

            GUILayout.Label($"Selected: {GetTraderNames()[_selectedTraderIndex]} -> {_selectedDialogType}");

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("INTERACT", GUILayout.Height(40)))
            {
                ExecuteInteraction();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        void ExecuteInteraction()
        {
            if (SceneManagerGetter == null)
            {
                Logger.LogError("[InteractionTester] SceneManagerGetter is null.");
                return;
            }

            var manager = SceneManagerGetter();
            if (manager == null)
            {
                Logger.LogError("[InteractionTester] SceneManager is not currently available.");
                return;
            }

            string targetId = _traderIds[_selectedTraderIndex];
            Logger.LogInfo($"[InteractionTester] Sending {targetId} : {_selectedDialogType}");

            try
            {
                manager.Interact(targetId, _selectedDialogType);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[InteractionTester] Error executing interaction: {ex.Message}");
            }
        }

        private GUIStyle GetLabelStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.fontStyle = FontStyle.Bold;
            return style;
        }
    }
}