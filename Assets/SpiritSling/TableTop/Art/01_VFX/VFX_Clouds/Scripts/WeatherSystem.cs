// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class WeatherSystem : MonoBehaviour
    {
        [Range(-0.20f, 1.0f)]
        [SerializeField]
        float _cloudsDensity;

        [SerializeField]
        ParticleSystem _lowCloudsPS;

        [SerializeField]
        AnimationCurve _lowCloudsPSEmitCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.165f, 1f), new Keyframe(.33f, 0f));

        ParticleSystem.EmissionModule _lowCloudsPSEmissionModule;

        [SerializeField]
        ParticleSystem _mediumCloudsPS;

        [SerializeField]
        AnimationCurve _mediumCloudsPSEmitCurve = new AnimationCurve(new Keyframe(0.165f, 0f), new Keyframe(0.495f, 1f), new Keyframe(.66f, 0f));

        ParticleSystem.EmissionModule _mediumCloudsPSEmissionModule;

        [SerializeField]
        ParticleSystem _stormCloudsPS;

        [SerializeField]
        AnimationCurve _stormCloudsPSEmitCurve = new AnimationCurve(new Keyframe(0.495f, 0f), new Keyframe(1f, 1f));

        ParticleSystem.EmissionModule _stormCloudsPSEmissionModule;

        [SerializeField]
        ParticleSystem _rainPS;

        [SerializeField]
        AnimationCurve _rainPSEmitCurve = new AnimationCurve(new Keyframe(0.495f, 0f), new Keyframe(1f, 1f));

        ParticleSystem.EmissionModule _rainPSEmissionModule;

        public float _nextTurnIncrement = 0.1f;

        float _lastCloudsDentisty = float.MaxValue;

        private void Start()
        {
            _lowCloudsPSEmissionModule = _lowCloudsPS.emission;
            _mediumCloudsPSEmissionModule = _mediumCloudsPS.emission;
            _stormCloudsPSEmissionModule = _stormCloudsPS.emission;
            _rainPSEmissionModule = _rainPS.emission;

            _lowCloudsPSEmissionModule.rateOverTimeMultiplier = 0;
            _mediumCloudsPSEmissionModule.rateOverTimeMultiplier = 0;
            _stormCloudsPSEmissionModule.rateOverTimeMultiplier = 0;
            _rainPSEmissionModule.rateOverTimeMultiplier = 0;
        }

        private void OnEnable()
        {
            TabletopGameEvents.GameStart += ResetMeteo;
            TabletopGameEvents.OnNextTurnCalled += IncreaseMeteo;
            TabletopGameEvents.GameClocStart += ClockStart;
            TabletopGameEvents.OnGameOver += ResetMeteo;
            TabletopGameEvents.OnMainMenuEnter += ResetMeteo;
            TabletopGameEvents.OnFirstMenuEnter += ResetMeteo;
        }

        private void OnDisable()
        {
            TabletopGameEvents.GameStart -= ResetMeteo;
            TabletopGameEvents.OnNextTurnCalled -= IncreaseMeteo;
            TabletopGameEvents.GameClocStart -= ClockStart;
            TabletopGameEvents.OnGameOver -= ResetMeteo;
            TabletopGameEvents.OnMainMenuEnter -= ResetMeteo;
            TabletopGameEvents.OnFirstMenuEnter -= ResetMeteo;
        }

        void ResetMeteo(BaseTabletopPlayer player)
        {
            if (player == null || player.IsHuman)
            {
                ResetMeteo();
            }
        }

        [ContextMenu("ResetMeteo")]
        void ResetMeteo()
        {
            _cloudsDensity = -_nextTurnIncrement;

            SetDensity(_cloudsDensity, 0);
        }

        [ContextMenu("IncreaseMeteo")]
        void IncreaseMeteo() //Debug only
        {
            SetDensity(_cloudsDensity + _nextTurnIncrement);
        }

        void IncreaseMeteo(byte nextPlayerIndex, bool forceNoNewRound)
        {
            var isNewRound = !forceNoNewRound && BaseTabletopPlayer.TabletopPlayers.Count > 0 && nextPlayerIndex == BaseTabletopPlayer.FirstPlayer.Index;

            if (isNewRound)
            {
                SetDensity(_cloudsDensity + _nextTurnIncrement);
            }
        }

        [ContextMenu("ClockStart")]
        void ClockStart()
        {
            SetDensity(1.0f);
        }

        void Update()
        {
            if (!Mathf.Approximately(_lastCloudsDentisty, _cloudsDensity))
            {
                _lastCloudsDentisty = _cloudsDensity;

                //Small clouds
                _lowCloudsPSEmissionModule.rateOverTimeMultiplier = _lowCloudsPSEmitCurve.Evaluate(_cloudsDensity);

                //Medium clouds 
                _mediumCloudsPSEmissionModule.rateOverTimeMultiplier = _mediumCloudsPSEmitCurve.Evaluate(_cloudsDensity);

                //Noah's arch
                _stormCloudsPSEmissionModule.rateOverTimeMultiplier = _stormCloudsPSEmitCurve.Evaluate(_cloudsDensity);

                //rain
                _rainPSEmissionModule.rateOverTimeMultiplier = _rainPSEmitCurve.Evaluate(_cloudsDensity);
            }
        }

        Coroutine currentRoutine;

        public void SetDensity(float value, float duration = 1.0f)
        {
            if (currentRoutine != null)
                StopCoroutine(currentRoutine);

            currentRoutine = StartCoroutine(LerpToDensity(value, duration));
        }

        private IEnumerator LerpToDensity(float value, float time)
        {
            var currentTime = 0f;
            var startValue = _cloudsDensity;
            while (currentTime <= time)
            {
                currentTime += Time.deltaTime;
                yield return null;

                _cloudsDensity = Mathf.Lerp(startValue, value, Mathf.Clamp01(currentTime / time));
            }
        }
    }
}
