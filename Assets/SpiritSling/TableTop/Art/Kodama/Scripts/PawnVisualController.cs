// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class PawnVisualController : MonoBehaviour
    {
        [SerializeField]
        private List<Renderer> Renderers;

        private static int VFXColorAmountProperty = Shader.PropertyToID("_VFXColorAmount");
        private static int VFXColorProperty = Shader.PropertyToID("_VFXColor");

        private TabletopConfig config;

        private void Start()
        {
            config = TabletopConfig.Get();
            PopulateRenderers();
        }

        private void PopulateRenderers()
        {
            Renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true).Cast<Renderer>().Concat(GetComponentsInChildren<MeshRenderer>(true)).
                ToList();
        }

        internal void SetFloat(int id, float value)
        {
            Renderers.ForEach(r => r.material.SetFloat(id, value));
        }

        internal void SetColor(int id, Color value)
        {
            Renderers.ForEach(r => r.material.SetColor(id, value));
        }

        private void SetVFXColorAmountProperty(float value) => SetFloat(VFXColorAmountProperty, value);

        internal void AnimateDamageVFX()
        {
            SetColor(VFXColorProperty, config.DamageVFXColor);
            StartCoroutine(VFXAnimationUtility.AnimateFlashFloatProperty(SetVFXColorAmountProperty));
        }

        internal void AnimateHealVFX()
        {
            SetColor(VFXColorProperty, config.HealVFXColor);
            StartCoroutine(VFXAnimationUtility.AnimateFlashFloatProperty(SetVFXColorAmountProperty));
        }
    }
}