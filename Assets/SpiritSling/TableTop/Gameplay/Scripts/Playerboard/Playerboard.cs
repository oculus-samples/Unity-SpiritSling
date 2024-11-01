// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// This class is managing each Playerboard
    /// </summary>
    [SelectionBase]
    public class Playerboard : NetworkBehaviour
    {
        [Header("Bindings")]
        [SerializeField]
        private Transform[] slingShotsSlots;

        [SerializeField]
        private PlayerboardUI ui;

        [SerializeField]
        private Transform visualTransform;

        private PlayerBoardVFXController playerBoardVFXController;

        [Networked]
        public int OwnerId { get; set; }

        public int SlingshotsCount => slingShotsSlots.Length;

        private void Awake()
        {
            if (ui != null)
            {
                ui.Playerboard = this;
            }
        }

        public override void Spawned()
        {
            base.Spawned();

            name = "PlayerBoard_" + OwnerId;
            transform.localScale = Vector3.one * TabletopConfig.Get().playerboardScale;

            // Position and attach to game volume
            if (GameVolume.Instance != null)
            {
                var spawnCell = SetupGameState.GetSpawnCell(BaseTabletopPlayer.TabletopPlayers.Count,
                    BaseTabletopPlayer.GetByPlayerId(OwnerId).Index);

                var playerBoardSpawn = GameVolume.Instance.GetClosestPlayerBoard(spawnCell.transform.position);
                transform.SetParent(playerBoardSpawn);
                transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            var player = BaseTabletopPlayer.GetByPlayerId(OwnerId);
            player.Board = this;

            playerBoardVFXController = Instantiate(TabletopGameManager.Instance.Config.PlayerBoardPrefab(player.Index), visualTransform).
                GetComponent<PlayerBoardVFXController>();
            playerBoardVFXController.OwnerId = OwnerId;

            TabletopGameManager.Instance.Playerboards.Add(this);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            TabletopGameManager.Instance.Playerboards.Remove(this);
        }

        public Transform GetSlot(int boardSlot)
        {
            if (boardSlot >= 0 && boardSlot < slingShotsSlots.Length) return slingShotsSlots[boardSlot];

            Log.Error($"Couldn't find slingshot slot {boardSlot}");
            return null;
        }
    }
}