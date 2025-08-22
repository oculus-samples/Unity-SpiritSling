// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Local player ends its turn and ask next player to take the lead
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class EndMyTurnState : TabletopGameState
    {
        public override void Enter()
        {
            base.Enter();

            TabletopGameEvents.OnGamePhaseChanged?.Invoke(TabletopGameStateMachine.Player, TableTopPhase.EndTurn);
            StartCoroutine(EndTurnRoutine());
        }

        private IEnumerator EndTurnRoutine()
        {
            Game.SetNextPlayer();

            yield return new WaitForSeconds(0.5f); // Let time to Fusion to update game data

            yield return ReleaseStateAuthorityOnBoardObjects();

            // Let everyone know we have finished our turn
            Game.RPC_NextTurn(Game.CurrentPlayerIndex);
        }
    }
}
