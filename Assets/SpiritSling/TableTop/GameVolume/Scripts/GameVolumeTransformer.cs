// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// A Transformer that translates the target, with optional parent-space constraints
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public abstract class GameVolumeTransformer : MonoBehaviour, ITransformer
    {
        public UnityEvent beginTransform;
        public UnityEvent updateTransform;
        public UnityEvent endTransform;

        protected IGrabbable _grabbable;

        private bool _hasAnchor;

        public virtual void Initialize(IGrabbable grabbable)
        {
            _grabbable = grabbable;

            var anchor = _grabbable.Transform.GetComponent<OVRSpatialAnchor>();
            if (anchor != null)
            {
                _hasAnchor = true;
            }
        }

        public virtual void BeginTransform()
        {
            if (_hasAnchor)
            {
                var anchor = _grabbable.Transform.GetComponent<OVRSpatialAnchor>();
                if (anchor != null)
                {
                    Destroy(anchor);
                }
            }

            beginTransform?.Invoke();
        }

        public virtual void UpdateTransform()
        {
            updateTransform?.Invoke();
        }

        public virtual void EndTransform()
        {
            endTransform?.Invoke();
        }
    }
}
