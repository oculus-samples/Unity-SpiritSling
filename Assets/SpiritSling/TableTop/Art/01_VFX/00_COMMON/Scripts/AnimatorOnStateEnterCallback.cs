// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class AnimatorOnStateEnterCallback : StateMachineBehaviour
{
    public delegate void OnStateEnterHandler();

    public event OnStateEnterHandler OnStateEnterEvent;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        OnStateEnterEvent?.Invoke();
    }
}