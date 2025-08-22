// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using Oculus.Interaction.Input;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class HandVisualReferences : Singleton<HandVisualReferences>
    {
        [SerializeField] private HandVisual LeftHandVisual;
        [SerializeField] public HandVisual RightHandVisual;
        
        public HandVisual GetHandVisual(Handedness handedness)
        {
            return handedness == Handedness.Left ? LeftHandVisual : RightHandVisual;
        }
    }
}
