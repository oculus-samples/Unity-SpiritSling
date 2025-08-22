// Copyright (c) Meta Platforms, Inc. and affiliates.

#if UNITY_EDITOR

using Meta.XR.Samples;
using UnityEditor;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    [CustomEditor(typeof(PoolManager))]
    public class PoolManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var myScript = (PoolManager)target;

            if (myScript.CreateInEditMode)
            {
                GUILayout.BeginVertical();
                GUILayout.Label("Editor helpers");
                if (GUILayout.Button("Add New Pool"))
                {
                    myScript.CreateEmptyPool();
                }

                if (GUILayout.Button("Create Pool Objects"))
                {
                    myScript.CreatePoolObjects();
                }

                if (GUILayout.Button("Clear All Pools"))
                {
                    myScript.DestroyAllPools();
                }

                GUILayout.EndVertical();
            }
        }
    }
}
#endif
