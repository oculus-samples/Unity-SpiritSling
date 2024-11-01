// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling.TableTop
{
    public class TabletopCreditsState : TabletopMenuBaseState
    {
        [Header("Credits menu")]
        [SerializeField]
        private CustomButton quitBtn;

        [SerializeField]
        private ScrollRect scrollView;

        [SerializeField]
        private float autoScrollSpeed = -10f;

        private float m_startScroll = 0.96f;

        public override void Awake()
        {
            base.Awake();

            quitBtn.onClick.AddListener(OnClickQuit);
        }

        public override void Enter()
        {
            scrollView.verticalNormalizedPosition = m_startScroll;

            base.Enter();
        }

        public override void Exit()
        {
            base.Exit();
        }

        private void OnClickQuit()
        {
            MenuStateMachine.ChangeState(MenuStateMachine._menuState);
        }

        public override void Update()
        {
            if (m_uiAnimation.State != UIAnimation.AnimationState.Opened)
            {
                scrollView.verticalNormalizedPosition = m_startScroll;
            }
            else
            {
                var increasePercent = autoScrollSpeed / scrollView.content.rect.height;
                var pos = scrollView.verticalNormalizedPosition + increasePercent * Time.deltaTime;
                if (pos > 1) pos -= 1;
                if (pos < 0) pos += 1;
                scrollView.verticalNormalizedPosition = pos;
            }
        }
    }
}