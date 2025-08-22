// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class WaitForPlayersState : TabletopMenuBaseState
    {
        [SerializeField]
        private TMP_Text playerCountProgressTxt;

        [SerializeField]
        public List<PlayerSlot> slots;

        [Serializable]
        public struct PlayerSlot
        {
            public GameObject go;
            public Image img;
            public TMP_Text text;
        }

        [SerializeField]
        public Button readyBtn;

        [SerializeField]
        public Button quitRoomBtn;

        private int cachedPlayerCount;
        private int cachedReadyCount;

        public override void Awake()
        {
            base.Awake();

            quitRoomBtn.onClick.AddListener(OnClickBack);
            readyBtn.onClick.AddListener(OnClickReady);
        }

        public override void Enter()
        {
            base.Enter();

            cachedPlayerCount = 0;
            cachedReadyCount = 0;
            StartCoroutine(InitializeReadyState());
        }

        private IEnumerator InitializeReadyState()
        {
            while (BaseTabletopPlayer.LocalPlayer == null)
            {
                yield return null;
            }

            BaseTabletopPlayer.LocalPlayer.IsReady = false;
            readyBtn.GetComponentInChildren<TMP_Text>().text = BaseTabletopPlayer.LocalPlayer.IsReady ? "Not yet!" : "Ready!";
        }

        public override void Update()
        {
            var runner = MenuStateMachine.ConnectionManager.Runner;
            var playerCount = BaseTabletopPlayer.TabletopPlayers.Count;
            var readyCount = MenuStateMachine.ConnectionManager.GetPlayerReadyCount();
            playerCountProgressTxt.text = $"Players: {runner.SessionInfo.PlayerCount}/{runner.SessionInfo.MaxPlayers}";

            if (playerCount != cachedPlayerCount || readyCount != cachedReadyCount)
                UpdateSlots(playerCount, runner.SessionInfo.MaxPlayers, readyCount);
        }

        private void UpdateSlots(int playerCount, int maxCount, int readyCount)
        {
            Log.Debug("UpdateSlots playerCount:" + playerCount + " maxCount:" + maxCount + " readyCount:" + readyCount);
            cachedPlayerCount = playerCount;
            cachedReadyCount = readyCount;
            var runner = MenuStateMachine.ConnectionManager.Runner;

            // Temp
            for (var i = 0; i < slots.Count; i++)
            {
                slots[i].go.SetActive(i < maxCount);
            }

            for (var i = 0; i < maxCount; i++)
            {
                var player = BaseTabletopPlayer.GetByPlayerIndex((byte)i);
                if (player == null)
                {
                    slots[i].img.color = Color.red;
                    slots[i].text.text = $"Player {(i + 1)}\n" + "Waiting...";
                }
                else if (!player.IsReady)
                {
                    slots[i].img.color = Color.yellow;
                    slots[i].text.text = $"Player {(i + 1)}\n" + "Joined";
                }
                else
                {
                    slots[i].img.color = Color.green;
                    slots[i].text.text = $"Player {(i + 1)}\n" + "Ready!";
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void OnClickReady()
        {
            StartCoroutine(ChangeReadyState());
        }

        private IEnumerator ChangeReadyState()
        {
            while (BaseTabletopPlayer.LocalPlayer == null)
            {
                yield return null;
            }

            BaseTabletopPlayer.LocalPlayer.IsReady = !BaseTabletopPlayer.LocalPlayer.IsReady;

            readyBtn.GetComponentInChildren<TMP_Text>().text = BaseTabletopPlayer.LocalPlayer.IsReady ? "Not yet!" : "Ready!";
        }

        private void OnClickBack()
        {
            MenuStateMachine.ConnectionManager.LeaveCurrentRoom();
            MenuStateMachine.ChangeState(MenuStateMachine.mainMenuState);
        }
    }
}
