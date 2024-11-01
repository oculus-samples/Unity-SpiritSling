// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class PlayerBoardAudio : MonoBehaviour
    {
        [SerializeField, Range(0, 3)]
        private byte playerIndex;

        [SerializeField]
        private Transform fireAudio;

        [SerializeField]
        private Transform stonesAudio;

        [SerializeField]
        private AudioFader loopWhenYourTurn;

        [SerializeField]
        private AudioFader loopWhenNotYourTurn;

        [SerializeField]
        private AudioFader endPhaseWarning;

        [SerializeField]
        private AudioClip startTurn;

        [SerializeField]
        private AudioClip startRound;

        [SerializeField]
        private AudioClip[] startPhase;

        [SerializeField, Min(0)]
        private float timeToWarnBeforeEndPhase = 5;

        private bool isYourTurn;

        private Coroutine endPhaseWarningCoroutine;

        private WaitForSeconds waitForEndPhaseWarning;

        private void Start()
        {
            waitForEndPhaseWarning = new WaitForSeconds(Mathf.Max(0, TabletopGameManager.Instance.Settings.timePhase - timeToWarnBeforeEndPhase));
            TabletopGameEvents.OnNextTurnCalled += OnNextTurn;
            TabletopGameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
        }

        private void OnDestroy()
        {
            TabletopGameEvents.OnNextTurnCalled -= OnNextTurn;
            TabletopGameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
        }

        private void OnNextTurn(byte nextPlayerIndex, bool forceNoNewRound)
        {
            if (playerIndex == nextPlayerIndex)
            {
                isYourTurn = true;

                AudioManager.Instance.Play(startTurn, AudioMixerGroups.SFX_PlayerboardFire, fireAudio);

                if (!BaseTabletopPlayer.LocalPlayer.IsGameOver && BaseTabletopPlayer.LocalPlayer.Index == nextPlayerIndex)
                {
                    AudioManager.Instance.Play(startRound, AudioMixerGroups.UI_Rounds);
                }

                loopWhenNotYourTurn.FadeOut();
                loopWhenYourTurn.FadeIn();
            }
            else if (isYourTurn)
            {
                // If your turn ended
                isYourTurn = false;
                loopWhenYourTurn.FadeOut();
                loopWhenNotYourTurn.FadeIn();
            }
        }

        private void OnGamePhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            if (endPhaseWarningCoroutine != null)
            {
                StopCoroutine(endPhaseWarningCoroutine);
                endPhaseWarningCoroutine = null;
            }
            else
            {
                // Stops the warning sound if it is playing
                endPhaseWarning.FadeOut();
            }

            if (player.Index != playerIndex || (phase != TableTopPhase.Move && phase != TableTopPhase.Summon && phase != TableTopPhase.Shoot))
            {
                return;
            }

            AudioManager.Instance.PlayRandom(startPhase, AudioMixerGroups.SFX_Phases, stonesAudio);
            endPhaseWarningCoroutine = StartCoroutine(EndPhaseWarning());
        }

        private IEnumerator EndPhaseWarning()
        {
            yield return waitForEndPhaseWarning;

            endPhaseWarning.FadeIn();
            endPhaseWarningCoroutine = null;
        }
    }
}