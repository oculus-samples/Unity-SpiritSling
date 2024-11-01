// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// Simple tweens function without external dependencies
    /// </summary>
    public static class Tweens
    {
        public static AnimationCurve EaseInOut => AnimationCurve.EaseInOut(0, 0, 1, 1);

        public static AnimationCurve EaseOut =>
            new(
                new Keyframe(0, 0, 0, 2),
                new Keyframe(1, 1, 0, 0));

        public static AnimationCurve BackOut =>
            new(
                new Keyframe(0, 0, 0, 4),
                new Keyframe(1, 1, 0, 0));

        public static AnimationCurve BounceOut =>
            new(
                new Keyframe(0, 0, 0, 0),
                new Keyframe(0.4f, 1, 5, -4),
                new Keyframe(0.7f, 1, 4, -3),
                new Keyframe(0.9f, 1, 3, -2),
                new Keyframe(1, 1, 2, 0));

        /// <summary>
        /// Float interpolation
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="step"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public static IEnumerator Lerp(float from, float to, float duration, AnimationCurve ease, Action<float> step)
        {
            for (var time = Time.deltaTime; time < duration; time += Time.deltaTime)
            {
                var t = ease.Evaluate(time / duration);
                step?.Invoke(Mathf.Lerp(from, to, t));

                yield return new WaitForEndOfFrame();
            }

            step?.Invoke(to);
        }

        /// <summary>
        /// Vector3 interpolation
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="step"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public static IEnumerator Lerp(Vector3 from, Vector3 to, float duration, AnimationCurve ease, Action<Vector3> step)
        {
            for (var time = Time.deltaTime; time < duration; time += Time.deltaTime)
            {
                var t = ease.Evaluate(time / duration);
                step?.Invoke(Vector3.Lerp(from, to, t));

                yield return new WaitForEndOfFrame();
            }

            step?.Invoke(to);
        }

        /// <summary>
        /// Color interpolation
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="duration"></param>
        /// <param name="step"></param>
        /// <param name="ease"></param>
        /// <returns></returns>
        public static IEnumerator Lerp(Color from, Color to, float duration, AnimationCurve ease, Action<Color> step)
        {
            for (var time = Time.deltaTime; time < duration; time += Time.deltaTime)
            {
                var t = ease.Evaluate(time / duration);
                step?.Invoke(Color.Lerp(from, to, t));

                yield return new WaitForEndOfFrame();
            }

            step?.Invoke(to);
        }
    }
}