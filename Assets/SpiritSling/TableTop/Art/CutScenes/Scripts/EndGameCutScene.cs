// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Playables;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class EndGameCutScene : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector playableDirector;

        [SerializeField]
        private PlayableAsset nonLoopingTimeline;

        [SerializeField]
        private PlayableAsset loopingTimeline;

        [SerializeField]
        private Renderer kodamaBody;

        [SerializeField]
        private Renderer kodamaFire;

        [SerializeField]
        private GameObject[] kodamaEyes;

        [SerializeField]
        private ParticleSystem kodamaFireParticles;

        [SerializeField, Tooltip("Kodama body materials for all variants")]
        private Material[] kodamaBodyMaterials;

        [SerializeField, Tooltip("Kodama fire materials for all variants")]
        private Material[] kodamaFireMaterials;

        [SerializeField, Tooltip("Kodama fire particles gradients for all variants")]
        private Gradient[] kodamaFireParticlesGradients;

        [SerializeField]
        private AudioFader kodamaPlayingMusic;

        [SerializeField]
        private AudioFader transitionWinEnd;

        [SerializeField]
        private AudioClip winMusic;

        private void Awake()
        {
            TabletopGameEvents.OnBoardClearedAfterVictory += OnWin;
        }

        private void OnDestroy()
        {
            TabletopGameEvents.OnBoardClearedAfterVictory -= OnWin;
        }

        private void OnWin(int winnerIndex)
        {
            kodamaBody.sharedMaterial = kodamaBodyMaterials[winnerIndex];
            kodamaFire.sharedMaterial = kodamaFireMaterials[winnerIndex];

            var colorOverLifetime = kodamaFireParticles.colorOverLifetime;
            colorOverLifetime.color = kodamaFireParticlesGradients[winnerIndex];

            for (var i = 0; i < kodamaEyes.Length; i++)
            {
                kodamaEyes[i].SetActive(winnerIndex >= i);
            }

            transform.SetParent(GameVolume.Instance.PlayerPivot, false);
            playableDirector.time = 0;
            playableDirector.gameObject.SetActive(true);
            playableDirector.Play(nonLoopingTimeline, DirectorWrapMode.Hold);
            transitionWinEnd.FadeIn();

            TabletopGameEvents.OnConnectionManagerShutdown += OnQuitGame;
        }

        public void PlayKodamaMusic()
        {
            kodamaPlayingMusic.transform.SetParent(kodamaBody.transform, false);
            kodamaPlayingMusic.FadeIn();
            AudioManager.Instance.PlayMusic(winMusic, AudioMixerGroups.Music_Win);
        }

        public void ChangeTimeline()
        {
            playableDirector.time = 0;
            playableDirector.Play(loopingTimeline, DirectorWrapMode.Loop);
        }

        private void OnQuitGame()
        {
            TabletopGameEvents.OnConnectionManagerShutdown -= OnQuitGame;

            kodamaPlayingMusic.transform.SetParent(transform, true);
            kodamaPlayingMusic.FadeOut();
            transitionWinEnd.FadeOut();
            playableDirector.gameObject.SetActive(false);
        }
    }
}
