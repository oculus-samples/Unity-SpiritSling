// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class AnimatorOnStateExitCallback : StateMachineBehaviour
{
    public delegate void OnStateExitHandler();

    public event OnStateExitHandler OnStateExitEvent;

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateExitEvent?.Invoke();
    }
}