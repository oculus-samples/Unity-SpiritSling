// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class TabletopMenuStateMachine : StateMachine
    {
        [Header("Bindings")]
        [SerializeField]
        private GameObject _connectionManagerPrefab;

        [SerializeField]
        private TabletopGameStateMachine[] gameStateMachines;

        [SerializeField]
        private GameObject _canvasRoot;

        [SerializeField]
        private UIConfig _uiConfig;

        [Header("States")]
        public RoomRequirementsState roomRequirementsState;

        public SetupTabletopPositionState setupTabletopPositionState;
        public TabletopMenuState _menuState;
        public TabletopTitleState titleState;
        public TabletopMainMenuState mainMenuState;
        public RoomLobbyState roomLobbyState;
        public PrivateGameRoomLobbyState privateGameRoomLobbyState;
        public NetworkErrorState networkErrorState;
        public TabletopTipsState tipsState;
        public TabletopCreditsState creditsState;
        public CountDownState countDownState;

        public GameObject UnifiedGameVolume { get; set; }
        public GameObject CanvasRoot => _canvasRoot;
        public UIConfig UIConfig => _uiConfig;
        public ConnectionManager ConnectionManager => _connectionManager;

        private ConnectionManager _connectionManager;

        private void Start()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            ChangeState(roomRequirementsState);
#else
            ChangeState(titleState);
#endif
        }

        public void BackToMenu()
        {
            if (UnifiedGameVolume == null)
            {
                ChangeState(setupTabletopPositionState);
            }
            else
            {
                ChangeState(mainMenuState);
            }
        }

        public override void Restart()
        {
            BackToMenu();
        }

        public override void ChangeState(State newState)
        {
            Log.Debug(
                $"> Menu: from {(CurrentState != null ? CurrentState.name : "null")} to " +
                $"{(newState != null ? newState.name : "null")}");

            // Ensures the canvas is active (it is hidden in game)
            _canvasRoot.SetActive(true);

            base.ChangeState(newState);
        }

        public void CreateNewConnectionManager()
        {
            var go = Instantiate(_connectionManagerPrefab);
            _connectionManager = go.GetComponent<ConnectionManager>();
            _connectionManager.MenuStateMachine = this;
            _connectionManager.GameStateMachines = gameStateMachines;
        }
    }
}
