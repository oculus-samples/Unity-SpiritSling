// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class HandTrackingState : TabletopMenuBaseState
    {
        [SerializeField]
        protected CustomButton goBtn;

        private bool _handTrackingEnabled;

        public override void Awake()
        {
            base.Awake();
            goBtn.onClick.AddListener(OnGoClicked);
        }

        public override void Enter()
        {
            base.Enter();
        }

        private IEnumerator RefreshButtonState()
        {
            yield return new WaitForSeconds(1f);

            _handTrackingEnabled = OVRPlugin.GetHandTrackingEnabled();
            goBtn.enabled = _handTrackingEnabled;
        }

        private void OnGoClicked()
        {
            ChangeToNextState();
        }
    }
}