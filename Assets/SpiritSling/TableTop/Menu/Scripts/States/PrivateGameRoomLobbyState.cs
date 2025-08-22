// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Oculus.Platform;
using Oculus.Platform.Models;
using TMPro;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class PrivateGameRoomLobbyState : RoomLobbyState
    {
        [SerializeField]
        protected CustomButton inviteBtn;

        [SerializeField]
        protected CustomButton makePublicBtn;

        [SerializeField]
        protected TMP_Text codeTxt;

        private string m_destinationName = "cohere_lobby_private";

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            inviteBtn.onClick.AddListener(OnClickInvite);
            makePublicBtn.onClick.AddListener(OnClickMakePublic);
        }

        /// <summary>
        /// Called when the state is entered.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            var roomName = MenuStateMachine.ConnectionManager.Runner.SessionInfo.Name;
            
            if (AppEntitlementCheck.IsReady && PlatformWrapper.Instance != null)
            {
                PlatformWrapper.Instance.SetPresence(m_destinationName, roomName, roomName);
            }

            codeTxt.text = roomName.Substring(roomName.Length - 4);
        }

        protected override IEnumerator InitializeReadyState()
        {
            yield return base.InitializeReadyState();
            
            makePublicBtn.IsInteractable = BaseTabletopPlayer.LocalPlayer == BaseTabletopPlayer.FirstPlayer;
        }
        protected override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            base.OnPlayerLeft(runner, player);
            makePublicBtn.IsInteractable = BaseTabletopPlayer.LocalPlayer == BaseTabletopPlayer.FirstPlayer;
        }
        public override void Exit()
        {
            ClearPresenceAndInvitation();
            base.Exit();
        }

        private void ClearPresenceAndInvitation()
        {
            // Close the possibility for new players to join the invite
            if (PlatformWrapper.Instance != null)
                PlatformWrapper.Instance.CloseInvitation();
            
            // Remove our presence from the private game invite
            if (PlatformWrapper.Instance != null)
                PlatformWrapper.Instance.ClearPresence();
        }
        /// <summary>
        /// Handles the invite button click event.
        /// </summary>
        protected void OnClickInvite()
        {
            if (AppEntitlementCheck.IsReady && PlatformWrapper.Instance != null)
            {
                PlatformWrapper.Instance.LaunchInvitePanel(
                    OnPanelResult,
                    OnJoinIntentReceived,
                    OnLeaveIntentReceived);
            }
        }

        protected void OnClickMakePublic()
        {
            //make public
            MenuStateMachine.ConnectionManager.IsPrivateRoom = false;
            
            BaseTabletopPlayer.LocalPlayer.SwitchPublic();
        }

        override protected void OnClickBack()
        {
            ClearPresenceAndInvitation();
            
            base.OnClickBack();
        }

        /// <summary>
        /// Handles the leave intent received event.
        /// </summary>
        /// <param name="message">The leave intent message.</param>
        private void OnLeaveIntentReceived(Message<GroupPresenceLeaveIntent> message)
        {
            Log.Debug("[Platform] OnLeaveIntentReceived");
        }

        /// <summary>
        /// Handles the join intent received event.
        /// </summary>
        /// <param name="message">The join intent message.</param>
        private void OnJoinIntentReceived(Message<GroupPresenceJoinIntent> message)
        {
            Log.Debug("[Platform] OnJoinIntentReceived");
        }

        /// <summary>
        /// Handles the result of the invite panel.
        /// </summary>
        /// <param name="message">The invite panel result message.</param>
        private void OnPanelResult(Message<InvitePanelResultInfo> message)
        {
            Log.Debug("[Platform] Invite panel closed message:" + message);
            if (message.IsError)
            {
                Log.Error("[Platform] Error:" + message.GetError().Message);
            }
            else
            {
                PlatformWrapper.Instance.GetSentInvites();
            }
        }
    }
}
