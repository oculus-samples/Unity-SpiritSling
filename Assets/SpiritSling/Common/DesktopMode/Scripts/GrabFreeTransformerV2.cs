// Copyright (c) Meta Platforms, Inc. and affiliates.

/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.XR.Samples;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// A Transformer that can translate, rotate and scale a transform using any
    /// number of GrabPoints while also constraining the transformation if desired.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class GrabFreeTransformerV2 : MonoBehaviour, ITransformer
    {
        [SerializeField]
        [Tooltip("Constrains the position of the object along different axes. Units are meters.")]
        private TransformerUtils.PositionConstraints _positionConstraints =
            new TransformerUtils.PositionConstraints
            {
                XAxis = new TransformerUtils.ConstrainedAxis(),
                YAxis = new TransformerUtils.ConstrainedAxis(),
                ZAxis = new TransformerUtils.ConstrainedAxis()
            };

        [SerializeField]
        [Tooltip("Constrains the rotation of the object along different axes. Units are degrees.")]
        private TransformerUtils.RotationConstraints _rotationConstraints =
            new TransformerUtils.RotationConstraints
            {
                XAxis = new TransformerUtils.ConstrainedAxis(),
                YAxis = new TransformerUtils.ConstrainedAxis(),
                ZAxis = new TransformerUtils.ConstrainedAxis()
            };

        [SerializeField]
        [Tooltip("Constrains the local scale of the object along different axes. Expressed as a scale factor.")]
        private TransformerUtils.ScaleConstraints _scaleConstraints =
            new TransformerUtils.ScaleConstraints
            {
                ConstraintsAreRelative = true,
                XAxis = new TransformerUtils.ConstrainedAxis
                {
                    ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange { Min = 1, Max = 1 }
                },
                YAxis = new TransformerUtils.ConstrainedAxis
                {
                    ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange { Min = 1, Max = 1 }
                },
                ZAxis = new TransformerUtils.ConstrainedAxis
                {
                    ConstrainAxis = true, AxisRange = new TransformerUtils.FloatRange { Min = 1, Max = 1 }
                },
            };

        private IGrabbable _grabbable;
        private Pose _grabDeltaInLocalSpace;
        private TransformerUtils.PositionConstraints _relativePositionConstraints;
        private TransformerUtils.ScaleConstraints _relativeScaleConstraints;

        private Quaternion _lastRotation = Quaternion.identity;
        private Vector3 _lastScale = Vector3.one;

        private GrabPointDelta[] _deltas;

        private struct GrabPointDelta
        {
            private const float _epsilon = 0.000001f;

            public Vector3 PrevCentroidOffset { get; private set; }
            public Vector3 CentroidOffset { get; private set; }

            public Quaternion PrevRotation { get; private set; }
            public Quaternion Rotation { get; private set; }

            public GrabPointDelta(Vector3 centroidOffset, Quaternion rotation)
            {
                PrevCentroidOffset = CentroidOffset = centroidOffset;
                PrevRotation = Rotation = rotation;
            }

            public void UpdateData(Vector3 centroidOffset, Quaternion rotation)
            {
                PrevCentroidOffset = CentroidOffset;
                CentroidOffset = centroidOffset;

                PrevRotation = Rotation;

                //Quaternions have two ways of expressing the same rotation.
                //This code ensures that the result is the same rotation but expressed in the desired sign.
                if (Quaternion.Dot(rotation, Rotation) < 0)
                {
                    rotation.x = -rotation.x;
                    rotation.y = -rotation.y;
                    rotation.z = -rotation.z;
                    rotation.w = -rotation.w;
                }

                Rotation = rotation;
            }

            public bool IsValidAxis()
            {
                return CentroidOffset.sqrMagnitude > _epsilon;
            }
        }

        public void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;
            _relativePositionConstraints = TransformerUtils.GenerateParentConstraints(_positionConstraints, _grabbable.Transform.localPosition);
            _relativeScaleConstraints = TransformerUtils.GenerateParentConstraints(_scaleConstraints, _grabbable.Transform.localScale);
        }

        public void BeginTransform()
        {
            var count = _grabbable.GrabPoints.Count;
            var centroid = GetCentroid(_grabbable.GrabPoints);

            //rent space only while using
            _deltas = ArrayPool<GrabPointDelta>.Shared.Rent(count);

            for (var i = 0; i < count; i++)
            {
                var centroidOffset = GetCentroidOffset(_grabbable.GrabPoints[i], centroid);
                _deltas[i] = new GrabPointDelta(centroidOffset, _grabbable.GrabPoints[i].rotation);
            }

            var targetTransform = _grabbable.Transform;
            _grabDeltaInLocalSpace = new Pose(
                targetTransform.InverseTransformVector(centroid - targetTransform.position),
                targetTransform.rotation);
            _lastRotation = Quaternion.identity;
            _lastScale = targetTransform.localScale;
        }

        public void UpdateTransform()
        {
            var count = _grabbable.GrabPoints.Count;
            var targetTransform = _grabbable.Transform;

            //Debug.Log("_grabbable.GrabPoints.Count:" + _grabbable.GrabPoints.Count);
            var localPosition = UpdateTransformerPointData(_grabbable.GrabPoints);

            _lastScale = UpdateScale(count) * _lastScale;
            targetTransform.localScale = TransformerUtils.GetConstrainedTransformScale(_lastScale, _relativeScaleConstraints);

            _lastRotation = UpdateRotation(count) * _lastRotation;
            var rotation = _lastRotation * _grabDeltaInLocalSpace.rotation;
            targetTransform.rotation = TransformerUtils.GetConstrainedTransformRotation(rotation, _rotationConstraints, targetTransform.parent);

            var position = localPosition - targetTransform.TransformVector(_grabDeltaInLocalSpace.position);

            //Debug.Log("position:" + position + " localPosition:" + localPosition + " targetTransform:" + targetTransform + " pose:" + _grabDeltaInLocalSpace.position);
            //targetTransform.position = TransformerUtils.GetConstrainedTransformPosition(position, _relativePositionConstraints, targetTransform.parent);
            targetTransform.position = TransformerUtils.GetConstrainedTransformPosition(
                localPosition, _relativePositionConstraints, targetTransform.parent);
        }

        public void EndTransform()
        {
            //return the uneeded space
            ArrayPool<GrabPointDelta>.Shared.Return(_deltas);
            _deltas = null;
        }

        private Vector3 UpdateTransformerPointData(List<Pose> poses)
        {
            var centroid = GetCentroid(poses);

            //Debug.Log("centroid:" + centroid + " poses:" + poses.Count);
            for (var i = 0; i < poses.Count; i++)
            {
                var centroidOffset = GetCentroidOffset(poses[i], centroid);

                //Debug.Log("centroidOffset:" + centroidOffset);
                _deltas[i].UpdateData(centroidOffset, poses[i].rotation);
            }

            return centroid;
        }

        private Vector3 GetCentroid(List<Pose> poses)
        {
            var count = poses.Count;
            var sumPosition = Vector3.zero;
            for (var i = 0; i < count; i++)
            {
                var pose = poses[i];
                sumPosition += pose.position;
            }

            return sumPosition / count;
        }

        private Vector3 GetCentroidOffset(Pose pose, Vector3 centre)
        {
            var centroidOffset = centre - pose.position;
            return centroidOffset;
        }

        private Quaternion UpdateRotation(int count)
        {
            var combinedRotation = Quaternion.identity;

            //each point can only affect a fraction of the rotation
            var fraction = 1f / count;
            for (var i = 0; i < count; i++)
            {
                var data = _deltas[i];

                //overall delta rotation since last update
                var rotDelta = data.Rotation * Quaternion.Inverse(data.PrevRotation);

                if (data.IsValidAxis())
                {
                    var aimingAxis = data.CentroidOffset.normalized;

                    //rotation along aiming axis
                    var dirDelta = Quaternion.FromToRotation(data.PrevCentroidOffset.normalized, aimingAxis);
                    combinedRotation = Quaternion.Slerp(Quaternion.identity, dirDelta, fraction) * combinedRotation;

                    //twist along the aiming axis
                    rotDelta.ToAngleAxis(out var angle, out var axis);
                    var projectionFactor = Vector3.Dot(axis, aimingAxis);
                    rotDelta = Quaternion.AngleAxis(angle * projectionFactor, aimingAxis);
                }

                combinedRotation = Quaternion.Slerp(Quaternion.identity, rotDelta, fraction) * combinedRotation;
            }

            return combinedRotation;
        }

        private float UpdateScale(int count)
        {
            var scaleDelta = 0f;
            for (var i = 0; i < count; i++)
            {
                var data = _deltas[i];
                if (data.IsValidAxis())
                {
                    var factor = Mathf.Sqrt(data.CentroidOffset.sqrMagnitude / data.PrevCentroidOffset.sqrMagnitude);
                    scaleDelta += factor / count;
                }
                else
                {
                    scaleDelta += 1f / count;
                }
            }

            return scaleDelta;
        }

        #region Inject

        public void InjectOptionalPositionConstraints(TransformerUtils.PositionConstraints constraints)
        {
            _positionConstraints = constraints;
        }

        public void InjectOptionalRotationConstraints(TransformerUtils.RotationConstraints constraints)
        {
            _rotationConstraints = constraints;
        }

        public void InjectOptionalScaleConstraints(TransformerUtils.ScaleConstraints constraints)
        {
            _scaleConstraints = constraints;
        }

        #endregion
    }
}
