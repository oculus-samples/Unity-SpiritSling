// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace SpiritSling
{
    public enum AudioMixerGroups
    {
        Ambiance,
        Ambiance_Mono,
        Ambiance_MonoCrickets,
        Ambiance_StereoPhase1,
        Ambiance_StereoPhase2,
        SFX,
        SFX_Kodama,
        SFX_KodamaWin,
        SFX_Sling,
        SFX_InteractionsKodama,
        SFX_InteractionsSlings,
        SFX_Shoot,
        SFX_Playerboard,
        SFX_PlayerboardFire,
        SFX_Loot,
        SFX_Tiles,
        SFX_TilesDecrease,
        SFX_KodamaVoices,
        SFX_Transitions,
        SFX_Countdown,
        SFX_Phases,
        UI,
        UI_Kodama,
        UI_Gameplay,
        UI_Powers,
        UI_Menu,
        UI_Transitions,
        UI_Rounds,
        Music,
        Music_Win,
        Music_Menu,
        Music_Gameplay,
        Music_PlayerboardAnims,
        SFX_KodamaIntro,
        SFX_KodamaWater,
        SFX_SkipPhaseSpat,
        UI_SkipPhase,
    }

    /// <summary>
    /// The AudioManager class is a singleton component that manages audio playback in a Unity project.
    /// It requires a PoolManager component to be attached to the same GameObject.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : Singleton<AudioManager>
    {
        private const string SFXAudioPoolName = "PooledAudioSource_SFX";
        private PoolManager _poolManager;
        private AudioSource _musicAudioSource;

        [SerializeField]
        private AudioMixer _audioMixer;

        protected override bool IsPersistent => false;

        private void Start()
        {
            _poolManager = PoolManager.Instance;
            _musicAudioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// Plays an audio clip as a sound effect (SFX) following the specified transform.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="group"></param>
        /// <param name="followTarget">The transform for the audio object to follow.</param>
        public AudioSource Play(AudioClip clip, AudioMixerGroups group, Transform followTarget)
        {
            if (clip == null) return null;

            var audioObject = _poolManager.GetPoolObject(SFXAudioPoolName);
            audioObject.GetComponent<PoolAudio>().FollowTarget = followTarget;
            return PlayInternal(clip, audioObject, group);
        }

        /// <summary>
        /// Plays an audio clip as a sound effect (SFX) at the specified position.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="group"></param>
        /// <param name="position">The position at which to play the audio.</param>
        public AudioSource Play(AudioClip clip, AudioMixerGroups group, Vector3 position = new Vector3())
        {
            if (clip == null) return null;

            var audioObject = _poolManager.GetPoolObject(SFXAudioPoolName);
            audioObject.transform.SetPositionAndRotation(position, Quaternion.identity);
            return PlayInternal(clip, audioObject, group);
        }

        /// <summary>
        /// Plays a random audio clip from the specified array as a sound effect (SFX) following the specified transform.
        /// </summary>
        /// <param name="clips">The array of audio clips to choose from.</param>
        /// <param name="group"></param>
        /// <param name="followTarget">The transform for the audio object to follow.</param>
        public void PlayRandom(AudioClip[] clips, AudioMixerGroups group, Transform followTarget)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            var index = Random.Range(0, clips.Length);
            Play(clips[index], group, followTarget);
        }

        /// <summary>
        /// Plays a random audio clip from the specified array as a sound effect (SFX) at the specified position.
        /// </summary>
        /// <param name="clips">The array of audio clips to choose from.</param>
        /// <param name="group"></param>
        /// <param name="position">The position at which to play the audio.</param>
        public void PlayRandom(AudioClip[] clips, AudioMixerGroups group, Vector3 position = new Vector3())
        {
            var index = Random.Range(0, clips.Length);
            Play(clips[index], group, position);
        }

        /// <summary>
        /// Plays an audio clip as music.
        /// </summary>
        public void PlayMusic(AudioClip clip, AudioMixerGroups group, float delay = 0, float fadeOutPrevious = 0, bool loop = true)
        {
            if (clip == null) return;

            StartCoroutine(
                _musicAudioSource.isPlaying ?
                    FadeOutAndPlayMusic(fadeOutPrevious, clip, group, delay, loop) :
                    PlayMusicDeferred(clip, group, delay, loop));
        }

        public void StopMusic(float fadeOut)
        {
            StartCoroutine(FadeOutMusic(fadeOut));
        }

        private IEnumerator PlayMusicDeferred(AudioClip clip, AudioMixerGroups group, float delay, bool loop)
        {
            _musicAudioSource.volume = 1;
            _musicAudioSource.clip = clip;
            _musicAudioSource.outputAudioMixerGroup = GetAudioMixerGroup(group);
            _musicAudioSource.loop = loop;

            yield return new WaitForSeconds(delay);

            _musicAudioSource.Play();
        }

        private IEnumerator FadeOutAndPlayMusic(float fadeOut, AudioClip clip, AudioMixerGroups group, float delay, bool loop)
        {
            yield return StartCoroutine(FadeOutMusic(fadeOut));

            StartCoroutine(PlayMusicDeferred(clip, group, delay, loop));
        }

        private IEnumerator FadeOutMusic(float fadeOut)
        {
            // Needed when we quit the game to prevent a null ref
            if (_musicAudioSource == null)
            {
                yield break;
            }

            var startVolume = _musicAudioSource.volume;

            while (_musicAudioSource.volume > 0)
            {
                _musicAudioSource.volume -= startVolume * Time.deltaTime / fadeOut;
                yield return null;
            }

            _musicAudioSource.Stop();
            _musicAudioSource.volume = 1;
        }

        /// <summary>
        /// Plays the audio clip using the specified audio object.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        /// <param name="audioObject">The GameObject containing the AudioSource component.</param>
        /// <param name="group"></param>
        private AudioSource PlayInternal(AudioClip clip, GameObject audioObject, AudioMixerGroups group)
        {
            var audioSource = audioObject.GetComponent<AudioSource>();
            audioSource.clip = clip;
            audioSource.gameObject.SetActive(true);
            audioSource.spatialize = clip.channels == 1;
            audioSource.outputAudioMixerGroup = GetAudioMixerGroup(group);
            audioSource.Play();
            StartCoroutine(ReturnToPoolAfterPlayback(audioObject, audioSource.clip.length));

            return audioSource;
        }

        /// <summary>
        /// Coroutine that returns the audio object to the pool after the specified delay.
        /// </summary>
        /// <param name="audioObject">The GameObject to return to the pool.</param>
        /// <param name="delay">The delay in seconds before returning the object to the pool.</param>
        /// <returns>An IEnumerator for the coroutine.</returns>
        private IEnumerator ReturnToPoolAfterPlayback(GameObject audioObject, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (audioObject != null)
            {
                var pool = audioObject.GetComponent<PoolObject>();
                pool.SendBackToPool();
            }
        }

        private AudioMixerGroup GetAudioMixerGroup(AudioMixerGroups group)
        {
            var groupName = group.ToString().Replace('_', '/');
            var groups = _audioMixer.FindMatchingGroups($"Master/{groupName}");

            if (groups.Length > 0)
            {
                return groups[0];
            }

            Log.Warning($"AudioMixer group: '{groupName}' was not found.");
            return null;
        }
    }
}
