// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR
using Meta.XR.Samples;
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

[MetaCodeSample("SpiritSling")]
public class GameSettings : ScriptableObject
{
    [Header("App settings")]
    [SerializeField] private string companyName;
    [SerializeField] private string productName;
    [SerializeField] private string version;
    [SerializeField] private int bundleVersionCode;
    [SerializeField] private string applicationIdentifier;
    [Header("Store settings")]
    [SerializeField] private PlatformSettings platformSettings;   
    [SerializeField] private string metaQuestAppID;
    [Header("Keystore (optional)")]
    [SerializeField] private string keystoreName;
    [SerializeField] private string keyaliasName;
    [SerializeField] private string keystorePassword;
    [Header("Photon")]
    [SerializeField] private PhotonAppSettings photonAppSettings;
    [SerializeField] private string photonAppIdFusion;
    [SerializeField] private string photonAppIdVoice;

    private void GetSettingsFromProject()
    {
        companyName = PlayerSettings.companyName;
        productName = PlayerSettings.productName;
        version = PlayerSettings.bundleVersion;
        bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
        applicationIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        metaQuestAppID = platformSettings.GetType()
            .GetField("ovrMobileAppID", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(platformSettings).ToString();
        
        photonAppIdFusion = photonAppSettings.AppSettings.AppIdFusion;
        photonAppIdVoice = photonAppSettings.AppSettings.AppIdVoice;
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
    private void ApplyGameSettings(bool incrementBuildCode = false)
    {
        // Store settings
        PlayerSettings.companyName = companyName;
        PlayerSettings.productName = productName;
        PlayerSettings.bundleVersion = version;
        PlayerSettings.Android.bundleVersionCode = bundleVersionCode;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationIdentifier);
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
            PlayerSettings.Android.keyaliasName = keyaliasName;
            if (!string.IsNullOrWhiteSpace(keystorePassword))
            {
                PlayerSettings.Android.keystorePass = keystorePassword;
                PlayerSettings.Android.keyaliasPass = keystorePassword;
            }
        }

        // Photon
        photonAppSettings.AppSettings.AppIdFusion = photonAppIdFusion;
        photonAppSettings.AppSettings.AppIdVoice = photonAppIdVoice;

        // Other settings
        if (incrementBuildCode)
        {
            PlayerSettings.Android.bundleVersionCode++;
            bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            EditorUtility.SetDirty(this);
        }

        AssetDatabase.SaveAssets();
    }

    private void BuildGame()
    {
        ApplyGameSettings(true);

        // Build
        var dir = Path.Combine(Directory.GetParent(Application.dataPath).ToString(), "Builds");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{applicationIdentifier}_{PlayerSettings.bundleVersion}.apk");
        var buildReport = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, BuildTarget.Android, BuildOptions.None);
        if (!Application.isBatchMode && buildReport.summary.result == BuildResult.Succeeded)
        {
            EditorUtility.RevealInFinder(path);
        }
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
            if (GUILayout.Button(ObjectNames.NicifyVariableName(nameof(GetSettingsFromProject))))
            {
                instance.GetSettingsFromProject();
            }
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
