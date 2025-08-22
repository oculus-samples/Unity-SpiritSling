// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    [CreateAssetMenu(fileName = "UI Config", menuName = "SpiritSling/TableTop/UI Config")]
    public class UIConfig : ScriptableObject
    {
        [Header("UI")]
        public AnimationCurve fadeInCurve;

        public float fadeInDuration;
        public float fadeInDelay;
        public AnimationCurve fadeOutCurve;
        public float fadeOutDuration;
        public float fadeOutDelay;
    }
}
