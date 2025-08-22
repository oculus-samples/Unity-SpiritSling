// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Linq;
using Fusion.XR.Shared.Rig;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// First state before starting the game flow
    /// </summary>
    public class SetupGameState : TabletopGameState
    {
        [SerializeField]
        private GameObject _tableTop;

        [SerializeField]
        private AudioClip _transitionAudioClip;

        public override void Enter()
        {
            var player = TabletopGameStateMachine.Player;
            _ = StartCoroutine(player.IsHuman ? SetupHumanRoutine(player) : SetupBotRoutine((TabletopAiPlayer)player));
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
        }

        private IEnumerator SetupHumanRoutine(BaseTabletopPlayer player)
        {
            yield return null; //wait one frame to not be at same time as awake and on enable stuff
            AudioManager.Instance.Play(_transitionAudioClip, AudioMixerGroups.UI_Transitions);
            yield return null;

            var runner = ConnectionManager.Instance.Runner;
            var playerCount = BaseTabletopPlayer.TabletopPlayers.Count;
            var isFirstPlayer = player.Index == BaseTabletopPlayer.FirstPlayer.Index;

            // Get the game configuration from the room properties
            if (isFirstPlayer)
                runner.Spawn(_tableTop);

            // Wait for gamevolume and TabletopGameManager
            while (TabletopGameManager.Instance == null)
                yield return null;

            // Setup game volume
            GameVolume.Instance.Attach(TabletopGameManager.Instance.transform, false);

            // Create the grid on the board
            TabletopGameManager.Instance.InitializeGrid(playerCount);
            while (TabletopGameManager.Instance.GridRenderer.IsReady == false)
            {
                yield return null;
            }

            // Mark as initialized
            player.IsGameInitialized = true;

            // Wait for all players to load the scene
            // Note: it's super important to wait for all players to have the grid before spawning a pawn on a player!
            while (BaseTabletopPlayer.TabletopPlayers.All(p => p.IsGameInitialized) == false)
            {
                yield return null;
            }

            //first lootbox spawn
            if (isFirstPlayer)
                LootsManager.Instance.SpawnNewLootBoxes();

            yield return new WaitForSeconds(1f);

            yield return SpawnBoardObjects(player, playerCount);

            TabletopGameEvents.OnGameBoardReady?.Invoke();
            TabletopGameManager.Instance.ResetCloc();
            // Mark as ready
            player.IsGameReady = true;

            // Wait for all other players
            while (BaseTabletopPlayer.TabletopPlayers.All(p => p.IsGameReady) == false)
            {
                yield return null;
            }

            // First player starts the match!
            if (isFirstPlayer)
            {
                if (TabletopGameManager.Instance.Object.HasStateAuthority == false)
                    TabletopGameManager.Instance.Object.RequestStateAuthority();

                SetupGameData();
            }

            TabletopGameEvents.OnSetupComplete?.Invoke();
        }

        private IEnumerator SetupBotRoutine(TabletopAiPlayer player)
        {
            yield return null; //wait one frame to not be at same time as awake and on enable stuff
            var playerCount = BaseTabletopPlayer.TabletopPlayers.Count;

            // Waits for the human player to have started spawning his board objects to do the same for the bot
            while (BaseTabletopPlayer.LocalPlayer.Board == null)
            {
                yield return null;
            }

            yield return SpawnBoardObjects(player, playerCount);

            player.GameSettings = TabletopGameManager.Instance.Settings;
            player.Initialize();
            // Mark as ready
            player.IsGameReady = true;
        }

        private IEnumerator SpawnBoardObjects(BaseTabletopPlayer player, int playerCount)
        {
            // Get spawn location
            var spawnCell = GetSpawnCell(playerCount, player.Index);

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_MAC
            if (player.IsHuman)
            {
                // Place players in front of it
                MoveLocalHardwareRig(spawnCell);
            }
#endif

            // Create Playerboard
            yield return TabletopGameManager.Instance.BoardObjects.SpawnPlayerBoard(spawnCell, player);
            while (player.Board == null)
            {
                yield return null;
            }

            // Create slingshots
            yield return TabletopGameManager.Instance.BoardObjects.SpawnSlingshots(player);
            while (player.Slingshots.Count < player.Board.SlingshotsCount)
            {
                yield return null;
            }

            // Create Kodama
            yield return TabletopGameManager.Instance.BoardObjects.SpawnPlayerKodama(spawnCell, player);
            while (player.Kodama == null)
            {
                yield return null;
            }
        }

        private void SetupGameData()
        {
            TabletopGameManager.Instance.ClocRounds = (byte)
                Mathf.Max(
                    TabletopGameManager.Instance.Config.defaultGameSettings.roundsBeforeCloc +
                    BaseTabletopPlayer.TabletopPlayers.Count, 1);

            TabletopGameManager.Instance.ClocRadius = (byte)(TabletopGameManager.Instance.Grid.Radius + 1);

            TabletopGameManager.Instance.Round = 0;
            TabletopGameManager.Instance.Phase = 0;
        }

        /// <summary>
        /// Find the current player's kodama spawn tile
        /// </summary>
        /// <param name="playerCount"></param>
        /// <param name="playerIndex"></param>
        /// <returns></returns>
        public static HexCellRenderer GetSpawnCell(int playerCount, int playerIndex)
        {
            var spawnCells = TabletopGameManager.Instance.Grid.FindFurthestPoints(playerCount);

            Assert.AreEqual(playerCount, spawnCells.Count);
            Assert.IsTrue(playerIndex >= 0 && playerIndex < spawnCells.Count);

            var spawnCell = spawnCells[playerIndex];

            // spawnCell.Height = 1; // Force cell height on spawn position

            // Update color and height on visual
            var spawnCellRenderer = TabletopGameManager.Instance.GridRenderer.GetCell(spawnCell);
            //spawnCellRenderer.SetState(HexCellRenderer.CellState.Selected);
            spawnCellRenderer.UpdateHeight();

            return spawnCellRenderer;
        }

        /// <summary>
        /// Force the position of the local player behind his start tile
        /// </summary>
        private static void MoveLocalHardwareRig(HexCellRenderer spawnCellRenderer)
        {
            var rig = FindAnyObjectByType<HardwareRig>();

            var centerPosition = TabletopGameManager.Instance.GridRenderer.
                GetCell(TabletopGameManager.Instance.Grid.Get(new Vector3Int(0, 0))).transform.position;
            var spawnPosition = spawnCellRenderer.transform.position;
            var fwdToCenter = (centerPosition - spawnPosition).SetY(0);

            var position =
                spawnPosition - fwdToCenter.normalized * TabletopConfig.Get().rigDistance;
            position = position.SetY(0);

            rig.transform.SetPositionAndRotation(position, Quaternion.LookRotation(fwdToCenter));
        }
    }
}