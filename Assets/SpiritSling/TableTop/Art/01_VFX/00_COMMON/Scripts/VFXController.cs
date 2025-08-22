// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace SpiritSling
{
    /// <summary>
    /// Common class used by VFX Team, to control all VFX Animators State Machine
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    [RequireComponent(typeof(Animator))]
    public class VFXController : MonoBehaviour
    {
        protected Animator _animator;

        [SerializeField]
        protected bool _activateOnStart = true;

        [SerializeField]
        protected bool _deactivateOnStart;

        [SerializeField]
        protected bool _triggerOnStart;

        [Header("Animator Triggers :")]
        [SerializeField]
        protected string _activate = "Activate";

        [SerializeField]
        protected string _deactivate = "Deactivate";

        [SerializeField]
        protected string _trigger = "Trigger";

        [SerializeField]
        protected string _triggerSpecial = "Special";

        [SerializeField]
        protected string _triggerAdditional = "Additional";

        [SerializeField]
        protected string _blend = "Blend";

        [Header("Audio Sources :")]
        [SerializeField]
        protected bool _mute;

        [SerializeField]
        protected AudioSource _activateAudioSource;

        [SerializeField]
        protected AudioSource _deactivateAudioSource;

        [SerializeField]
        protected AudioSource _triggerAudioSource;

        [SerializeField]
        protected AudioSource _triggerSpecialAudioSource;

        [SerializeField]
        protected AudioSource _triggerAdditionalAudioSource;

        [Header("Particles Systems :")]
        [SerializeField]
        protected ParticleSystem[] _activateParticles;

        [SerializeField]
        protected ParticleSystem[] _deactivateParticles;

        [SerializeField]
        protected ParticleSystem[] _triggerParticles;

        [SerializeField]
        protected ParticleSystem[] _triggerSpecialParticles;

        [SerializeField]
        protected ParticleSystem[] _triggerAdditionalParticles;

        [Header("Events :")]
        public UnityEvent OnActivate;

        public UnityEvent OnDeactivate;
        public UnityEvent OnTrigger;
        public UnityEvent OnSpecial;
        public UnityEvent OnAdditional;

        private void Start()
        {
            _animator = GetComponent<Animator>();

            if (_activateOnStart)
                Activate();
            if (_deactivateOnStart)
                Deactivate();
            if (_triggerOnStart)
                Trigger();
        }

        public virtual void Blend(float value)
        {
            if (_animator)
                _animator.SetFloat(_blend, value);
        }

        [ContextMenu("Activate")]
        public virtual void Activate()
        {
            OnActivate?.Invoke();

            if (_animator)
            {
                _animator.ResetTrigger(_trigger);
                _animator.ResetTrigger(_triggerAdditional);
                _animator.ResetTrigger(_triggerSpecial);
                _animator.ResetTrigger(_activate);
                _animator.ResetTrigger(_deactivate);

                _animator.SetTrigger(_activate);
            }

            StopAllParticles();
            PlayParticles(_activateParticles);

            //Debug.Log("<color=red>Activate  </color>");

            if (!_mute && _activateAudioSource)
                _activateAudioSource.Play();
        }

        [ContextMenu("Deactivate")]
        public virtual void Deactivate()
        {
            OnDeactivate?.Invoke();

            if (_animator)
            {
                _animator.ResetTrigger(_trigger);
                _animator.ResetTrigger(_triggerAdditional);
                _animator.ResetTrigger(_triggerSpecial);
                _animator.ResetTrigger(_activate);
                _animator.ResetTrigger(_deactivate);

                _animator.SetTrigger(_deactivate);
            }

            StopAllParticles();
            PlayParticles(_deactivateParticles);

            //Debug.Log("<color=red>Deactivate  </color>");

            if (!_mute && _deactivateAudioSource)
                _deactivateAudioSource.Play();
        }

        [ContextMenu("Trigger")]
        public virtual void Trigger()
        {
            OnTrigger?.Invoke();

            if (_animator)
            {
                _animator.ResetTrigger(_trigger);
                _animator.SetTrigger(_trigger);
            }

            StopAllParticles();
            PlayParticles(_triggerParticles);

            //Debug.Log("<color=red>TRIGER  </color>");

            if (!_mute && _triggerAudioSource)
                _triggerAudioSource.Play();
        }

        [ContextMenu("TriggerSpecial")]
        public virtual void TriggerSpecial()
        {
            OnSpecial?.Invoke();

            if (_animator)
            {
                _animator.ResetTrigger(_triggerSpecial);
                _animator.SetTrigger(_triggerSpecial);
            }

            StopAllParticles();
            PlayParticles(_triggerSpecialParticles);

            //Debug.Log("<color=red>TRIGER SPECIAL / </color>");
            if (!_mute && _triggerSpecialAudioSource)
                _triggerSpecialAudioSource.Play();
        }

        [ContextMenu("TriggerAdditional")]
        public virtual void TriggerAdditional()
        {
            OnAdditional?.Invoke();

            if (_animator)
            {
                _animator.ResetTrigger(_triggerAdditional);
                _animator.SetTrigger(_triggerAdditional);
            }

            StopAllParticles();
            PlayParticles(_triggerAdditionalParticles);

            if (!_mute && _triggerAdditionalAudioSource)
                _triggerAdditionalAudioSource.Play();
        }

        private void PlayParticles(ParticleSystem[] particles)
        {
            foreach (var particle in particles)
            {
                if (particle != null)
                    particle.Play();
            }
        }

        private void StopParticles(ParticleSystem[] particles)
        {
            foreach (var particle in particles)
            {
                if (particle != null)
                    particle.Stop();
            }
        }

        private void StopAllParticles()
        {
            StopParticles(_activateParticles);
            StopParticles(_deactivateParticles);
            StopParticles(_triggerParticles);
            StopParticles(_triggerSpecialParticles);
            StopParticles(_triggerAdditionalParticles);
        }
    }
}
