// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling.TableTop
{
    public static class RendererExtensions
    {
        public static void SetFloat(this MeshRenderer renderer, int propertyID, float value)
        {
            renderer.materials.ForEach(m => m.SetFloat(propertyID, value));
        }

        public static void SetColor(this MeshRenderer renderer, int propertyID, Color value)
        {
            renderer.materials.ForEach(m => m.SetColor(propertyID, value));
        }

        public static void SetInt(this MeshRenderer renderer, int propertyID, int value)
        {
            renderer.materials.ForEach(m => m.SetInt(propertyID, value));
        }
    }
}