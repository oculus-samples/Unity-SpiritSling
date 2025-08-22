// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// The kodama is being dragged by the player
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class SlingshotDraggedState : PawnDraggedState
    {
        protected override HexCell GetCenterCell()
        {
            var ownerKodama = Pawn.Owner.Kodama;
            return ownerKodama != null && ownerKodama.CurrentCell != null ? ownerKodama.CurrentCell : null;
        }

        protected override int GetMoveRange() => TabletopGameManager.Instance.Settings.kodamaSummonRange;
        protected override bool CanDropInLava() => false;

        protected override void NotifyOtherPlayers(Vector3Int centerPosition, Vector3Int closestCellPosition)
        {
            TabletopGameManager.Instance.RPC_HighlightDropSlingshotChanged(centerPosition, closestCellPosition);
        }
    }
}
