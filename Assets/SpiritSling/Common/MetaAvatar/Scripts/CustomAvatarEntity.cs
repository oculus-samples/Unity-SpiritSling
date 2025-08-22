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

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR.BuildingBlocks;
using Oculus.Avatar2;
using UnityEngine;

namespace SpiritSling
{
    public interface IAvatarBehaviour
    {
        // synced to network, can be 0 if user not entitled
        public ulong OculusId { get; }

        // synced to network, indicating which avatar from sample assets is used
        // this should be initialized randomly for each user before entity spawned
        public int LocalAvatarIndex { get; }
        public bool HasInputAuthority { get; }
        public void ReceiveStreamData(byte[] bytes);
        public bool ShouldReduceLOD(int nAvatars);
    }

    /// <summary>
    /// Avatar Entity implementation for Networked Avatar, loads remote/local avatar according to IAvatarBehaviour
    /// and also provide fallback solution to local zip avatar with a randomized preloaded avatar from sample assets
    /// when the user is not entitled (no Oculus Id) or has no avatar setup
    /// </summary>
    public class CustomAvatarEntity : OvrAvatarEntity
    {
        private const int MAX_BYTES_TO_LOG = 5;

        [SerializeField]
        private StreamLOD streamLOD = StreamLOD.Medium;

        [SerializeField,
         Tooltip(
             "Index 0 if there is 1 avatar, index 1 if there are 2 avatars, etc. If there are more avatars than this array length, the last cell value will be used.")]
        private float[] intervalToSendDataInSecPerAvatarCount;

        [SerializeField]
        private bool hideLocalAvatar = true;

        private static int _avatarCount;

        /// <summary>
        /// Used only for remote avatars.
        /// </summary>
        private List<byte[]> _streamedDataArray;

        /// <summary>
        /// Used only for remote avatars.
        /// </summary>
        private List<byte[]> _poolDataArray;

        private float _cycleStartTime;
        private bool _isSkeletonLoaded;
        private bool _initialAvatarLoaded;
        private IAvatarBehaviour _avatarBehaviour;

        private float IntervalToSendDataInSec
        {
            get
            {
                var index = _avatarCount - 1;
                if (index >= intervalToSendDataInSecPerAvatarCount.Length)
                {
                    index = intervalToSendDataInSecPerAvatarCount.Length - 1;
                }

                return intervalToSendDataInSecPerAvatarCount[index];
            }
        }

        /// <summary>
        /// Could be triggered by any changes like oculus id, local avatar index, network connection etc.
        /// </summary>
        public void ReloadAvatarManually()
        {
            if (!_initialAvatarLoaded)
            {
                return;
            }

            _isSkeletonLoaded = false;
            EntityActive = false;
            Teardown();
            CreateEntity();
            LoadAvatar();
        }

        protected override void Awake()
        {
            _avatarBehaviour = this.GetInterfaceComponent<IAvatarBehaviour>();
            if (_avatarBehaviour == null)
            {
                throw new InvalidOperationException("Using AvatarEntity without an IAvatarBehaviour");
            }

            if (!_avatarBehaviour.HasInputAuthority)
            {
                _streamedDataArray = new List<byte[]>(MAX_BYTES_TO_LOG);
                _poolDataArray = new List<byte[]>(MAX_BYTES_TO_LOG);
                for (var i = 0; i < MAX_BYTES_TO_LOG; i++)
                {
                    _poolDataArray.Add(new byte[CustomAvatarBehaviourFusion.AVATAR_DATA_SIZE]);
                }
            }
        }

        private void OnEnable()
        {
            _avatarCount++;
        }

        private void OnDisable()
        {
            _avatarCount--;
        }

        private void Start()
        {
            if (_avatarBehaviour == null)
            {
                return;
            }

            ConfigureAvatar();
            base.Awake(); // creating avatar entity here

            // If it is the local player's avatar
            if (_avatarBehaviour.HasInputAuthority)
            {
                SetActiveView(CAPI.ovrAvatar2EntityViewFlags.FirstPerson);
                if (hideLocalAvatar)
                {
                    Hidden = true;
                }
            }

            if (!_creationInfo.features.HasFlag(CAPI.ovrAvatar2EntityFeatures.UseDefaultModel))
            {
                LoadAvatar();
                _initialAvatarLoaded = true;
            }
        }

        private void ConfigureAvatar()
        {
            if (_avatarBehaviour.HasInputAuthority)
            {
                SetIsLocal(true);
                _creationInfo.features = CAPI.ovrAvatar2EntityFeatures.Preset_Default;
                var entityInputManager = OvrAvatarManager.Instance.gameObject.GetComponent<OvrAvatarInputManager>();
                SetInputManager(entityInputManager);
                var lipSyncInput = FindAnyObjectByType<OvrAvatarLipSyncContext>();
                SetLipSync(lipSyncInput);
                gameObject.name = "LocalAvatar";
            }
            else
            {
                SetIsLocal(false);
                _creationInfo.features = CAPI.ovrAvatar2EntityFeatures.Preset_Remote;
                gameObject.name = "RemoteAvatar";
            }
        }

        protected override void OnDefaultAvatarLoaded()
        {
            base.OnDefaultAvatarLoaded();
            Log.Info("OnDefaultAvatarLoaded");
            LoadAvatar();
            _initialAvatarLoaded = true;
        }

        private void LoadAvatar()
        {
            if (_avatarBehaviour.OculusId == 0)
            {
                LoadLocalAvatar();
            }
            else
            {
                StartCoroutine(TryToLoadUserAvatar());
            }
        }

        private IEnumerator TryToLoadUserAvatar()
        {
            while (!OvrAvatarEntitlement.AccessTokenIsValid())
            {
                yield return null;
            }

            _userId = _avatarBehaviour.OculusId;
            var hasAvatarRequest = OvrAvatarManager.Instance.UserHasAvatarAsync(_userId);
            while (hasAvatarRequest.IsCompleted == false)
            {
                yield return null;
            }

            if (hasAvatarRequest.Result == OvrAvatarManager.HasAvatarRequestResultCode.HasAvatar)
            {
                LoadUser();
            }
            else // fallback to local avatar
            {
                LoadLocalAvatar();
            }
        }

        private void LoadLocalAvatar()
        {
            // we only load local avatar from zip after Avatar Sample Assets is installed
            var assetPath = $"{_avatarBehaviour.LocalAvatarIndex}{GetAssetPostfix()}";
            LoadAssets(new[] { assetPath }, AssetSource.Zip);
        }

        private string GetAssetPostfix(bool isFromZip = true)
        {
            return "_" + OvrAvatarManager.Instance.GetPlatformGLBPostfix(_creationInfo.renderFilters.quality, isFromZip)
                       + OvrAvatarManager.Instance.GetPlatformGLBVersion(_creationInfo.renderFilters.quality, isFromZip)
                       + OvrAvatarManager.Instance.GetPlatformGLBExtension(isFromZip);
        }

        protected override void OnSkeletonLoaded()
        {
            base.OnSkeletonLoaded();
            _isSkeletonLoaded = true;
        }

        private void Update()
        {
            if (!_isSkeletonLoaded || _streamedDataArray.Count <= 0 || IsLocal) return;

            var firstBytesInList = _streamedDataArray[0];
            if (firstBytesInList != null)
            {
                //Apply the remote avatar state and smooth the animation
                ApplyStreamData(firstBytesInList);
                SetPlaybackTimeDelay(IntervalToSendDataInSec / 2);
            }

            _poolDataArray.Add(firstBytesInList);
            _streamedDataArray.RemoveAt(0);
        }

        private void LateUpdate()
        {
            if (!_isSkeletonLoaded)
            {
                return;
            }

            var elapsedTime = Time.time - _cycleStartTime;
            if (elapsedTime > IntervalToSendDataInSec)
            {
                RecordAndSendStreamDataIfHasAuthority();
                _cycleStartTime = Time.time;
            }
        }

        private void RecordAndSendStreamDataIfHasAuthority()
        {
            if (!IsLocal || _avatarBehaviour == null)
            {
                return;
            }

            var bytes = RecordStreamData(_avatarBehaviour.ShouldReduceLOD(_avatarCount) ? StreamLOD.Low : streamLOD);
            _avatarBehaviour.ReceiveStreamData(bytes);
        }

        public void AddToStreamDataList(byte[] tempBytes)
        {
            if (_streamedDataArray.Count == MAX_BYTES_TO_LOG)
            {
                _poolDataArray.Add(_streamedDataArray[^1]);
                _streamedDataArray.RemoveAt(_streamedDataArray.Count - 1);
            }

            var byteArray = _poolDataArray[0];
            _poolDataArray.RemoveAt(0);

            Array.Copy(tempBytes, byteArray, tempBytes.Length);

            _streamedDataArray.Add(byteArray);
        }
    }
}