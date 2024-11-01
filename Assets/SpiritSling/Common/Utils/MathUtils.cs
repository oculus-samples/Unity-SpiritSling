// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using UnityEngine;

namespace SpiritSling
{
    /// <summary>
    /// Simple math function without external dependencies
    /// </summary>
    public static class MathUtils
    {
        public static float DistanceXZPlane(Vector3 a, Vector3 b)
        {
            var num1 = a.x - b.x;
            var num3 = a.z - b.z;
            return (float)Math.Sqrt(num1 * num1 + num3 * num3);
        }

        public static Vector3 SetY(this Vector3 v, float y)
        {
            v.y = y;
            return v;
        }

        /// <summary>
        /// Converts a value from one range to another and clamps it to the new range. The ranges are inclusives.
        /// Examples: ClampedRemap(5, 1, 10, 0, 1) = 0.5 ; ClampedRemap(20, 1, 10, 0, 1) = 1
        /// </summary>
        /// <param name="originalValue">the value from the original range. It may be outside that range.</param>
        /// <param name="originalLowRange">must be lower than originalHighRange</param>
        /// <param name="originalHighRange">must be higher than originalLowRange</param>
        /// <param name="newLowRange">must be lower than newHighRange</param>
        /// <param name="newHighRange">must be higher than newLowRange</param>
        /// <returns>the converted value, contained in the new range.</returns>
        public static float ClampedRemap(float originalValue, float originalLowRange, float originalHighRange, float newLowRange, float newHighRange)
        {
            var normal = Mathf.InverseLerp(originalLowRange, originalHighRange, originalValue);
            return Mathf.Lerp(newLowRange, newHighRange, normal);
        }
    }
}