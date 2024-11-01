// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    [CreateAssetMenu(fileName = "UI Config", menuName = "Cohere/TableTop/UI Config")]
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