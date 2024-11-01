// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// State for victorious player.
    /// </summary>
    public class WinState : TabletopGameState
    {
        public override void Enter()
        {
            base.Enter();
            TabletopGameEvents.OnGamePhaseChanged?.Invoke(TabletopGameStateMachine.Player, TableTopPhase.Victory);
            StartCoroutine(ClearTabletop());
        }

        private IEnumerator ClearTabletop()
        {
            // Ensure we take authority on the board and the pawns
            yield return RequestStateAuthorityOnBoardObjects();

            // Check if a player has left without despawning it's pawns
            CheckForPawnsToRemove();
        }

        public override void Update()
        {
            // Don't do anything in the WinState's Update
        }
    }
}