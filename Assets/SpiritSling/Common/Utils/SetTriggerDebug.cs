// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

public class SetTriggerDebug : MonoBehaviour
{
    [SerializeField]
    private List<Animator> _animators;

    [SerializeField]
    private string _triggerName;

    [ContextMenu("Trigger")]
    public void Trigger()
    {
        foreach (var a in _animators)
            a.SetTrigger(_triggerName);
    }
}