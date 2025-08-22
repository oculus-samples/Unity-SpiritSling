// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    public class ShaderWarmer : MonoBehaviour
    {
        [SerializeField]
        private ShaderVariantCollection[] collections;

        [SerializeField]
        private bool warmupOnStart = false;
        public static bool IsReady { get; private set; }
        public static ShaderWarmer Instance { get; private set; }

        private bool _isRunning = false;

        private void Awake()
        {
            Instance = this;
        }

        private IEnumerator Start()
        {
            if(warmupOnStart)
                yield return PrewarmShaders();
        }

        public IEnumerator PrewarmShaders()
        {
            if (_isRunning)
                yield break;

            _isRunning = true;
            yield return new WaitForEndOfFrame();

            for (var i = 0; i < collections.Length; ++i)
            {
                if (collections[i] != null && !collections[i].isWarmedUp)
                {
                    var now = System.DateTime.Now;
                    collections[i].WarmUp();
                    var span = System.DateTime.Now - now;
                    Log.Info("Shader warming took: " + span);
                }
                else
                {
                    if (collections[i] != null)
                    {
                        Log.Info("Shaders were already warm!");
                    }
                }

                yield return new WaitForEndOfFrame();
            }
            _isRunning = false;
            IsReady = true;
        }
    }
}
