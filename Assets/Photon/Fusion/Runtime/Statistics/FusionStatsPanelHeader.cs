namespace Fusion.Statistics {
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

  [Flags]
  public enum RenderSimStats {
    InPackets = 1 << 0,
    OutPackets = 1 << 1,
    RTT = 1 << 2,
    InBandwidth = 1 << 3,
    OutBandwidth = 1 << 4,
    Resimulations = 1 << 5,
    ForwardTicks = 1 << 6,
    InputReceiveDelta = 1 << 7,
    TimeResets = 1 << 8,
    StateReceiveDelta = 1 << 9,
    SimulationTimeOffset = 1 << 10,
    SimulationSpeed = 1 << 11,
    InterpolationOffset = 1 << 12,
    InterpolationSpeed = 1 << 13,
    InputInBandwidth = 1 << 14,
    InputOutBandwidth = 1 << 15,
    AverageInPacketSize = 1 << 16,
    AverageOutPacketSize = 1 << 17,
    InObjectUpdates = 1 << 18,
    OutObjectUpdates = 1 << 19,
    ObjectsAllocatedMemoryInUse = 1 << 20,
    GeneralAllocatedMemoryInUse = 1 << 21,
  }
  
  public class FusionStatsPanelHeader : MonoBehaviour {
    [SerializeField] private Text _statsHeaderTitle;
    [SerializeField] private Dropdown _statsDropdown;
    [SerializeField] private FusionStatsGraphDefault _defaultGraphPrefab;

    public RectTransform ContentRect;

    private Dictionary<RenderSimStats,FusionStatsGraphDefault> _defaultStatsGraph;
    private FusionStatistics _fusionStatistics;
    private RenderSimStats _statsToRender;

    public void SetupHeader(string title, FusionStatistics fusionStatistics) {
      _statsHeaderTitle.text = title;
      _fusionStatistics = fusionStatistics;
      
      SetupDropdown();
    }

    private void SetupDropdown() {
      _defaultStatsGraph = new Dictionary<RenderSimStats, FusionStatsGraphDefault>();
      
      List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

      options.Add(new Dropdown.OptionData("Toggle Stats"));

      foreach (var option in Enum.GetNames(typeof(RenderSimStats))) {
        options.Add(new Dropdown.OptionData(option));
      }

      _statsDropdown.options = options;

      _statsDropdown.onValueChanged.AddListener(OnDropDownChanged);
    }

    internal void SetStatsToRender(RenderSimStats stats) {
      // Early exit
      if (stats == _statsToRender) return;
      
      // For each possible stat
      foreach (RenderSimStats renderSimStat in Enum.GetValues(typeof(RenderSimStats))) {
        // If it is set on the stats received
        if ((stats & renderSimStat) == renderSimStat) {
          // And if it is not already set on the stats to render... add it
          if ((_statsToRender & renderSimStat) != renderSimStat) {
            AddStat(renderSimStat);
          }
        }
        // else if is NOT set on the stats received
        else {
          // And if it is set on the stats to render... remove
          if ((_statsToRender & renderSimStat) == renderSimStat) {
            RemoveStat(renderSimStat);
          }
        }
      }
      
      // Make sure they are equal now.
      _statsToRender = stats;
    }

    private void AddStat(RenderSimStats stat) {
      _statsToRender |= stat; // Set the flag
      InstantiateStatGraph(stat);
    }

    private void RemoveStat(RenderSimStats stat) {
      _statsToRender &= ~stat; // Removed the flag
      DestroyStatGraph(stat);
    }
    
    private void OnDropDownChanged(int arg0) {
      if (arg0 <= 0) return; // No stat selected.
      arg0--; // Remove the first label
      
      RenderSimStats stat = (RenderSimStats)(1 << arg0);

      if ((_statsToRender & stat) == stat) {
        RemoveStat(stat);
      } else {
        AddStat(stat);
      }

      // Set the first label again.
      _statsDropdown.SetValueWithoutNotify(0);
      
      _fusionStatistics.UpdateStatsEnabled(_statsToRender);
    }

    private void InstantiateStatGraph(RenderSimStats stat) {
      FusionStatsGraphDefault graph = Instantiate(_defaultGraphPrefab, ContentRect);
      graph.SetupDefaultGraph(stat);
      TryApplyCustomStatConfig(graph);
      _defaultStatsGraph.Add(stat, graph);
    }

    private void DestroyStatGraph(RenderSimStats stat) {
      if (_defaultStatsGraph.Remove(stat, out var statsGraphDefault)) {
        Destroy(statsGraphDefault.gameObject);
      }
    }

    private void TryApplyCustomStatConfig(FusionStatsGraphDefault graph) {
      // Need to do this way because unity cannot serialize a dictionary.
      foreach (var config in _fusionStatistics.StatsConfig) {
        if (config.Stat == graph.Stat) {
          ApplyCustomStatsConfig(graph, config);
        }
      }
    }

    private void ApplyCustomStatsConfig(FusionStatsGraphDefault graph, FusionStatistics.FusionStatisticsStatCustomConfig config) {
      graph.SetThresholds(config.Threshold1, config.Threshold2, config.Threshold3);
    }

    internal void ApplyStatsConfig(List<FusionStatistics.FusionStatisticsStatCustomConfig> statsConfig) {
      foreach (var config in statsConfig) {
        if (_defaultStatsGraph.TryGetValue(config.Stat, out var graph)) {
          ApplyCustomStatsConfig(graph, config);
        }
      }
    }
  }
}