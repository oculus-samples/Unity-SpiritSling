// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using Photon.Voice.Unity;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// Class managing the player's avatar object on the network
    /// </summary>
	[MetaCodeSample("SpiritSling")]
    public abstract class NetworkPlayer : NetworkBehaviour
    {
        /// <summary>
        /// Output for voice
        /// </summary>
        [Header("Voice")]
        [SerializeField]
        private Recorder recorder;

        [SerializeField]
        private Speaker speaker;

        /// <summary>
        /// Fusion player object related to this player
        /// </summary>
        public PlayerRef PlayerRef => playerRef;

        /// <summary>
        /// Fusion player ID
        /// </summary>
        public virtual int PlayerId => PlayerRef.PlayerId;

        /// <summary>
        /// A simple local spawn boolean because you cannot access any network property before Spawned()
        /// </summary>
        public bool IsSpawned { get; set; }

        /// <summary>
        /// Name of the player
        /// </summary>
        public virtual string DisplayName => $"Player {PlayerId}";

        /// <summary>
        /// Voice chat enabled (as receiver or sender for local player)
        /// </summary>
        public bool IsVoiceEnabled { get; private set; }

        /// <summary>
        /// Flag that the player is ready to play in the lobby 
        /// </summary>
        [Networked]
        public bool IsReady { get; set; }

        protected PlayerRef playerRef; // cache ref so we can access it on remote despawn

        /// <summary>
        /// On spawn display the avatar ID by it's feet
        /// If it's the local player, then hide the head and hands by default
        /// </summary>
        public override void Spawned()
        {
            base.Spawned();
            playerRef = Object.StateAuthority;

            Runner.MakeDontDestroyOnLoad(gameObject);
            IsSpawned = true;

            SetVoiceEnabled(true);

            // Self: disable speaker
            if (HasStateAuthority)
            {
                if (speaker != null)
                {
                    speaker.gameObject.SetActive(false);
                }
            }
            else if (recorder != null)
            {
                // Others: disable recorder transmitting
                recorder.RecordingEnabled = false;
                recorder.TransmitEnabled = false;
                recorder.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Mute or un-mute a player
        /// </summary>
        /// <param name="yes"></param>
        public void SetVoiceEnabled(bool yes)
        {
            IsVoiceEnabled = yes;

            // Self: stop transmitting
            if (HasStateAuthority)
            {
                if (recorder != null)
                {
                    recorder.RecordingEnabled = yes;
                    recorder.TransmitEnabled = yes;
                }
            }
            else if (speaker != null)
            {
                // Others: stop listening 
                speaker.gameObject.SetActive(yes);
            }
        }

        public void SwitchPublic()
        {
            RPC_SwitchPublic();
        }
        
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SwitchPublic()
        {
            ConnectionManager.Instance.MenuStateMachine.ChangeState(ConnectionManager.Instance.MenuStateMachine.roomLobbyState);
        }
        
        /// <summary>
        /// Force start game, called by the first player in the room
        /// </summary>
        public void StartGame()
        {
            RPC_StartGame(PlayerRef);
        }
        
        /// <summary>
        /// Force starting the game, in private mode
        /// without waiting for everyone to join
        /// </summary>
        /// <param name="player"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_StartGame(PlayerRef player)
        {
            ConnectionManager.Instance.StartGameScene();
            OnGameStart();
        }

        protected virtual void OnGameStart()
        {
            
        }
        
    }
}
