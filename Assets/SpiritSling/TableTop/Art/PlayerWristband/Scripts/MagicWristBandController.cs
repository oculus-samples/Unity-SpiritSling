// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Oculus.Interaction.Input;
using Unity.Mathematics;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class MagicWristBandController : NetworkBehaviour
    {
        private static readonly int s_Activation = Shader.PropertyToID("_Activation");

        [SerializeField] private WristBandHandsSync handSync;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform[] hpTransforms;
        [SerializeField] private VFXLineRendererController lineRendererController;
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Transform PlayerboardFireTransform;
        [SerializeField] private ParticleSystemForceField forceField;

        public bool DisplayHP;

        private List<VFXHealthPointController> _availableHealthPointControllers;
        private GameObject hpPrefab;

        private float defaultParticleLifeTime = 10;
        private ParticleSystem system;

        private GameObject magicWristBand;
        private PawnVisualController pawnVisualController;
        private Kodama kodama;

        private int StateAuthorityIndex;

        private bool canComputeDistanceToPlayerBoard;
        private bool isDisplayed = false;

        /// <summary> We need to cache this property to keep it from changing at the wrong time for the health points display logic </summary>
        private int currentHealthpoints = 4;
        private bool healthPointsAvailable = false;

        private TabletopConfig Config => TabletopConfig.Get();

        public override void Spawned()
        {
            base.Spawned();

            StateAuthorityIndex = BaseTabletopPlayer.GetByPlayerId(Object.StateAuthority.PlayerId).Index;
            RegisterCallbacks();

            if (handSync.handedness == Handedness.Left)
                visual.transform.localRotation = Quaternion.Euler(180, 0, 0);

            // only display on our wrists, not the other players
            DisplayHP = BaseTabletopPlayer.LocalPlayer.PlayerRef == Object.StateAuthority;

            var config = TabletopConfig.Get();
            if (DisplayHP)
                hpPrefab = config.PlayerHealthPointVisual(StateAuthorityIndex);

            forceField.enabled = false;
            lineRenderer.enabled = false;

            Log.Debug("HandSync: " + handSync + " DisplayHP: " + DisplayHP + "handedness: " + handSync.handedness);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_availableHealthPointControllers != null)
            {
                for (var i = 0; i < _availableHealthPointControllers.Count; ++i)
                {
                    Destroy(_availableHealthPointControllers[i].gameObject);
                }
                _availableHealthPointControllers.Clear();
            }

            if (magicWristBand != null)
                Destroy(magicWristBand);

            base.Despawned(runner, hasState);
            UnregisterCallbacks();
        }

        private void SetWristBandVisual()
        {
            lineRenderer.material = Config.GetMagicWristBandLineRendererMaterial(StateAuthorityIndex);

            magicWristBand = Instantiate(Config.GetMagicWristBand(StateAuthorityIndex), visual);
            pawnVisualController = visual.gameObject.AddComponent<PawnVisualController>();
            AnimateActivationDelayed();

            system = magicWristBand.GetComponentInChildren<ParticleSystem>();
            defaultParticleLifeTime = system.main.startLifetime.constant;
        }

        private void SetDisplayState(bool state)
        {
            if (magicWristBand != null)
            {
                magicWristBand.SetActive(state);
            }
            _availableHealthPointControllers?.ForEach(x => x.SetVisibility(state));
        }

        private void OnSetupComplete()
        {
            // Delayed initialization to ensure the object is properly spawned
            DelayedAction(Init, 0.5f);
        }

        private void DelayedAction(Action action, float seconds)
        {
            StartCoroutine(DelayAction(action, seconds));
        }

        private void Init()
        {
            SetWristBandVisual();

            if (DisplayHP)
            {
                kodama = BaseTabletopPlayer.LocalPlayer.Kodama;
                var manager = kodama.vfxHealthPointManager;
                manager.OnSpawnHealthPoints += SpawnHealthPoints;
                manager.OnDamage += Damage;
                manager.OnHeal += Heal;
            }

            if (Object.StateAuthority == BaseTabletopPlayer.LocalPlayer.PlayerRef)
            {
                if (PlayerboardFireTransform != null)
                {
                    canComputeDistanceToPlayerBoard = true;
                    lineRendererController._lineEndTargetTransform = PlayerboardFireTransform;
                    lineRenderer.enabled = true;
                }
            }
            else
            {
                forceField.enabled = false;
                canComputeDistanceToPlayerBoard = false;
                lineRendererController.enabled = false;
                lineRenderer.enabled = false;
            }
        }

        private void OnRequestQuitGame()
        {
            DeActivateWristBand();
        }

        private static IEnumerator DelayAction(Action action, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            action();
        }

        private void AnimateActivationDelayed()
        {
            StartCoroutine(DelayAction(AnimateActivation, 1));

            return;

            void AnimateActivation() => StartCoroutine(VFXAnimationUtility.AnimateFloatProperty(SetWristBandActivateProperty, 0, 1, 3));
        }

        private void OnKodamaDeath(BaseTabletopPlayer tabletopPlayer)
        {
            if (tabletopPlayer.Index == StateAuthorityIndex)
            {
                DeActivateWristBand();
            }
        }

        private void DeActivateWristBand()
        {
            _availableHealthPointControllers?.ForEach(vfxHealthPointController =>
            {
                if (vfxHealthPointController != null)
                {
                    vfxHealthPointController.KillHealthPoint();
                }
            });
            canComputeDistanceToPlayerBoard = false;
            system.Stop();
            StartCoroutine(VFXAnimationUtility.AnimateFloatProperty(SetWristBandActivateProperty, 1, 0, 3));
        }

        private void SetWristBandActivateProperty(float value)
        {
            if (pawnVisualController != null)
            {
                pawnVisualController.SetFloat(s_Activation, value);
            }
        }

        private void RegisterCallbacks()
        {
            TabletopGameEvents.OnSetupComplete += OnSetupComplete;
            TabletopGameEvents.OnKodamaDeath += OnKodamaDeath;
            TabletopGameEvents.OnRequestLeaveToLobby += OnRequestQuitGame;
            TabletopGameEvents.OnRequestQuitGame += OnRequestQuitGame;
        }

        private void UnregisterCallbacks()
        {
            TabletopGameEvents.OnSetupComplete -= OnSetupComplete;
            TabletopGameEvents.OnKodamaDeath -= OnKodamaDeath;
            TabletopGameEvents.OnRequestLeaveToLobby -= OnRequestQuitGame;
            TabletopGameEvents.OnRequestQuitGame -= OnRequestQuitGame;
        }

        private void SpawnHealthPoints()
        {
            // Don't display HP on non dominant hand
            if (handSync && !HandVisualReferences.Instance.GetHandVisual(handSync.handedness).Hand.IsDominantHand)
            {
                DisplayHP = false;
            }

            if (!DisplayHP)
            {
                return;
            }

            if (_availableHealthPointControllers?.Count > 0)
            {
                foreach (var c in _availableHealthPointControllers)
                {
                    c.EnableHealthPoint(kodama.transform.position);
                }
            }
            else
            {
                _availableHealthPointControllers = new List<VFXHealthPointController>(TabletopGameManager.Instance.Settings.kodamaStartHealthPoints);
                for (var i = 0; i < TabletopGameManager.Instance.Settings.kodamaStartHealthPoints; i++)
                {
                    var x = Instantiate(hpPrefab, hpTransforms[i].position, Quaternion.identity).GetComponent<VFXHealthPointController>();
                    _availableHealthPointControllers.Add(x);
                    x.EnableHealthPoint(kodama.transform.position);
                }
            }

            healthPointsAvailable = _availableHealthPointControllers.Count > 0;
        }
        internal void Damage(int healthPoints, int amount)
        {
            currentHealthpoints = healthPoints;

            if (!DisplayHP)
                return;

            for (int i = healthPoints; i <= _availableHealthPointControllers.Count - 1; i++)
            {
                _availableHealthPointControllers[i].KillHealthPoint();
            }

            pawnVisualController.AnimateDamageVFX();
        }
        internal void Heal(int healthPoints, Vector3 position)
        {
            currentHealthpoints = healthPoints;

            if (!DisplayHP)
                return;

            Vector3 spawnPosition = position;

            for (int i = 0; i <= _availableHealthPointControllers.Count - 1; i++)
            {
                if (i < healthPoints)
                {
                    _availableHealthPointControllers[i].EnableHealthPoint(spawnPosition);
                    RepositionHealthPoints(i);
                }
            }

            pawnVisualController.AnimateHealVFX();
        }

        /// <summary> This method is used on update to reposition dynamically the health points around the kodama </summary>
        private void RepositionHealthPoints(int remainingPoints)
        {
            if (!DisplayHP)
                return;

            if (remainingPoints <= 0) return;

            for (var i = 0; i < remainingPoints; i++)
            {
                _availableHealthPointControllers[i].targetPosition = hpTransforms[i].position;
            }
        }

        private void Update()
        {
            if (isDisplayed != handSync.IsVisible)
            {
                isDisplayed = handSync.IsVisible;
                SetDisplayState(handSync.IsVisible);

            }

            if (isDisplayed)
            {
                if (healthPointsAvailable)
                {
                    RepositionHealthPoints(currentHealthpoints);
                }
            }

            if (canComputeDistanceToPlayerBoard)
            {
                var distanceToPlayerBoard = math.distance(PlayerboardFireTransform.position, transform.position);
                var activeForceField = distanceToPlayerBoard > Config.WristBandFarAwayMinDistance;
                forceField.enabled = activeForceField;
                lineRenderer.enabled = activeForceField;
                lineRendererController.enabled = activeForceField;
                var main = system.main;
                main.startLifetime = forceField.enabled ? defaultParticleLifeTime * 2 : defaultParticleLifeTime;
            }
        }
    }
}