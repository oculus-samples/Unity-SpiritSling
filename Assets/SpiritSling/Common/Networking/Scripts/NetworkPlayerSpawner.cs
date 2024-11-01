// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using SpiritSling.TableTop;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// The NetworkPlayerSpawner is responsible for spawner visual avatars for every players in a room
    /// </summary>
    public class NetworkPlayerSpawner : MonoBehaviour, INetworkRunnerCallbacks
    {
        #region Serialized Fields

        [SerializeField]
        private GameObject networkPlayerPrefab;

        #endregion

        #region Network Callbacks

        /// <summary>
        /// Detecting any player joining the room
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="player"></param>
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Log.Info("OnPlayerJoined PlayerId:" + player.PlayerId);
            if (runner.IsServer || player == runner.LocalPlayer)
            {
                SpawnLocalAvatar(runner, player);
            }
        }

        /// <summary>
        /// Spawning the local player avatar on the network
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="player"></param>
        private async void SpawnLocalAvatar(NetworkRunner runner, PlayerRef player)
        {
            // Create a unique position for the player
            var spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) - 2f, 0, 0);
            var asyncOp = runner.SpawnAsync(networkPlayerPrefab, spawnPosition, Quaternion.identity, player);

            await asyncOp;
        }

        /// <summary>
        /// On player leave, we make sure to remove the associated avatar
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="player"></param>
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Log.Info("OnPlayerLeft PlayerId:" + player.PlayerId);

            TabletopHumanPlayer.HandlePlayerDisconnect(player);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        #endregion
    }
}