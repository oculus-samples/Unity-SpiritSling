// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using System.Linq;
using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class TabletopHumanPlayer : BaseTabletopPlayer
    {
        public override string DisplayName => HasStateAuthority ? "Me" : DEFAULT_PLAYER_NAMES[Index];

        public override bool IsHuman => true;

        public CustomAvatarEntity AvatarEntity { get; set; }

        private Coroutine handleDisconnectRoutine;

        public override void Spawned()
        {
            base.Spawned();

            if (Object.HasStateAuthority)
            {
                LocalPlayer = this;
                StartCoroutine(WaitAndApplySettings());
            }

            UpdateGameVolumeRotation();
            TabletopGameEvents.GameStart += UpdateGameVolumeRotation;
            TabletopGameEvents.OnPlayerJoin?.Invoke(this);

#if UNITY_EDITOR
            name = $"Tabletop Player {Object.StateAuthority}";
#endif
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            TabletopGameEvents.GameStart -= UpdateGameVolumeRotation;
        }

        /// <summary>
        /// Waits for the settings to be spawned by the network and apply them.
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitAndApplySettings()
        {
            TabletopGameSettingsHolder settingsHolder;
            do
            {
                yield return null;
                settingsHolder = FindAnyObjectByType<TabletopGameSettingsHolder>();
            }
            while (settingsHolder == null);

            if (settingsHolder.GameSettings.showAvatars)
            {
                FindAnyObjectByType<CustomAvatarSpawnerFusion>().SpawnAvatar();
            }
        }

        /// <summary>
        /// In case of private game, the maximum number of players can be different than the real number of players.
        /// So, we need to update the game volume rotation when starting the game in order to have players facing their game player board.
        /// </summary>
        public void UpdateGameVolumeRotation()
        {
            var playerCount = Runner.SessionInfo.PlayerCount;

            if (GameVolume.Instance == null)
            {
                Log.Error("[GAMEVOLUME] Could not find GameVolume");
                return;
            }

            // Ensures local player exists in the case where a remote player is spawned before the local one (is it possible?)
            if (LocalPlayer != null)
            {
                GameVolume.Instance.RotatePivotForPlayerIndex(LocalPlayer.Index, playerCount);
            }
        }

        protected void Update()
        {
            if (transform.parent == null &&
                GameVolume.Instance != null)
            {
                Log.Debug("Reparenting player to game volume pivot");
                GameVolume.Instance.Attach(transform, true);
            }
        }

        /// <summary>
        /// Player has left and will remove its TabletopPlayer and all its pawns.
        /// We need to updated the PlayerList and make sure we're not softlocked
        /// </summary>
        /// <param name="playerRef"></param>
        public static void HandlePlayerDisconnect(PlayerRef playerRef)
        {
            Log.Info($"Removing player {playerRef.PlayerId}");

            // Remove null and players who left
            var leaver = TabletopPlayers.FirstOrDefault(p => p != null && p.PlayerId == playerRef.PlayerId);
            RemovePlayer(leaver);

            // Reorder the players only if the game hasn't started
            if (TabletopGameManager.Instance == null && LocalPlayer != null && LocalPlayer.Index != leaver.Index)
            {
                ReorderPlayers(leaver);
                LocalPlayer.UpdateGameVolumeRotation();
            }

            if (leaver != null)
            {
                leaver.ClearPawns();
            }

            TabletopGameEvents.OnPlayerLeave?.Invoke(leaver);

            if (TabletopGameManager.Instance == null) // not in game
            {
                return;
            }

            TabletopGameEvents.OnGameOver?.Invoke(leaver);

            // Was it the current player?
            // If yes, we need player 0 to take back control and move on to next player
            // But only if not already winner
            var wasCurrentPlayer = TabletopGameManager.Instance.Object.StateAuthority == playerRef;
            if (wasCurrentPlayer && LocalPlayer.IsGameOver == false && LocalPlayer.IsWinner == false
                && TabletopPlayers.Count > 0 && TabletopPlayers[0].Index == LocalPlayer.Index)
            {
                // The first player will handle the recovery
                LocalPlayer.HandleCurrentPlayerHasLeft();
            }
        }

        private void HandleCurrentPlayerHasLeft()
        {
            if (handleDisconnectRoutine != null)
            {
                StopCoroutine(handleDisconnectRoutine);
                handleDisconnectRoutine = null;
            }

            handleDisconnectRoutine = StartCoroutine(HandleCurrentPlayerHasLeftRoutine());
        }

        private IEnumerator HandleCurrentPlayerHasLeftRoutine()
        {
            Log.Warning("Current player has disconnected. P1 is recovering the game.");

            if (TabletopGameManager.Instance.Object.HasStateAuthority == false)
                TabletopGameManager.Instance.Object.RequestStateAuthority();
            while (TabletopGameManager.Instance.Object.HasStateAuthority == false)
            {
                yield return null;
            }

            // Setup current player
            TabletopGameManager.Instance.SetNextPlayer();

            yield return new WaitForSeconds(0.5f);

            TabletopGameManager.Instance.Object.ReleaseStateAuthority();
            while (TabletopGameManager.Instance.Object.HasStateAuthority)
            {
                yield return null;
            }

            // Call for next turn
            TabletopGameManager.Instance.RPC_NextTurn(TabletopGameManager.Instance.CurrentPlayerIndex);

            handleDisconnectRoutine = null;
        }
    }
}
