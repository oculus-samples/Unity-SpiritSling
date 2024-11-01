// Copyright (c) Meta Platforms, Inc. and affiliates.

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Not local player turn
    /// </summary>
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