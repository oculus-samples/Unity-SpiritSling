// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Avatar2;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.AvatarIntegration
{
    public class HandTrackingInputTrackingDelegate : IOvrAvatarInputTrackingDelegate
    {
        private IHand _leftHand;
        private IHand _rightHand;
        private IHmd _hmd;

        public HandTrackingInputTrackingDelegate(IHand leftHand, IHand rightHand, IHmd hmd)
        {
            _leftHand = leftHand;
            _rightHand = rightHand;
            _hmd = hmd;
        }

        public bool GetInputTrackingState(
            out OvrAvatarInputTrackingState inputTrackingState)
        {
            inputTrackingState = default;

            bool hasData = false;
            if (_hmd.TryGetRootPose(out Pose headPose))
            {
                inputTrackingState.headsetActive = true;
                inputTrackingState.headset =
                    InteractionAvatarConversions.PoseToAvatarTransform(headPose);
                hasData = true;
            }

            if (_leftHand.GetRootPose(out Pose leftHandRootPose))
            {
                inputTrackingState.leftControllerActive = true;
                inputTrackingState.leftController =
                    InteractionAvatarConversions.PoseToAvatarTransform(leftHandRootPose);
                hasData = true;
            }

            if (_rightHand.GetRootPose(out Pose rightHandRootPose))
            {
                inputTrackingState.rightControllerActive = true;
                inputTrackingState.rightController =
                    InteractionAvatarConversions.PoseToAvatarTransform(rightHandRootPose);
                hasData = true;
            }

            return hasData;
        }
    }
}
