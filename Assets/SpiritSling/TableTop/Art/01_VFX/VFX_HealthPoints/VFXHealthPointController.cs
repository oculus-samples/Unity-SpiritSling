// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// This Controller controls a single HealthPointVFX
    /// </summary>
    /// <seealso cref="VFXHealthPointManager"/>
    [MetaCodeSample("SpiritSling")]
    [RequireComponent(typeof(Animator))]
    public class VFXHealthPointController : MonoBehaviour
    {
        private static int EnableAnimation = Animator.StringToHash("Activate");
        private static int DisableAnimation = Animator.StringToHash("Deactivate");

        internal Vector3 targetPosition;

        private Vector3 localSpawnPosition;
        
        private List<Renderer> renderers = new();

        [SerializeField]
        private Animator animator;

        private bool IsActive;

        private bool isVisible;

        private void Start()
        {
            renderers = new List<Renderer>(GetComponentsInChildren<Renderer>());
        }
        
        public void SetVisibility(bool visible)
        {
            if (isVisible != visible)
            {
                isVisible = visible;
                foreach (var r in renderers)
                {
                    r.enabled = visible;
                }
            }
        }

        public void EnableHealthPoint(Vector3 spawnPosition)
        {
            if (IsActive)
            {
                return;
            }
            transform.position = spawnPosition;
            if (gameObject.activeSelf == false)
            {
                gameObject.SetActive(true);
            }

            IsActive = true;
            animator.SetTrigger(EnableAnimation);
        }

        public void Update()
        {
            FollowTargetPoint();
        }

        public void FollowTargetPoint()
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, 0.1f);
            transform.LookAt(targetPosition);
        }

        public void KillHealthPoint()
        {
            if (IsActive == false)
            {
                return;
            }
            IsActive = false;
            animator.SetTrigger(DisableAnimation);
        }
    }
}
