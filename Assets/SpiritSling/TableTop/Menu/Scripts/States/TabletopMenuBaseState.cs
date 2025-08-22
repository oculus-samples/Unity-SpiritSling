// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public abstract class TabletopMenuBaseState : State
    {
        [SerializeField]
        protected TabletopMenuBaseState nextState;

        [Header("Main Panel")]
        [SerializeField]
        protected GameObject mainPanel;

        public TabletopMenuStateMachine MenuStateMachine => (TabletopMenuStateMachine)StateMachine;

        protected UIAnimation m_uiAnimation;

        public override void Awake()
        {
            m_uiAnimation = new UIAnimation();
            m_uiAnimation.TargetGo = mainPanel;
            m_uiAnimation.OnFadeOutComplete.AddListener(OnFadeOutComplete);

            base.Awake();
        }

        public override void Enter()
        {
            m_uiAnimation.Init(MenuStateMachine.UIConfig);
            FadeIn();

            if (!MenuStateMachine.UnifiedGameVolume)
                return;

            AddTransformerListeners(MenuStateMachine.UnifiedGameVolume);
        }

        public override void Update()
        {
        }

        public override void Exit()
        {
            m_uiAnimation.FadeOut();

            // nothing to remove
            if (MenuStateMachine.CurrentState == MenuStateMachine.countDownState)
                return;

            if (!MenuStateMachine.UnifiedGameVolume)
                return;

            RemoveTransformerListeners(MenuStateMachine.UnifiedGameVolume);
        }

        protected void ChangeToNextState(bool delayed = false)
        {
            if (delayed)
            {
                StartCoroutine(DelayedChangedState());
            }
            else
            {
                StateMachine.ChangeState(nextState);
            }
        }

        private IEnumerator DelayedChangedState()
        {
            yield return null;

            StateMachine.ChangeState(nextState);
        }

        protected void FadeIn()
        {
            m_uiAnimation.FadeIn();
        }

        protected void FadeOut()
        {
            m_uiAnimation.FadeOut();
        }

        protected virtual void OnGameVolumeTransformed()
        {
            mainPanel.SetActive(false);
        }

        protected virtual void OnGameVolumeReleased()
        {
            mainPanel.SetActive(true);
        }

        protected void AddTransformerListeners(GameObject gameVolume)
        {
            var transformers = gameVolume.GetComponentsInChildren<GameVolumeTransformer>();
            foreach (var transformer in transformers)
            {
                transformer.updateTransform.AddListener(OnGameVolumeTransformed);
                transformer.endTransform.AddListener(OnGameVolumeReleased);
            }
        }

        protected void RemoveTransformerListeners(GameObject gameVolume)
        {
            var transformers = gameVolume.GetComponentsInChildren<GameVolumeTransformer>();
            foreach (var transformer in transformers)
            {
                transformer.updateTransform.RemoveListener(OnGameVolumeTransformed);
                transformer.endTransform.RemoveListener(OnGameVolumeReleased);
            }
        }

        protected virtual void OnFadeOutComplete() { }
    }
}
