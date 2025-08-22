// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public sealed class TabletopAmbianceManager : MonoBehaviour
    {
        [SerializeField]
        private AudioFader stereoGamePhase1;

        [SerializeField]
        private AudioFader stereoGamePhase2;

        [SerializeField]
        private Transform monoAmbiancesContainer;

        [SerializeField]
        private AudioClip _beginGameTransition;

        private AudioFader currentStereoAmbiance;

        private AudioFader[] monoAmbiances;

        private void Awake()
        {
            monoAmbiances = monoAmbiancesContainer.GetComponentsInChildren<AudioFader>();

            TabletopGameEvents.GameStart += OnGameStart;
            TabletopGameEvents.OnGameBoardReady += OnGameBoardReady;
            TabletopGameEvents.GameClocStart += OnGameClocStart;
            TabletopGameEvents.OnGameOver += OnGameOver;
            TabletopGameEvents.OnRequestQuitGame += OnRequestQuitGame;
            TabletopGameEvents.OnRequestLeaveToLobby += OnRequestQuitGame;
        }

        private IEnumerator Start()
        {
            // delayed start
            yield return new WaitForSeconds(1f);

            foreach (var monoAmbiance in monoAmbiances)
            {
                monoAmbiance.FadeIn();
                for (var i = 0; i < 5; ++i)
                    yield return null;
            }
        }

        private void OnDestroy()
        {
            TabletopGameEvents.GameStart -= OnGameStart;
            TabletopGameEvents.OnGameBoardReady -= OnGameBoardReady;
            TabletopGameEvents.GameClocStart -= OnGameClocStart;
            TabletopGameEvents.OnGameOver -= OnGameOver;
            TabletopGameEvents.OnRequestQuitGame -= OnRequestQuitGame;
            TabletopGameEvents.OnRequestLeaveToLobby -= OnRequestQuitGame;
        }

        private void OnGameStart()
        {
            AudioManager.Instance.Play(_beginGameTransition, AudioMixerGroups.SFX_Transitions, transform);
        }

        private void OnGameBoardReady()
        {
            stereoGamePhase1.FadeIn();
            currentStereoAmbiance = stereoGamePhase1;
        }

        private void OnGameClocStart()
        {
            stereoGamePhase1.FadeOut();
            stereoGamePhase2.FadeIn();
            currentStereoAmbiance = stereoGamePhase2;
        }

        private void OnGameOver(BaseTabletopPlayer player)
        {
            if (player != null && !player.IsHuman)
            {
                return;
            }

            if (currentStereoAmbiance == stereoGamePhase2)
            {
                stereoGamePhase2.FadeOut();
            }

            stereoGamePhase1.FadeIn();
            currentStereoAmbiance = stereoGamePhase1;
        }

        private void OnRequestQuitGame()
        {
            if (currentStereoAmbiance != null)
            {
                currentStereoAmbiance.FadeOut();
                currentStereoAmbiance = null;
            }
        }
    }
}
