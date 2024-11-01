// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    public class VFXMaterialController : MonoBehaviour
    {
        [SerializeField]
        protected Renderer[] _renderers;

        [SerializeField]
        protected Material[] _playerMaterials = new Material[4];

        private int _playerIndex = -1;

        [SerializeField]
        public int debugMaterialIndex = -1;

        [SerializeField]
        Pawn pawnObject;

        private void Start()
        {
            if (!pawnObject)
                pawnObject = GetComponentInParent<Pawn>();

            if (pawnObject == null)
            {
                Log.Error(name + ": null pawnObject ");
                Destroy(this);
                return;
            }

#if UNITY_EDITOR
            if (debugMaterialIndex >= 0)
                _playerIndex = debugMaterialIndex;
            else
#endif
                _playerIndex = pawnObject.OwnerIndex;

            UpdateMaterial();
        }

        [ContextMenu("UpdateMaterial")]
        public void UpdateMaterial()
        {
#if UNITY_EDITOR
            if (debugMaterialIndex >= 0)
                _playerIndex = debugMaterialIndex;
#endif

            //Debug.Log("<color=red>" + name + ": UpdateMaterial  index: " + _playerIndex+"</color>");

            foreach (var r in _renderers)
            {
                if (r)
                    r.material = _playerMaterials[_playerIndex];
            }
        }
    }
}