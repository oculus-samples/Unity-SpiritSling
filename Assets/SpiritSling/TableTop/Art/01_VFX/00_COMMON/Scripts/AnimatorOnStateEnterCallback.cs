// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
public class AnimatorOnStateEnterCallback : StateMachineBehaviour
{
    public delegate void OnStateEnterHandler();

    public event OnStateEnterHandler OnStateEnterEvent;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateEnterEvent?.Invoke();
    }
}
