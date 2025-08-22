// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

[MetaCodeSample("SpiritSling")]
[ExecuteInEditMode]
public class LookAtCamera : MonoBehaviour
{
    public bool lockX; // Lock the X axis
    public bool lockY; // Lock the Y axis
    public bool lockZ; // Lock the Z axis
    public Transform target;
    Vector3 targetPosition;
    Vector3 currentPosition;
    Vector3 direction;

    private void Start()
    {
        if (target == null)
            target = Camera.main.transform;
    }

    void Update()
    {
        targetPosition = target.position;
        currentPosition = transform.position;

        direction = targetPosition - currentPosition;

        // Calculate the direction to look at
        // Lock the axes as needed
        if (lockX) direction.x = 0;
        if (lockY) direction.y = 0;
        if (lockZ) direction.z = 0;

        // Look at the target
        transform.rotation = Quaternion.LookRotation(direction);

        //Debug.DrawRay(currentPosition, direction,Color.red);
    }
}
