// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// The kodama has been dropped on a tile
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class PawnDroppedState : PawnState
    {
        private bool dropSucceed;

        public override void Enter()
        {
            TabletopGameManager.Instance.RPC_ClearHighlights();
            PawnMovement.DraggedObject = null;
            TabletopGameEvents.OnPawnDragEnd?.Invoke(Pawn);

            var droppableCellExist = Movement.DroppableCell != null;
            var samePosition = droppableCellExist && Pawn.IsOnGrid &&
                               Pawn.Position == Movement.DroppableCell.Position;

            float releaseHeight;
            if (!droppableCellExist)
            {
                releaseHeight = 0;
            }
            else
            {
                var target = TabletopGameManager.Instance.GridRenderer.GetCell(Movement.DroppableCell).transform;
                releaseHeight = Pawn.transform.position.y - target.position.y;
            }

            if (droppableCellExist && samePosition == false)
            {
                Pawn.StopDragging(true, releaseHeight);

                // Join the new tile
                Pawn.MoveTo(Movement.DroppableCell, Vector3.zero);
                dropSucceed = true;
            }
            else
            {
                Pawn.StopDragging(false, releaseHeight);
                Pawn.ResetPosition(true, true);
                dropSucceed = false;
            }

            PawnStateMachine.ChangeState(PawnStateMachine.idleState);
        }

        public override void Exit()
        {
            base.Exit();

            if (dropSucceed)
            {
                Pawn.OnMove?.Invoke(Movement);
            }
            else
            {
                Pawn.OnMoveFailed?.Invoke(Movement);
                TabletopGameEvents.OnPawnDragCanceled?.Invoke();
            }
        }
    }
}
