// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class AnimateMultipleMaterialsFloatProperty : MonoBehaviour
{
    //Ce script permet a l'animation d'edit plusieurs materials sur un mesh, car l'animation ne sait pas lire les arrays, ou structs [serialized] de l'inspector

    [Header("Material 1")]
    public float valueMaterial1;

    public string propertyName1 = "_Emissive";
    private int _propertyID1;

    [Header("Material 2")]
    public float valueMaterial2;

    public string propertyName2 = "_Emissive";
    private int _propertyID2;

    [Header("Material 3")]
    public float valueMaterial3;

    public string propertyName3 = "_Emissive";
    private int _propertyID3;

    [Header("Debug Execute In Edit Mode")]
    [SerializeField]
    private bool _debug;

    private Renderer _renderer;
    
    private Material[] _materials;
    private Material[] _sharedMaterials;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (Application.isPlaying)
            _materials = _renderer.materials;
        else 
            _sharedMaterials = _renderer.sharedMaterials;
        
        _propertyID1 = Shader.PropertyToID(propertyName1);
        _propertyID2 = Shader.PropertyToID(propertyName2);
        _propertyID3 = Shader.PropertyToID(propertyName3);
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            _materials[0].SetFloat(_propertyID1, valueMaterial1);

            if (1 < _renderer.materials.Length)
                _materials[1].SetFloat(_propertyID2, valueMaterial2);

            if (2 < _renderer.materials.Length)
                _materials[2].SetFloat(_propertyID3, valueMaterial3);
        }
        else if (_debug) // runtime execution
        {
            _sharedMaterials[0].SetFloat(propertyName1, valueMaterial1);

            if (1 < _renderer.sharedMaterials.Length)
                _sharedMaterials[1].SetFloat(propertyName2, valueMaterial2);

            if (2 < _renderer.sharedMaterials.Length)
                _sharedMaterials[2].SetFloat(propertyName3, valueMaterial3);
        }
    }
}
