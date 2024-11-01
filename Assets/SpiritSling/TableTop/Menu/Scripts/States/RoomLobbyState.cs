// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class RoomLobbyState : TabletopMenuBaseState
    {
        [SerializeField]
        protected List<PlayerScreen> screens;

        [SerializeField]
        protected CustomButton readyBtn;

        [SerializeField]
        protected CustomButton quitRoomBtn;

        [SerializeField]
        protected TMP_Text countdownTxt;

        [SerializeField]
        protected int countdownSecs = 3;

        protected int m_cachedPlayerCount;
        protected int m_cachedReadyCount;

        protected float m_countdown;
        protected Coroutine m_cdCoroutine;

        public override void Awake()
        {
            base.Awake();

            quitRoomBtn.onClick.AddListener(OnClickBack);
            readyBtn.onClick.AddListener(OnClickReady);
        }

        public override void Enter()
        {
            base.Enter();

            MenuStateMachine.ConnectionManager.Events.PlayerJoined.AddListener(OnPlayerJoined);
            MenuStateMachine.ConnectionManager.Events.PlayerLeft.AddListener(OnPlayerLeft);
            m_cachedPlayerCount = 0;
            m_cachedReadyCount = 0;
            m_cdCoroutine = null;
            m_countdown = countdownSecs;
            countdownTxt.text = "" + countdownSecs;
            TabletopGameEvents.OnLobbyEnter?.Invoke();
            StartCoroutine(InitializeReadyState());
        }

        protected virtual IEnumerator InitializeReadyState()
        {
            while (BaseTabletopPlayer.LocalPlayer == null)
            {
                yield return null;
            }
            MenuStateMachine.ConnectionManager.SetCountdown(countdownSecs);

            BaseTabletopPlayer.LocalPlayer.IsReady = false;
            readyBtn.IsInteractable = true;
        }

        public override void Update()
        {
            var runner = MenuStateMachine.ConnectionManager.Runner;
            if (runner != null)
            {
                // Doesn't count bots
                var playersCount = BaseTabletopPlayer.HumanPlayersCount;
                var readyCount = MenuStateMachine.ConnectionManager.GetPlayerReadyCount();
                var slotsCount = Mathf.Min(runner.SessionInfo.MaxPlayers, playersCount);

                if (playersCount != m_cachedPlayerCount || readyCount != m_cachedReadyCount)
                {
                    UpdateSlots(playersCount, slotsCount, readyCount);
                }

                if (BaseTabletopPlayer.LocalPlayer != null)
                {
                    var isPlayerNotReady = !BaseTabletopPlayer.LocalPlayer.IsReady;
                    if (readyBtn.IsInteractable != isPlayerNotReady)
                    {
                        readyBtn.IsInteractable = isPlayerNotReady;
                    }
                    countdownTxt.text = "" + BaseTabletopPlayer.LocalPlayer.Countdown;
                }
            }
        }

        /// <summary>
        /// Update the player status
        /// </summary>
        /// <param name="playerCount"></param>
        /// <param name="maxCount"></param>
        /// <param name="readyCount"></param>
        protected void UpdateSlots(int playerCount, int maxCount, int readyCount)
        {
            m_cachedPlayerCount = playerCount;
            m_cachedReadyCount = readyCount;

            var slots = SelectScreen(maxCount);

            for (var i = 0; i < maxCount; i++)
            {
                var player = BaseTabletopPlayer.GetByPlayerIndex((byte)i);
                if (player == null || !player.IsReady)
                {
                    slots[i].SetState(PlayerSlot.State.Waiting);
                }
                else
                {
                    slots[i].SetState(PlayerSlot.State.Ready);
                }

                slots[i].PlayerName = player == null ? BaseTabletopPlayer.DEFAULT_PLAYER_NAMES[i] : player.DisplayName;
            }
        }

        /// <summary>
        /// Activate the UI screen for the matching player count
        /// </summary>
        /// <param name="playerCount"></param>
        /// <returns></returns>
        protected List<PlayerSlot> SelectScreen(int playerCount)
        {
            var screensToHide = screens.FindAll(s => s.playerCount != playerCount);
            foreach (var s in screensToHide)
                s.gameObject.SetActive(false);

            var screenToShow = screens.Find(s => s.playerCount == playerCount);
            screenToShow.gameObject.SetActive(true);
            return screenToShow.slots;
        }

        public override void Exit()
        {
            MenuStateMachine.ConnectionManager.Events.PlayerJoined.RemoveListener(OnPlayerJoined);
            MenuStateMachine.ConnectionManager.Events.PlayerLeft.RemoveListener(OnPlayerLeft);

            m_cdCoroutine = null;
            MenuStateMachine.ConnectionManager.StopCountdown();

            base.Exit();
        }

        /// <summary>
        /// Set the player has ready to player and notify the other players
        /// </summary>
        protected virtual void OnClickReady()
        {
            if (BaseTabletopPlayer.LocalPlayer.IsReady)
                return;

            readyBtn.IsInteractable = false;
            StartCoroutine(ChangeReadyState());
        }

        protected IEnumerator ChangeReadyState()
        {
            while (BaseTabletopPlayer.LocalPlayer == null)
            {
                yield return null;
            }

            BaseTabletopPlayer.LocalPlayer.IsReady = true;
            CheckStartCountdown();
        }

        protected virtual void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Log.Debug("[LOBBY] player joined");
            AbortCountdown();
        }
        protected virtual void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Log.Debug("[LOBBY] player left");
            AbortCountdown();
        }
        private void CheckStartCountdown()
        {
            var nbPlayers = BaseTabletopPlayer.HumanPlayersCount;

            bool canCountdown = BaseTabletopPlayer.LocalPlayer.Index == 0 && BaseTabletopPlayer.LocalPlayer.IsReady
                && (nbPlayers > 1 || MenuStateMachine.ConnectionManager.Runner.SessionInfo.MaxPlayers == 1);
            // if first player is ready and enough player in lobby, start coroutine
            if (canCountdown)
            {
                m_cdCoroutine = StartCoroutine(CountdownCoroutine());
            }
        }

        private IEnumerator CountdownCoroutine()
        {
            //wait for all other player to be ready
            while (MenuStateMachine.ConnectionManager.GetPlayerReadyCount() < BaseTabletopPlayer.HumanPlayersCount)
            {
                if (BaseTabletopPlayer.LocalPlayer == null || !BaseTabletopPlayer.LocalPlayer.IsReady)
                {
                    yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }

            var nbPlayersWhenCdStarts = BaseTabletopPlayer.HumanPlayersCount;
            m_countdown = countdownSecs;

            MenuStateMachine.ConnectionManager.StartCountdown();

            while (m_countdown > 0f)
            {
                bool allPlayersReady = MenuStateMachine.ConnectionManager.GetPlayerReadyCount() == nbPlayersWhenCdStarts;
                if (BaseTabletopPlayer.HumanPlayersCount != nbPlayersWhenCdStarts || !allPlayersReady)
                {
                    MenuStateMachine.ConnectionManager.StopCountdown();
                    MenuStateMachine.ConnectionManager.SetCountdown(countdownSecs);
                    yield break;
                }

                MenuStateMachine.ConnectionManager.SetCountdown(Mathf.CeilToInt(m_countdown));
                m_countdown -= Time.deltaTime;
                yield return null;
            }

            MenuStateMachine.ConnectionManager.SetCountdown(0);
            BaseTabletopPlayer.LocalPlayer.StartGame();
        }

        virtual protected void OnClickBack()
        {
            Log.Debug("OnClickBack");

            AbortCountdown();

            MenuStateMachine.ConnectionManager.LeaveCurrentRoom();
            // MenuStateMachine.ChangeState will be done in RestartDelayed after OnShutdown callback
        }

        private void AbortCountdown()
        {
            if (m_cdCoroutine != null)
            {
                StopCoroutine(m_cdCoroutine);
                m_cdCoroutine = null;
                MenuStateMachine.ConnectionManager.StopCountdown();
                MenuStateMachine.ConnectionManager.SetCountdown(countdownSecs);
            }

            if (BaseTabletopPlayer.LocalPlayer != null)
            {
                BaseTabletopPlayer.LocalPlayer.Countdown = countdownSecs;
                BaseTabletopPlayer.LocalPlayer.IsReady = false;
            }
        }
    }
}