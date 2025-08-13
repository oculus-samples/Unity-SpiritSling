// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEditor.SceneManagement;

namespace SpiritSling.Editor
{
    /// <summary>
    /// Service to launch First Scene from Editor Build Settings instead of current opened Scenes
    /// </summary>
    [InitializeOnLoad]
    public static class PlayModeSceneOverride
    {
        private static SceneAsset _playModeStartSceneAsset;
        private static readonly string ENABLE_SCENE_OVERRIDE = "EnablePlayModeSceneOverride";

        static PlayModeSceneOverride()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                if (EditorPrefs.GetBool(ENABLE_SCENE_OVERRIDE))
                {
                    // Load the first scene in the EditorBuildSettings
                    if (EditorBuildSettings.scenes.Length > 0)
                    {
                        _playModeStartSceneAsset
                            = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
                        if (_playModeStartSceneAsset != null)
                        {
                            EditorSceneManager.playModeStartScene = _playModeStartSceneAsset;
                        }
                    }
                }
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorPrefs.SetBool(ENABLE_SCENE_OVERRIDE, false);
                EditorSceneManager.playModeStartScene = null;
            }
        }

        [MenuItem("SpiritSling/Play From Boot #p")] // Shortcut: Shift + P
        private static void PlayFromBoot()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorPrefs.SetBool(ENABLE_SCENE_OVERRIDE, true);
                EditorApplication.isPlaying = true;
            }
            else
            {
                EditorApplication.isPlaying = false;
            }
        }
    }
}