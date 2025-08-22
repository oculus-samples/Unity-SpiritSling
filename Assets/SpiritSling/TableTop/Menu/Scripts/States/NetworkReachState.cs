// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class NetworkReachState : TabletopMenuBaseState
    {
        [SerializeField]
        protected CustomButton goBtn;

        private bool _canTransitionToNextState;

        private Coroutine _refreshCoroutine;
        public override void Awake()
        {
            base.Awake();
            goBtn.onClick.AddListener(OnGoClicked);
        }

        public override void Enter()
        {
            base.Enter();

            _canTransitionToNextState = IsNetworkReachable();

            if (!_canTransitionToNextState)
            {
                _refreshCoroutine = StartCoroutine(RefreshButtonState());
            }
        }

        public override void Update()
        {
            if (_canTransitionToNextState)
            {
                ChangeToNextState();
            }

        }
        private IEnumerator RefreshButtonState()
        {
            while (!_canTransitionToNextState)
            {
                goBtn.IsInteractable = IsNetworkReachable();
                yield return new WaitForSeconds(1f);                
            }
        }

        private bool IsNetworkReachable()
        {
            return Application.internetReachability != NetworkReachability.NotReachable;
        }

        private void OnGoClicked()
        {
            _canTransitionToNextState = true;
            StopCoroutine(_refreshCoroutine);
        }
    }
}
