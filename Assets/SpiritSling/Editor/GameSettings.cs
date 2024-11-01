// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using Fusion.Photon.Realtime;
using Oculus.Platform;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Assertions;
using Application = UnityEngine.Application;

public class GameSettings : ScriptableObject, IPostprocessBuildWithReport
{
    [Header("Store settings")]
    [SerializeField] private PlatformSettings platformSettings;
    [SerializeField] private string companyName;
    [SerializeField] private string productName;
    [SerializeField] private string version;
    [SerializeField] private int bundleVersionCode;
    [SerializeField] private string applicationIdentifier;
    [SerializeField] private string metaQuestAppID;
    [Header("Keystore (optional)")]
    [SerializeField] private string keystoreName;
    [SerializeField] private string keyaliasName;
    [SerializeField] private string keystorePassword;
    [Header("Photon")]
    [SerializeField] private PhotonAppSettings photonAppSettings;
    [SerializeField] private string photonAppIdFusion;
    [SerializeField] private string photonAppIdVoice;

    private void ApplyGameSettings()
    {
        // Store settings
        PlayerSettings.companyName = companyName;
        PlayerSettings.productName = productName;
        PlayerSettings.bundleVersion = version;
        PlayerSettings.Android.bundleVersionCode = bundleVersionCode;
        PlayerSettings.applicationIdentifier = applicationIdentifier;
        platformSettings.GetType()
            .GetField("ovrMobileAppID", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(platformSettings, metaQuestAppID);
        EditorUtility.SetDirty(platformSettings);

        // Android keystore
        if (string.IsNullOrEmpty(keystoreName) || string.IsNullOrEmpty(keyaliasName) || string.IsNullOrEmpty(keystorePassword))
        {
            PlayerSettings.Android.useCustomKeystore = false;
        }
        else
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = keystoreName;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = keyaliasName;
            PlayerSettings.Android.keyaliasPass = keystorePassword;
        }

        // Photon
        photonAppSettings.AppSettings.AppIdFusion = photonAppIdFusion;
        photonAppSettings.AppSettings.AppIdVoice = photonAppIdVoice;

        // Other settings
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Master);
        PlayerSettings.Android.bundleVersionCode++;
        AssetDatabase.SaveAssets();
    }

    private void BuildGame()
    {
        ApplyGameSettings();

        // Build
        string path = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), $"{applicationIdentifier}_{PlayerSettings.bundleVersion}.apk");
        BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, BuildTarget.Android, BuildOptions.None);
    }

    int IOrderedCallback.callbackOrder { get; }

    void IPostprocessBuildWithReport.OnPostprocessBuild(BuildReport report)
    {
        bundleVersionCode++;
        EditorUtility.SetDirty(this);
    }

    [CustomEditor(typeof(GameSettings))]
    internal class GameSettingsInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(16);

            var instance = target as GameSettings;
            Assert.IsNotNull(instance);
            if (GUILayout.Button(ObjectNames.NicifyVariableName(nameof(ApplyGameSettings))))
            {
                instance.ApplyGameSettings();
            }
            if (GUILayout.Button(ObjectNames.NicifyVariableName(nameof(BuildGame))))
            {
                instance.BuildGame();
            }
        }
    }
}
#endif