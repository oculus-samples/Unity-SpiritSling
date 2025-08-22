// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using Photon.Voice.Fusion;
using SpiritSling.TableTop;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// The connection manager is responsible for managing the network matchmaking of a particular game type
    /// It manages creating, joining and listing rooms, and handles the associated UI
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class ConnectionManager : Fusion.Behaviour, INetworkRunnerCallbacks
    {
        public static ConnectionManager Instance;

        #region Matchmaking variables

        /// <summary>
        /// The game type is a custom filter for the room and lobby
        /// </summary>
        public enum GameType
        {
            TableTop,
            Portal
        }

        public GameType gameType;

        [SerializeField]
        private GameObject settingsHolderPrefab;

        [SerializeField]
        private NetworkPlayerSpawner playerSpawner;

        public TabletopMenuStateMachine MenuStateMachine { get; set; }

        public TabletopGameStateMachine[] GameStateMachines { get; set; }

        private NetworkRunner m_runner;
        private NetworkEvents m_events;
        private FusionVoiceClient m_fusionVoiceClient;
        private List<SessionInfo> m_cachedSessionList;
        private string m_lastRoomName;

        #endregion

        #region Properties

        public bool CanJoinRoom { get; set; }
        public string LastError { get; set; }
        public bool RejoinLastRoom { get; set; }
        public int BotCount { get; private set; }
        public NetworkEvents Events => m_events;
        public NetworkRunner Runner => m_runner;

        private readonly string IsPrivate = "isprivate";
        public bool IsPrivateRoom
        {
            get
            {
                
                return Runner.SessionInfo.Properties[IsPrivate] == true;
            }
            set
            {
                Runner.SessionInfo.IsVisible = !value;

                var properties = 
                    Runner.SessionInfo.Properties.ToDictionary(p => p.Key, p=> p.Value);
                properties[IsPrivate] = value;
                Runner.SessionInfo.UpdateCustomProperties(properties);
            }
        }

        public string LastRoomName => m_lastRoomName;

        #endregion

        #region Methods

        private void Awake()
        {
            Instance = this;
        }

        public void CreateRunner()
        {
            Debug.Assert(m_runner == null && m_fusionVoiceClient == null);

            // Disable and re-enable the GameObject to avoid the execution of NetworkRunner's and FusionVoiceClient's awake before both components are added.
            gameObject.SetActive(false);
            var child = new GameObject("Network Runner");
            child.transform.parent = transform;
            m_runner = child.AddComponent<NetworkRunner>();
            m_fusionVoiceClient = child.AddComponent<FusionVoiceClient>();
            m_events = child.AddComponent<NetworkEvents>();
            gameObject.SetActive(true);

            m_events.OnShutdown = new NetworkEvents.ShutdownEvent();
            m_events.OnDisconnectedFromServer = new NetworkEvents.DisconnectFromServerEvent();
            m_events.OnConnectFailed = new NetworkEvents.ConnectFailedEvent();
            m_events.OnSessionListUpdate = new NetworkEvents.SessionListUpdateEvent();
            m_events.PlayerJoined = new NetworkEvents.PlayerEvent();
            m_events.PlayerLeft = new NetworkEvents.PlayerEvent();

            m_events.OnShutdown.AddListener(OnShutdown);
            m_events.OnDisconnectedFromServer.AddListener(OnDisconnectedFromServer);
            m_events.OnConnectFailed.AddListener(OnConnectFailed);
            m_events.OnSessionListUpdate.AddListener(OnSessionListUpdated);
            m_events.PlayerJoined.AddListener(playerSpawner.OnPlayerJoined);
            m_events.PlayerLeft.AddListener(playerSpawner.OnPlayerLeft);
        }

        public async Task<bool> PrepareLobby()
        {
            // Join the lobby for the specified game type, to retrieve the room list associated
            var lobbyJoined = await JoinLobby();

            if (!lobbyJoined)
                return false;

            await WaitForRunnerToBeReady();
            return true;
        }

        IEnumerator RestartDelayed(bool error)
        {
            foreach (var gameStateMachine in GameStateMachines)
            {
                gameStateMachine.gameObject.SetActive(false);
            }

            LeaveCurrentRoom();
            while (GetComponent<NetworkRunner>())
                yield return null;

            if (MenuStateMachine.CurrentState == MenuStateMachine.networkErrorState)
                yield return null;
            else if (error)
                MenuStateMachine.ChangeState(MenuStateMachine.networkErrorState);
            else
                MenuStateMachine.Restart();
        }

        /// <summary>
        /// When a room is a full, we start the game scene associated to the current game type 
        /// for the connected player on the room
        /// </summary>
        public void StartGameScene()
        {
            StartCoroutine(StartGameSceneRoutine());
        }

        private IEnumerator StartGameSceneRoutine()
        {
            // Close the room to prevent other players to join
            m_runner.SessionInfo.IsOpen = false;
            m_runner.SessionInfo.IsVisible = false;

            yield return null;

            // Display the tips screen while loading
            MenuStateMachine.ChangeState(MenuStateMachine.tipsState);
        }

        /// <summary>
        /// Get a random public room to join
        /// </summary>
        /// <returns></returns>
        private SessionInfo GetRoomToJoin(int maxPlayerCount = -1)
        {
            if (m_cachedSessionList == null)
            {
                return null;
            }

            var session = m_cachedSessionList.Find(
                s => s.PlayerCount < s.MaxPlayers && s.IsOpen && (maxPlayerCount <= 0 || s.MaxPlayers == maxPlayerCount)
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_STANDALONE_WIN
                     && s.Name.Contains("_DEV_")
#else
                                                              && s.Name.StartsWith(Application.version)
                                                              && !s.Name.Contains("_DEV_")
#endif
            );

            return session;
        }

        #endregion

        #region Networking

        /// <summary>
        /// Join the custom lobby room for the current game type
        /// </summary>
        /// <returns></returns>
        public async Task<bool> JoinLobby()
        {
            var result = await m_runner.JoinSessionLobby(SessionLobby.Custom, gameType.ToString());

            if (result.Ok)
            {
                Log.Info($"Lobby successfully joined: {gameType.ToString()}");
            }
            else
            {
                LastError = $"Please check your internet connection and try again.";
                Log.Error(LastError);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Join or create a public room with a specific amount of players
        /// </summary>
        /// <param name="maxPlayerCount"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public async Task<bool> JoinOrCreatePublicRoom(int maxPlayerCount, TabletopGameSettings settings)
        {
            var hasJoined = await JoinPublicRoom(maxPlayerCount);
            if (!hasJoined)
                hasJoined = await CreateRoom(maxPlayerCount, settings, true);
            return hasJoined;
        }

        /// <summary>
        /// Join a random room inside the current lobby
        /// </summary>
        /// <returns></returns>
        public async Task<bool> JoinPublicRoom(int maxPlayerCount = -1)
        {
            await WaitForRunnerToBeReady();

            //playerReadyStates = new Dictionary<PlayerRef, bool>();
            var session = GetRoomToJoin(maxPlayerCount);

            // Check if there are any Sessions to join
            if (session != null)
            {
                Log.Info($"Joining {session.Name}");

                // Join
                var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
                var result = await m_runner.StartGame(
                    new StartGameArgs { GameMode = GameMode.Shared, SessionName = session.Name, SceneManager = sceneManager });

                if (result.Ok)
                {
                    // all good
                    m_lastRoomName = session.Name;
                    Log.Info($"Room successfully joined: {session.Name}");
                    return true;
                }

                LastError = "Failed to join room:" + result.ShutdownReason;

                Log.Error(LastError);
                return false;
            }

            return false;
        }

        /// <summary>
        /// Join a random room inside the current lobby
        /// </summary>
        /// <returns></returns>
        public async Task<bool> JoinPrivateRoom(string roomName)
        {
            await WaitForRunnerToBeReady();

            Log.Info($"Joining {roomName}");

            // Join
            var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
            var result = await m_runner.StartGame(
                new StartGameArgs { GameMode = GameMode.Shared, SessionName = roomName, SceneManager = sceneManager });

            if (result.Ok)
            {
                // all good
                m_lastRoomName = roomName;
                Log.Info($"Room successfully joined: {roomName}");
                return true;
            }

            LastError = "Failed to join room:" + result.ShutdownReason;

            Log.Error(LastError);
            return false;
        }
        
        public async Task<bool> JoinPrivateRoomByCode(string roomCode)
        {
            await WaitForRunnerToBeReady();

            Log.Info($"Joining {roomCode}");
            
            var roomName = Application.version;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_STANDALONE_WIN
            roomName += "_DEV_";
#endif
            roomName += gameType + roomCode;
            // Join
            var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
            var result = await m_runner.StartGame(
                new StartGameArgs { GameMode = GameMode.Shared, SessionName = roomName, SceneManager = sceneManager,
                    EnableClientSessionCreation = false});

            if (result.Ok)
            {
                // all good
                m_lastRoomName = roomName;
                Log.Info($"Room successfully joined: {roomName}");
                return true;
            }

            LastError = "Failed to join room:" + result.ShutdownReason;

            Log.Error(LastError);
            return false;
        }

        /// <summary>
        /// Join another lobby room in order to quit the current room
        /// </summary>
        /// <returns></returns>
        public void LeaveCurrentRoom()
        {
            Log.Info("Quitting current room");

            // Destroying and recreating the network Runner
            // as it cannot be reused
            Destroy(m_fusionVoiceClient);
            m_fusionVoiceClient = null;
            Destroy(m_runner);
            m_runner = null;
        }

        /// <summary>
        /// Create a new room for the current game type
        /// </summary>
        /// <returns></returns>
        public async Task<bool> CreateRoom(int maxPlayerCount, TabletopGameSettings gameSettings, bool publicRoom, string prevRoomName = null, int botCount = 0)
        {
            BotCount = botCount;
            
            var random = new System.Random();
            var roomCode = new string(Enumerable.Repeat('A', 4)
                .Select(c => (char)('A' + random.Next(26)))
                .ToArray());
            
            Dictionary<string, SessionProperty> customProps = new();
            customProps["type"] = (int)gameType;
            customProps[IsPrivate] = !publicRoom;
            customProps["code"] = roomCode;

            Log.Info($"Creating Room - players={maxPlayerCount} bots={BotCount} type={gameType}");

            var sessionName = Application.version;
#if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_STANDALONE_WIN
            sessionName += "_DEV_";
#endif
            sessionName += gameType + roomCode;

            // Create a room with a specific name previously created
            if (!string.IsNullOrEmpty(prevRoomName))
            {
                sessionName = prevRoomName;
            }

            var sceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>();
            var result = await m_runner.StartGame(
                new StartGameArgs
                {
                    GameMode = GameMode.Shared,
                    PlayerCount = maxPlayerCount,
                    SessionName = sessionName,
                    CustomLobbyName = gameType.ToString(),
                    SessionProperties = customProps,
                    SceneManager = sceneManager,
                    IsVisible = publicRoom
                });

            if (result.Ok)
            {
                Log.Info("Room successfully created");
                m_lastRoomName = sessionName;
                if (gameType == GameType.TableTop)
                {
                    // Create a networked data holder
                    await m_runner.SpawnAsync(
                        settingsHolderPrefab, null, null, Runner.LocalPlayer, (_, obj) =>
                        {
                            var h = obj.GetComponent<TabletopGameSettingsHolder>();
                            h.GameSettings = gameSettings;
                            h.name = "GAME SETTINGS";
                        });
                }

                return true;
            }

            LastError = "Failed to create room:" + result.ShutdownReason;
            Log.Error(LastError);
            return false;
        }

        private async Task WaitForRunnerToBeReady()
        {
            while (!m_runner.IsCloudReady)
                await Task.Delay(100);
        }

        #endregion

        #region Player Readiness


        /// <summary>
        /// Get how many players are ready
        /// </summary>
        /// <returns></returns>
        public int GetPlayerReadyCount()
        {
            var count = 0;
            foreach (var player in m_runner.ActivePlayers)
            {
                var playerObj = BaseTabletopPlayer.GetByPlayerId(player.PlayerId);
                if (playerObj != null && playerObj.IsReady)
                    count++;
            }

            return count;
        }

        public void SetCountdown(int cd)
        {
            foreach (var player in m_runner.ActivePlayers)
            {
                var playerObj = BaseTabletopPlayer.GetByPlayerId(player.PlayerId);
                if (playerObj != null && playerObj.IsHuman)
                {
                    playerObj.SetCountdown(cd);
                }
            }
        }
        public void StartCountdown()
        {
            foreach (var player in m_runner.ActivePlayers)
            {
                var playerObj = BaseTabletopPlayer.GetByPlayerId(player.PlayerId);
                if (playerObj != null && playerObj.IsHuman)
                {
                    playerObj.StartCountdown();
                }
            }
        }
        
        public void StopCountdown()
        {
            if (m_runner == null)
                return;
            
            foreach (var player in m_runner.ActivePlayers)
            {
                var playerObj = BaseTabletopPlayer.GetByPlayerId(player.PlayerId);
                if (playerObj != null && playerObj.IsHuman)
                {
                    playerObj.StopCountdown();
                }
            }
        }

        #endregion

        #region Network Callbacks

        /// <summary>
        /// Receive the List of Sessions from the current Lobby
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="sessionList"></param>
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Log.Info($"Session List Updated with {sessionList.Count} session(s) in lobby:" + gameType);
            m_cachedSessionList = sessionList;

            for (var i = 0; i < sessionList.Count; i++)
            {
                Log.Info(
                    $"Session {sessionList[i].Name} players: {sessionList[i].PlayerCount}/{sessionList[i].MaxPlayers} open:{sessionList[i].IsOpen}");
            }

            CanJoinRoom = GetRoomToJoin() != null;
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
        }

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Log.Warning("Shutdown!");
            BaseTabletopPlayer.ClearTabletop();
            switch (shutdownReason)
            {
                case ShutdownReason.GameNotFound:
                    LastError = "Game not found.";
                    break;
                case ShutdownReason.GameIsFull:
                    LastError = "Game is full.";
                    break;
                case ShutdownReason.GameClosed:
                    LastError = "Game is closed.";
                    break;                    
                case ShutdownReason.MaxCcuReached:
                    LastError = "Max CCU reached.";
                    break;
                default:
                    LastError = "You have been disconnected.";
                    break;
            }
            
            TabletopGameEvents.OnConnectionManagerShutdown?.Invoke();

            StartCoroutine(RestartDelayed(shutdownReason != ShutdownReason.Ok));
        }

        public void OnConnectedToServer(NetworkRunner runner) { }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Log.Error("OnDisconnectedFromServer reason:" + reason);
            LastError = "You have been disconnected.";
            BaseTabletopPlayer.ClearTabletop();

            StartCoroutine(RestartDelayed(true));
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Log.Error("OnConnectFailed reason:" + reason);
            LastError = "Connection Failed: " + reason;
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key,
            ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }

        #endregion
    }
}
