namespace Fusion.Editor {
  using System.IO;
  using System.Linq;
  using UnityEditor;
  using UnityEditor.AssetImporters;
  using UnityEngine;

  [ScriptedImporter(1, ExtensionWithoutDot, NetworkProjectConfigImporter.ImportQueueOffset + 1)]
  public class FusionWeaverTriggerImporter : ScriptedImporter {
    public const string DependencyName = "FusionILWeaverTriggerImporter/ConfigHash";
    public const string Extension = "." + ExtensionWithoutDot;
    public const string ExtensionWithoutDot = "fusionweavertrigger";
    
    [Tooltip("If enabled, runs the weaver when weaving-related changes are detected in the config file.")]
    public bool RunWeaverOnConfigChanges = true;

    public override void OnImportAsset(AssetImportContext ctx) {
      ctx.DependsOnCustomDependency(DependencyName);
      if (RunWeaverOnConfigChanges) {
        ILWeaverUtils.RunWeaver();
      }
    }

    private static void RefreshDependencyHash() {
      if (EditorApplication.isCompiling || EditorApplication.isUpdating) {
        return;
      }
      
      var configPath = NetworkProjectConfigUtilities.GetGlobalConfigPath();
      if (string.IsNullOrEmpty(configPath)) {
        return;
      }

      try {
        var cfg = NetworkProjectConfigImporter.LoadConfigFromFile(configPath);
        var hash = new Hash128();

        foreach (var path in cfg.AssembliesToWeave) {
          hash.Append(path);
        }

        hash.Append(cfg.UseSerializableDictionary ? 1 : 0);
        hash.Append(cfg.NullChecksForNetworkedProperties ? 1 : 0);
        hash.Append(cfg.CheckRpcAttributeUsage ? 1 : 0);
        hash.Append(cfg.CheckNetworkedPropertiesBeingEmpty ? 1 : 0);

        AssetDatabaseUtils.RegisterCustomDependencyWithMppmWorkaround(DependencyName, hash);
        AssetDatabase.Refresh();
      } catch {
        // ignore the error
      }
    }

    private class Postprocessor : AssetPostprocessor {
      private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
        foreach (var path in importedAssets) {
          if (path.EndsWith(NetworkProjectConfigImporter.Extension)) {
            EditorApplication.delayCall -= RefreshDependencyHash;
            EditorApplication.delayCall += RefreshDependencyHash;
          }
        }
      }
    }
  }
}