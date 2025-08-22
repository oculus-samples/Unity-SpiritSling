// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Avatar2;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    [RequireComponent(typeof(OvrAvatarEntity), typeof(IAvatarBehaviour))]
    public class CustomAvatarAttachments : MonoBehaviour
    {
        public OvrAvatarSocketDefinition LeftHandWristSocket { get; private set; }

        public OvrAvatarSocketDefinition RightHandWristSocket { get; private set; }

        private void Start()
        {
            var avatarEntity = GetComponent<OvrAvatarEntity>();
            LeftHandWristSocket = CreateWristSocket(avatarEntity, "LeftHandWrist", CAPI.ovrAvatar2JointType.LeftHandWrist);
            RightHandWristSocket = CreateWristSocket(avatarEntity, "RightHandWrist", CAPI.ovrAvatar2JointType.RightHandWrist);
        }

        private OvrAvatarSocketDefinition CreateWristSocket(OvrAvatarEntity avatarEntity, string socketName, CAPI.ovrAvatar2JointType jointType)
        {
            return avatarEntity.CreateSocket(
                socketName,
                jointType,
                position: new Vector3(0, 0, 0),
                eulerAngles: new Vector3(0, 0, 0)
            );
        }
    }
}
