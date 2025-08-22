// Copyright (c) Meta Platforms, Inc. and affiliates.

#if ISDK_OPENXR_HAND
using Oculus.Avatar2;
using Oculus.Interaction.Input;
using UnityEngine;
using UnityEngine.Assertions;
using static Oculus.Interaction.Input.HandMirroring;

namespace Oculus.Interaction.AvatarIntegration
{
    public class OpenXRHandTrackingDelegate : IOvrAvatarHandTrackingDelegate
    {
        private static readonly int[] _sourceJoints = new int[]
        {
             (int) HandJointId.Invalid,
             (int) HandJointId.HandThumb1,
             (int) HandJointId.HandThumb2,
             (int) HandJointId.HandThumb3,
             (int) HandJointId.HandIndex1,
             (int) HandJointId.HandIndex2,
             (int) HandJointId.HandIndex3,
             (int) HandJointId.HandMiddle1,
             (int) HandJointId.HandMiddle2,
             (int) HandJointId.HandMiddle3,
             (int) HandJointId.HandRing1,
             (int) HandJointId.HandRing2,
             (int) HandJointId.HandRing3,
             (int) HandJointId.HandPinky0,
             (int) HandJointId.HandPinky1,
             (int) HandJointId.HandPinky2,
             (int) HandJointId.HandPinky3,
        };

        private static readonly HandsSpace _inputHands = HandTranslationUtils.openXRHands;

        private static readonly HandSpace _avatarHandSpace = new HandSpace(
            Vector3.left, Vector3.up, Vector3.forward);
        private static readonly HandSpace _avatarRootLeft = new HandSpace(
            Vector3.back, Vector3.up, Vector3.right);
        private static readonly HandSpace _avatarRootRight = new HandSpace(
            Vector3.back, Vector3.up, Vector3.left);
        private static Quaternion _leftRootOffset = Quaternion.Euler(0f, -90f, 180f);
        private static Quaternion _rightRootOffset = Quaternion.Euler(0f, -90f, 0f);

        private static readonly HandsSpace _avatarRootHands = new HandsSpace(_avatarRootLeft, _avatarRootRight);

        private static readonly int JOINTS_PER_HAND = _sourceJoints.Length;

        private readonly IHand _leftHand;
        private readonly IHand _rightHand;

        public OpenXRHandTrackingDelegate(IHand leftHand, IHand rightHand)
        {
            _leftHand = leftHand;
            Assert.IsNotNull(_leftHand);

            _rightHand = rightHand;
            Assert.IsNotNull(_rightHand);
        }

        public bool GetHandData(OvrAvatarTrackingHandsState handData)
        {
            bool hasData = false;

            // tracking status flags
            handData.isConfidentLeft = _leftHand.IsHighConfidence;
            handData.isConfidentRight = _rightHand.IsHighConfidence;
            handData.isTrackedLeft = _leftHand.IsTrackedDataValid;
            handData.isTrackedRight = _rightHand.IsTrackedDataValid;

            // wrist positions
            Pose handRoot;
            if (_leftHand.GetRootPose(out handRoot))
            {
                hasData = true;
                handRoot = TransformPose(handRoot,
                    _inputHands[Handedness.Left], _avatarRootHands[Handedness.Left]);
                handRoot.rotation = handRoot.rotation * _leftRootOffset;
                handData.wristPosLeft = new CAPI.ovrAvatar2Transform(handRoot.position, handRoot.rotation);
            }

            if (_rightHand.GetRootPose(out handRoot))
            {
                hasData = true;
                handRoot = TransformPose(handRoot,
                    _inputHands[Handedness.Right], _avatarRootHands[Handedness.Right]);
                handRoot.rotation = handRoot.rotation * _rightRootOffset;
                handData.wristPosRight = new CAPI.ovrAvatar2Transform(handRoot.position, handRoot.rotation);
            }

            // joint rotations
            int destOffset = 0;
            hasData |= CopyJointRotations(_leftHand, handData.boneRotations, destOffset);

            destOffset = JOINTS_PER_HAND;
            hasData |= CopyJointRotations(_rightHand, handData.boneRotations, destOffset);

            handData.handScaleLeft = _leftHand.Scale;
            handData.handScaleRight = _rightHand.Scale;

            return hasData;
        }

        private bool CopyJointRotations(IHand hand,
            CAPI.ovrAvatar2Quatf[] destination, int destinationOffset)
        {
            if (!hand.GetJointPosesLocal(out ReadOnlyHandJointPoses localJoints))
            {
                return false;
            }
            HandSpace inputHand = _inputHands[hand.Handedness];

            for (int i = 0; i < JOINTS_PER_HAND; ++i)
            {
                Quaternion localPose = Quaternion.identity;

                int index = _sourceJoints[i];
                if (index >= 0)
                {
                    localPose = TransformRotation(localJoints[index].rotation,
                   inputHand, _avatarHandSpace);
                }

                destination[destinationOffset + i] = localPose;
            }
            return true;
        }
    }
}
#endif
