// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class ActivateGrabsInView : MonoBehaviour
    {
        private HandGrabInteractable _handGrab;
        private VFXController _vfxController;

        private ConicalFrustum _frustum;

        private void Awake()
        {
            if (OVRManager.OVRManagerinitialized)
            {
                var head = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().centerEyeAnchor;
                _frustum = head.GetComponentInChildren<ConicalFrustum>();
            }

            _handGrab = GetComponentInChildren<HandGrabInteractable>();
            _vfxController = GetComponentInChildren<VFXController>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_frustum == null || _frustum.IsPointInConeFrustum(_handGrab.transform.position))
            {
                if (_handGrab.State == InteractableState.Disabled)
                {
                    _handGrab.Enable();
                    _vfxController.Activate();
                }
            }
            else
            {
                if (_handGrab.State == InteractableState.Normal)
                {
                    _vfxController.Deactivate();
                    _handGrab.Disable();
                }
            }
        }
    }
}
