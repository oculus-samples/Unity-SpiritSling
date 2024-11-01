// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// The kodama is being dragged by the player
    /// </summary>
    public class KodamaDraggedState : PawnDraggedState
    {
        protected override HexCell GetCenterCell() => Pawn.CurrentCell;
        protected override int GetMoveRange() => TabletopGameManager.Instance.Settings.kodamaMoveRange;
        protected override bool CanDropInLava() => true;

        protected override void NotifyOtherPlayers(Vector3Int centerPosition, Vector3Int closestCellPosition)
        {
            TabletopGameManager.Instance.RPC_HighlightDropKodamaChanged(centerPosition, closestCellPosition);
        }
    }
}