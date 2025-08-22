// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class UIAnimation
    {
        public UnityEvent OnFadeOutComplete;

        public GameObject TargetGo { get; set; }

        public AnimationState State => m_animState;

        public  enum AnimationState { Closed, FadingIn, Opened, FadingOut }

        private AnimationState m_animState;
        private Vector3 m_initScale;
        private UIConfig m_uiConfig;

        public UIAnimation()
        {
            m_animState = AnimationState.Closed;
            OnFadeOutComplete = new UnityEvent();
        }

        public void Init(UIConfig config)
        {
            m_uiConfig = config;

            if (m_initScale == Vector3.zero)
                m_initScale = TargetGo.transform.localScale;

            TargetGo.transform.localScale = Vector3.zero;
        }

        #region Animations

        public async void FadeIn()
        {
            if (m_uiConfig == null)
                return;

            if (m_animState is AnimationState.FadingIn or AnimationState.Opened)
                return;

            Log.Debug("Fading In " + TargetGo.name);

            m_animState = AnimationState.FadingIn;
            TargetGo.SetActive(true);

            var curve = m_uiConfig.fadeInCurve;
            var duration = m_uiConfig.fadeInDuration;
            var delay = m_uiConfig.fadeInDelay;

            await AnimateScale(Vector3.zero, m_initScale, curve, duration, delay);
            OnFadeInComplete();
        }

        public async void FadeOut()
        {
            if (m_animState is AnimationState.FadingOut or AnimationState.Closed)
                return;

            // Log.Debug("Fading Out " + TargetGo.name);

            m_animState = AnimationState.FadingOut;

            var curve = m_uiConfig.fadeOutCurve;
            var duration = m_uiConfig.fadeOutDuration;
            var delay = m_uiConfig.fadeOutDelay;

            await AnimateScale(TargetGo.transform.localScale, TargetGo.transform.localScale, curve, duration, delay);
            // If the object hasn't been destroyed
            if (TargetGo != null)
            {
                OnFadeOutCompleteHandler();
            }
        }

        protected async Task AnimateScale(Vector3 startScale, Vector3 targetScale, AnimationCurve curve, float duration, float delay)
        {
            if (TargetGo == null)
                return;

            TargetGo.transform.localScale = startScale;
            float time = 0;
            await Task.Delay((int)(delay * 1000));
            while (time <= duration)
            {
                // If the object has been destroyed
                if (TargetGo == null)
                {
                    break;
                }
                TargetGo.transform.localScale = curve.Evaluate(time / duration) * targetScale;
                time += Time.deltaTime;
                await Task.Yield();
            }
        }

        protected void OnFadeInComplete()
        {
            m_animState = AnimationState.Opened;
        }

        protected void OnFadeOutCompleteHandler()
        {
            m_animState = AnimationState.Closed;
            TargetGo.SetActive(false);

            OnFadeOutComplete?.Invoke();
        }

        #endregion
    }
}
