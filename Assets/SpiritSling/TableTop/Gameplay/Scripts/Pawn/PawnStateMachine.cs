// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// State machine for the Kodamas
    /// </summary>
    public class PawnStateMachine : StateMachine
    {
        [Header("Bindings")]
        public PawnMovement movement;

        public Pawn pawn;

        [Header("Pawn States")]
        public PawnIdleState idleState;

        public PawnHoverState hoverState;
        public PawnDraggedState dragState;
        public PawnDroppedState dropState;

        // public override void ChangeState(State newState)
        // {
        //     base.ChangeState(newState);
        //     
        //     Log.Debug($"{name} {newState}");
        // }
    }
}