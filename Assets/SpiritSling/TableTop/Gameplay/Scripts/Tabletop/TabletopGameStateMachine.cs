// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// State machine for the table top game
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class TabletopGameStateMachine : StateMachine
    {
        [Header("States")]
        public SetupGameState setupGameState;

        public NewRoundState newRoundState;
        public NewTurnState newTurnState;
        public MyTurnPhase1State phase1State;
        public MyTurnPhase2State phase2State;
        public MyTurnPhase3State phase3State;
        public OtherPlayerTurnState otherPlayerTurnState;
        public EndMyTurnState endMyTurnState;
        public GameOverState gameOverState;
        public WinState winState;
        public ExitGameState exitState;

        [Header("Other")]
        [SerializeField]
        private AudioClip newRoundClip;

        public BaseTabletopPlayer Player { get; set; }

        private State changingState;
        private Coroutine changingStateRoutine;

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            TabletopGameEvents.OnNextTurnCalled -= CheckForNewTurn;
            TabletopGameEvents.OnRequestSkipPhase -= TrySkipPhase;
            TabletopGameEvents.OnRequestQuitGame -= OnRequestQuitGame;
            TabletopGameEvents.OnRequestLeaveToLobby -= OnRequestLeaveToLobby;
            TabletopGameEvents.OnWin -= OnWin;
            StateChanged -= OnStateChanged;

            Clear();
            Player = null;
        }

        private void OnEnable()
        {
            TabletopGameEvents.OnNextTurnCalled += CheckForNewTurn;
            TabletopGameEvents.OnRequestSkipPhase += TrySkipPhase;
            TabletopGameEvents.OnRequestQuitGame += OnRequestQuitGame;
            TabletopGameEvents.OnRequestLeaveToLobby += OnRequestLeaveToLobby;
            TabletopGameEvents.OnWin += OnWin;

            StateChanged += OnStateChanged;

            base.ChangeState(setupGameState);
        }

        public override void ChangeState(State newState)
        {
            if (newState != exitState)
            {
                if (Player.IsWinner)
                {
                    if (newState != winState)
                    {
                        Log.Info($"Ignoring new state {newState}: player is winner");
                        newState = winState;
                    }
                }
                else if (Player.IsGameOver && newState != gameOverState)
                {
                    Log.Info($"Ignoring new state {newState}: player is eliminated");
                    newState = gameOverState;
                }
            }

            // This should almost never happen
            // Except on few exceptional use case like the game over/victory
            if (changingStateRoutine != null)
            {
                Log.Warning($"Interrupting changing state! {changingState} -> {newState}");
                StopCoroutine(changingStateRoutine);
                changingStateRoutine = null;
            }

            changingStateRoutine = StartCoroutine(WaitAnimationAndChangeState(newState));
        }

        private IEnumerator WaitAnimationAndChangeState(State newState)
        {
            var anyAnimation = true;
            changingState = newState;

            // Wait until all animations are over before changing game state
            while (anyAnimation)
            {
                yield return null;
                anyAnimation = IsAnyAnimationPlaying();
            }

            changingStateRoutine = null;
            SetState(newState);
        }

        private void OnStateChanged(State prev, State current)
        {
            Log.Debug($"> Game: from {(prev ? prev.name : "none")} to {(current ? current.name : current)}");
#if UNITY_EDITOR
            if (TabletopGameManager.Instance)
                TabletopGameManager.Instance.gameObject.name = $"TableTop - {current.name}";
#endif            
        }

        protected void CheckForNewTurn(byte nextPlayerIndex, bool forceNoNewRound)
        {
            var isNewRound = !forceNoNewRound && BaseTabletopPlayer.TabletopPlayers.Count > 0 && nextPlayerIndex == BaseTabletopPlayer.FirstPlayer.Index;

            Log.Info($"New turn! Next player={nextPlayerIndex} New round={isNewRound}");

            if (Player.IsGameOver == false && Player.Index == nextPlayerIndex)
            {
                // Local player turn!
                ChangeState(isNewRound ? newRoundState : newTurnState);
            }

            if (isNewRound)
            {
                AudioManager.Instance.Play(newRoundClip, AudioMixerGroups.UI_Gameplay);
            }
        }

        void OnRequestLeaveToLobby()
        {
            exitState.BackToLobby = true;
            ChangeState(exitState);
        }

        void OnRequestQuitGame()
        {
            exitState.BackToLobby = false;
            ChangeState(exitState);
        }

        private void OnWin(BaseTabletopPlayer winner)
        {
            if (Player == winner)
            {
                ChangeState(winState);
            }
        }

        void TrySkipPhase()
        {
            if ((_currentState as TabletopGameState).AllowManualSkip)
            {
                (_currentState as TabletopGameState).ChangeToNextState();
            }
        }

        public static bool IsAnyAnimationPlaying()
        {
            if (TabletopGameManager.Instance == null)
                return false;

            // use traditional for loop to avoid garbage generated by Any or foreach
            var kodamas = TabletopGameManager.Instance.Kodamas;
            for (var i = 0; i < kodamas.Count; i++)
            {
                if (kodamas[i].IsPlayingAnimation)
                {
                    return true;
                }
            }

            var slingshots = TabletopGameManager.Instance.Slingshots;
            for (var i = 0; i < slingshots.Count; i++)
            {
                if (slingshots[i].IsPlayingAnimation)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
