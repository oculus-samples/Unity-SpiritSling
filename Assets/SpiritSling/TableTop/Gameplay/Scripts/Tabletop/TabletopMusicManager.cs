// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    public sealed class TabletopMusicManager : MonoBehaviour
    {
        [Header("Music")]
        [SerializeField]
        private AudioClip _menuMusic;

        [SerializeField]
        private AudioClip _musicPhase1Music;

        [SerializeField]
        private AudioClip _musicPhase2Music;

        [Header("Transitions SFX")]
        [SerializeField]
        private AudioClip _menuTransition;

        [SerializeField]
        private AudioClip _gamePhaseTransition;

        private void Awake()
        {
            TabletopGameEvents.OnBoardClearedAfterVictory += OnWin;
            TabletopGameEvents.OnGameBoardReady += OnGameStart;
            TabletopGameEvents.GameClocStart += OnGameClocStart;
            TabletopGameEvents.OnConnectionManagerShutdown += OnConnectionManagerShutdown;
        }

        private void OnDestroy()
        {
            TabletopGameEvents.OnBoardClearedAfterVictory -= OnWin;
            TabletopGameEvents.OnGameBoardReady -= OnGameStart;
            TabletopGameEvents.GameClocStart -= OnGameClocStart;
            TabletopGameEvents.OnConnectionManagerShutdown -= OnConnectionManagerShutdown;
        }

        private void Start()
        {
            AudioManager.Instance.PlayMusic(_menuMusic, AudioMixerGroups.Music_Menu, 5);
        }

        private void OnConnectionManagerShutdown()
        {
            // If application isn't quitting
            if (Application.isPlaying)
            {
                AudioManager.Instance.Play(_menuTransition, AudioMixerGroups.UI_Transitions);
                AudioManager.Instance.PlayMusic(_menuMusic, AudioMixerGroups.Music_Menu, 0, 3);
            }
        }

        private void OnGameStart()
        {
            AudioManager.Instance.PlayMusic(_musicPhase1Music, AudioMixerGroups.Music_Gameplay, 3, 2);
        }

        private void OnGameClocStart()
        {
            AudioManager.Instance.Play(_gamePhaseTransition, AudioMixerGroups.UI_Transitions);
            AudioManager.Instance.PlayMusic(_musicPhase2Music, AudioMixerGroups.Music_Gameplay, 3, 2);
        }

        private void OnWin(int _)
        {
            AudioManager.Instance.StopMusic(1);
        }
    }
}