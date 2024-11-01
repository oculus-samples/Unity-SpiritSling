namespace Fusion.Statistics {
  using System;
  using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;

  [RequireComponent(typeof(NetworkRunner))]
  [AddComponentMenu("Fusion/Statistics/Fusion Statistics")]
  public class FusionStatistics : SimulationBehaviour, ISpawned {

    // Setup prefabs
    private GameObject _statsCanvasPrefab;
    private FusionNetworkObjectStatsGraphCombine _objectGraphCombinePrefab;

    private const string STATS_CANVAS_PREFAB_PATH = "FusionStatsResources/FusionStatsRenderPanel";
    private const string STATS_OBJECT_COMBINE_PREFAB_PATH = "FusionStatsResources/NetworkObjectStatistics";
    
    private List<FusionStatsGraphBase> _statsGraph;
    private FusionStatsPanelHeader _header;
    private FusionStatsCanvas _statsCanvas;
    private GameObject _statsPanelObject;
    private Dictionary<FusionNetworkObjectStatistics, FusionNetworkObjectStatsGraphCombine> _objectStatsGraphCombines;

    [Header("Editor side toggle for the stats.")]
    [SerializeField] private RenderSimStats _statsEnabled;

    [SerializeField]
    private List<FusionStatisticsStatCustomConfig> _statsConfig = new List<FusionStatisticsStatCustomConfig>();

    internal List<FusionStatisticsStatCustomConfig> StatsConfig => _statsConfig;

    [System.Serializable]
    public struct FusionStatisticsStatCustomConfig {
      public RenderSimStats Stat;
      public float Threshold1;
      public float Threshold2;
      public float Threshold3;
    }
    
    private void Awake() {
      _statsGraph = new List<FusionStatsGraphBase>();
      _statsCanvasPrefab = Resources.Load<GameObject>(STATS_CANVAS_PREFAB_PATH);
      _objectGraphCombinePrefab = Resources.Load<FusionNetworkObjectStatsGraphCombine>(STATS_OBJECT_COMBINE_PREFAB_PATH);

      if (_statsCanvasPrefab == null || _objectGraphCombinePrefab == null) {
        Log.Error($"Error loading the required assets for Fusion Statistics, destroying stats instance. Make sure that the following paths are valid for the Fusion Statistics resource assets: \n 1. {STATS_CANVAS_PREFAB_PATH} \n 2. {STATS_OBJECT_COMBINE_PREFAB_PATH}");
        Destroy(this);
      }
    }

    void ISpawned.Spawned() {
      SetupStatisticsPanel();
    }

    /// <summary>
    /// Sets the custom configuration for Fusion Statistics.
    /// </summary>
    /// <param name="customConfig">The list of custom configurations for Fusion Statistics.</param>
    public void SetStatsCustomConfig(List<FusionStatisticsStatCustomConfig> customConfig) {
      if (customConfig == default) {
        Log.Warn("Trying to set a null Fusion Statistics custom stats config");
        return;
      }
      
      _statsConfig = customConfig;
      ApplyCustomConfig();
    }

    private void ApplyCustomConfig() {
      if (!_header) return;
      _header.ApplyStatsConfig(_statsConfig);
    }

    /// <summary>
    /// Called from a custom editor script.
    /// Will update any editor information into the fusion statistics.
    /// </summary>
    public void OnEditorChange() {
      RenderEnabledStats();
      ApplyCustomConfig();
    }
    
    private void RenderEnabledStats() {
      if (!_header) return;
      _header.SetStatsToRender(_statsEnabled);
    }
    
    internal void UpdateStatsEnabled(RenderSimStats stats) {
      _statsEnabled = stats;
    }

    /// <summary>
    /// Sets up the statistics panel for Fusion statistic tracking.
    /// </summary>
    public void SetupStatisticsPanel() {
      if (_statsPanelObject != null) return;

      // Was not registered on the Runner yet
      if (Runner == null) {
        var runner = GetComponent<NetworkRunner>();
        
        if (runner.IsRunning == false) {
          Log.Warn($"Network Runner on ({runner.gameObject}) is not yet running.");
          return;
        }
        
        runner.AddGlobal(this);
        // Return because when spawned is called the setup method will be called again.
        return;
      }
      
      _objectStatsGraphCombines = new Dictionary<FusionNetworkObjectStatistics, FusionNetworkObjectStatsGraphCombine>();
      
      _statsPanelObject = Instantiate(_statsCanvasPrefab, transform);
      _statsCanvas = _statsPanelObject.GetComponentInChildren<FusionStatsCanvas>();
      _statsCanvas.SetupStatsCanvas(this, CloseButtonAction);
      _header = _statsPanelObject.GetComponentInChildren<FusionStatsPanelHeader>();
      _header.SetupHeader(Runner.LocalPlayer.ToString(), this);

      _statsPanelObject.AddComponent<FusionBasicBillboard>();
      ApplyCustomConfig();
      
      if (_statsEnabled != 0)
        RenderEnabledStats();
      
      // Setup Event system
      if (!EventSystem.current) {
        Log.Debug("Fusion Statistics: No event system detected, creating one.");
        new GameObject("EventSystem-FusionStatistics", typeof(EventSystem), typeof(StandaloneInputModule));
      }
    }

    private void CloseButtonAction() {
      var keys = _objectStatsGraphCombines.Keys.ToArray();
      foreach (var fusionNetworkObjectStatistics in keys) {
        MonitorNetworkObject(fusionNetworkObjectStatistics.NetworkObject, fusionNetworkObjectStatistics, false);
      }
      _objectStatsGraphCombines.Clear();
      _statsGraph.Clear();
      
      Destroy(_statsPanelObject);
      _statsPanelObject = null;

      if (Runner.TryGetFusionStatistics(out var statisticsManager)) {
        statisticsManager.ObjectStatisticsManager.ClearMonitoredNetworkObjects();
      }
    }

    public bool MonitorNetworkObject(NetworkObject networkObject, FusionNetworkObjectStatistics objectStatisticsInstance, bool monitor) {

      if (Runner.TryGetFusionStatistics(out var statisticsManager)) {
        statisticsManager.ObjectStatisticsManager.MonitorNetworkObjectStatistics(networkObject.Id, monitor);
      }
      
      if (monitor) {
        
        // If Id already monitored on the stats, return false to destroy the object statistics instance.
        if (_objectStatsGraphCombines.ContainsKey(objectStatisticsInstance))
          return false;
        
        var graphCombine = Instantiate(_objectGraphCombinePrefab, _header.ContentRect);
        graphCombine.SetupNetworkObject(networkObject, this, objectStatisticsInstance);
        _objectStatsGraphCombines.Add(objectStatisticsInstance, graphCombine);
      } else {
        
        if (_objectStatsGraphCombines.Remove(objectStatisticsInstance, out var graphCombine)) {
          Destroy(graphCombine.gameObject);
          Destroy(objectStatisticsInstance);
        }
      }

      return true;
    }

    void UpdateAllGraphs(FusionStatisticsManager statisticsManager) {
      var now = DateTime.Now;
      foreach (var statsGraphBase in _statsGraph) {
        statsGraphBase.UpdateGraph(Runner, statisticsManager, ref now);
      }
    }

    public void RegisterGraph(FusionStatsGraphBase graph) {
      _statsGraph.Add(graph);
    }

    public void UnregisterGraph(FusionStatsGraphBase graph) {
      _statsGraph.Remove(graph);
    }

    private void Update() {
      // Safety exit
      if (!Runner) return;
      
      
      Profiler.BeginSample("Fusion Statistics Update Graph");

      // Collect and update
      if (Runner.TryGetFusionStatistics(out var statisticsManager)) {
        UpdateAllGraphs(statisticsManager);
      }
      
      Profiler.EndSample();
    }
  }
}