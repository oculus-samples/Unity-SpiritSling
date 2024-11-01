// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace SpiritSling
{
    /// <summary>
    /// Custom behaviour for all buttons of the project
    /// </summary>
    [SelectionBase]
    public class CustomButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField]
        private Renderer backgroundRenderer;

        [SerializeField]
        private float hoverScale = 1.1f;

        [SerializeField]
        private float pressedScale = 0.8f;

        [SerializeField, Min(0), Tooltip("When the button is cliked, any other click performed during this delay is ignored.")]
        private float delayBetweenClicks = 0.05f;

        [SerializeField]
        private bool resetStateOnDisable = true;

        public UnityEvent onClick = new();

        [SerializeField]
        private Material defaultMaterial;

        [SerializeField]
        private Material hoverMaterial;

        [SerializeField]
        private Material pressedMaterial;

        [SerializeField]
        private Material disabledMaterial;

        [Header("Audio")]
        [SerializeField]
        private AudioClip hoverAudioClip;

        [SerializeField]
        private AudioClip clickAudioClip;

        private bool _isInteractable;
        private float _startScale;
        private Coroutine _scaleRoutine;

        private PokeInteractable _poke;

        private float lastValidClickTime;

        public bool IsInteractable
        {
            get => _isInteractable;
            set
            {
                _isInteractable = value;
                backgroundRenderer.material = _isInteractable ? defaultMaterial : disabledMaterial;
                transform.localScale = Vector3.one * _startScale;
            }
        }

        private void OnDestroy()
        {
            if (_poke != null)
            {
                _poke.WhenPointerEventRaised -= WhenPointerEventRaised;
            }
        }

        private void Awake()
        {
            _startScale = transform.localScale.x;
            IsInteractable = true;

            if (TryGetComponent(out _poke))
            {
                _poke.WhenPointerEventRaised += WhenPointerEventRaised;
            }
        }

        private void OnEnable()
        {
            if (resetStateOnDisable && IsInteractable)
            {
                // Resets button visual to its initial state
                IsInteractable = true;
            }
        }

        private void OnDisable()
        {
            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
            }
        }

        private void WhenPointerEventRaised(PointerEvent obj)
        {
            switch (obj.Type)
            {
                case PointerEventType.Hover:
                    OnPointerEnter(null);
                    break;

                case PointerEventType.Cancel:
                case PointerEventType.Unhover:
                    OnPointerExit(null);
                    break;

                case PointerEventType.Select:
                    OnPointerDown(null);
                    break;

                case PointerEventType.Unselect:
                    OnPointerUp(null);
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_isInteractable)
                return;

            backgroundRenderer.material = hoverMaterial;
            SetScale(hoverScale);
            AudioManager.Instance.Play(hoverAudioClip, AudioMixerGroups.UI_Menu);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isInteractable)
                return;

            backgroundRenderer.material = defaultMaterial;
            SetScale(1f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_isInteractable)
                return;

            backgroundRenderer.material = pressedMaterial;
            SetScale(pressedScale);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isInteractable || Time.time < lastValidClickTime + delayBetweenClicks)
                return;

            backgroundRenderer.material = defaultMaterial;
            SetScale(1f);

            onClick.Invoke();
            AudioManager.Instance.Play(clickAudioClip, AudioMixerGroups.UI_Menu);
            lastValidClickTime = Time.time;
        }

        private void SetScale(float scale)
        {
            if (_scaleRoutine != null) StopCoroutine(_scaleRoutine);
            if (gameObject.activeInHierarchy)
            {
                _scaleRoutine = StartCoroutine(SetScaleRoutine(_startScale * scale));
            }
        }

        private IEnumerator SetScaleRoutine(float s)
        {
            yield return Tweens.Lerp(
                transform.localScale.x, s, 0.25f, Tweens.EaseOut, (step =>
                {
                    transform.localScale = Vector3.one * step;
                }));

            _scaleRoutine = null;
        }
    }
}