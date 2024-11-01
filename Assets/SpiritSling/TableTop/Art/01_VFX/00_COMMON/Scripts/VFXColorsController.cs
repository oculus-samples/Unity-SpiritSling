// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    public class VFXColorsController : MonoBehaviour
    {
        [SerializeField]
        protected Renderer[] _renderers;

        [SerializeField]
        protected Color[] _playerColor = new Color[4];

        [SerializeField]
        protected Color[] _playerColorVariant = new Color[4];

        [SerializeField]
        protected string[] _propertyNames;

        [SerializeField]
        protected string[] _propertyNamesVariant;

        private int _playerIndex = -1;

        [SerializeField]
        public int debugColorIndex = -1;

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
            if (debugColorIndex >= 0)
                _playerIndex = debugColorIndex;
            else
#endif
                _playerIndex = pawnObject.OwnerIndex;

            UpdateColors();
        }

        [ContextMenu("UpdateColors")]
        public void UpdateColors()
        {
#if UNITY_EDITOR
            if (debugColorIndex >= 0)
                _playerIndex = debugColorIndex;
#endif

            //Debug.Log("<color=red>" + name + ": UpdateColor  index: " + _playerIndex+"</color>");

            foreach (var r in _renderers)
            {
                for (var i = 0; i < r.sharedMaterials.Length; i++)
                {
                    foreach (var property in _propertyNames)
                    {
                        if (r.sharedMaterials[i].HasProperty(property))
                        {
                            var col = new Color(
                                _playerColor[_playerIndex].r, _playerColor[_playerIndex].g, _playerColor[_playerIndex].b,
                                r.sharedMaterials[i].GetColor(property).a);

                            if (Application.isPlaying)
                                r.materials[i].SetColor(property, col);
#if UNITY_EDITOR
                            else
                                r.sharedMaterials[i].SetColor(property, col);
#endif
                        }
                    }

                    foreach (var property in _propertyNamesVariant)
                    {
                        if (r.sharedMaterials[i].HasProperty(property))
                        {
                            var col = new Color(
                                _playerColorVariant[_playerIndex].r, _playerColorVariant[_playerIndex].g, _playerColorVariant[_playerIndex].b,
                                r.sharedMaterials[i].GetColor(property).a);

                            if (Application.isPlaying)
                                r.materials[i].SetColor(property, _playerColorVariant[_playerIndex]);
#if UNITY_EDITOR

                            else
                                r.sharedMaterials[i].SetColor(property, _playerColorVariant[_playerIndex]);
#endif
                        }
                    }
                }
            }
        }
    }
}