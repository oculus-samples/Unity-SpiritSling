// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;
using Hand = OVRPlugin.Hand;

namespace SpiritSling.TableTop
{
    public class HandPositionShaderGlobalPropertySetter : MonoBehaviour
    {
        private static readonly int s_leftHandPos = Shader.PropertyToID("_LeftHandPos");
        private static readonly int s_rightHandPos = Shader.PropertyToID("_RightHandPos");

        public Hand handType = Hand.HandLeft;

        private int selectedHandProperty;

        private void Start()
        {
            selectedHandProperty = handType switch
            {
                Hand.HandLeft => s_leftHandPos,
                Hand.HandRight => s_rightHandPos,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        private void Update()
        {
            Shader.SetGlobalVector(selectedHandProperty, transform.position);
        }
    }
}