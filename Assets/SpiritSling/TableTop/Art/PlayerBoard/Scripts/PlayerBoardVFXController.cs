// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    public class PlayerBoardVFXController : MonoBehaviour
    {
        [SerializeField]
        private PlayerBoardStoneController[] stoneControllers;

        [SerializeField]
        private Animator fireAnimator;

        private static readonly int s_activate = Animator.StringToHash("Activate");
        private static readonly int s_deactivate = Animator.StringToHash("Deactivate");
        private static readonly int s_gameOver = Animator.StringToHash("GameOver");

        [SerializeField]
        private AnimateMultipleFloatMaterialProperty fireFloatProperty;

        public int OwnerId { get; set; }

        public void Start()
        {
            stoneControllers.ForEach(s => s.OwnerId = OwnerId);

            RegisterCallBacks();
        }

        private void OnDestroy()
        {
            UnregisterCallBacks();
        }

        private void RegisterCallBacks()
        {
            TabletopGameEvents.GameStart += OnGameStart;
            TabletopGameEvents.OnKodamaDeath += OnKodamaDeath;
            TabletopGameEvents.OnGamePhaseChanged += OnPhaseChanged;
            TabletopGameEvents.OnShootPhaseEnd += OnShootPhaseEnd;
        }

        private void UnregisterCallBacks()
        {
            TabletopGameEvents.GameStart -= OnGameStart;
            TabletopGameEvents.OnKodamaDeath -= OnKodamaDeath;
            TabletopGameEvents.OnGamePhaseChanged -= OnPhaseChanged;
            TabletopGameEvents.OnShootPhaseEnd -= OnShootPhaseEnd;
        }

        private void OnGameStart()
        {
            EnableFire();
        }

        private void OnPhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            var iCanPlay = player != null && OwnerId == player.PlayerId;
            if (phase == TableTopPhase.Move && TabletopGameManager.Instance.Round == 1)
            {
                EnableFire();
            }

            if (iCanPlay)
            {
                switch (phase)
                {
                    case TableTopPhase.Move:
                        OnMovePhase();
                        break;
                    case TableTopPhase.Summon:
                        OnSummonPhase();
                        break;
                    case TableTopPhase.Shoot:
                        OnShootPhase();
                        break;
                }
            }
            else if (phase == TableTopPhase.Move)
            {
                DisableFire();
            }

            if ((iCanPlay == false && phase == TableTopPhase.Move)
                || phase == TableTopPhase.Victory || phase == TableTopPhase.EndPhase || phase == TableTopPhase.EndTurn)
            {
                stoneControllers[0].Deactivate();
                stoneControllers[1].Deactivate();
                stoneControllers[2].Deactivate();
            }
        }

        private void OnKodamaDeath(BaseTabletopPlayer player)
        {
            if (player != null && player.PlayerId == OwnerId)
            {
                foreach (var stoneController in stoneControllers)
                {
                    stoneController.Deactivate();
                }

                fireAnimator.SetTrigger(s_gameOver);
                AnimateFireProperty(1f, 0f);
            }
        }

        private void OnMovePhase()
        {
            EnableFire();
            stoneControllers[0].Activate();
        }

        private void OnSummonPhase()
        {
            stoneControllers[0].Deactivate();
            stoneControllers[1].Activate();
        }

        private void OnShootPhase()
        {
            stoneControllers[1].Deactivate();
            stoneControllers[2].Activate();
        }

        private void OnShootPhaseEnd()
        {
            stoneControllers[2].Deactivate();
            DisableFire();
        }

        bool isFireEnabled;

        private void DisableFire()
        {
            if (isFireEnabled == false)
            {
                return;
            }

            isFireEnabled = false;
            fireAnimator.SetTrigger(s_deactivate);
        }

        private void EnableFire()
        {
            if (isFireEnabled)
            {
                return;
            }

            isFireEnabled = true;
            fireAnimator.SetTrigger(s_activate);
        }

        private Coroutine m_firePropertyCoroutine;

        private void AnimateFireProperty(float startValue = 0f, float endValue = 1f, float duration = 1f)
        {
            if (m_firePropertyCoroutine != null)
            {
                StopCoroutine(m_firePropertyCoroutine);
            }

            m_firePropertyCoroutine = StartCoroutine(VFXAnimationUtility.AnimateFloatProperty(SetFireProperty, startValue, endValue, duration));
        }

        private void SetFireProperty(float value) => fireFloatProperty.valueProperty1 = value;
    }
}