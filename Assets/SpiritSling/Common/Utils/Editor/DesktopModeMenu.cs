// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;

namespace SpiritSling.Editor
{
    /// <summary>
    /// Service to launch First Scene from Editor Build Settings instead of current opened Scenes
    /// </summary>
    [InitializeOnLoad]
    public static class DesktopModeMenuItem
    {
        private const string MENU_NAME = "Cohere/Enable Desktop Mode";

        private static bool enabled;

        static DesktopModeMenuItem()
        {
            enabled = DesktopModeEnabler.IsDesktopMode;

            EditorApplication.delayCall += () =>
            {
                PerformAction(enabled);
            };
        }

        [MenuItem(MENU_NAME)]
        private static void ToggleAction()
        {
            PerformAction(!enabled);
        }

        public static void PerformAction(bool yes)
        {
            Menu.SetChecked(MENU_NAME, yes);
            DesktopModeEnabler.IsDesktopMode = yes;

            enabled = yes;
        }
    }
}