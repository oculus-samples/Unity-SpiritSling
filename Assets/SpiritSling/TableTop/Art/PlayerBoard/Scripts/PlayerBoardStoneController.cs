// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.VFX;
using static SpiritSling.TableTop.VFXAnimationUtility;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    [RequireComponent(typeof(Animator))]
    public class PlayerBoardStoneController : MonoBehaviour
    {
        [SerializeField]
        private MeshRenderer stoneRenderer;

        private static readonly int s_stoneTimeProperty = Shader.PropertyToID("_StoneTime");

        private static readonly int s_activate = Animator.StringToHash("Activate");
        private static readonly int s_deactivate = Animator.StringToHash("Deactivate");

        [SerializeField]
        private Animator _animator;

        [SerializeField]
        private MeshRenderer lightBeamRenderer;

        [SerializeField]
        private Transform stoneTransform;

        private VisualEffect skipPhaseVFX;
        private VisualEffect stoneFallVFX;

        [SerializeField]
        private GameObject stoneFallVFXPrefab;

        [SerializeField]
        private AudioClip cancelSkipAudioClip;

        [SerializeField]
        private AudioSource[] skipAudioSources;

        public int OwnerId { get; set; }

        private TabletopConfig Config => TabletopConfig.Get();

        private bool isActivated;

        private float _stoneStayStartTime;

        private bool _isHover;
        private bool IsHover
        {
            get => _isHover;
            set
            {
                _isHover = value;
                SetStoneHover(_isHover);
            }
        }

        private ConicalFrustum _frustum;

        private readonly float baseHeight = 0.03f;
        public float StoneHeight;

        private float stoneTransformY => baseHeight + StoneHeight * 0.05f;

        private void Start()
        {
            if (OVRManager.OVRManagerinitialized)
            {
                var head = OVRManager.instance.GetComponentInChildren<OVRCameraRig>().centerEyeAnchor;
                _frustum = head.GetComponentInChildren<ConicalFrustum>();
            }

            InitializeVFX();
            RegisterCallbacks();
        }

        private void OnDestroy()
        {
            if (skipPhaseVFX != null)
                Destroy(skipPhaseVFX.gameObject);
            if (stoneFallVFX != null)
                Destroy(stoneFallVFX.gameObject);
            UnregisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            TabletopGameEvents.OnPawnDragStart += OnPawnDragStart;
        }

        private void UnregisterCallbacks()
        {
            TabletopGameEvents.OnPawnDragStart -= OnPawnDragStart;
        }

        private void OnValidate()
        {
            stoneTransform.localPosition = new Vector3(0, stoneTransformY, 0);
        }

        private void Update()
        {
            if (!Mathf.Approximately(stoneTransform.localPosition.y, stoneTransformY))
            {
                stoneTransform.localPosition = new Vector3(0, stoneTransformY, 0);
                if (StoneHeight == 0)
                {
                    PlayStoneFallVFX();
                }
            }

            if (isActivated && CanSkipPhase() && IsHover && _stoneStayStartTime > 0)
            {
                if (Time.time - _stoneStayStartTime > Config.StoneSkipMinStayDuration)
                {
                    IsHover = false;
                    _stoneStayStartTime = 0;
                    if (TabletopGameManager.Instance)
                    {
                        TabletopGameManager.Instance.SkipPhase();
                    }
                    PlaySkipPhaseVFX();
                }
            }
        }

        private bool CanSkipPhase()
        {
            return TabletopGameManager.Instance != null
                   && TabletopGameManager.Instance.CanSkipPhase == true
                   && PawnMovement.IsDraggingPawn == false
                   && BaseTabletopPlayer.LocalPlayer != null
                   && OwnerId == BaseTabletopPlayer.LocalPlayer.PlayerId;
        }

        public void OnStoneHover()
        {
            if (!isActivated || IsHover || !CanSkipPhase() || !_frustum || !_frustum.IsPointInConeFrustum(stoneTransform.position))
            {
                return;
            }

            // Starts the skip phase
            _stoneStayStartTime = Time.time;
            IsHover = true;
            foreach (var skipAudioSource in skipAudioSources)
            {
                skipAudioSource.Play();
            }
        }

        public void OnStoneUnhover()
        {
            if (!IsHover)
            {
                return;
            }

            // Cancels the skip phase
            IsHover = false;
            AudioManager.Instance.Play(cancelSkipAudioClip, AudioMixerGroups.UI_SkipPhase);
            foreach (var skipAudioSource in skipAudioSources)
            {
                skipAudioSource.Stop();
            }
        }

        #region CallBack Methods

        private void OnPawnDragStart()
        {
            _stoneStayStartTime = 0;
            IsHover = false;
        }

        #endregion

        #region VFX

        private void InitializeVFX()
        {
            skipPhaseVFX = Instantiate(Config.PawnSpawnVFX, stoneTransform).GetComponent<VisualEffect>();
            skipPhaseVFX.gameObject.SetActive(false);
            skipPhaseVFX.SetMesh("Shape", stoneRenderer.GetComponent<MeshFilter>().mesh);

            stoneFallVFX = Instantiate(stoneFallVFXPrefab, transform).GetComponent<VisualEffect>();
            stoneFallVFX.gameObject.SetActive(false);
        }

        private void PlaySkipPhaseVFX()
        {
            skipPhaseVFX.gameObject.SetActive(true);
            skipPhaseVFX.Play();
        }

        private void PlayStoneFallVFX()
        {
            stoneFallVFX.gameObject.SetActive(true);
            stoneFallVFX.Play();
        }

        #endregion

        #region Animation

        private void StopIfNotNull(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }

        private void SetStoneTimeValue(float value) => stoneRenderer.material.SetFloat(s_stoneTimeProperty, value);

        private void SetStoneHover(bool value)
        {
            if (value)
                stoneRenderer.material.EnableKeyword("_HOVER");
            else
                stoneRenderer.material.DisableKeyword("_HOVER");
        }

        Coroutine m_stoneTimeCoroutine;

        private void SetStoneTimeValueCoroutine()
        {
            StopIfNotNull(m_stoneTimeCoroutine);
            StartCoroutine(AnimateFloatProperty(SetStoneTimeValue, 0, 1, TabletopGameManager.Instance.Settings.timePhase));
        }

        public void Activate()
        {
            if (isActivated)
                return;

            isActivated = true;
            _animator.SetTrigger(s_activate);
            SetStoneTimeValueCoroutine();

            _stoneStayStartTime = 0;
            IsHover = false;
        }

        public void Deactivate()
        {
            if (isActivated == false)
                return;

            isActivated = false;
            IsHover = false;

            _animator.SetTrigger(s_deactivate);
            StopIfNotNull(m_stoneTimeCoroutine);
        }

        #endregion
    }
}
