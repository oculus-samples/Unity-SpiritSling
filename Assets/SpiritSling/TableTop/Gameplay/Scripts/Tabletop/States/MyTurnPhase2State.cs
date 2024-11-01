// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Local player turn phase 2
    /// </summary>
    public class MyTurnPhase2State : TabletopGameState
    {
        private int summonCount;

        public override void Enter()
        {
            base.Enter();

            cooldown = Game.Settings.timePhase;
            summonCount = 0;

            Pawn.OnMoveAnimationEnd += OnPawnMoveSucceed;

            Game.Phase = (byte)TableTopPhase.Summon;
        }

        public override void Exit()
        {
            base.Exit();

            Pawn.OnMoveAnimationEnd -= OnPawnMoveSucceed;
        }

        private void OnPawnMoveSucceed()
        {
            Game.CurrentPlayer.Kodama.RPC_OnSummon();
            summonCount++;
            if (summonCount >= TabletopGameManager.Instance.Settings.kodamaSummonPerTurn)
            {
                StartCoroutine(NextPhase());
            }
        }
        
        private IEnumerator NextPhase()
        {
            Game.Phase = (byte)TableTopPhase.EndPhase;
            yield return new WaitForSeconds(Game.Config.phaseDelay);
            ChangeToNextState();
        }
    }
}