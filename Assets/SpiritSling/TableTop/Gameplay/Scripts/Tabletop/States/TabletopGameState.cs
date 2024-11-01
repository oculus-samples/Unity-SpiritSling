// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Common behaviour and data for all TableTop states
    /// </summary>
    public abstract class TabletopGameState : State
    {
        [SerializeField]
        protected TabletopGameState nextState;

        [SerializeField]
        protected bool allowManualSkip;

        public TabletopGameStateMachine TabletopGameStateMachine => StateMachine as TabletopGameStateMachine;

        protected float cooldown;

        private float authRetryCooldown;
        private bool gotAuthority;

        public override void Enter()
        {
            TabletopGameManager.Instance.CanSkipPhase = allowManualSkip;
        }

        public override void Update()
        {
            // Avoid running timers in some cases
            if (TabletopGameStateMachine.IsAnyAnimationPlaying()) return;
            if (TabletopGameStateMachine.Player == null || TabletopGameStateMachine.Player.IsGameOver) return;
            if (Game == null) return;

            // Cooldown: max time of a turn
            if (cooldown > 0)
            {
                cooldown -= Time.deltaTime;
                Game.Timer = cooldown;

                if (cooldown < 0)
                {
                    TimeOut();
                }
            }
        }

        protected virtual void TimeOut()
        {
            ChangeToNextState();
        }

        public override void Exit()
        {
        }

        /// <summary>
        /// Get a list of all NetworkObject the current player should get authority on
        /// </summary>
        /// <returns></returns>
        protected List<NetworkObject> GetAllGameNetworkObjects()
        {
            var boardAndPawns = new List<NetworkObject>();
            boardAndPawns.Add(Game.Object);
            foreach (var kodama in Game.Kodamas) boardAndPawns.Add(kodama.Object);
            foreach (var slingshot in Game.Slingshots) boardAndPawns.Add(slingshot.Object);
            foreach (var lootbox in LootsManager.Instance.LootBoxesOnBoard)
                if (lootbox != null)
                    boardAndPawns.Add(lootbox.Object);

            return boardAndPawns;
        }

        /// <summary>
        /// Request  authority on the board and on all pawns
        /// </summary>
        /// <param name="request">true to request the authority, false to release it</param>
        /// <returns></returns>
        protected IEnumerator RequestStateAuthorityOnBoardObjects()
        {
            // -- Get all
            var boardObjects = GetAllGameNetworkObjects();

            // -- Request all
            foreach (var networkObject in boardObjects)
            {
                if (networkObject != null && !networkObject.HasStateAuthority && networkObject.isActiveAndEnabled)
                {
                    networkObject.RequestStateAuthority();
                }
            }

            // -- Wait all
            foreach (var networkObject in boardObjects)
            {
                var log = true;
                if (networkObject != null && networkObject.isActiveAndEnabled)
                {
                    while (!networkObject.HasStateAuthority)
                    {
                        if (log)
                        {
                            Log.Debug(" Waiting for authority on:" + networkObject);
                            log = false;
                        }

                        yield return null;
                    }
                }
            }

            yield return new WaitForSeconds(0.2f); // Wait a bit
        }

        /// <summary>
        /// Release authority on all board objects
        /// </summary>
        /// <returns></returns>
        protected IEnumerator ReleaseStateAuthorityOnBoardObjects()
        {
            // -- Get all
            var boardObjects = GetAllGameNetworkObjects();

            // -- Release all
            foreach (var networkObjects in boardObjects)
            {
                if (networkObjects != null && networkObjects.HasStateAuthority && networkObjects.isActiveAndEnabled)
                {
                    networkObjects.ReleaseStateAuthority();
                }
            }

            // -- Wait all
            foreach (var networkObjects in boardObjects)
            {
                var log = true;
                if (networkObjects != null && networkObjects.isActiveAndEnabled)
                {
                    while (networkObjects.HasStateAuthority)
                    {
                        if (log)
                        {
                            Log.Debug(" Waiting for releasing authority on:" + networkObjects);
                            log = false;
                        }

                        yield return null;
                    }
                }
            }

            yield return new WaitForSeconds(0.2f); // Wait a bit
        }


        /// <summary>
        /// Check if a pawn has no more owner, and remove it if so
        /// </summary>
        protected void CheckForPawnsToRemove()
        {
            // We're sure to have authority on all pawns here
            // So we can remove pawns without players
            foreach (var nob in GetAllGameNetworkObjects())
            {
                if (nob.TryGetComponent(out Pawn pawn))
                {
                    if (BaseTabletopPlayer.TabletopPlayers.Any(p => p.PlayerId == pawn.OwnerId) == false)
                    {
                        Game.Runner.Despawn(pawn.Object);
                    }
                }
            }
        }

        public TabletopGameManager Game => TabletopGameManager.Instance;
        public LootsManager Loots => LootsManager.Instance;

        public virtual void ChangeToNextState()
        {
            if (nextState != null)
            {
                StateMachine.ChangeState(nextState);
            }
        }

        public bool AllowManualSkip => allowManualSkip;
    }
}