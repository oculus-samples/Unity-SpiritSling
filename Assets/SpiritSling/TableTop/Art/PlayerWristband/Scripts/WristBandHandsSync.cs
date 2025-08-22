// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction.Input;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class WristBandHandsSync : NetworkBehaviour
    {
        public Handedness handedness;

        public bool IsVisible { get; private set; }

        private bool canSync;

        private Transform syncTarget;

        public override void Spawned()
        {
            base.Spawned();
            var player = BaseTabletopPlayer.GetByPlayerId(Object.StateAuthority.PlayerId) as TabletopHumanPlayer;
            var avatar = player.AvatarEntity;

            if (avatar != null)
            {
                var localAvatarAttachments = avatar.GetComponent<CustomAvatarAttachments>();
                var wristSocket = handedness == Handedness.Left ? localAvatarAttachments.LeftHandWristSocket : localAvatarAttachments.RightHandWristSocket;
                syncTarget = wristSocket.socketObj.transform;
                canSync = true;
                IsVisible = true;
            }
        }

        private void LateUpdate()
        {
            if (canSync)
            {
                transform.SetPositionAndRotation(syncTarget.position, syncTarget.rotation);
            }
        }
    }
}
