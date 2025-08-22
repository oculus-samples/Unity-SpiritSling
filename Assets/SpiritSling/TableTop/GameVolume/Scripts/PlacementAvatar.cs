// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections.Generic;
using Oculus.Avatar2;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class PlacementAvatar : MonoBehaviour
    {
        private static readonly int AVATAR_COLOR_ID = Shader.PropertyToID("u_BaseColorFactor");

        [SerializeField]
        private Collider _fakeAvatarCollider;

        [SerializeField]
        private OvrAvatarEntity _fakeAvatar;

        private float _avatarRadius;

        private List<MeshRenderer> _fakeAvatarRenderers;

        private void Awake()
        {
            _avatarRadius = _fakeAvatarCollider.bounds.size.x * 0.5f;
            _fakeAvatar.OnUserAvatarLoadedEvent.AddListener(OnAvatarLoaded);
        }

        private void OnDestroy()
        {
            var gameVolume = GetComponentInParent<GameVolume>();
            if (gameVolume != null)
            {
                var transformers = gameVolume.GetComponentsInChildren<GameVolumeTransformer>();
                foreach (var transformer in transformers)
                {
                    transformer.updateTransform.RemoveListener(OnGameVolumeTransformed);
                }

                gameVolume.SnapToValidPos.RemoveListener(OnGameVolumeTransformed);
            }
        }

        private void OnAvatarLoaded(OvrAvatarEntity _)
        {
            _fakeAvatarRenderers = new List<MeshRenderer>(_fakeAvatar.GetComponentsInChildren<MeshRenderer>(true));

            var gameVolume = GetComponentInParent<GameVolume>();
            var transformers = gameVolume.GetComponentsInChildren<GameVolumeTransformer>();
            foreach (var transformer in transformers)
            {
                transformer.updateTransform.AddListener(OnGameVolumeTransformed);
            }

            gameVolume.SnapToValidPos.AddListener(OnGameVolumeTransformed);

            // Updates to initial state
            OnGameVolumeTransformed();
        }

        private void OnGameVolumeTransformed()
        {
            if (_fakeAvatarRenderers == null || _fakeAvatarRenderers.Count == 0)
            {
                return;
            }

            // Removes potentially destroyed components
            _fakeAvatarRenderers.RemoveAll(meshRender => meshRender == null);

            var validPosition = GameVolumeManager.Instance.IsValidFloorPosition(transform.position, _avatarRadius);
            foreach (var meshRenderer in _fakeAvatarRenderers)
            {
                meshRenderer.sharedMaterial.SetColor(AVATAR_COLOR_ID, validPosition ? Color.white : Color.red);
            }
        }
    }
}
