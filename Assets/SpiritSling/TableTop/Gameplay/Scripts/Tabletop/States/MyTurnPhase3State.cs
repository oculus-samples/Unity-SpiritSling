// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SpiritSling.TableTop.TabletopGameManager;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Local player turn phase 3
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class MyTurnPhase3State : TabletopGameState
    {
        private int shotsCountThisTurn;
        private bool[] completedAnims;
        private WaitForSeconds waitForTileHeightAnimation;
        private bool hasTimeOut;

        public override void Awake()
        {
            base.Awake();
            waitForTileHeightAnimation = new WaitForSeconds(HexCellRenderer.UPDATE_HEIGHT_ANIMATION_LENGTH);
        }

        public override void Enter()
        {
            base.Enter();

            cooldown = Game.Settings.timePhase;
            shotsCountThisTurn = 0;
            hasTimeOut = false;

            TabletopGameEvents.OnPawnDragCanceled += OnDragCancel;
            SlingBall.OnShotSequenceDone += OnSlingBallShotDone;

            Game.Phase = (byte)TableTopPhase.Shoot;
        }

        public override void Exit()
        {
            TabletopGameEvents.OnPawnDragCanceled -= OnDragCancel;
            SlingBall.OnShotSequenceDone -= OnSlingBallShotDone;
            if (!hasTimeOut)
            {
                TabletopGameEvents.OnShootPhaseEnd?.Invoke();
            }
            base.Exit();
        }

        protected override void TimeOut()
        {
            hasTimeOut = true;
            if (PawnMovement.DraggedObject == null)
            {
                ChangeToNextState();
            }
            TabletopGameEvents.OnShootPhaseEnd?.Invoke();
        }

        private void OnDragCancel()
        {
            if (hasTimeOut)
            {
                ChangeToNextState();
            }
        }

        private void OnSlingBallShotDone(Slingshot slingshot, HexCell targetCell, bool hitCliff)
        {
            if (targetCell != null)
            {

                // Same cell? Cancel
                if (slingshot.CurrentCell == targetCell)
                {
                    slingshot.SlingBall.RefreshGrab();
                    // Bots always go to next state
                    if (hasTimeOut || (slingshot.Owner != null && !slingshot.Owner.IsHuman))
                    {
                        ChangeToNextState();
                    }
                }
                else
                {
                    // Execute damage resolution as a routine, so we can apply it step by step 
                    StartCoroutine(ShotRoutine(slingshot, targetCell, hitCliff));
                }
            }
            else
            {
                // Missed shot, we still move to the next turn
                GoToNextTurn();
            }
        }

        private IEnumerator ShotRoutine(Slingshot slingshot, HexCell targetCell, bool hitCliff)
        {
            Log.Debug($"Shot {targetCell} from {slingshot}");

            var targetPawn = targetCell.Pawn;

            if (targetPawn == null)
            {
                Instance.RPC_PlayHitTileSound(targetCell.Position);
            }

            // Wait shield impact if immune Kodama
            if (targetPawn is Kodama targetKodama && targetKodama.IsImmune)
            {
                targetKodama.ImpactShield();
                //Wait ImuneShield Animation
                yield return new WaitForSeconds(0.2f);
            }

            // Target tile diminution
            Instance.RPC_ChangeCellHeight(
            targetCell.Position, Mathf.Max(-1, targetCell.Height - 1)
            , false, true, hitCliff);


            if (targetPawn != null && !hitCliff)
            {
                // Pushback!
                // -- Plan the pushback chain before it happens
                var pushbackSequence = Instance.GetPushbackChain(
                    targetCell.Position, slingshot.CurrentCell.Position);

                PlayReactionIfPossible(targetPawn, pushbackSequence);

                // Execute pushback on each pawn
                completedAnims = new bool[pushbackSequence.Count];

                var index = pushbackSequence.Count - 1;
                foreach (var element in pushbackSequence)
                {
                    StartCoroutine(PushbackElement(element, index));
                    index--;
                }

                // Wait for animations to finish before going to next state
                var allAnimsCompleted = false;
                while (!allAnimsCompleted)
                {
                    allAnimsCompleted = true;
                    for (var i = 0; i < completedAnims.Length; i++)
                    {
                        allAnimsCompleted &= completedAnims[i];
                    }

                    if (allAnimsCompleted == false)
                    {
                        yield return null;
                    }
                }

                // The pushback may have triggered a lootbox, so wait for animations if there are (like respawn of a kodama due to a height down loot)
                foreach (var kodama in Game.Kodamas)
                {
                    yield return kodama.WaitForAnotherAnimation();
                }
                foreach (var sling in Game.Slingshots)
                {
                    yield return sling.WaitForAnotherAnimation();
                }
            }
            else
            {
                // Waits for the tile, and eventually pawn, going down animation
                yield return waitForTileHeightAnimation;

                // Check for lava
                if (targetPawn != null && targetCell.Height < 0)
                {
                    Log.Info(targetPawn.name + " falls down in lava due to a cliff shot");
                    targetPawn.DamageLava(Instance.Settings.lavaDamage);

                    // Waits for the pawn animations to end before going to next turn (which will change state authorities)
                    yield return targetPawn.WaitForAnotherAnimation();
                }
            }

            Log.Debug("End of shot");

            GoToNextTurn();
        }

        private void PlayReactionIfPossible(Pawn hitPawn, List<TabletopGameManager.PushbackData> pushbackSequence)
        {
            // If the current player hits one of his pawns
            if (hitPawn.OwnerId == Game.CurrentPlayer.PlayerId)
            {
                return;
            }

            // If the current player's kodama will be pushed back
            foreach (var element in pushbackSequence)
            {
                if (element.pawn == Game.CurrentPlayer.Kodama)
                {
                    return;
                }
            }

            Game.CurrentPlayer.Kodama.RPC_SetReaction(Random.Range(1, 6));
        }

        private IEnumerator PushbackElement(TabletopGameManager.PushbackData element, int index)
        {
            var pawn = element.pawn;
            completedAnims[index] = false;

            // Pushback (change tile with anim)
            pawn.Pushback(element.position, index, element.direction);

            // First pawn: takes damages
            if (index == 0)
            {
                pawn.Damage(Instance.Settings.slingshotDamage);
            }

            // Wait for RPC + anim
            yield return pawn.WaitForAnotherAnimation();

            if (pawn.IsKilled == false)
            {
                // Out of board
                if (element.position == HexGridRenderer.OutOfBoardPosition)
                {
                    // We need to trigger some logic at the end of the pushback animation if outside the board 
                    element.pawn.OutOfBoard();
                }
                else
                {
                    // Check for lava
                    var newCell = Instance.Grid.Get(pawn.Position);
                    if (newCell != null && newCell.Height < 0)
                    {
                        Log.Info(pawn.name + " was pushed in lava");
                        pawn.DamageLava(Instance.Settings.lavaDamage);
                    }
                }
            }

            yield return pawn.WaitForAnotherAnimation();

            completedAnims[index] = true;
        }

        private void GoToNextTurn()
        {
            shotsCountThisTurn++;
            if (shotsCountThisTurn >= Instance.Settings.slingBallShotsPerTurn || hasTimeOut)
            {
                Game.Phase = (byte)TableTopPhase.EndPhase;
                ChangeToNextState();
            }
            else if (BaseTabletopPlayer.GetByPlayerIndex(Game.CurrentPlayerIndex) != null)
            {
                // The player may shoot again
                foreach (var sling in Game.CurrentPlayer.Slingshots)
                {
                    sling.SlingBall.RefreshGrab();
                }
            }
        }
    }
}
