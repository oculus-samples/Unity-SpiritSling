// Copyright (c) Meta Platforms, Inc. and affiliates.

using Oculus.Avatar2;
using Oculus.Interaction.Input;
using UnityEngine;

namespace Oculus.Interaction.AvatarIntegration
{
    public class HandTrackingInputManager : OvrAvatarInputManager
    {
        [SerializeField, Interface(typeof(IHmd))]
        private UnityEngine.Object _hmd;
        private IHmd Hmd;

        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _leftHand;
        private IHand LeftHand;

        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _rightHand;
        private IHand RightHand;


        protected virtual void Awake()
        {
            Hmd = _hmd as IHmd;
            LeftHand = _leftHand as IHand;
            RightHand = _rightHand as IHand;
        }

        protected virtual void Start()
        {
            this.AssertField(Hmd, nameof(Hmd));
            this.AssertField(LeftHand, nameof(LeftHand));
            this.AssertField(RightHand, nameof(RightHand));

#if AVATARS_29_7_OR_NEWER
            _inputTrackingWrapper.InputTrackingDelegate =
                new HandTrackingInputTrackingDelegate(LeftHand, RightHand, Hmd);
#if ISDK_OPENXR_HAND
            _handTrackingWrapper.HandTrackingDelegate =
                new OpenXRHandTrackingDelegate(LeftHand, RightHand);
#else
            _handTrackingWrapper.HandTrackingDelegate =
                new HandTrackingDelegate(LeftHand, RightHand);
#endif
#endif
        }

#if AVATARS_29_7_OR_NEWER
        private class InputTrackingProviderWrapper : OvrAvatarInputTrackingProviderBase
        {
            public IOvrAvatarInputTrackingDelegate InputTrackingDelegate { get; set; }
            public override bool GetInputTrackingState(out OvrAvatarInputTrackingState inputTrackingState)
            {
                if (InputTrackingDelegate != null)
                {
                    return InputTrackingDelegate.GetInputTrackingState(out inputTrackingState);
                }
                inputTrackingState = default;
                return false;
            }
        }

        private class HandTrackingProviderWrapper : OvrAvatarHandTrackingPoseProviderBase
        {
            public IOvrAvatarHandTrackingDelegate HandTrackingDelegate { get; set; }
            public override bool GetHandData(OvrAvatarTrackingHandsState handData)
            {
                if (HandTrackingDelegate != null)
                {
                    return HandTrackingDelegate.GetHandData(handData);
                }
                return false;
            }
        }

        private InputTrackingProviderWrapper _inputTrackingWrapper = new();
        private HandTrackingProviderWrapper _handTrackingWrapper = new();

        protected override void OnTrackingInitialized()
        {
            base.OnTrackingInitialized();
            _inputTrackingProvider = _inputTrackingWrapper;
            _handTrackingProvider = _handTrackingWrapper;
        }
#else
        private bool _setupBodyTracking = false;

        private void Update()
        {
            if (!_setupBodyTracking)
            {
                if (BodyTracking == null)
                {
                    return;
                }

                _setupBodyTracking = true;
                BodyTracking.InputTrackingDelegate =
                    new HandTrackingInputTrackingDelegate(LeftHand, RightHand, Hmd);

#if ISDK_OPENXR_HAND
                BodyTracking.HandTrackingDelegate = new OpenXRHandTrackingDelegate(LeftHand, RightHand);
#else
                BodyTracking.HandTrackingDelegate = new HandTrackingDelegate(LeftHand, RightHand);
#endif
            }
        }
#endif

        #region Inject
        public void InjectAllHandTrackingInputManager(Hmd hmd, IHand leftHand, IHand rightHand)
        {
            InjectHmd(hmd);
            InjectLeftHand(leftHand);
            InjectRightHand(rightHand);
        }
        public void InjectHmd(IHmd hmd)
        {
            _hmd = hmd as UnityEngine.Object;
            Hmd = hmd;
        }
        public void InjectLeftHand(IHand leftHand)
        {
            _leftHand = leftHand as UnityEngine.Object;
            LeftHand = leftHand;
        }
        public void InjectRightHand(IHand rightHand)
        {
            _rightHand = rightHand as UnityEngine.Object;
            RightHand = rightHand;
        }
        #endregion
    }
}
