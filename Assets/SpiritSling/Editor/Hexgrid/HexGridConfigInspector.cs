// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEditor;
using UnityEngine;

namespace SpiritSling.TableTop.Editor
{
    [CustomEditor(typeof(HexGridConfig))]
    public class HexGridConfigInspector : UnityEditor.Editor
    {
        private Texture2D helperImage;

        private void OnEnable()
        {
            helperImage = Resources.Load<Texture2D>("sliceHelper");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Show a helper image
            if (helperImage != null)
            {
                // Draw the image in the inspector
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.Space();
                GUILayout.Label(helperImage);
                EditorGUILayout.Space();
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}