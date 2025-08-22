// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    [RequireComponent(typeof(AudioSource))]
    public class AudioFader : MonoBehaviour
    {
        [SerializeField]
        private AnimationCurve fadeIn = AnimationCurve.EaseInOut(0, 0, 2, 1);

        [SerializeField]
        private AnimationCurve fadeOut = AnimationCurve.EaseInOut(0, 1, 2, 0);

        private AudioSource audioSource;

        private Coroutine fadeCoroutine;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        public void FadeIn()
        {
            if (!audioSource.isPlaying)
            {
                Fade(fadeIn);
            }
        }

        public void FadeOut()
        {
            if (audioSource.isPlaying)
            {
                Fade(fadeOut);
            }
        }

        private void Fade(AnimationCurve curve)
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
            }

            fadeCoroutine = StartCoroutine(FadeCoroutine(curve));
        }

        private IEnumerator FadeCoroutine(AnimationCurve curve)
        {
            float elapsedTime = 0;
            var duration = curve.keys[^1].time;

            if (curve == fadeIn)
            {
                audioSource.Play();
            }

            while (elapsedTime < duration)
            {
                audioSource.volume = curve.Evaluate(elapsedTime);
                yield return null;

                elapsedTime += Time.deltaTime;
            }

            audioSource.volume = curve.Evaluate(duration);

            if (curve == fadeOut)
            {
                audioSource.Stop();
            }

            fadeCoroutine = null;
        }
    }
}
