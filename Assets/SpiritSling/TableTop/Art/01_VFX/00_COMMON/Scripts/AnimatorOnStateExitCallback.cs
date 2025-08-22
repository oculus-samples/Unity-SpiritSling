// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
public class AnimatorOnStateExitCallback : StateMachineBehaviour
{
    public delegate void OnStateExitHandler();

    public event OnStateExitHandler OnStateExitEvent;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateExitEvent?.Invoke();
    }
}
