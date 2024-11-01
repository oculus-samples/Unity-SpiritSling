// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class HandHudController : MonoBehaviour
    {
        private List<MeshRenderer> renderers;

        [SerializeField]
        private ParticleSystem changePhaseVFXPrimary;

        [SerializeField]
        private ParticleSystem changePhaseVFXSecondary;

        private static readonly int s_phaseTimeProperty = Shader.PropertyToID("_PhaseTime");
        private static readonly int s_phaseIndexProperty = Shader.PropertyToID("_PhaseIndex");
        private static readonly int s_playerTurnActivation = Shader.PropertyToID("_PlayerTurnActivation");
        private static readonly int s_Activation = Shader.PropertyToID("_Activation");
        private static readonly int s_playerColor = Shader.PropertyToID("_PlayerColor");

        private bool _isDisplayed;

        private void SetHudColorCallback()
        {
            var playerColor = TabletopConfig.Get().PlayerColor(BaseTabletopPlayer.LocalPlayer.Index);

            // var playerColor = TabletopConfig.Get().playersColors[2];
            SetHudColor(playerColor);

            var main = changePhaseVFXPrimary.main;
            main.startColor = playerColor;

            var main2 = changePhaseVFXSecondary.main;
            main2.startColor = playerColor;
        }

        private void OnDestroy()
        {
            UnRegisterCallbacks();
        }

        private void Start()
        {
            renderers = GetComponentsInChildren<MeshRenderer>().ToList();
            renderers.Add(GetComponent<MeshRenderer>());

            SetPlayerTurnActivationProperty(0);
            SetActivationProperty(0);
            SetPhaseTimeProperty(0);
            RegisterCallbacks();
        }

        private void OnDisable()
        {
            UnRegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            TabletopGameEvents.OnGameBoardReady += SetHudColorCallback;
            TabletopGameEvents.OnGamePhaseChanged += OnPhaseChanged;
            TabletopGameEvents.OnRequestQuitGame += DisableHud;
            TabletopGameEvents.OnShootPhaseEnd += OnShootPhaseEnd;
            TabletopGameEvents.GameStart += OnGameStart;
            TabletopGameEvents.OnFirstMenuEnter += DisableHud;
        }

        private void UnRegisterCallbacks()
        {
            TabletopGameEvents.OnGameBoardReady -= SetHudColorCallback;
            TabletopGameEvents.OnGamePhaseChanged -= OnPhaseChanged;
            TabletopGameEvents.OnRequestQuitGame -= DisableHud;
            TabletopGameEvents.OnShootPhaseEnd -= OnShootPhaseEnd;
            TabletopGameEvents.GameStart -= OnGameStart;
            TabletopGameEvents.OnFirstMenuEnter -= DisableHud;
        }

        private void OnGameStart()
        {
            SetPlayerTurnActivationProperty(0);
            SetPhaseTimeProperty(0);
            SetPhaseIndexProperty(-1);
            AnimateActivationProperty(0, 1, 1);
            _isDisplayed = true;
        }

        private void DisableHud()
        {
            if (!_isDisplayed)
                return;

            PlayPrimaryVFX();
            PlaySecondaryVFX();
            AnimateActivationProperty(1, 0, 1);
            _isDisplayed = false;
        }

        private void OnShootPhaseEnd()
        {
            StopIfNotNull(m_timeCoroutine);
            ChangePhase(2, 3, () => AnimatePlayerTurnActivation(1, 0, 1));
        }

        private void OnPhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            if (player.Index != BaseTabletopPlayer.LocalPlayer.Index)
            {
                return;
            }

            switch (phase)
            {
                case TableTopPhase.Setup:
                    {
                        SetPhaseTimeProperty(0);
                        SetPhaseIndexProperty(-1);
                        AnimatePlayerTurnActivation(0, 1, 1);
                        break;
                    }
                case TableTopPhase.Move:
                    AnimatePhaseTime();
                    ChangePhase(-1, 0, PlaySecondaryVFX);
                    break;
                case TableTopPhase.Summon:
                    AnimatePhaseTime();
                    ChangePhase(0, 1, PlaySecondaryVFX);
                    break;
                case TableTopPhase.Shoot:
                    AnimatePhaseTime();
                    ChangePhase(1, 2, PlaySecondaryVFX);
                    break;
            }
        }

        private void PlayPrimaryVFX() => changePhaseVFXPrimary.Play();
        private void PlaySecondaryVFX() => changePhaseVFXSecondary.Play();

        private void SetFloatProperty(int id, float value) => renderers.ForEach(x => x.material.SetFloat(id, value));
        private void SetColorProperty(int id, Color value) => renderers.ForEach(x => x.material.SetColor(id, value));

        private void SetPhaseTimeProperty(float value) => SetFloatProperty(s_phaseTimeProperty, value);
        private void SetPhaseIndexProperty(float value) => SetFloatProperty(s_phaseIndexProperty, value);
        private void SetPlayerTurnActivationProperty(float value) => SetFloatProperty(s_playerTurnActivation, value);
        private void SetActivationProperty(float value) => SetFloatProperty(s_Activation, value);
        private void SetHudColor(Color value) => SetColorProperty(s_playerColor, value);

        private void ChangePhase(int oldIndex, int newIndex, Action onComplete = null)
        {
            AnimateChangeRuneIndex(oldIndex, newIndex, onComplete);
        }

        private Coroutine m_runeIndexCoroutine;

        private void AnimateChangeRuneIndex(int oldIndex, int newIndex, Action onComplete = null)
        {
            PlayPrimaryVFX();
            StopIfNotNull(m_runeIndexCoroutine);
            m_runeIndexCoroutine = AnimateProperty(SetPhaseIndexProperty, oldIndex, newIndex, 1, null, onComplete);
        }

        private Coroutine m_timeCoroutine;

        private void AnimatePhaseTime(Action onComplete = null)
        {
            StopIfNotNull(m_timeCoroutine);
            m_timeCoroutine = AnimateProperty(SetPhaseTimeProperty, 0, 1, TabletopGameManager.Instance.Settings.timePhase, null, onComplete);
        }

        private Coroutine m_playerTurnActivationCoroutine;

        private void AnimatePlayerTurnActivation(float startValue, float endValue, float duration, Action onComplete = null)
        {
            StopIfNotNull(m_playerTurnActivationCoroutine);
            m_playerTurnActivationCoroutine = AnimateProperty(SetPlayerTurnActivationProperty, startValue, endValue, duration, null, onComplete);
        }

        private Coroutine m_activationCoroutine;

        private void AnimateActivationProperty(float startValue, float endValue, float duration, Action onComplete = null)
        {
            StopIfNotNull(m_activationCoroutine);
            m_activationCoroutine = AnimateProperty(SetActivationProperty, startValue, endValue, duration, math.sqrt, onComplete);
        }

        private Coroutine AnimateProperty(Action<float> setter, float initialValue, float targetValue, float duration = 1,
            Func<float, float> mappingFunction = null, Action onComplete = null)
        {
            return StartCoroutine(AnimatePropertyNormalized(setter, initialValue, targetValue, duration, mappingFunction, onComplete));
        }

        private IEnumerator AnimatePropertyNormalized(Action<float> setter, float initialValue, float targetValue, float duration,
            Func<float, float> mappingFunction = null, Action onComplete = null)
        {
            var currentTime = 0f;
            while (currentTime < duration)
            {
                currentTime += Time.deltaTime;
                var linearValue = math.lerp(initialValue, targetValue, currentTime / duration);
                setter?.Invoke(mappingFunction?.Invoke(linearValue) ?? linearValue);
                yield return null;
            }

            setter?.Invoke(targetValue);
            onComplete?.Invoke();
        }

        private void StopIfNotNull(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }
}