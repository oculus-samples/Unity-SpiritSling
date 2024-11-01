// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using SpiritSling.TableTop;
using Unity.Mathematics;
using UnityEngine;
using static SpiritSling.TableTop.VFXAnimationUtility;

namespace SpiritSling
{
    public class StartTileDecalController : MonoBehaviour
    {
        private static int s_embersAmountProperty = Shader.PropertyToID("_EmbersAmount");
        private static int s_dirtAmountProperty = Shader.PropertyToID("_DirtAmount");
        private static readonly int s_randomSeed = Shader.PropertyToID("_RandomSeed");

        [SerializeField]
        private Pawn _pawn;

        private PawnStateMachine _pawnStateMachine;

        private GameObject _startTileVFX;

        private MeshRenderer meshRenderer;

        public Material mat;

        // Start is called before the first frame update
        private void Start()
        {

            // apparently decal projectors are not instanced, so we have to create a new material for each instance
        }

        private bool Activated;

        public void Activate()
        {
            if (Activated)
                return;

            if (_startTileVFX == null)
            {
                _startTileVFX = Instantiate(TabletopConfig.Get().PawnStartTileDecalVFX, _pawn.transform.position, quaternion.identity);
                meshRenderer = _startTileVFX.GetComponentInChildren<MeshRenderer>();                
            }

            SetDirtAmountProperty(0);
            SetEmbersAmountProperty(1);
            SetRandomSeed();

            _startTileVFX.SetActive(true);
            Activated = true;
            _startTileVFX.transform.position = _pawn.CurrentCellRenderer.transform.position;
            _startTileVFX.transform.rotation = _pawn.CurrentCellRenderer.transform.rotation;
            _startTileVFX.transform.parent = _pawn.CurrentCellRenderer.transform;
            SetEmbersAmountProperty(1);
            AnimateDirtProperty(0, 1, 1, () => AnimateEmbersProperty(1, 0, 1));
        }

        public void Deactivate()
        {
            if (Activated == false)
                return;

            Activated = false;
            AnimateDirtProperty(1, 0, 1);
        }

        private void OnDestroy()
        {
            if(_startTileVFX)
                Destroy(_startTileVFX);
            meshRenderer = null;
        }

        private void SetDirtAmountProperty(float value)
        {
            meshRenderer.material.SetFloat(s_dirtAmountProperty, value);
        }

        private void SetEmbersAmountProperty(float value)
        {
            meshRenderer.material.SetFloat(s_embersAmountProperty, value);
        }

        private void SetRandomSeed()
        {
            meshRenderer.material.SetFloat(s_randomSeed, UnityEngine.Random.value);
        }

        private Coroutine _dirtPropertyCoroutine;

        public void AnimateDirtProperty(float startValue, float endValue, float duration, Action onComplete = null)
        {
            if (_dirtPropertyCoroutine != null)
                StopCoroutine(_dirtPropertyCoroutine);

            _dirtPropertyCoroutine
                = StartCoroutine(AnimateFloatProperty(SetDirtAmountProperty, startValue, endValue, duration, math.sqrt, onComplete));
        }

        private Coroutine _embersPropertyCoroutine;

        public void AnimateEmbersProperty(float startValue, float endValue, float duration, Action onComplete = null)
        {
            if (_embersPropertyCoroutine != null)
                StopCoroutine(_dirtPropertyCoroutine);

            _embersPropertyCoroutine
                = StartCoroutine(AnimateFloatProperty(SetEmbersAmountProperty, startValue, endValue, duration, math.sqrt, onComplete));
        }
    }
}