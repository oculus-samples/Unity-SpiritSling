// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace SpiritSling
{
    public interface IPoolable
    {
        /// <summary>
        /// Reset object to default state to be used again in the pool
        /// </summary>
        void ResetPoolObject();
    }
}