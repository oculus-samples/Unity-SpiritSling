// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace SpiritSling
{
    public class SceneLoadWhenReady : MonoBehaviour
    {
        public UnityEvent OnReady;

        private IEnumerator Start()
        {
            while (!PoolManager.IsAvailable && !PoolManager.Instance.IsReady)
            {
                yield return null;
            }

            if (!DesktopModeEnabler.IsDesktopMode)
            {
                while (!OVRManager.OVRManagerinitialized)
                {
                    yield return null;
                }
            }

            while (!AppEntitlementCheck.IsReady)
            {
                yield return null;
            }
            
            while (!AskMicrophonePermission.IsReady)
            {
                yield return null;
            }

            StartCoroutine(ShaderWarmer.Instance.PrewarmShaders());
            while (!ShaderWarmer.IsReady)
            {
                yield return null;
            }
            
            yield return null;

            OnReady?.Invoke();
        }
    }
}