// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling
{
    public class PoolAudio : PoolObject
    {
        public Transform FollowTarget { get; set; }

        private void LateUpdate()
        {
            if (FollowTarget != null)
            {
                FollowTarget.GetPositionAndRotation(out var position, out var rotation);
                transform.SetPositionAndRotation(position, rotation);
            }
        }
    }
}