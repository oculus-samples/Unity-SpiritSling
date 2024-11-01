// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Local player turn phase 1
    /// </summary>
    public class MyTurnPhase1State : TabletopGameState
    {
        private WaitForSeconds waitBeforeKodamaEncourage;

        private Coroutine kodamaEncourage;

        private bool isKodamaEncouraging;

        public override void Enter()
        {
            base.Enter();

            cooldown = Game.Settings.timePhase;

            Pawn.OnMoveAnimationEnd += OnPawnMoveSucceed;
            TabletopGameEvents.OnPawnDragStart += StopEncouraging;

            Game.Phase = (byte)TableTopPhase.Move;

            kodamaEncourage = StartCoroutine(KodamaEncourageCoroutine());
        }

        private IEnumerator KodamaEncourageCoroutine()
        {
            waitBeforeKodamaEncourage ??= new WaitForSeconds(Game.Settings.timePhase * 0.5f);
            yield return waitBeforeKodamaEncourage;

            Game.CurrentPlayer.Kodama.RPC_EncouragePlayer(true);
            isKodamaEncouraging = true;
            kodamaEncourage = null;
        }

        private void StopEncouraging()
        {
            if (kodamaEncourage != null)
            {
                StopCoroutine(kodamaEncourage);
                kodamaEncourage = null;
            }
            else if (isKodamaEncouraging)
            {
                var currentPlayer = BaseTabletopPlayer.GetByPlayerIndex(Game.CurrentPlayerIndex);
                if (currentPlayer != null && currentPlayer.Kodama.HasStateAuthority)
                {
                    currentPlayer.Kodama.RPC_EncouragePlayer(false);
                }
                isKodamaEncouraging = false;
            }
        }

        public override void Exit()
        {
            base.Exit();
            Pawn.OnMoveAnimationEnd -= OnPawnMoveSucceed;
            TabletopGameEvents.OnPawnDragStart -= StopEncouraging;

            StopEncouraging();
        }

        private void OnPawnMoveSucceed()
        {
            StartCoroutine(NextPhase());
        }

        private IEnumerator NextPhase()
        {
            Game.Phase = (byte)TableTopPhase.EndPhase;
            yield return new WaitForSeconds(Game.Config.phaseDelay);
            ChangeToNextState();
        }
    }
}