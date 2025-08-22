// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
[ExecuteInEditMode]
[RequireComponent(typeof(MeshRenderer))]
public class AnimateMultipleFloatMaterialProperty : MonoBehaviour
{
    //Ce script permet a l'animation d'edit plusieurs materials sur un mesh, car l'animation ne sait pas lire les arrays, ou structs [serialized] de l'inspector

    [Header("property 1")]
    public float valueProperty1;

    public string propertyName1 = "_Emissive";
    private int _propertyID1;

    [Header("property 2")]
    public float valueProperty2;

    public string propertyName2 = "_Emissive";
    private int _propertyID2;

    [Header("property 3")]
    public float valueProperty3;

    public string propertyName3 = "_Emissive";
    private int _propertyID3;

    [Header("Debug Execute In Edit Mode")]
    [SerializeField]
    private bool _debug;

    Renderer _renderer;

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
        _propertyID2 = Shader.PropertyToID(propertyName2);
        _propertyID3 = Shader.PropertyToID(propertyName3);
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            _material.SetFloat(_propertyID1, valueProperty1);
            _material.SetFloat(_propertyID2, valueProperty2);
            _material.SetFloat(_propertyID3, valueProperty3);
        }
        else if (_debug) // runtime execution
        {
            _sharedMaterial.SetFloat(propertyName1, valueProperty1);
            _sharedMaterial.SetFloat(propertyName2, valueProperty2);
            _sharedMaterial.SetFloat(propertyName3, valueProperty3);
        }
    }
}
