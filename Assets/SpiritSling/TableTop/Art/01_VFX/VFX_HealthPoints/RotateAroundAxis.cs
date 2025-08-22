// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
public class RotateAroundAxis : MonoBehaviour
{
    public bool IsActive;
    public float Speed = 1f;
    public Vector3 Axis = Vector3.up;

    private void Update()
    {
        if (IsActive)
        {
            transform.Rotate(Axis, Speed * Time.deltaTime);
        }
    }
}
