// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    public static class ArrayExtensions
    {
        public static void ForEach<T>(this T[] array, Action<T> action) => Array.ForEach(array, action);
    }
}
