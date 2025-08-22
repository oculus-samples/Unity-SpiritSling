// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Not local player turn
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class OtherPlayerTurnState : TabletopGameState
    {
        public override void Enter()
        {
            // Ensure we don't retain any NetworkObject
            var allGameNetworkObjects = GetAllGameNetworkObjects();
            foreach (var nob in allGameNetworkObjects)
            {
                nob.ReleaseStateAuthority();
            }
        }
    }
}
