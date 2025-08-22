// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using UnityEngine;
using static OVRPlugin;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class NetworkWristBandSpawner : NetworkBehaviour
    {
        public GameObject WristBandPrefab;

        public WristBandHandsSync LeftHandSync;
        public WristBandHandsSync RightHandSync;

        // Start is called before the first frame update
        public override void Spawned()
        {
            if (Object.StateAuthority == BaseTabletopPlayer.LocalPlayer.PlayerRef)
            {
                SpawnWristband();
            }
        }

        private void SpawnWristband()
        {
            if (LeftHandSync != null)
            {
                Runner.SpawnAsync(WristBandPrefab, LeftHandSync.transform.position, LeftHandSync.transform.rotation, Runner.LocalPlayer,
                    (runner, obj) => SetupWristBand(obj, LeftHandSync, Hand.HandLeft));
            }
            if (RightHandSync != null)
            {
                Runner.SpawnAsync(WristBandPrefab, RightHandSync.transform.position, RightHandSync.transform.rotation, Runner.LocalPlayer,
                    (runner, obj) => SetupWristBand(obj, RightHandSync, Hand.HandRight));
            }
        }

        private void SetupWristBand(NetworkObject wristband, WristBandHandsSync handTransform, Hand hand)
        {
            var p = wristband.GetComponent<MagicWristBandController>();
            // p.handSync = handTransform;
            // p.hand = hand;
        }

    }
}
