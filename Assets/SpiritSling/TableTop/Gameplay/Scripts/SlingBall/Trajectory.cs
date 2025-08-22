// Copyright (c) Meta Platforms, Inc. and affiliates.

/// <summary>
/// This class provides methods for calculating and analyzing the trajectory
/// of an object in 3D space given an initial angle and velocity.
/// It can compute the position, range, and maximum height of the trajectory,
/// as well as the required velocity to reach a specific target.
/// </summary>
/// <author>Anthony KOZAK</author>

using Meta.XR.Samples;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class Trajectory : MonoBehaviour
    {
        [Range(0, 90)]
        [SerializeField]
        private float m_Angle = 45f; // The launch angle in degrees, ranging from 0 to 90.

        [SerializeField]
        private float m_Velocity = 10f; // The initial velocity in units per second.

        private float m_Range; // The calculated horizontal range of the trajectory.
        private float m_TimeInAir; // The calculated time the object stays in the air.
        private Matrix4x4 m_Matrix; // A transformation matrix for converting between coordinate spaces.
        private Vector3 m_CachedVecToTarget;

        /// <summary>
        /// Gets or sets the transformation matrix used for trajectory calculations.
        /// </summary>
        public Matrix4x4 Matrix
        {
            get => m_Matrix;
            set => m_Matrix = value;
        }

        /// <summary>
        /// Gets or sets the launch angle in degrees.
        /// </summary>
        public float Angle
        {
            get => m_Angle;
            set => m_Angle = value;
        }

        /// <summary>
        /// Gets or sets the initial velocity in units per second.
        /// </summary>
        public float Velocity
        {
            get => m_Velocity;
            set => m_Velocity = value;
        }

        /// <summary>
        /// Calculates the launch angle based on the forward direction vector.
        /// </summary>
        public void CalculateAngleByForwardVector()
        {
            var projectedVec = m_Matrix.inverse.MultiplyVector(transform.forward);
            Angle = Mathf.Atan2(projectedVec.y, projectedVec.z) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Calculates the transformation matrix to a specified target position.
        /// </summary>
        /// <param name="target">The target position in world space.</param>
        public void CalculateMatrixToTarget(Vector3 target)
        {
            var vecToTarget = target - transform.position;
            var vecToTargetNoY = new Vector3(vecToTarget.x, 0, vecToTarget.z);
            m_Matrix.SetTRS(transform.position, Quaternion.LookRotation(vecToTargetNoY, Vector3.up), Vector3.one);
        }

        /// <summary>
        /// Calculates the transformation matrix from the current transform's position and forward direction.
        /// </summary>
        public void CalculateMatrixFromCurrentTransform()
        {
            Vector3 forward = transform.forward;
            m_CachedVecToTarget.Set(forward.x, 0, forward.z);
            m_Matrix.SetTRS(transform.position, Quaternion.LookRotation(m_CachedVecToTarget, Vector3.up), Vector3.one);
        }

        /// <summary>
        /// Gets the normalized direction vector of the trajectory based on the angle and matrix.
        /// </summary>
        /// <returns>The normalized direction vector.</returns>
        public Vector3 GetDirection()
        {
            var angleRad = Angle * Mathf.Deg2Rad;
            var direction = Quaternion.AngleAxis(Angle, -Vector3.right) * Vector3.forward;
            direction = m_Matrix.MultiplyVector(direction);
            return direction.normalized;
        }

        /// <summary>
        /// Calculates the maximum height reached by the trajectory.
        /// </summary>
        /// <returns>The maximum height in units.</returns>
        public float GetMaxHeight()
        {
            var angleRad = Angle * Mathf.Deg2Rad;
            return (Velocity * Velocity * Mathf.Sin(angleRad) * Mathf.Sin(angleRad)) / (2 * Physics.gravity.magnitude);
        }

        /// <summary>
        /// Calculates the horizontal range of the trajectory.
        /// </summary>
        /// <returns>The range in units.</returns>
        public float GetRange()
        {
            var angleRad = Angle * Mathf.Deg2Rad;
            m_Range = (Velocity * Velocity * Mathf.Sin(2 * angleRad)) / Physics.gravity.magnitude;
            return Mathf.Abs(m_Range);
        }

        /// <summary>
        /// Calculates the total time the object stays in the air until it hits the ground.
        /// </summary>
        /// <returns>The time in seconds.</returns>
        public float GetTimeInAir()
        {
            var angleRad = Angle * Mathf.Deg2Rad;
            m_TimeInAir = (2 * Velocity * Mathf.Sin(angleRad)) / Physics.gravity.magnitude;
            return m_TimeInAir;
        }

        /// <summary>
        /// Calculates the time required to reach a specific range along the trajectory.
        /// </summary>
        /// <param name="point">The target point in world space.</param>
        /// <returns>The time in seconds.</returns>
        public float GetTimeToReachPointRange(Vector3 point)
        {
            point = m_Matrix.inverse.MultiplyPoint3x4(point);
            return GetTimeAtX(point.z);
        }

        /// <summary>
        /// Calculates the time required to reach a specific height along the trajectory.
        /// </summary>
        /// <param name="point">The target point in world space.</param>
        /// <returns>The time in seconds.</returns>
        public float GetTimeToReachPointHeight(Vector3 point)
        {
            point = m_Matrix.inverse.MultiplyPoint3x4(point);
            return GetTimeAtY(point.y);
        }

        /// <summary>
        /// Calculates the time to reach a specified X position in a 2D plane.
        /// </summary>
        /// <param name="x">The target x-coordinate in units.</param>
        /// <returns>The time in seconds.</returns>
        public float GetTimeAtX(float x)
        {
            return x / (Velocity * Mathf.Cos(Angle * Mathf.Deg2Rad));
        }

        /// <summary>
        /// Calculates the time to reach a specified Y position in a 2D plane.
        /// </summary>
        /// <param name="y">The target y-coordinate in units.</param>
        /// <returns>The time in seconds.</returns>
        public float GetTimeAtY(float y)
        {
            var solutions = SolveQuadraticEquation(-0.5f * Physics.gravity.magnitude, Velocity * Mathf.Sin(Angle * Mathf.Deg2Rad), -y);
            if (solutions.Count > 0)
                return solutions[solutions.Count - 1];

            return float.NaN;
        }

        /// <summary>
        /// Solves the quadratic equation ax² + bx + c = 0 and returns the real roots.
        /// </summary>
        /// <param name="a">The coefficient of x².</param>
        /// <param name="b">The coefficient of x.</param>
        /// <param name="c">The constant term.</param>
        /// <returns>A list of real roots.</returns>
        public static List<float> SolveQuadraticEquation(float a, float b, float c)
        {
            var discriminant = b * b - 4 * a * c;
            var roots = new List<float>();

            if (discriminant >= 0)
            {
                var sqrtDiscriminant = Mathf.Sqrt(discriminant);
                roots.Add((-b + sqrtDiscriminant) / (2 * a));
                if (discriminant > 0)
                    roots.Add((-b - sqrtDiscriminant) / (2 * a));
            }

            return roots;
        }

        public Vector3 GetPointAtY(float y)
        {
            var time = GetTimeToReachPointHeight(Vector3.up * y);
            return GetPointAtTime(time);
        }

        /// <summary>
        /// Calculates the x-coordinate of the trajectory at a specific time in 2D.
        /// </summary>
        /// <param name="time">The time in seconds.</param>
        /// <returns>The x-coordinate in units.</returns>
        public float GetPointX(float time)
        {
            return Velocity * Mathf.Cos(Angle * Mathf.Deg2Rad) * time;
        }

        /// <summary>
        /// Calculates the y-coordinate of the trajectory at a specific time in 2D.
        /// </summary>
        /// <param name="time">The time in seconds.</param>
        /// <returns>The y-coordinate in units.</returns>
        public float GetPointY(float time)
        {
            return (Velocity * Mathf.Sin(Angle * Mathf.Deg2Rad) - 0.5f * Physics.gravity.magnitude * time) * time;
        }

        /// <summary>
        /// Calculates the 3D position of the trajectory at a specific time.
        /// </summary>
        /// <param name="time">The time in seconds.</param>
        /// <returns>The position in 3D space.</returns>
        public Vector3 GetPointAtTime(float time)
        {
            var point = new Vector3(0, GetPointY(time), GetPointX(time));
            return m_Matrix.MultiplyPoint3x4(point);
        }

        /// <summary>
        /// Calculates the initial velocity required to reach a target point given a specific launch angle.
        /// </summary>
        /// <param name="target">The target point in world space.</param>
        /// <returns>The required initial velocity in units per second.</returns>
        public float GetVelocityByAngleAndTarget(Vector3 target)
        {
            var gravity = Physics.gravity.magnitude;
            var angleRad = Angle * Mathf.Deg2Rad;

            var planarTarget = new Vector3(target.x, 0, target.z);
            var planarPosition = new Vector3(transform.position.x, 0, transform.position.z);
            var distance = Vector3.Distance(planarTarget, planarPosition);
            var yOffset = transform.position.y - target.y;

            var initialVelocity = (1 / Mathf.Cos(angleRad))
                                  * Mathf.Sqrt((0.5f * gravity * Mathf.Pow(distance, 2)) / (distance * Mathf.Tan(angleRad) + yOffset));
            var velocityVector = new Vector3(0, initialVelocity * Mathf.Sin(angleRad), initialVelocity * Mathf.Cos(angleRad));
            m_Velocity = velocityVector.magnitude;

            return m_Velocity;
        }
    }
}
