// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Last state before returning to menu
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class ExitGameState : TabletopGameState
    {
        [SerializeField]
        private StateMachine _menuStateMachine;

        public bool BackToLobby { get; set; } = false;

        public override void Enter()
        {
            base.Enter();

            // Retrieves the player before disabling the state machine (it sets the player to null)
            var isHumanPlayer = TabletopGameStateMachine.Player != null && TabletopGameStateMachine.Player.IsHuman;
            StateMachine.gameObject.SetActive(false);

            if (isHumanPlayer)
            {
                BaseTabletopPlayer.TabletopPlayers.Clear();

                _menuStateMachine.gameObject.SetActive(true);

                //shutdown event will proceed with restarting menustatemachine
                TabletopGameManager.Instance.Runner.Shutdown(false);
            }
        }
    }
}
