// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
public class VFXProjectileFollow : MonoBehaviour
{
    [SerializeField]
    Transform _targetTransform;

    [SerializeField]
    private bool follow;

    Vector3 _startPosition;

    void Start()
    {
        if (_targetTransform) //follow projectile
            _startPosition = (_targetTransform.position);
    }

    // Update is called once per frame
    void Update()
    {
        if (follow && _targetTransform) //follow projectile
        {
            transform.position = (_targetTransform.position);
        }
        else // cancel
        {
            transform.position = _startPosition;
        }
    }
}
