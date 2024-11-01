// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

public class SetPositionToMaterial : MonoBehaviour
{
    [SerializeField]
    string positionName = "_WorldPos";

    private int _id;
    private Renderer _renderer;

    void Start()
    {
        _id = Shader.PropertyToID(positionName);
        _renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (_renderer.material.HasProperty(_id))
        {
            _renderer.material.SetVector(_id, transform.position);
        }
    }
}