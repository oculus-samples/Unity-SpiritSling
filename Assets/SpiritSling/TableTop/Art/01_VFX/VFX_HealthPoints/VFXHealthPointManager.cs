// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <seealso cref="VFXHealthPointController"/>
    public class VFXHealthPointManager : MonoBehaviour
    {
        [SerializeField][Range(0, 0.1f)] private float _healhPointsVisualHeight = 0.02f;
        [SerializeField] private Kodama _kodama;

        private List<VFXHealthPointController> _availableHealthPointControllers;

        private float radius = 0.03f;

        public delegate void DamageHandler(int newHP, int amount);
        public delegate void HealHandler(int newHP, Vector3 position);
        public delegate void HPPositionUpdateHandler(int HPsToUpdate);
        public delegate void GenericEventHandler();

        public GenericEventHandler OnSpawnHealthPoints;
        public HealHandler OnHeal;
        public DamageHandler OnDamage;
        public HPPositionUpdateHandler OnRepositionHealthPoints;

        // this one is set by the associated MagicWristBandController
        // public List<MagicWristBandController> MagicWristBandControllers;
        private bool _healthPointsInitialised = false;

        private GameObject PlayerHealthPointVisual()
        {
            return TabletopConfig.Get().PlayerHealthPointVisual(_kodama.OwnerIndex);
        }

        private void Start()
        {
            _availableHealthPointControllers = new List<VFXHealthPointController>(TabletopGameManager.Instance.Settings.kodamaStartHealthPoints);
        }

        /// <summary> We need to cache this property to keep it from changing at the wrong time for the health points display logic </summary>
        private int currentHealthpoints = 4;

        internal void SpawnHealthPoints()
        {
            if (_healthPointsInitialised)
                return;

            currentHealthpoints = TabletopGameManager.Instance.Settings.kodamaStartHealthPoints;
            for (int i = 0; i < TabletopGameManager.Instance.Settings.kodamaStartHealthPoints; i++)
            {
                var angle = (360f / TabletopGameManager.Instance.Settings.kodamaStartHealthPoints * i);
                var rotation = Quaternion.Euler(0, angle, 0);
                var x = Instantiate(PlayerHealthPointVisual(), _kodama.pawnVisualController.gameObject.transform).GetComponent<VFXHealthPointController>();
                x.transform.localPosition = Vector3.up * _healhPointsVisualHeight;
                x.transform.rotation = rotation;
                _availableHealthPointControllers.Add(x);
                x.EnableHealthPoint(_kodama.transform.position);
            }
            OnSpawnHealthPoints?.Invoke();

            _healthPointsInitialised = true;

        }

        private void OnDestroy()
        {
            for (var i = 0; i < _availableHealthPointControllers.Count; ++i)
            {
                Destroy(_availableHealthPointControllers[i]);
            }
            _availableHealthPointControllers.Clear();
        }

        /// <summary>
        /// Show all HealthPoints
        /// </summary>
        public void ShowAllHealthPoints()
        {
            if (!_healthPointsInitialised)
                SpawnHealthPoints();

            for (int i = 0; i < _availableHealthPointControllers.Count - 1; i++)
            {
                _availableHealthPointControllers[i].SetVisibility(true);
            }
        }

        // /// <summary> This method is used when the kodama respawns </summary>
        // public void RepositionRemainingLifePoints(int remainingPoints)
        // {
        //     if (remainingPoints <= 0) return;
        //     
        //     RepositionHealthPoints(remainingPoints);
        // }

        /// <summary> Method used to trigger the Damage animation externally </summary>
        internal void Damage(int healthPoints, int amount)
        {
            currentHealthpoints = healthPoints;

            for (int i = healthPoints; i <= _availableHealthPointControllers.Count - 1; i++)
            {
                _availableHealthPointControllers[i].KillHealthPoint();
            }

            OnDamage?.Invoke(healthPoints, amount);
        }

        /// <summary> Method used to trigger the Heal animation externally </summary>
        internal void Heal(int healthPoints)
        {
            currentHealthpoints = healthPoints;
            Vector3 spawnPosition = _kodama.transform.position + _kodama.transform.up * 0.2f;

            for (int i = 0; i <= _availableHealthPointControllers.Count - 1; i++)
            {
                if (i < healthPoints)
                {
                    _availableHealthPointControllers[i].EnableHealthPoint(spawnPosition);
                    RepositionHealthPoints(i);
                }
            }

            OnHeal?.Invoke(currentHealthpoints, spawnPosition);
        }

        /// <summary> This method is used on update to reposition dynamically the health points around the kodama </summary>
        public void RepositionHealthPoints(int remainingPoints)
        {
            if (remainingPoints <= 0 || _availableHealthPointControllers.Count <= 0)
                return;

            int points = Math.Min(remainingPoints, _availableHealthPointControllers.Count);
            float angleStep = 2 * math.PI / points;
            float startAngle = Time.time;

            Vector3 center = _kodama.pawnVisualController.transform.position + _healhPointsVisualHeight * _kodama.pawnVisualController.transform.up;

            for (int i = 0; i < points; i++)
            {
                float angle = startAngle + angleStep * i;
                Vector3 offset = new Vector3(math.cos(angle), 0, math.sin(angle)) * radius;
                _availableHealthPointControllers[i].targetPosition = center + offset;
            }

            OnRepositionHealthPoints?.Invoke(remainingPoints);
        }

        private void Update()
        {
            RepositionHealthPoints(currentHealthpoints);
        }
    }
}