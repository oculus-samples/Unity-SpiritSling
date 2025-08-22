// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public static class VFXAnimationUtility
    {
        public static IEnumerator AnimateFlashFloatProperty(Action<float> setter)
        {
            return AnimateFloatProperty(setter, 0f, 1f, 1, FlashCurve);
        }

        private static float FlashCurve(float x)
        {
            var a = 1 - 2 * x;
            return 1 - a * a;
        }

        public static IEnumerator AnimateFloatProperty(Action<float> setter, float startValue, float endValue, float duration = 1,
            Func<float, float> mappingFunction = null, Action onComplete = null)
        {
            var currentTime = 0f;
            var linearValue = startValue;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                linearValue = math.lerp(startValue, endValue, math.saturate(currentTime / duration));
                setter.Invoke(mappingFunction?.Invoke(linearValue) ?? linearValue);

                yield return null;
            }

            setter.Invoke(mappingFunction?.Invoke(linearValue) ?? linearValue);
            onComplete?.Invoke();
        }
    }
}
