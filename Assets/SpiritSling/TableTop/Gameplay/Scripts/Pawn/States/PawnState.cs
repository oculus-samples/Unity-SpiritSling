// Copyright (c) Meta Platforms, Inc. and affiliates.


using Meta.XR.Samples;
namespace SpiritSling.TableTop
{
    /// <summary>
    /// Common behaviour and data for all Kodama states
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public abstract class PawnState : State
    {
        public PawnStateMachine PawnStateMachine => StateMachine as PawnStateMachine;

        public PawnMovement Movement => PawnStateMachine.movement;

        public Pawn Pawn => PawnStateMachine.pawn;

        public override void Enter() { }

        public override void Exit() { }

        public override void Update() { }
    }
}
