// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritSling
{
    public class VFXMiddlePosition : MonoBehaviour
    {
        [SerializeField] private Transform start;
        [SerializeField] private Transform end;

        void Update()
        {
            transform.position = (start.position + end.position) / 2f;
        }
    }
}
