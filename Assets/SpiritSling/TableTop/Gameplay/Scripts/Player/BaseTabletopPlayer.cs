// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public abstract class BaseTabletopPlayer : NetworkPlayer
    {
        [Networked]
        public int Countdown { get; set; }
        
        [SerializeField]    
        private AudioClip countdownAudioClip;

        
        private AudioSource countdownAudioSource;
        
        #region Static fields and properties

        public static readonly string[] DEFAULT_PLAYER_NAMES = { "Player 1", "Player 2", "Player 3", "Player 4" };

        /// <summary>
        /// List of all players in the game
        /// </summary>
        public static List<BaseTabletopPlayer> TabletopPlayers { get; private set; } = new();

        public static int HumanPlayersCount
        {
            get
            {
                var count = 0;
                foreach (var player in TabletopPlayers)
                {
                    if (player.IsHuman)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// The local human player reference
        /// </summary>
        public static TabletopHumanPlayer LocalPlayer { get; protected set; }

        public static BaseTabletopPlayer FirstPlayer => TabletopPlayers.First();
        
        #endregion

        #region Static methods

        protected static void AddPlayer(BaseTabletopPlayer player)
        {
            TabletopPlayers.Add(player);
                        
            ReorderPlayers(player);

            Log.Info($"Adding new player {player.PlayerId} index {player.Index}");
        }

        protected static void RemovePlayer(BaseTabletopPlayer player)
        {
            if (player != null)
            {
                TabletopPlayers.Remove(player);
            }

            // Safety clean
            TabletopPlayers = TabletopPlayers.Where(p => p != null).ToList();
        }

        protected static void ReorderPlayers(BaseTabletopPlayer changedPlayer)
        {
            bool newPlayer = TabletopPlayers.FirstOrDefault(p => p == changedPlayer);
            var nbPlayers = TabletopPlayers.Count;

            // Re-order everyone
            if (newPlayer)
            {
                TabletopPlayers = TabletopPlayers.OrderBy(p => p.PlayerId).ToList();
                switch (nbPlayers)
                {
                    case 3:
                        {
                            TabletopPlayers[0].Index = 2;
                            TabletopPlayers[1].Index = 1;
                            TabletopPlayers[2].Index = 0;
                        }
                        break;
                    case 4:
                        {
                            TabletopPlayers[0].Index = 3;
                            TabletopPlayers[1].Index = 1;
                            TabletopPlayers[2].Index = 0;
                            TabletopPlayers[3].Index = 2;
                        }
                        break;
                    default:
                    {
                        foreach (var p in TabletopPlayers)
                        {
                            p.Index = (byte)TabletopPlayers.IndexOf(p);
                        }
                    }
                    break;
                }
            }
            else
            {
                TabletopPlayers = TabletopPlayers.OrderBy(p => p.Index).ToList();
                var leaverIndex = changedPlayer.Index;
                Debug.Log($"[LOBBY] Player {leaverIndex+1} left, reordering.");
                switch (nbPlayers)
                {
                    case 1:
                        {
                            TabletopPlayers[0].Index = 0;
                        }
                        break;
                    case 2:
                        {
                            TabletopPlayers[0].Index = 1;
                            TabletopPlayers[1].Index = 0;
                        }
                        break;
                    case 3:
                        {
                            switch (leaverIndex)
                            {
                                case 0:
                                    TabletopPlayers[0].Index = 2;
                                    TabletopPlayers[1].Index = 0;
                                    TabletopPlayers[2].Index = 1;
                                    break;
                                case 1:
                                    TabletopPlayers[0].Index = 1;
                                    TabletopPlayers[1].Index = 2;
                                    TabletopPlayers[2].Index = 0;
                                    break;
                                case 2:
                                    TabletopPlayers[0].Index = 0;
                                    TabletopPlayers[1].Index = 1;
                                    TabletopPlayers[2].Index = 2;
                                    break;
                                case 3:
                                    TabletopPlayers[0].Index = 2;
                                    TabletopPlayers[1].Index = 0;
                                    TabletopPlayers[2].Index = 1;
                                    break;                                
                            }
                        }
                        break;
                }
            }
            TabletopPlayers = TabletopPlayers.OrderBy(p => p.Index).ToList();
        }

        public static void ClearTabletop()
        {
            LocalPlayer = null;
            TabletopPlayers.Clear();
        }

        /// <summary>
        /// Get a player using its player index
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public static BaseTabletopPlayer GetByPlayerIndex(byte playerIndex)
        {
            return TabletopPlayers.FirstOrDefault(t => t.Index == playerIndex);
        }

        /// <summary>
        /// Get a player using its networked player ref PlayerId
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static BaseTabletopPlayer GetByPlayerId(int playerId)
        {
            return TabletopPlayers.FirstOrDefault(t => t.PlayerId == playerId);
        }

        #endregion

        #region Member fields and properties

        [SerializeField, Min(0)]
        private float delayBeforeStartingWinAnimation = 0.5f;

        public abstract bool IsHuman { get; }

        /// <summary>
        /// Index for player (0 = first, 1 = second, etc).
        /// Never change passed the setup phase, even if a player is removed from the list.
        /// Setup when adding to the player list 
        /// </summary>
        public int Index { get; set; }

        public Kodama Kodama { get; set; }

        public Playerboard Board { get; set; }

        public List<Slingshot> Slingshots { get; } = new();

        /// <summary>
        /// Player has loaded the scene/core systems
        /// </summary>
        [Networked]
        public NetworkBool IsGameInitialized { get; set; }

        /// <summary>
        /// Player is ready to start the game
        /// </summary>
        [Networked]
        public NetworkBool IsGameReady { get; set; }

        /// <summary>
        /// Player cannot play anymore
        /// </summary>
        [Networked]
        public NetworkBool IsGameOver { get; set; }

        /// <summary>
        /// Player has won the game
        /// </summary>
        [Networked]
        public NetworkBool IsWinner { get; set; }

        /// <summary>
        /// Player is in a cutscene
        /// </summary>
        [Networked]
        public NetworkBool HasStartCutsceneCompleted { get; set; }

        #endregion

        #region Member methods
        
        
        public void SetCountdown(int cd)
        {
            RPC_SetCountdown(cd);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_SetCountdown(int cd)
        {
            Countdown = cd;
        }
        
        public void StartCountdown()
        {
            RPC_StartCountdown();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_StartCountdown()
        {
            countdownAudioSource = AudioManager.Instance.Play(countdownAudioClip, AudioMixerGroups.SFX_Countdown);
        }
        
        public void StopCountdown()
        {
            RPC_StopCountdown();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_StopCountdown()
        {
            if (countdownAudioSource != null)
            {
                countdownAudioSource.Stop();
                countdownAudioSource = null;                
            }

        }
        
        protected override void OnGameStart()
        {
            base.OnGameStart();

            countdownAudioSource = null;

        }
        public override void Spawned()
        {
            base.Spawned();
            AddPlayer(this);
        }

        
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SetDefeated()
        {
            if (IsGameOver)
            {
                Log.Error("Player has already lost");
                return;
            }

            IsGameOver = true;
            IsWinner = false;
            Log.Warning($"Game Over Player {Index} :(");
            RPC_GameOver();
        }

        /// <summary>
        /// Propagate game end state to all players locally
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_GameOver()
        {
            // One player is now "game over"
            Log.Info($"Player {Index} game ended.");

            // -- Propagate info to local systems BEFORE any list modification
            TabletopGameEvents.OnGameOver?.Invoke(this);

            // -- Remove player from the players list (this remove the player from the turn)
            RemovePlayer(this);
        }

        /// <summary>
        /// End the game for current player, declare Victory
        /// </summary>
        public void Victory()
        {
            if (HasStateAuthority)
            {
                IsWinner = true;
                IsGameOver = true;
            }
            Log.Warning($"Player {Index} is the winner!");
            TabletopGameEvents.OnWin?.Invoke(this);

            // Starts the coroutine on the local player to be sure that it will play correctly even if the winner leaves the game
            LocalPlayer.StartWinCoroutine(Index);
        }

        public void StartWinCoroutine(int winnerIndex)
        {
            StartCoroutine(WinCoroutine(winnerIndex));
        }

        private IEnumerator WinCoroutine(int winnerIndex)
        {
            // Destroy & hide all loots (use a copy of the list because the original is modified by the destruction of the loots)
            var loots = LootsManager.Instance.LootBoxesOnBoard.ToArray();
            foreach (var lootBox in loots)
            {
                lootBox.gameObject.SetActive(false);
                if (lootBox.HasStateAuthority)
                {
                    lootBox.Damage(lootBox.HealthPoints);
                }
            }

            yield return new WaitForSeconds(delayBeforeStartingWinAnimation);

            var winner = GetByPlayerIndex((byte)winnerIndex);
            // Winner may have been disconnected
            if (winner != null)
            {
                var slingshots = winner.Slingshots;

                // Kodama may have been killed by CLOC
                if (winner.Kodama != null)
                {
                    yield return winner.Kodama.WinDisappearAnim();
                }

                foreach (var slingshot in slingshots)
                {
                    if (slingshot != null)
                    {
                        yield return slingshot.WinDisappearAnim();
                    }
                }
            }

            yield return TabletopGameManager.Instance.GridRenderer.HideGridAnimation();

            // Propagate win to other systems
            TabletopGameEvents.OnBoardClearedAfterVictory?.Invoke(winnerIndex);
        }

        public void ClearPawns()
        {
            if (LocalPlayer == null || Kodama == null || !Kodama.HasStateAuthority)
            {
                return;
            }

            Log.Info($"Removing pawns of player {this}");

            // Use the local player's runner because if this player leaved, his runner can be null
            LocalPlayer.Runner.Despawn(Kodama.Object);

            var slingshots = Slingshots.ToArray();
            foreach (var s in slingshots)
            {
                if (s && s.HasStateAuthority)
                {
                    LocalPlayer.Runner.Despawn(s.Object);
                }
            }
            Slingshots.Clear();
        }

        #endregion
    }
}