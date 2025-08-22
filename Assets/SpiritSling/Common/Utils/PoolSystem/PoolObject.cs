// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    public class PoolObject : MonoBehaviour
    {
        internal Pool ParentPool { get; private set; }

        internal bool Available { get; set; } = true;

        internal bool DestroyedByPool { private get; set; }

        private bool _quitting;
        
        public void SendBackToPool()
        {
            if (_quitting)
                return;
            
            Debug.Assert(ParentPool != null, $"[Pool] Pool Object {gameObject.name} not attached to its parent pool.");

            //attach back to parent pool
            if(ParentPool != null)
                ParentPool.BackToPool(gameObject);
        }

        internal void SetParentPool(Pool parent)
        {
            if (ParentPool == null)
            {
                ParentPool = parent;
            }
        }

        private void Awake()
        {
            SetParentPool(GetComponentInParent<Pool>(true));
        }

        private void OnDestroy()
        {
            if (_quitting)
                return;

            if (!DestroyedByPool)
            {
                Debug.Assert(DestroyedByPool,
                    $"[Pool] Pool Object {gameObject.name} destroyed outside of its pool {(ParentPool == null ? string.Empty : ParentPool.PoolId)}");
                ParentPool.RemovePoolObject(gameObject);
            }
        }

        private void OnApplicationQuit()
        {
            _quitting = true;
        }
    }
}
