// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class NetworkErrorState : TabletopMenuBaseState
    {
        [SerializeField]
        public CustomButton okBtn;

        [SerializeField]
        public TMP_Text messageTxt;

        public override void Awake()
        {
            base.Awake();

            okBtn.onClick.AddListener(OnClickOk);
        }

        private void OnClickOk()
        {
            MenuStateMachine.ChangeState(MenuStateMachine.mainMenuState);
        }

        public override void Enter()
        {
            base.Enter();
            messageTxt.text = MenuStateMachine.ConnectionManager.LastError;
            TabletopGameEvents.OnFirstMenuEnter?.Invoke();
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}