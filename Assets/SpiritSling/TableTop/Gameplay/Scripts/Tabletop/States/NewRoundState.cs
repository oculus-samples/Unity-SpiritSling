// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Setup a new round and let the first player play
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class NewRoundState : TabletopGameState
    {
        public override void Enter()
        {
            base.Enter();

            StartCoroutine(NewRound());
        }

        /// <summary>
        /// Update round count and apply CLOC
        /// </summary>
        /// <returns></returns>
        private IEnumerator NewRound()
        {
            yield return new WaitForSeconds(0.5f);

            yield return RequestStateAuthorityOnBoardObjects();

            // Copies the list, because some lootboxes may be destroyed by lava and thus removed from Loots.LootBoxesOnBoard
            var lootBoxesOnBoard = new List<LootBox>(Loots.LootBoxesOnBoard);
            foreach (var lootBox in lootBoxesOnBoard)
            {
                lootBox.CheckForLava();
            }

            // Update round and cloc data
            Game.Round++;

            if (Game.ClocRounds > 0)
            {
                Game.ClocRounds--;
            }
            if (Game.ClocRounds <= 0)
            {
                Game.RPC_ApplyCLOC(Game.ClocRadius, Game.Settings.isClocRandom);

                int r = Game.ClocRadius;
                if (r > 0) r--;

                Game.ClocRadius = (byte)r;

                // Wait for players with a dead or respawning kodama to be removed from the list or to finish the respawn
                while (BaseTabletopPlayer.TabletopPlayers.Count(p => p.Kodama.IsKilled || p.Kodama.CurrentCell == null || p.Kodama.CurrentCell.Height < 0) > 0)
                {
                    yield return null;
                }
            }

            if (Game.HasSomeoneWon())
            {
                Log.Info("No new round because someone has won");
            }
            else
            {
                Log.Info($"Round={Game.Round} CLOC={Game.ClocRounds}");
                Loots.SpawnNewLootBoxes();
                yield return new WaitForSeconds(0.5f);
                ChangeToNextState();
            }
        }
    }
}
