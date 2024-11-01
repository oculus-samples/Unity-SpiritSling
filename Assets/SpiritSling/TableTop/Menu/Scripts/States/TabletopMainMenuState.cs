// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling.TableTop
{
    public class TabletopMainMenuState : TabletopMenuBaseState
    {
        [SerializeField]
        protected GameObject loadingPopup;

        [SerializeField]
        protected CustomButton backBtn;

        [Header("Public Game")]
        [SerializeField]
        private Button joinOrCreate1P;

        [SerializeField]
        private CustomButton joinOrCreate2P;

        [SerializeField]
        private CustomButton joinOrCreate3P;

        [SerializeField]
        private CustomButton joinOrCreate4P;

        [Header("SETTINGS")]
        [SerializeField]
        protected Button settingsBtn;

        [SerializeField]
        protected SettingsMenu settingsPanel;

        [Header("Private Game")]
        [SerializeField]
        private CustomButton createPrivateGameBtn;
        [SerializeField]
        private CustomButton joinPrivateGameBtn;
        [SerializeField]
        private TMP_InputField joinCodeTxt;
        
        [SerializeField]
        private CustomButton trainingBtn;

        private List<string> privateSessionsAlreadyJoined;
        private UIAnimation m_loadingUIAnimation;

        private bool triedToJoinGame;

        private bool isMainVersion;

        public override void Awake()
        {
            base.Awake();
            Log.Debug("createPrivateGameBtn:" + createPrivateGameBtn);

            createPrivateGameBtn.onClick.AddListener(() => OnClickCreatePrivateGame());
            joinPrivateGameBtn.onClick.AddListener(() => OnClickJoinPrivateGame());
            backBtn.onClick.AddListener(OnClickBack);

            if (settingsBtn != null)
            {
                settingsBtn.onClick.AddListener(OnClickSettings);
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
                settingsBtn.gameObject.SetActive(false);
#endif
            }

            if (joinOrCreate1P != null)
            {
                joinOrCreate1P.onClick.AddListener(() => OnClickJoinOrCreatePublicRoom(1));
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
                joinOrCreate1P.gameObject.SetActive(false);
#endif
            }

            joinOrCreate2P.onClick.AddListener(() => OnClickJoinOrCreatePublicRoom(2));
            joinOrCreate3P.onClick.AddListener(() => OnClickJoinOrCreatePublicRoom(3));
            joinOrCreate4P.onClick.AddListener(() => OnClickJoinOrCreatePublicRoom(4));

            trainingBtn.onClick.AddListener(CreateTrainingRoom);

            //keep it, as it initialize settings even for final game
            settingsPanel.Init();

            privateSessionsAlreadyJoined = new List<string>();

            m_loadingUIAnimation = new UIAnimation();
            m_loadingUIAnimation.TargetGo = loadingPopup;

            isMainVersion = !Application.version.Contains('-');// not a main version if semantic version contains '-'
        }

        public override async void Enter()
        {
            AddTransformerListeners(MenuStateMachine.UnifiedGameVolume);

            // Loading the lobby
            m_loadingUIAnimation.Init(MenuStateMachine.UIConfig);
            m_loadingUIAnimation.FadeIn();

            m_uiAnimation.Init(MenuStateMachine.UIConfig);
            mainPanel.SetActive(false);

            HandleConnectionManager();

            var lobbyReady = await MenuStateMachine.ConnectionManager.PrepareLobby();
            if (!lobbyReady)
            {
                MenuStateMachine.ChangeState(MenuStateMachine.networkErrorState);
                return;
            }

            // Setup the whole menu logic and the network runner
            MenuStateMachine.CanvasRoot.SetActive(true);

            // Check for invites and join the private room
            if (DetectPrivateInvitations())
                return;

            if (AppEntitlementCheck.IsReady && PlatformWrapper.Instance != null)
            {
                // Ensures that private lobby invitations and presence are cleared when returning to main menu
                PlatformWrapper.Instance.CloseInvitation();
                PlatformWrapper.Instance.ClearPresence();
            }

            FadeIn();
            m_loadingUIAnimation.FadeOut();

            triedToJoinGame = false;
            joinPrivateGameBtn.IsInteractable = true;
            
            PlatformWrapper.OnLaunchParamsChanged += OnLaunchParamsChanged;

            TabletopGameEvents.OnMainMenuEnter?.Invoke();
        }

        protected override void OnGameVolumeTransformed()
        {
            if (m_loadingUIAnimation.State != UIAnimation.AnimationState.Closed)
            {
                loadingPopup.SetActive(false);
            }
            else
            {
                base.OnGameVolumeTransformed();
            }
        }

        protected override void OnGameVolumeReleased()
        {
            if (m_loadingUIAnimation.State != UIAnimation.AnimationState.Closed)
            {
                loadingPopup.SetActive(true);
            }
            else
            {
                base.OnGameVolumeReleased();
            }
        }
        public override void Update()
        {
            var isInteractable = !triedToJoinGame && joinCodeTxt.text.Length == 4;
            if(isInteractable != joinPrivateGameBtn.IsInteractable)
            {
                joinPrivateGameBtn.IsInteractable = isInteractable;
            } 
            
            if (isMainVersion)
                return;

            //debug access
            if (OVRInput.GetDown(OVRInput.RawButton.RThumbstick))
            {
                if (settingsBtn != null)
                {
                    settingsBtn.gameObject.SetActive(!settingsBtn.gameObject.activeSelf);
                }

                if (joinOrCreate1P != null)
                {
                    joinOrCreate1P.gameObject.SetActive(!joinOrCreate1P.gameObject.activeSelf);
                }
            }
        }

        private void HandleConnectionManager()
        {
            // Creates the network manager if not existing yet,
            // of it has been destroyed by photon
            if (MenuStateMachine.ConnectionManager == null)
            {
                MenuStateMachine.CreateNewConnectionManager();
            }

            if (MenuStateMachine.ConnectionManager.Runner == null)
            {
                MenuStateMachine.ConnectionManager.CreateRunner();
            }
        }

        private bool DetectPrivateInvitations()
        {
            if (AppEntitlementCheck.IsReady && PlatformWrapper.Instance != null)
            {
                var launchDetails = PlatformWrapper.Instance.GetAppLaunchDetails();
                if (!string.IsNullOrEmpty(launchDetails.TrackingID) &&
                    !privateSessionsAlreadyJoined.Contains(launchDetails.TrackingID))
                {
                    m_loadingUIAnimation.FadeIn();
                    FadeOut();

                    JoinPrivateRoom(launchDetails.MatchSessionID, launchDetails.TrackingID);
                    return true;
                }
            }

            if (ConnectionManager.Instance.RejoinLastRoom)
            {
                m_loadingUIAnimation.FadeIn();
                FadeOut();

                // Join the last room but duplicate it using an additional char
                // This is to force Photon to destroy the original room and start fresh
                var lastRoom = ConnectionManager.Instance.LastRoomName + "#";
                CreateOrJoinPrivateRoom(lastRoom);
                return true;
            }

            return false;
        }

        private async void JoinPrivateRoom(string matchSessionID, string trackingId)
        {
            // Reference the invitation as processed so 
            // the user can leave without being added to it again
            privateSessionsAlreadyJoined.Add(trackingId);
            
            var succeed = await MenuStateMachine.ConnectionManager.JoinPrivateRoom(matchSessionID);

            if (succeed)
            {
                ConnectionManager.Instance.RejoinLastRoom = false;
                MenuStateMachine.ChangeState(MenuStateMachine.privateGameRoomLobbyState);
            }
            else
            {
                Debug.LogError("Could not join private room");
                MenuStateMachine.ChangeState(MenuStateMachine.networkErrorState);
            }   
        }

        public override void Exit()
        {
            FadeOut();

            m_loadingUIAnimation.FadeOut();

            RemoveTransformerListeners(MenuStateMachine.UnifiedGameVolume);

            PlatformWrapper.OnLaunchParamsChanged -= OnLaunchParamsChanged;
        }

        /// <summary>
        /// Callback when launch params have changed
        /// </summary>
        private void OnLaunchParamsChanged()
        {
            DetectPrivateInvitations();
        }

        /// <summary>
        /// Displays the room creation panel
        /// </summary>
        private void OnClickCreatePrivateGame()
        {
            CreateOrJoinPrivateRoom();
        }

        private void OnClickJoinPrivateGame()
        {
            JoinPrivateRoom();
            triedToJoinGame = true;
            joinPrivateGameBtn.IsInteractable = false;
        }

        private async void CreateOrJoinPrivateRoom(string roomName = null)
        {
            var data = settingsPanel.GetGameData();

            // Hide panel
            FadeOut();
            m_loadingUIAnimation.FadeIn();

            // Create the room with the given data
            var roomCreated = await MenuStateMachine.ConnectionManager.CreateRoom(4, data, false, roomName);

            if (roomCreated)
            {
                MenuStateMachine.ChangeState(MenuStateMachine.privateGameRoomLobbyState);
                var sessionName = MenuStateMachine.ConnectionManager.Runner.SessionInfo.Name;
                privateSessionsAlreadyJoined.Add(sessionName);
                ConnectionManager.Instance.RejoinLastRoom = false;
            }
            else
            {
                Debug.LogError("Could not create a room");
                MenuStateMachine.ChangeState(MenuStateMachine.networkErrorState);
            }
        }

        private async void JoinPrivateRoom()
        {
            var roomJoined = await MenuStateMachine.ConnectionManager.JoinPrivateRoomByCode(joinCodeTxt.text.ToUpper());
            if (roomJoined)
            {
                var sessionInfo =MenuStateMachine.ConnectionManager.Runner.SessionInfo;
                var sessionName = sessionInfo.Name;
                if(sessionInfo.IsVisible) // game was made public
                    MenuStateMachine.ChangeState(MenuStateMachine.roomLobbyState);
                else    
                    MenuStateMachine.ChangeState(MenuStateMachine.privateGameRoomLobbyState);
                
                privateSessionsAlreadyJoined.Add(sessionName);
                ConnectionManager.Instance.RejoinLastRoom = false;
            }
            //else
            //{
            //    Debug.LogError("Could not join room");
            //    MenuStateMachine.ChangeState(MenuStateMachine.networkErrorState);
            //}
        }
        private void OnClickBack()
        {
            MenuStateMachine.ChangeState(MenuStateMachine._menuState);
        }

        private void OnClickSettings()
        {
            settingsPanel.gameObject.SetActive(true);
        }

        /// <summary>
        /// Displays the room creation panel
        /// </summary>
        private async void OnClickJoinOrCreatePublicRoom(int pPlayerCount)
        {
            Log.Debug("OnClickJoinOrCreatePublicRoom");
            var data = settingsPanel.GetGameData();

            // Hide panel
            FadeOut();
            m_loadingUIAnimation.FadeIn();

            // Create the room with the given data
            var roomCreated = await MenuStateMachine.ConnectionManager.JoinOrCreatePublicRoom(pPlayerCount, data);

            if (roomCreated)
            {
                MenuStateMachine.ChangeState(MenuStateMachine.roomLobbyState);
            }
            else
            {
                Debug.LogError("Could not join or create a room");
                MenuStateMachine.ChangeState(MenuStateMachine.networkErrorState);
            }
        }

        private async void CreateTrainingRoom()
        {
            var data = settingsPanel.GetGameData();

            // Hide panel
            FadeOut();
            m_loadingUIAnimation.FadeIn();

            // Create the room with the given data
            var roomCreated = await MenuStateMachine.ConnectionManager.CreateRoom(1, data, false, null, 1);

            if (roomCreated)
            {
                MenuStateMachine.ConnectionManager.StartGameScene();
            }
            else
            {
                Debug.LogError("Could not create a room");
                MenuStateMachine.ChangeState(MenuStateMachine.networkErrorState);
            }
        }
    }
}