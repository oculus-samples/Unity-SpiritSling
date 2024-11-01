// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using Oculus.Interaction;
using UnityEngine;
using static Oculus.Interaction.TransformerUtils;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// A Transformer that rotates the target about an axis, given two grab points.
    /// Updates apply relative rotational changes, relative to the angle change between the two
    /// grab points each frame.
    /// The axis is defined by a pivot transform: a world position and up vector.
    /// </summary>
    public class TwoGrabGameVolumeTransformer : GameVolumeTransformer
    {
        [SerializeField, Optional]
        private Transform _planeTransform;

        [SerializeField, Optional]
        private Vector3 _localPlaneNormal = new Vector3(0, 1, 0);

        [Serializable]
        public class TwoGrabPlaneConstraints
        {
            public FloatConstraint MaxScale;
            public FloatConstraint MinScale;
            public FloatConstraint MaxY;
            public FloatConstraint MinY;
        }

        [SerializeField]
        private TwoGrabPlaneConstraints _constraints;

        public TwoGrabPlaneConstraints Constraints
        {
            get => _constraints;
            set => _constraints = value;
        }

        public struct TwoGrabPlaneState
        {
            public Pose Center;
            public float PlanarDistance;
        }

        public override void Initialize(IGrabbable grabbable)
        {
            base.Initialize(grabbable);
        }

        private Pose _localToTarget;
        private float _localMagnitudeToTarget;

        private Vector3 WorldPlaneNormal()
        {
            var t = _planeTransform != null ? _planeTransform : _grabbable.Transform;
            return t.TransformDirection(_localPlaneNormal).normalized;
        }

        public override void BeginTransform()
        {
            base.BeginTransform();

            var target = _grabbable.Transform;
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];
            var planeNormal = WorldPlaneNormal();

            var twoGrabPlaneState = TwoGrabPlane(grabA.position, grabB.position, planeNormal);
            _localToTarget = WorldToLocalPose(twoGrabPlaneState.Center, target.worldToLocalMatrix);
            _localMagnitudeToTarget = WorldToLocalMagnitude(twoGrabPlaneState.PlanarDistance, target.worldToLocalMatrix);
        }

        public override void UpdateTransform()
        {
            var target = _grabbable.Transform;
            var grabA = _grabbable.GrabPoints[0];
            var grabB = _grabbable.GrabPoints[1];
            var planeNormal = WorldPlaneNormal();

            var twoGrabPlaneState = TwoGrabPlane(grabA.position, grabB.position, planeNormal);

            var prevDistInWorld = LocalToWorldMagnitude(_localMagnitudeToTarget, target.localToWorldMatrix);
            var scaleDelta = prevDistInWorld != 0 ? twoGrabPlaneState.PlanarDistance / prevDistInWorld : 1f;

            var targetScale = scaleDelta * target.localScale.x;
            if (_constraints.MinScale.Constrain)
            {
                targetScale = Mathf.Max(_constraints.MinScale.Value, targetScale);
            }

            if (_constraints.MaxScale.Constrain)
            {
                targetScale = Mathf.Min(_constraints.MaxScale.Value, targetScale);
            }

            target.localScale = (targetScale / target.localScale.x) * target.localScale;

            var result = AlignLocalToWorldPose(target.localToWorldMatrix, _localToTarget, twoGrabPlaneState.Center);
            target.position = result.position;
            target.rotation = result.rotation;

            target.position = ConstrainAlongDirection(
                target.position, target.parent != null ? target.parent.position : Vector3.zero,
                planeNormal, _constraints.MinY, _constraints.MaxY);

            base.UpdateTransform();
        }

        public static TwoGrabPlaneState TwoGrabPlane(Vector3 p0, Vector3 p1, Vector3 planeNormal)
        {
            var centroid = p0 * 0.5f + p1 * 0.5f;

            var p0planar = Vector3.ProjectOnPlane(p0, planeNormal);
            var p1planar = Vector3.ProjectOnPlane(p1, planeNormal);

            var planarDelta = p1planar - p0planar;
            var poseDir = Quaternion.LookRotation(planarDelta, planeNormal);

            return new TwoGrabPlaneState { Center = new Pose(centroid, poseDir), PlanarDistance = planarDelta.magnitude };
        }

        #region Inject

        public void InjectOptionalPlaneTransform(Transform planeTransform)
        {
            _planeTransform = planeTransform;
        }

        public void InjectOptionalConstraints(TwoGrabPlaneConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}