namespace Fusion.Statistics
{
  using System;
  using UnityEngine;
using UnityEngine.UI;
  public class FusionStatsGraphDefault : FusionStatsGraphBase
    {
        internal RenderSimStats Stat => _selectedStats;
        private RenderSimStats _selectedStats;
        [SerializeField] private Text _descriptionText;

        protected override void Initialize(TimeSpan bufferTimeSpan, TimeSpan refreshTimeSpan) {
          base.Initialize(bufferTimeSpan, refreshTimeSpan);
          _descriptionText.text = _selectedStats.ToString();
        }

        public override void UpdateGraph(NetworkRunner runner, FusionStatisticsManager statisticsManager, ref DateTime now) {
          var value = FusionStatisticsHelper.GetStatDataFromSnapshot(_selectedStats, statisticsManager.CompleteSnapshot);
          AddValueToBuffer(value, ref now);
        }

        internal void SetupDefaultGraph(RenderSimStats stat) {
          _selectedStats = stat;
          
          FusionStatisticsHelper.GetStatGraphDefaultSettings(_selectedStats, out var valueTextFormat, out var valueTextMultiplier, out var ignoreZeroOnAverage, out var ignoreZeroOnBuffer, out var bufferTimeSpan, out var refreshTimeSpan);
          
          SetValueTextFormat(valueTextFormat);
          SetValueTextMultiplier(valueTextMultiplier);
          SetIgnoreZeroValues(ignoreZeroOnAverage, ignoreZeroOnBuffer);
          Initialize(bufferTimeSpan, refreshTimeSpan);
        }
    }
}
