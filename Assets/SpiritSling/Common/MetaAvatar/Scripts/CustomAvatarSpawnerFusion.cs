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

using Fusion;
using Meta.XR.MultiplayerBlocks.Shared;
using Oculus.Avatar2;
using UnityEngine;

namespace SpiritSling
{
    public class CustomAvatarSpawnerFusion : MonoBehaviour
    {
#pragma warning disable CS0414 // If Avatar SDK not installed these fields are not used, disable warning but retain serialization
        [Tooltip("Control when you want to load the avatar.")]
        [SerializeField]
        private bool loadAvatarWhenConnected = true;

        [SerializeField]
        private GameObject avatarBehavior;

        [Tooltip(
            "If you're using Avatar Sample Assets as fallback avatars, and has manually adapted the preset zip file " +
            "for optimizing app size. Change this to the size of available avatars count in the preset zip file.")]

        // developer might want to delete some avatars from the sample asset zip
        // e.g. the game has a maximum player count, they won't need more unique sample avatars
        [SerializeField]
        private int preloadedSampleAvatarSize = 32;

        [Tooltip("Reduce quality automatically to improve performance when many avatars are spawned.")]
        [SerializeField]
        private bool dynamicLOD = true;

        [SerializeField] private Transform trackingSpace;

        private NetworkRunner _networkRunner;
        private bool _entitlementCompleted;
        private PlatformInfo _platformInfo;
#pragma warning restore CS0414

        private void Awake()
        {
            PlatformInit.GetEntitlementInformation(OnEntitlementFinished);
        }

        private void OnEntitlementFinished(PlatformInfo info)
        {
            _platformInfo = info;
            Log.Debug(
                $"Entitlement callback:isEntitled: {info.IsEntitled} oculusName: {info.OculusUser?.OculusID} oculusID: {info.OculusUser?.ID}");

            if (info.IsEntitled)
            {
                OvrAvatarEntitlement.SetAccessToken(info.Token);
            }

            _entitlementCompleted = true;

            if (loadAvatarWhenConnected)
            {
                SpawnAvatar();
            }
        }

        public void SpawnAvatar()
        {
            if (_networkRunner == null)
            {
                _networkRunner = FindAnyObjectByType<NetworkRunner>();
            }

            // Spawn Avatar
            _ = _networkRunner.Spawn(
                avatarBehavior,
                Vector3.zero,
                Quaternion.identity,
                _networkRunner.LocalPlayer,
                (runner, obj) => // onBeforeSpawned
                {
                    var avatarBehaviourFusion = obj.GetComponent<CustomAvatarBehaviourFusion>();
                    avatarBehaviourFusion.LocalAvatarIndex = (avatarBehaviourFusion.Object.StateAuthority.PlayerId - 1) % preloadedSampleAvatarSize;
                    if (_platformInfo.IsEntitled)
                    {
                        avatarBehaviourFusion.OculusId = _platformInfo.OculusUser?.ID ?? 0;
                    }

                    avatarBehaviourFusion.DynamicLOD = dynamicLOD;
                    avatarBehaviourFusion.TrackingSpace = trackingSpace;
                }
            );
        }
    }
}