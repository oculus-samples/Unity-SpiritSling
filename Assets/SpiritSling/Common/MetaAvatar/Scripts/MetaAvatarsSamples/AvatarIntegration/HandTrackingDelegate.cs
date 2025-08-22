// Copyright (c) Meta Platforms, Inc. and affiliates.

#if !ISDK_OPENXR_HAND
using Oculus.Avatar2;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.AvatarIntegration
{
    public class HandTrackingDelegate : IOvrAvatarHandTrackingDelegate
    {
        private IHand _leftHand;
        private IHand _rightHand;
        private const int JOINTS_PER_HAND = 17;
        public HandTrackingDelegate(IHand leftHand, IHand rightHand)
        {
            _leftHand = leftHand;
            Assert.IsNotNull(_leftHand);

            _rightHand = rightHand;
            Assert.IsNotNull(_rightHand);
        }

        public bool GetHandData(OvrAvatarTrackingHandsState handData)
        {
            // tracking status flags
            handData.isConfidentLeft = _leftHand.IsHighConfidence;
            handData.isConfidentRight = _rightHand.IsHighConfidence;
            handData.isTrackedLeft = _leftHand.IsTrackedDataValid;
            handData.isTrackedRight = _rightHand.IsTrackedDataValid;
            // wrist positions
            Pose wristPose;
            if (_leftHand.GetRootPose(out wristPose))
            {
                handData.wristPosLeft = InteractionAvatarConversions.PoseToAvatarTransformFlipZ(wristPose);
            }

            if (_rightHand.GetRootPose(out wristPose))
            {
                handData.wristPosRight = InteractionAvatarConversions.PoseToAvatarTransformFlipZ(wristPose);
            }
            // joint rotations
            int sourceOffset = (int)HandJointId.HandThumb0;
            int destOffset = 0;
            CopyJointRotations(_leftHand, sourceOffset, handData.boneRotations, destOffset);

            destOffset = JOINTS_PER_HAND;
            CopyJointRotations(_rightHand, sourceOffset, handData.boneRotations, destOffset);

            handData.handScaleLeft = _leftHand.Scale;
            handData.handScaleRight = _rightHand.Scale;

            return true;
        }

        private void CopyJointRotations(IHand hand, int sourceOffset,
            CAPI.ovrAvatar2Quatf[] destination, int destinationOffset)
        {
            if (!hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                return;
            }
            for (int i = 0; i < JOINTS_PER_HAND; ++i)
            {
                destination[destinationOffset + i] = InteractionAvatarConversions.UnityToAvatarQuaternionFlipX(localJoints[sourceOffset + i].rotation);
            }
        }
    }
}
#endif
