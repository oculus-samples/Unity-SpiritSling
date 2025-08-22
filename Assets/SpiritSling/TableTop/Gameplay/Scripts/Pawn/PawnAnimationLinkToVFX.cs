// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using SpiritSling.TableTop;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// Does the link between the animation events and the related VFX for a pawn.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class PawnAnimationLinkToVFX : MonoBehaviour
    {
        private Pawn pawn;

        private GameObject _deathVFX;

        private GameObject _deathVFXPrefab => TabletopConfig.Get().PlayerPawnDeathVFX(pawn.OwnerIndex);

        private void Awake()
        {
            pawn = GetComponentInParent<Pawn>();
        }

        private void Start()
        {
            _deathVFX = InstantiateVFX(_deathVFXPrefab);
        }

        private void OnDestroy()
        {
            if (_deathVFX != null)
                Destroy(_deathVFX);
        }

        private GameObject InstantiateVFX(GameObject prefab)
        {
            var vfx = Instantiate(prefab, pawn.transform);
            vfx.SetActive(false);
            return vfx;
        }

        public void DeathVFX()
        {
            _deathVFX.SetActive(true);
        }
    }
}
