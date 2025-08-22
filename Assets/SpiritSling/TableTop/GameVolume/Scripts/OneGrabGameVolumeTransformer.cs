// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using Oculus.Interaction;
using UnityEngine;
using static Oculus.Interaction.TransformerUtils;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// A Transformer that translates the target, with optional parent-space constraints
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class OneGrabGameVolumeTransformer : GameVolumeTransformer
    {
        [Serializable]
        public class OneGrabTranslateConstraints
        {
            public bool ConstraintsAreRelative;
            public FloatConstraint MinX;
            public FloatConstraint MaxX;
            public FloatConstraint MinY;
            public FloatConstraint MaxY;
            public FloatConstraint MinZ;
            public FloatConstraint MaxZ;
        }

        [SerializeField]
        private OneGrabTranslateConstraints _constraints =
            new OneGrabTranslateConstraints
            {
                MinX = new FloatConstraint(),
                MaxX = new FloatConstraint(),
                MinY = new FloatConstraint(),
                MaxY = new FloatConstraint(),
                MinZ = new FloatConstraint(),
                MaxZ = new FloatConstraint()
            };

        public OneGrabTranslateConstraints Constraints
        {
            get
            {
                return _constraints;
            }

            set
            {
                _constraints = value;
                GenerateParentConstraints();
            }
        }

        private OneGrabTranslateConstraints _parentConstraints;

        private Vector3 _initialPosition;

        public override void Initialize(IGrabbable grabbable)
        {
            base.Initialize(grabbable);
            _initialPosition = _grabbable.Transform.localPosition;
            GenerateParentConstraints();
        }

        private void GenerateParentConstraints()
        {
            if (!_constraints.ConstraintsAreRelative)
            {
                _parentConstraints = _constraints;
            }
            else
            {
                _parentConstraints = new OneGrabTranslateConstraints();

                _parentConstraints.MinX = new FloatConstraint();
                _parentConstraints.MinY = new FloatConstraint();
                _parentConstraints.MinZ = new FloatConstraint();
                _parentConstraints.MaxX = new FloatConstraint();
                _parentConstraints.MaxY = new FloatConstraint();
                _parentConstraints.MaxZ = new FloatConstraint();

                if (_constraints.MinX.Constrain)
                {
                    _parentConstraints.MinX.Constrain = true;
                    _parentConstraints.MinX.Value = _constraints.MinX.Value + _initialPosition.x;
                }

                if (_constraints.MaxX.Constrain)
                {
                    _parentConstraints.MaxX.Constrain = true;
                    _parentConstraints.MaxX.Value = _constraints.MaxX.Value + _initialPosition.x;
                }

                if (_constraints.MinY.Constrain)
                {
                    _parentConstraints.MinY.Constrain = true;
                    _parentConstraints.MinY.Value = _constraints.MinY.Value + _initialPosition.y;
                }

                if (_constraints.MaxY.Constrain)
                {
                    _parentConstraints.MaxY.Constrain = true;
                    _parentConstraints.MaxY.Value = _constraints.MaxY.Value + _initialPosition.y;
                }

                if (_constraints.MinZ.Constrain)
                {
                    _parentConstraints.MinZ.Constrain = true;
                    _parentConstraints.MinZ.Value = _constraints.MinZ.Value + _initialPosition.z;
                }

                if (_constraints.MaxZ.Constrain)
                {
                    _parentConstraints.MaxZ.Constrain = true;
                    _parentConstraints.MaxZ.Value = _constraints.MaxZ.Value + _initialPosition.z;
                }
            }
        }

        private Pose _localToTarget;

        public override void BeginTransform()
        {
            base.BeginTransform();

            var grabPose = _grabbable.GrabPoints[0];
            var target = _grabbable.Transform;
            _localToTarget = WorldToLocalPose(grabPose, target.worldToLocalMatrix);
        }

        public override void UpdateTransform()
        {
            var target = _grabbable.Transform;
            var grabPose = _grabbable.GrabPoints[0];

            var initialGrabRotation = target.rotation * _localToTarget.rotation;
            var targetPose = new Pose(grabPose.position, initialGrabRotation);

            var result = AlignLocalToWorldPose(target.localToWorldMatrix, _localToTarget, targetPose);
            target.position = result.position;
            target.rotation = result.rotation;

            ConstrainTransform();

            base.UpdateTransform();
        }

        private void ConstrainTransform()
        {
            var target = _grabbable.Transform;

            var constrainedPosition = target.localPosition;

            if (_parentConstraints.MinX.Constrain)
            {
                constrainedPosition.x = Mathf.Max(constrainedPosition.x, _parentConstraints.MinX.Value);
            }

            if (_parentConstraints.MaxX.Constrain)
            {
                constrainedPosition.x = Mathf.Min(constrainedPosition.x, _parentConstraints.MaxX.Value);
            }

            if (_parentConstraints.MinY.Constrain)
            {
                constrainedPosition.y = Mathf.Max(constrainedPosition.y, _parentConstraints.MinY.Value);
            }

            if (_parentConstraints.MaxY.Constrain)
            {
                constrainedPosition.y = Mathf.Min(constrainedPosition.y, _parentConstraints.MaxY.Value);
            }

            if (_parentConstraints.MinZ.Constrain)
            {
                constrainedPosition.z = Mathf.Max(constrainedPosition.z, _parentConstraints.MinZ.Value);
            }

            if (_parentConstraints.MaxZ.Constrain)
            {
                constrainedPosition.z = Mathf.Min(constrainedPosition.z, _parentConstraints.MaxZ.Value);
            }

            target.localPosition = constrainedPosition;
        }

        #region Inject

        public void InjectOptionalConstraints(OneGrabTranslateConstraints constraints)
        {
            _constraints = constraints;
        }

        #endregion
    }
}
