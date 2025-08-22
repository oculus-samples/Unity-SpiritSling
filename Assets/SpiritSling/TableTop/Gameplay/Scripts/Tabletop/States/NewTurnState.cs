// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Setup a new turn
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class NewTurnState : TabletopGameState
    {
        public override void Enter()
        {
            base.Enter();

            TabletopGameEvents.OnGamePhaseChanged?.Invoke(TabletopGameStateMachine.Player, 0);
            StartCoroutine(NewTurn());
        }

        private IEnumerator NewTurn()
        {
            // Ensure we take authority on the board and the pawns
            yield return RequestStateAuthorityOnBoardObjects();

            // Check if a player has left without despawning it's pawns
            CheckForPawnsToRemove();

            var player = TabletopGameStateMachine.Player;
            // Reset Kodama immunity
            if (player.Kodama)
            {
                player.Kodama.ResetImmunity();
            }

            ChangeToNextState();
        }
    }
}
