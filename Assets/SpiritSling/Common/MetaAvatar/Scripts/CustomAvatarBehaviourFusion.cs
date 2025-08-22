// Copyright (c) Meta Platforms, Inc. and affiliates.

/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.XR.Samples;
using System.Collections;
using Fusion;
using SpiritSling.TableTop;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    public class CustomAvatarBehaviourFusion : NetworkBehaviour, IAvatarBehaviour
    {
        private const float LERP_TIME = 0.5f;
        public const int AVATAR_DATA_SIZE = 600;

        [Networked, OnChangedRender(nameof(OnAvatarIdChanged))]
        public ulong OculusId { get; set; }

        [Networked, OnChangedRender(nameof(OnAvatarIdChanged))]
        public int LocalAvatarIndex { get; set; }

        public Transform TrackingSpace { get; set; }

        [Networked, Capacity(AVATAR_DATA_SIZE), OnChangedRender(nameof(OnDataReceived))]
        private NetworkArray<byte> AvatarData { get; }

        private byte[] tempAvatarData;

        public CustomAvatarEntity AvatarEntity { get; private set; }

        public override void Spawned()
        {
            AvatarEntity = GetComponent<CustomAvatarEntity>();
            StartCoroutine(SetPlayerAvatar());

            tempAvatarData = new byte[AVATAR_DATA_SIZE];
        }

        private IEnumerator SetPlayerAvatar()
        {
            var player = BaseTabletopPlayer.GetByPlayerId(Object.StateAuthority.PlayerId) as TabletopHumanPlayer;
            while (player == null)
            {
                yield return null;
                player = BaseTabletopPlayer.GetByPlayerId(Object.StateAuthority.PlayerId) as TabletopHumanPlayer;
            }
            
            player.AvatarEntity = AvatarEntity;
            
            if (!HasInputAuthority)
            {
                // If this is a remote player, finds its player and parents the avatar to it
                transform.SetParent(player.transform, false);
            }
        }

        private void OnAvatarIdChanged()
        {
            if (AvatarEntity != null)
            {
                AvatarEntity.ReloadAvatarManually();
            }
        }

        private void OnDataReceived()
        {
            // Apply the data only for remote avatars
            if (!HasInputAuthority)
            {
                AvatarData.CopyTo(tempAvatarData);
                AvatarEntity.AddToStreamDataList(tempAvatarData);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || TrackingSpace == null)
            {
                return;
            }

            Vector3 position = Vector3.Lerp(transform.position, TrackingSpace.position, LERP_TIME);
            Quaternion rotation = Quaternion.Lerp(transform.rotation, TrackingSpace.rotation, LERP_TIME);
            transform.SetPositionAndRotation(position, rotation);
        }

        #region IAvatarBehaviour

        public void ReceiveStreamData(byte[] bytes)
        {
            AvatarData.CopyFrom(bytes, 0, bytes.Length);
        }

        public bool ShouldReduceLOD(int nAvatars) => true; // For Fusion LOD must be low as RPC maximum payload size is 512 bytes

        public bool DynamicLOD { get; set; }

        #endregion
    }
}
