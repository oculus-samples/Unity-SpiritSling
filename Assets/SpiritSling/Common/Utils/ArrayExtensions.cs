// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;

namespace SpiritSling
{
    public static class ArrayExtensions
    {
        public static void ForEach<T>(this T[] array, Action<T> action) => Array.ForEach(array, action);
    }
}