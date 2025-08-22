// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class TabletopMenuState : TabletopMenuBaseState
    {
        [Header("Menu")]
        [SerializeField]
        private CustomButton playButton;

        [SerializeField]
        private CustomButton quitBtn;

        [SerializeField]
        private CustomButton creditsBtn;

        [SerializeField]
        private TMP_Text versionTxt;

        public override void Awake()
        {
            base.Awake();

            playButton.onClick.AddListener(OnClickPlay);
            quitBtn.onClick.AddListener(OnClickQuit);
            creditsBtn.onClick.AddListener(OnClickCredits);

            versionTxt.text = Application.version;
        }

        public override void Enter()
        {
            base.Enter();
            TabletopGameEvents.OnFirstMenuEnter?.Invoke();
        }

        public override void Exit()
        {
            base.Exit();
        }

        /// <summary>
        /// Displays the room creation panel
        /// </summary>
        private void OnClickPlay()
        {
            MenuStateMachine.ChangeState(MenuStateMachine.mainMenuState);
        }

        private void OnClickCredits()
        {
            MenuStateMachine.ChangeState(MenuStateMachine.creditsState);
        }

        private void OnClickQuit()
        {
            Application.Quit();
        }
    }
}
