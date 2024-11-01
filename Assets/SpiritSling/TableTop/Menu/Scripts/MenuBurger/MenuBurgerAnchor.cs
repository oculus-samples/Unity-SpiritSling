// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Just a script that can easily be found in the hierarchy to know where to attach the burger menu
    /// </summary>
    public class MenuBurgerAnchor : MonoBehaviour
    {
        public Vector3 shift;
        public Quaternion shiftRotation;
        public OVRPlugin.Handedness handedness;
    }
}