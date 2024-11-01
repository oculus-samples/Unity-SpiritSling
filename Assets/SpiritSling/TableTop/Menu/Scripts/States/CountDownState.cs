// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Linq;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class CountDownState : TabletopMenuBaseState
    {
        [SerializeField]
        private StartGameCutScene gameCutScene;

        [SerializeField]
        private AudioClip countDownAudioClip;

        [SerializeField]
        private AnimationClip countDownAnimationClip;

        private bool m_cutsceneStarted;
        private bool m_countdownStarted;

        public override void Enter()
        {
            m_cutsceneStarted = false;
            m_countdownStarted = false;

            BaseTabletopPlayer.LocalPlayer.HasStartCutsceneCompleted = false;

            StartCoroutine(StartCutscene());
            m_uiAnimation.Init(MenuStateMachine.UIConfig);
        }

        public override void Update()
        {
            base.Update();

            if (!m_cutsceneStarted)
            {
                if (gameCutScene.playableDirector.time > 0)
                    m_cutsceneStarted = true;
            }


            if (m_cutsceneStarted && !m_countdownStarted)
            {
                if (gameCutScene.TimeLeft <= 2f) // cutscene ended (arbitrary value because cutscenes ends with idle kodama animations)
                {
                    m_countdownStarted = true;
                    StartCoroutine(StartCountdown());
                }
            }
        }

        private IEnumerator StartCutscene()
        {
            // Wait for all kodamas
            while (BaseTabletopPlayer.TabletopPlayers.Any(p => p.Kodama == null))
            {
                yield return null;
            }

            gameCutScene.Play();
        }

        private IEnumerator StartCountdown()
        {
            // Display the game count down
            FadeIn();

            AudioManager.Instance.Play(countDownAudioClip, AudioMixerGroups.SFX_Countdown);

            // Wait for the countdown to end
            yield return new WaitForSeconds(countDownAnimationClip.length);

            MenuStateMachine.CanvasRoot.SetActive(false);
            gameCutScene.Stop();
            BaseTabletopPlayer.LocalPlayer.HasStartCutsceneCompleted = true;

            // Wait for all players
            var waitForPlayers = true;
            while (waitForPlayers)
            {
                waitForPlayers = BaseTabletopPlayer.TabletopPlayers.Any(t => t.HasStartCutsceneCompleted == false);
                yield return null;
            }

            // First player starts the game at the end of his count down
            if (BaseTabletopPlayer.LocalPlayer == BaseTabletopPlayer.FirstPlayer)
            {
                TabletopGameManager.Instance.RPC_NextTurn((byte)BaseTabletopPlayer.FirstPlayer.Index);
            }

            MenuStateMachine.Clear();
        }
    }
}