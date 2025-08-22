// Copyright (c) Meta Platforms, Inc. and affiliates.


using Meta.XR.Samples;
namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    public interface IPoolable
    {
        /// <summary>
        /// Reset object to default state to be used again in the pool
        /// </summary>
        void ResetPoolObject();
    }
}
