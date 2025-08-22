// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class AnimateMaterialFloatProperty : MonoBehaviour
{
    //Ce script permet a l'animation d'edit plusieurs materials sur un mesh, car l'animation ne sait pas lire les arrays, ou structs [serialized] de l'inspector

    [Header("Material 1")]
    public float valueMaterial1;

    public string propertyName1 = "_Emissive";
    private int _propertyID1;

    [Header("Debug Execute In Edit Mode")]
    [SerializeField]
    private bool _debug;

    private Renderer _renderer;
    private Material _material;
    private Material _sharedMaterial;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (Application.isPlaying)
            _material = _renderer.material;
        else
            _sharedMaterial = _renderer.sharedMaterial;
        
        _propertyID1 = Shader.PropertyToID(propertyName1);
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            _material.SetFloat(_propertyID1, valueMaterial1);
        }
        else if (_debug) // runtime execution
        {
            _sharedMaterial.SetFloat(propertyName1, valueMaterial1);
        }
    }
}
