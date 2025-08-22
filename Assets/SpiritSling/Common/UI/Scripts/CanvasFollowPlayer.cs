// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// Makes a canvas following a player head at a certain distance.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class CanvasFollowPlayer : MonoBehaviour
    {
        [SerializeField]
        private float distance = 2f;
        [SerializeField]
        private float yDistance = -0.2f;
        
        [SerializeField]
        private float lerp = 1f;

        [SerializeField]
        private bool fixedPosition;
        [SerializeField]
        private float distanceThreshold = 0.5f;

        [SerializeField]
        private bool fixedXRotation = true;

        [SerializeField]
        private bool applyAtStart = true;

        [SerializeField]
        private bool applyOnUpdate;

        public bool ApplyOnUpdate { get => applyOnUpdate; set { applyOnUpdate = value; } }
        
        private Vector3 lastFwd;
        private Vector3 lastPos;
        private OVRCameraRig hardwareRig;
        private bool isPlaced;
        private Vector3 lastPlaceRigHeadPos;

        private bool requireLerp;

        void Start()
        {
            lastPos = transform.position;
            if (applyAtStart)
                PlaceCanvas(false);
        }

        void Update()
        {
            if (!isPlaced)
                PlaceCanvas(false);
            else if (applyOnUpdate)
                PlaceCanvas(true);
        }

        /// <summary>
        /// Place the canvas at a specified distance from the player
        /// </summary>
        /// <param name="applyLerp">If true, applies a lerp, and needs to be called on Update()</param>
        private void PlaceCanvas(bool applyLerp)
        {
            var playerFlatForward = GetHead().forward;
            if (fixedXRotation)
                playerFlatForward = playerFlatForward.SetY(0);

            lastFwd = applyLerp ? Vector3.Lerp(lastFwd, playerFlatForward, lerp * Time.deltaTime) : playerFlatForward;
            lastFwd.Normalize();
            
            if (!fixedPosition)
            {
                var wantedPos = GetHead().position + lastFwd * distance + GetHead().up * yDistance;

                var dist = (wantedPos - lastPos).magnitude;
                if (dist < 0.02)
                {
                    requireLerp = false;
                }
                else if (dist > distanceThreshold)
                {
                    requireLerp = true;
                }

                if (requireLerp)
                {
                    transform.rotation = Quaternion.LookRotation(lastFwd);
                    
                    lastPos = applyLerp ? Vector3.Lerp(lastPos, wantedPos, lerp * Time.deltaTime) : wantedPos;
                    transform.position = lastPos;                    
                }
            }

            isPlaced = true;
            lastPlaceRigHeadPos = GetHead().position;
        }

        /// <summary>
        /// Get the current OVR Rig
        /// </summary>
        /// <returns></returns>
        private bool GetRig()
        {
            if (hardwareRig == null || !hardwareRig.gameObject.activeSelf)
                hardwareRig = FindAnyObjectByType<OVRCameraRig>();

            if (hardwareRig == null)
                return false;

            return true;
        }

        /// <summary>
        /// Get the current player's main camera position
        /// </summary>
        /// <returns></returns>
        private Transform GetHead() => Camera.main.transform;
    }
}
