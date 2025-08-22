// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    [ExecuteInEditMode]
    public class TableWorldPos : MonoBehaviour
    {
        public static TableWorldPos Instance;
        [SerializeField]
        Vector3 _offset = Vector3.zero;

        int _id = Shader.PropertyToID("_TableWorldPosition");

        Vector3 _tempsPos = Vector3.negativeInfinity;
        
        private void Awake()
        {
            if(Instance!= null && Instance != this)
            {
                Log.Error(@"You are trying to create multiple instances of TableWorldPos on " + gameObject.name);
            }
            Instance = this;
        }

        private void Update()
        {
            if (_tempsPos != transform.position + _offset)
            {
                Shader.SetGlobalVector(_id, transform.position + _offset);
                _tempsPos = transform.position + _offset;
            }
        }
    }
}
